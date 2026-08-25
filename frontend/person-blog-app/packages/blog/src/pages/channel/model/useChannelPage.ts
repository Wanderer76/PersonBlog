import { useCallback, useEffect, useState } from 'react';
import type { Playlist } from '@/entities/playlist';
import type { Post } from '@/entities/post';
import { getChannel } from '@/shared/api/generated/channel/channel';
import type { VideoMetadataModel } from '@/shared/api/generated/models';
import type { ChannelInfo, ChannelPageModel, ChannelTab } from './types';

const PAGE_SIZE = 10;
const channelApi = getChannel();

interface ChannelResponse {
  id?: string;
  userId?: string;
  name?: string | null;
  description?: string | null;
  createdAt?: string;
  photoUrl?: string | null;
  subscribersCount?: number;
  isSubscribed?: boolean;
}

interface ChannelPostResponse {
  id?: string;
  title?: string | null;
  description?: string | null;
  previewId?: string | null;
  videoData?: VideoMetadataModel;
  viewCount?: number;
  createdAt?: string;
  state?: number;
  errorMessage?: string | null;
}

interface ChannelPostsResponse {
  posts?: ChannelPostResponse[];
  totalPageCount?: number;
}

interface ChannelPlaylistResponse {
  id?: string;
  title?: string | null;
  thumbnailUrl?: string | null;
  postCount?: number;
  canEdit?: boolean;
}

const normalizeChannel = (data: ChannelResponse): ChannelInfo => ({
  id: data.id ?? '',
  userId: data.userId,
  name: data.name || 'Канал без названия',
  description: data.description || '',
  createdAt: data.createdAt ?? '',
  photoUrl: data.photoUrl || undefined,
  subscribersCount: data.subscribersCount ?? 0,
  isSubscribed: data.isSubscribed ?? false,
});

const normalizePost = (post: ChannelPostResponse): Post => ({
  id: post.id ?? '',
  title: post.title || 'Без названия',
  description: post.description || undefined,
  type: 1,
  state: post.state ?? 0,
  viewCount: post.viewCount ?? 0,
  createdAt: post.createdAt ?? '',
  videoInfo: {
    previewUrl: post.previewId || undefined,
    processState: post.state ?? 0,
    state: post.state ?? 0,
    videoMetadata: post.videoData,
  },
  errorMessage: post.errorMessage || undefined,
});

const normalizePlaylist = (playlist: ChannelPlaylistResponse): Playlist => ({
  id: playlist.id ?? '',
  title: playlist.title || 'Плейлист без названия',
  thumbnailUrl: playlist.thumbnailUrl || '',
  postCount: playlist.postCount ?? 0,
  canEdit: playlist.canEdit ?? false,
});

export const useChannelPage = (channelId: string, activeTab: ChannelTab): ChannelPageModel => {
  const [channel, setChannel] = useState<ChannelInfo | null>(null);
  const [videos, setVideos] = useState<Post[]>([]);
  const [playlists, setPlaylists] = useState<Playlist[]>([]);
  const [page, setPage] = useState(1);
  const [hasMoreVideos, setHasMoreVideos] = useState(true);
  const [hasLoadedPlaylists, setHasLoadedPlaylists] = useState(false);
  const [isChannelLoading, setIsChannelLoading] = useState(true);
  const [isVideosLoading, setIsVideosLoading] = useState(false);
  const [isPlaylistsLoading, setIsPlaylistsLoading] = useState(false);
  const [channelError, setChannelError] = useState<string | null>(null);
  const [videosError, setVideosError] = useState<string | null>(null);
  const [playlistsError, setPlaylistsError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    const loadChannel = async () => {
      setIsChannelLoading(true);
      setChannelError(null);
      try {
        const response = await channelApi.getApiChannelChannelId(channelId, { signal: controller.signal });
        setChannel(normalizeChannel(response.data as unknown as ChannelResponse));
      } catch (error) {
        if (!controller.signal.aborted) {
          console.error('Ошибка загрузки канала:', error);
          setChannelError('Не удалось загрузить канал');
        }
      } finally {
        if (!controller.signal.aborted) setIsChannelLoading(false);
      }
    };

    void loadChannel();

    return () => controller.abort();
  }, [channelId]);

  useEffect(() => {
    const controller = new AbortController();

    const loadVideos = async () => {
      setIsVideosLoading(true);
      setVideosError(null);
      try {
        const response = await channelApi.getApiChannelPostsChannelId(
          channelId,
          { page, size: PAGE_SIZE },
          { signal: controller.signal },
        );
        const data = response.data as unknown as ChannelPostsResponse;
        const nextVideos = (data.posts ?? []).map(normalizePost).filter(post => post.id);
        setVideos(previous => {
          if (page === 1) return nextVideos;
          const ids = new Set(previous.map(post => post.id));
          return [...previous, ...nextVideos.filter(post => !ids.has(post.id))];
        });
        setHasMoreVideos(page < (data.totalPageCount ?? 0));
      } catch (error) {
        if (!controller.signal.aborted) {
          console.error('Ошибка загрузки видео:', error);
          setVideosError('Не удалось загрузить видео');
        }
      } finally {
        if (!controller.signal.aborted) setIsVideosLoading(false);
      }
    };

    void loadVideos();

    return () => controller.abort();
  }, [channelId, page]);

  useEffect(() => {
    if (!channelId || activeTab !== 'playlists' || hasLoadedPlaylists) return;

    const controller = new AbortController();

    const loadPlaylists = async () => {
      setIsPlaylistsLoading(true);
      setPlaylistsError(null);
      try {
        const response = await channelApi.getApiChannelPlayListsChannelId(channelId, { signal: controller.signal });
        const data = response.data as unknown as ChannelPlaylistResponse[];
        setPlaylists(data.map(normalizePlaylist).filter(playlist => playlist.id));
        setHasLoadedPlaylists(true);
      } catch (error) {
        if (!controller.signal.aborted) {
          console.error('Ошибка загрузки плейлистов:', error);
          setPlaylistsError('Не удалось загрузить плейлисты');
        }
      } finally {
        if (!controller.signal.aborted) setIsPlaylistsLoading(false);
      }
    };

    void loadPlaylists();

    return () => controller.abort();
  }, [activeTab, channelId, hasLoadedPlaylists]);

  const loadMoreVideos = useCallback(() => {
    if (!isVideosLoading && hasMoreVideos) setPage(previous => previous + 1);
  }, [hasMoreVideos, isVideosLoading]);

  const updateSubscription = useCallback((isSubscribed: boolean) => {
    setChannel(previous => previous ? {
      ...previous,
      isSubscribed,
      subscribersCount: Math.max(0, previous.subscribersCount + (isSubscribed ? 1 : -1)),
    } : previous);
  }, []);

  return {
    channel,
    videos,
    playlists,
    isChannelLoading,
    isVideosLoading,
    isPlaylistsLoading,
    channelError,
    videosError,
    playlistsError,
    hasMoreVideos,
    loadMoreVideos,
    updateSubscription,
  };
};
