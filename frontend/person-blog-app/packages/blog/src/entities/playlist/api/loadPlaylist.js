import { getPlayList } from '@/shared/api/generated/play-list/play-list';

const playListApi = getPlayList();
const playlistPageSize = 100;

export const loadPlaylist = async (playlistId, signal) => {
  const requestPage = (page) => playListApi.getApiPlayListItemId(playlistId, {
    signal,
    params: { page, pageSize: playlistPageSize },
  });

  const firstResponse = await requestPage(1);
  const firstPage = firstResponse.data.postPage;
  const totalPageCount = firstPage?.totalPageCount ?? 1;

  if (totalPageCount <= 1) return firstResponse.data;

  const remainingResponses = await Promise.all(
    Array.from({ length: totalPageCount - 1 }, (_, index) => requestPage(index + 2)),
  );

  return {
    ...firstResponse.data,
    postPage: {
      ...firstPage,
      items: [
        ...(firstPage?.items ?? []),
        ...remainingResponses.flatMap((response) => response.data.postPage?.items ?? []),
      ],
    },
  };
};
