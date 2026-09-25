export interface ProfileData {
  id: string;
  name: string | null;
  photoUrl?: string;
  totalPostsCount: number;
  createdAt: string | null;
  email?: string;
}

export interface HasBlogResponse {
  hasBlog: boolean | null;
}

export interface VideoProcessingProgress {
  postId: string;
  percent: number;
  status: 'processing' | 'completed' | 'failed';
  error?: string | null;
}
