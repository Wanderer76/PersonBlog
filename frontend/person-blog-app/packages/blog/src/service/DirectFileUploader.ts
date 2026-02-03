// src/utils/DirectFileUploader.ts
import API from "../scripts/apiMethod";


export interface InitiateUploadRequest {
    postId: string;
    objectName: string;
    size: number;
    contentType: string;
    fileExtension: string;
    fileName: string;
    duration: number;
}

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

export interface GenerateUrlResponse {
    url: string;
    uploadId: string;
    partNumber: number;
    expiresAt: string;
    httpMethod: string;
}

export interface MultipartUploadPart {
    partNumber: number;
    eTag: string;
    size: number;
    uploadedAt: string;
}

export interface CompleteUploadRequest {
    postId: string;
    uploadId: string;
    parts: MultipartUploadPart[];
}

export class DirectFileUploader {
    private uploadId: string = '';
    private postId: string = '';
    private objectName: string = '';
    private size: Number = 0;
    private chunkSize: number = 5 * 1024 * 1024; // 5MB
    private parts: MultipartUploadPart[] = [];
    private onProgress?: (progress: number) => void;

    constructor(chunkSize?: number) {
        if (chunkSize) {
            this.chunkSize = chunkSize;
        }
    }

    setProgressCallback(callback: (progress: number) => void) {
        this.onProgress = callback;
    }

    async initiateUpload(postId: string, duration: number, file: File): Promise<InitiateUploadResponse> {
        const response = await API.post('/profile/api/VideoUpload/initiate', {
            postId: postId,
            objectName: file.name,
            size: file.size,
            contentType: file.type,
            fileExtension: '.mp4',
            fileName: file.name,
            duration: duration,

        });
        const session = response.data;

        this.postId = postId;
        this.uploadId = session.uploadId;
        this.objectName = file.name;
        this.size = file.size;
        this.parts = [];
        return response;
    }

    async uploadFile(file: File): Promise<void> {
        const totalParts = Math.ceil(file.size / this.chunkSize);

        // Загружаем все части параллельно с ограничением
        const maxConcurrent = 5;
        const partNumbers = Array.from({ length: totalParts }, (_, i) => i + 1);

        for (let i = 0; i < totalParts; i += maxConcurrent) {
            const chunkParts = partNumbers.slice(i, i + maxConcurrent);
            await Promise.all(
                chunkParts.map(partNumber => this.uploadPart(file, partNumber, totalParts))
            );
        }

        // Завершаем загрузку
        await this.completeUpload();
    }

    private async uploadPart(file: File, partNumber: number, totalParts: number): Promise<void> {
        // Генерируем подписанную ссылку
        const urlResponse = await API.post('/profile/api/VideoUpload/generate-url', {
            uploadId: this.uploadId,
            partNumber,
            expiryMinutes: 10
        });

        const { url } = urlResponse.data as GenerateUrlResponse;

        // Вырезаем часть файла
        const start = (partNumber - 1) * this.chunkSize;
        const end = Math.min(start + this.chunkSize, file.size);
        const chunk = file.slice(start, end);

        // Загружаем напрямую в MinIO
        const uploadResponse = await fetch(url, {
            method: 'PUT',
            body: chunk,
            headers: {
                'Content-Type': 'application/octet-stream'
            }
        });

        if (!uploadResponse.ok) {
            throw new Error(`Failed to upload part ${partNumber}: ${uploadResponse.statusText}`);
        }

        // Получаем ETag из ответа
        const rawETag = uploadResponse.headers.get('ETag') ||
            uploadResponse.headers.get('etag') ||
            uploadResponse.headers.get('ETAG');


        console.log(`[DEBUG Part ${partNumber}] Raw ETag: "${rawETag}" (length: ${rawETag?.length})`);

        // ВАШ ТЕКУЩИЙ КОД
        const eTag = rawETag?.replace(/"/g, '') || '';

        console.log(`[DEBUG Part ${partNumber}] Cleaned ETag: "${eTag}" (length: ${eTag.length})`);
        console.log(`[DEBUG Part ${partNumber}] Has quotes after clean: ${eTag.includes('"')}`);

        const part: MultipartUploadPart = {
            partNumber,
            eTag,
            size: chunk.size,
            uploadedAt: new Date().toISOString()
        };

        this.parts.push(part);

        // Обновляем прогресс
        const progress = Math.round(((this.parts.length) / totalParts) * 100);
        this.onProgress?.(progress);

        console.log(`Part ${partNumber}/${totalParts} uploaded successfully`);
    }

    private async completeUpload(): Promise<void> {
        const sortedParts = this.parts.sort((a, b) => a.partNumber - b.partNumber);

        const request: CompleteUploadRequest = {
            uploadId: this.uploadId,
            postId:this.postId,
            parts: sortedParts
        };

        await API.post('/profile/api/VideoUpload/complete', request);
    }

    async abortUpload(): Promise<void> {
        if (!this.uploadId) return;

        await API.post('/profile/api/VideoUpload/abort', {
            uploadId: this.uploadId
        });
    }

    getUploadId(): string {
        return this.uploadId;
    }

    getObjectName(): string {
        return this.objectName;
    }
}