import type { Playlist } from '@/entities/playlist';
import type { Post } from '@/entities/post';

export type ChannelTab = 'videos' | 'playlists' | 'about';

export interface ChannelInfo {
  id: string;
  userId?: string;
  name: string;
  description: string;
  createdAt: string;
  photoUrl?: string;
  subscribersCount: number;
  isSubscribed: boolean;
}

export interface ChannelPageModel {
  channel: ChannelInfo | null;
  videos: Post[];
  playlists: Playlist[];
  isChannelLoading: boolean;
  isVideosLoading: boolean;
  isPlaylistsLoading: boolean;
  channelError: string | null;
  videosError: string | null;
  playlistsError: string | null;
  hasMoreVideos: boolean;
  loadMoreVideos: () => void;
  updateSubscription: (isSubscribed: boolean) => void;
}
