// types/music.ts
export interface ArtistInfo {
  id: string;
  name: string;
}

export interface TrackFileInfo {
  duration: number;
  fileSize: number;
  url: string;
}

export interface TrackViewItem {
  id: string;
  name: string;
  thumbnailUrl: string | null;
  albumId: string | null;
  isLiked: boolean;
  trackInfo: TrackFileInfo;
  artists: ArtistInfo[];
}

export interface PagedListViewModel<T> {
  totalPageCount: number;
  pageSize: number;
  items: T[];
}

export interface AudioPlayerState {
  isPlaying: boolean;
  currentTime: number;
  duration: number;
  volume: number;
  isLoading: boolean;
  error?: string;
}