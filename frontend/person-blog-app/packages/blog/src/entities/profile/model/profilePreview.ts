import { createContext, useContext } from 'react';
import type { BlogModel } from '@/shared/api/generated/models';

export const initialPerson = { name: '', username: null as string | null, photoUrl: '', createdAt: null as string | null, interests: [] as string[] };
export const profilePreviewFeatures = { friendsEnabled: false, messagesEnabled: false };
export const initialPermissions = { publishVideo: { isAllowed: false }, publishText: { isAllowed: false } };
export type Person = typeof initialPerson;
export type LoadStatus = 'loading' | 'ready' | 'error';
export type BlogState = { status: LoadStatus; blog: BlogModel | null };
type ProfilePreview = BlogState & {
    person: Person;
    profileStatus: LoadStatus;
    updatePerson: (person: Person) => void;
    reloadProfile: () => void;
    reloadBlog: () => void;
    features: typeof profilePreviewFeatures;
    permissions: typeof initialPermissions;
};
export const ProfilePreviewContext = createContext<ProfilePreview | null>(null);

export function useProfilePreview() {
    const value = useContext(ProfilePreviewContext);
    if (!value) throw new Error('ProfilePreviewProvider is required');
    return value;
}
