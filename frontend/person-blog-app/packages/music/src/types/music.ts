// types/music.ts
export interface ArtistInfo {
  id: string;
  name: string;
}

export interface TrackFileInfo {
  duration: number;
  fileSize: number;
  fileUrl: string;
}

export interface TrackViewItem {
  id: string;
  name: string;
  thumbnailUrl?: string;
  albumId?: string;
  trackInfo: TrackFileInfo;
  artists: ArtistInfo[];
}

export interface PagedListViewModel<T> {
  totalPageCount: number;
  pageSize: number;
  items: T[];
}