import { customInstance } from '@/shared/api/mutator';

export type ProfileModel = {
    id: number;
    name: string;
    userId: string;
    photoUrl: string | null;
    profileState: number;
    createdAt: string;
    interests: string[];
};

export type ProfileContext = {
    blog: {
        hasBlog: boolean;
        id: string | null;
    };
    features: {
        friendsEnabled: boolean;
        messagesEnabled: boolean;
    };
    permissions: {
        publishVideo: { isAllowed: boolean };
        publishText: { isAllowed: boolean };
    };
};

export const getMyProfile = (signal?: AbortSignal) =>
    customInstance<ProfileModel>({ url: '/api/Profile/my', method: 'GET', signal });

export const getProfileContext = (signal?: AbortSignal) =>
    customInstance<ProfileContext>({ url: '/api/Profile/context', method: 'GET', signal });
