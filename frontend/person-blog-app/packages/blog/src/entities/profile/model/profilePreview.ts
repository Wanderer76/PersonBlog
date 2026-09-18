import { createContext, useContext } from 'react';
import type { BlogModel } from '@/shared/api/generated/models';

export const initialPerson = { name: 'Артём Иванов', username: 'admin', photoUrl: '', createdAt: '2026-08-14', interests: [] as string[] };
export const profilePreviewFeatures = { friendsEnabled: false, messagesEnabled: false };
export type Person = typeof initialPerson;
export type BlogState = { status: 'loading' | 'ready' | 'error'; blog: BlogModel | null };
type ProfilePreview = BlogState & { person: Person; updatePerson: (person: Person) => void; reloadBlog: () => void; features: typeof profilePreviewFeatures };
export const ProfilePreviewContext = createContext<ProfilePreview | null>(null);

export function useProfilePreview() {
    const value = useContext(ProfilePreviewContext);
    if (!value) throw new Error('ProfilePreviewProvider is required');
    return value;
}
