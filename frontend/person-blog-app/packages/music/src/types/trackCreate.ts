// --- Интерфейсы ---
export interface Genre {
    id: string;
    name: string;
}

export interface Artist {
    id: string;
    name: string;
}

export interface CreateView {
    genres: Genre[];
}
export interface TrackFileMetadata {
    trackFileId: string;
    artistId?: string;
    title: string;
    artist: string;
    album: string;
    year: number;
    genre: string;
    duration: number;
    bitrate: string;
    hasCover: boolean;
    coverBase64?: string;
    coverMimeType?: string;
    originalFileName: string;
    fileSize: number;
    thumbnailId?: string;
}

export interface TrackCreateRequest {
    name: string;
    artistId: string | null;
    postId: string | null;
    artistName: string;
    albumId: string | null;
    year: number;
    trackFileId: string;
    thumbnailId: string | null;
    genres: string[];
}

export interface GenreOption {
    value: string;
    label: string;
}

export interface ArtistOption {
    value: string | null;
    label: string;
}