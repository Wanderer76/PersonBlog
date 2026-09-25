import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { PlaylistCard } from '@/entities/playlist';
import { BigVideoCard } from '@/entities/post';
import { ChannelSubscribeButton } from '@/features/channel-subscription';
import DefaultProfileIcon from '@/shared/assets/defaultProfilePic.png';
import { useIntersectionObserver } from '@/shared/hooks/useIntersectionObserver';
import { getLocalDateTime } from '@/shared/lib/date';
import { Tabs } from '@/shared/ui/Tabs/Tabs';
import { PageShell } from '@/widgets/page-shell';
import type { ChannelTab } from '../model/types';
import { useChannelPage } from '../model/useChannelPage';
import styles from './ChannelPage.module.css';

const TAB_ITEMS = [
  { id: 'videos', label: 'Видео' },
  { id: 'playlists', label: 'Плейлисты' },
  { id: 'about', label: 'О канале' },
];

interface ChannelPageContentProps {
  channelId: string;
}

const ChannelPageContent = ({ channelId }: ChannelPageContentProps) => {
  const [activeTab, setActiveTab] = useState<ChannelTab>('videos');
  const {
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
  } = useChannelPage(channelId, activeTab);
  const { lastElementRef } = useIntersectionObserver({
    onLoadMore: loadMoreVideos,
    hasMore: hasMoreVideos && !isVideosLoading,
  });

  const handleTabChange = (tabId: string) => setActiveTab(tabId as ChannelTab);

  return (
    <PageShell className={styles.channelContainer} contentClassName={styles.profileContainer}>
      {isChannelLoading && !channel && <div className={styles.pageState}>Загрузка канала...</div>}
      {channelError && !channel && <div className={styles.pageState} role="alert">{channelError}</div>}

      {channel && (
        <>
          <header className={styles.profileHeader}>
            <div className={styles.avatarSection}>
              <div className={styles.avatarWrapper}>
                <img
                  src={channel.photoUrl || DefaultProfileIcon}
                  alt={channel.name}
                  className={styles.profileAvatar}
                />
              </div>

              <div className={styles.profileInfo}>
                <h1 className={styles.blogTitle}>{channel.name}</h1>
                <div className={styles.profileMeta}>
                  <span>👥 {channel.subscribersCount} подписчиков</span>
                  <span>📅 Канал создан: {channel.createdAt ? getLocalDateTime(channel.createdAt) : '—'}</span>
                </div>
                {channel.description && <p className={styles.postDescription}>{channel.description}</p>}
              </div>
            </div>

            <ChannelSubscribeButton
              channelId={channelId}
              isSubscribed={channel.isSubscribed}
              onChange={updateSubscription}
            />
          </header>

          <section className={styles.postsSection}>
            <Tabs
              activeTab={activeTab}
              onChange={handleTabChange}
              items={TAB_ITEMS}
              ariaLabel="Разделы канала"
            />

            {activeTab === 'videos' && (
              <div className={styles.postsGrid}>
                {videos.map((video, index) => (
                  <BigVideoCard
                    key={video.id}
                    ref={index === videos.length - 1 ? lastElementRef : undefined}
                    videoCardModel={{
                      postId: video.id,
                      title: video.title || 'Без названия',
                      previewUrl: video.videoInfo.previewUrl,
                      viewCount: video.viewCount,
                      creator: {
                        blogId: channel.id,
                        name: channel.name,
                        avatarUrl: channel.photoUrl,
                      },
                    }}
                  />
                ))}
                {isVideosLoading && <p className={styles.loadingState}>Загрузка видео...</p>}
                {videosError && <p className={styles.errorState} role="alert">{videosError}</p>}
                {!isVideosLoading && !videosError && videos.length === 0 && (
                  <p className={styles.emptyState}>Нет доступных видео</p>
                )}
              </div>
            )}

            {activeTab === 'playlists' && (
              <div className={styles.postsGrid}>
                {playlists.map(playlist => <PlaylistCard key={playlist.id} playlist={playlist} />)}
                {isPlaylistsLoading && <p className={styles.loadingState}>Загрузка плейлистов...</p>}
                {playlistsError && <p className={styles.errorState} role="alert">{playlistsError}</p>}
                {!isPlaylistsLoading && !playlistsError && playlists.length === 0 && (
                  <p className={styles.emptyState}>Нет доступных плейлистов</p>
                )}
              </div>
            )}

            {activeTab === 'about' && (
              <div className={styles.aboutSection}>
                <h2>О канале</h2>
                <p>{channel.description || 'Описание отсутствует'}</p>

                <div className={styles.details}>
                  <div className={styles.detailItem}>
                    <span>Дата создания:</span>
                    <span>{channel.createdAt ? getLocalDateTime(channel.createdAt) : '—'}</span>
                  </div>
                  <div className={styles.detailItem}>
                    <span>Подписчиков:</span>
                    <span>{channel.subscribersCount}</span>
                  </div>
                </div>
              </div>
            )}
          </section>
        </>
      )}
    </PageShell>
  );
};

const ChannelPage = () => {
  const { channelId } = useParams<{ channelId: string }>();

  if (!channelId) {
    return (
      <PageShell className={styles.channelContainer} contentClassName={styles.profileContainer}>
        <div className={styles.pageState} role="alert">Не указан идентификатор канала</div>
      </PageShell>
    );
  }

  return <ChannelPageContent key={channelId} channelId={channelId} />;
};

export default ChannelPage;
