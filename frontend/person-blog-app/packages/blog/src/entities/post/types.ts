export interface VideoMetadata {
  duration?: number;
}

export interface VideoInfo {
  previewUrl?: string;
  processState: number; // 0: processing, 1: published, 2: uploading
  state: number;
  videoMetadata?: VideoMetadata;
}

export interface Post {
  id: string;
  title: string;
  description?: string;
  type: number;
  state: number;
  viewCount?: number;
  createdAt?: string;
  videoInfo: VideoInfo;
  errorMessage?: string;
}

export interface PostsPageResponse {
  items: Post[];
  totalPostsCount: number;
  totalPageCount: number;
  currentPage: number;
}
