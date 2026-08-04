import API from '../lib/api/client';

export interface InitiateUploadResponse {
    uploadId: string;
    bucketId: string;
    objectName: string;
    contentType: string;
    createdAt: string;
    status: string;
    totalSize: number;
    totalParts: number;
}

interface GenerateUrlResponse {
    url: string;
}

interface MultipartUploadPart {
    partNumber: number;
    eTag: string;
    size: number;
    uploadedAt: string;
}

interface CompleteUploadRequest {
    uploadId: string;
    parts: MultipartUploadPart[];
}

const MAX_CONCURRENT_UPLOADS = 5;
const MAX_PART_ATTEMPTS = 3;

const getFileExtension = (fileName: string) => {
    const separatorIndex = fileName.lastIndexOf('.');
    return separatorIndex >= 0 ? fileName.slice(separatorIndex).toLowerCase() : '';
};

const delay = (milliseconds: number) => new Promise(resolve => setTimeout(resolve, milliseconds));

export class DirectFileUploader {
    private uploadId = '';
    private readonly chunkSize: number;
    private parts: MultipartUploadPart[] = [];
    private onProgress?: (progress: number) => void;
    private abortController: AbortController | null = null;

    constructor(chunkSize = 5 * 1024 * 1024) {
        this.chunkSize = chunkSize;
    }

    setProgressCallback(callback: (progress: number) => void) {
        this.onProgress = callback;
    }

    async initiateUpload(postId: string, duration: number, file: File): Promise<InitiateUploadResponse> {
        const { data } = await API.post<InitiateUploadResponse>('/profile/api/VideoUpload/initiate', {
            postId,
            objectName: file.name,
            size: file.size,
            contentType: file.type,
            fileExtension: getFileExtension(file.name),
            fileName: file.name,
            duration
        });

        this.uploadId = data.uploadId;
        this.parts = [];
        return data;
    }

    async uploadFile(file: File): Promise<void> {
        if (!this.uploadId) throw new Error('Upload session is not initialized');
        this.abortController = new AbortController();

        const totalParts = Math.ceil(file.size / this.chunkSize);
        const partNumbers = Array.from({ length: totalParts }, (_, index) => index + 1);

        for (let index = 0; index < totalParts; index += MAX_CONCURRENT_UPLOADS) {
            const batch = partNumbers.slice(index, index + MAX_CONCURRENT_UPLOADS);
            await Promise.all(batch.map(partNumber => this.uploadPartWithRetry(file, partNumber, totalParts)));
        }

        await this.completeUpload();
    }

    private async uploadPartWithRetry(file: File, partNumber: number, totalParts: number): Promise<void> {
        let lastError: unknown;
        for (let attempt = 1; attempt <= MAX_PART_ATTEMPTS; attempt += 1) {
            try {
                await this.uploadPart(file, partNumber, totalParts);
                return;
            } catch (error: unknown) {
                lastError = error;
                if (attempt < MAX_PART_ATTEMPTS) await delay(500 * 2 ** (attempt - 1));
            }
        }
        throw lastError instanceof Error ? lastError : new Error(`Failed to upload part ${partNumber}`);
    }

    private async uploadPart(file: File, partNumber: number, totalParts: number): Promise<void> {
        const { data } = await API.post<GenerateUrlResponse>('/profile/api/VideoUpload/generate-url', {
            uploadId: this.uploadId,
            partNumber,
            expiryMinutes: 10
        });

        const start = (partNumber - 1) * this.chunkSize;
        const chunk = file.slice(start, Math.min(start + this.chunkSize, file.size));
        const response = await fetch(data.url, {
            method: 'PUT',
            body: chunk,
            signal: this.abortController?.signal,
            headers: { 'Content-Type': 'application/octet-stream' }
        });

        if (!response.ok) throw new Error(`Failed to upload part ${partNumber}: ${response.status}`);

        const eTag = response.headers.get('ETag')?.replace(/"/g, '') ?? '';
        if (!eTag) throw new Error('Storage did not expose the ETag response header');

        this.parts.push({ partNumber, eTag, size: chunk.size, uploadedAt: new Date().toISOString() });
        this.onProgress?.(Math.round((this.parts.length / totalParts) * 100));
    }

    private async completeUpload(): Promise<void> {
        const request: CompleteUploadRequest = {
            uploadId: this.uploadId,
            parts: [...this.parts].sort((left, right) => left.partNumber - right.partNumber)
        };
        await API.post('/profile/api/VideoUpload/complete', request);
    }

    async abortUpload(): Promise<void> {
        if (!this.uploadId) return;
        const uploadId = this.uploadId;
        this.abortController?.abort();
        this.abortController = null;
        this.uploadId = '';
        this.parts = [];
        await API.post('/profile/api/VideoUpload/abort', { uploadId });
    }
}
