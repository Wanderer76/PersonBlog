import { useRef, useEffect, useCallback } from 'react';

interface UseIntersectionObserverProps {
  onLoadMore: () => void;
  hasMore: boolean;
  threshold?: number;
}

export const useIntersectionObserver = ({ 
  onLoadMore, 
  hasMore, 
  threshold = 0.1 
}: UseIntersectionObserverProps) => {
  const observerRef = useRef<IntersectionObserver | null>(null);

  const lastElementRef = useCallback((node: HTMLElement | null) => {
    if (observerRef.current) {
      observerRef.current.disconnect();
    }

    if (!hasMore) return;

    observerRef.current = new IntersectionObserver((entries) => {
      if (entries[0].isIntersecting) {
        onLoadMore();
      }
    }, { threshold });

    if (node) {
      observerRef.current.observe(node);
    }
  }, [hasMore, onLoadMore, threshold]);

  useEffect(() => {
    return () => {
      if (observerRef.current) {
        observerRef.current.disconnect();
      }
    };
  }, []);

  return { lastElementRef };
};