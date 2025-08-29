// services/api.ts
import API from '../scripts/apiMethod';
import type { PagedListViewModel, TrackViewItem } from '../types/music';


export const musicApi = {
  getTracks: async (page: number = 1, pageSize: number = 20): Promise<PagedListViewModel<TrackViewItem>> => {
    const response = await API.get(`TrackSearch/filtered`, {
      params: { page, pageSize }
    });
    return response.data;
  },

  searchTracks: async (query: string, page: number = 1, pageSize: number = 20): Promise<PagedListViewModel<TrackViewItem>> => {
    const response = await API.get(`TrackSearch/filtered`, {
      params: { query, page, pageSize }
    });
    return response.data;
  },

  getTrackPresignedUrl: async (trackId: string): Promise<string> => {
    const response = await API.get(`TrackListen/redirect/${trackId}`);
    if (response.status !== 200) throw new Error('Failed to get presigned URL');
    const data = await response.data;
    return data;
  },
};