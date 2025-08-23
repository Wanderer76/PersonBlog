export type PostBanRequest = {
    postId: string;
    userMessage: string | null;
};

export type BanRequestsResponse = {
    totalCount: number;
    totalPages: number;
    currentPage: number; // 0-based
    pageSize: number;
    banRequests: PostBanRequestItemModel[];
};

export type PostBanRequestsViewModel = {
    items: PostBanRequestItemModel[];
    count: number;
};

export type PostBanRequestItemModel = {
    id: number,
    postId: string,
    filename: string,
    reasonId: string,
    userMessage: string,
    createdAt: string,
    title: string
}

export type GroupedPostComplaint = {
  postId: string;
  title: string;
  complaintsCount: number;
  lastComplaintAt: string; // ISO
};

export type GroupedComplaintsResponse = {
  items: GroupedPostComplaint[];
  count: number;
};

export type BanRequest = {
  id: string;
  postId: string;
  title: string;
  reasonId: string | number;
  userMessage: string | null;
  createdAt: string;
};
