import { useState } from 'react';
import { getSubscriber } from '@/shared/api/generated/subscriber/subscriber';
import { Button } from '@/shared/ui/Button/Button';
import styles from './ChannelSubscribeButton.module.css';

interface ChannelSubscribeButtonProps {
  channelId: string;
  isSubscribed: boolean;
  onChange: (isSubscribed: boolean) => void;
}

const subscriberApi = getSubscriber();

export const ChannelSubscribeButton = ({ channelId, isSubscribed, onChange }: ChannelSubscribeButtonProps) => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleClick = async () => {
    if (isLoading) return;
    setIsLoading(true);
    setError(null);

    try {
      if (isSubscribed) {
        await subscriberApi.postApiSubscriberUnsubscribeBlogId(channelId);
      } else {
        await subscriberApi.postApiSubscriberSubscribeBlogId(channelId);
      }
      onChange(!isSubscribed);
    } catch {
      setError('Не удалось изменить подписку');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className={styles.root}>
      <Button
        type="button"
        variant={isSubscribed ? 'secondary' : 'primary'}
        loading={isLoading}
        onClick={handleClick}
      >
        {isSubscribed ? 'Вы подписаны' : 'Подписаться'}
      </Button>
      {error && <span className={styles.error} role="alert">{error}</span>}
    </div>
  );
};
