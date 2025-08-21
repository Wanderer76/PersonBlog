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