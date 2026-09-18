import { useEffect, useState, type ReactNode } from 'react';
import axios from 'axios';
import { getMyProfile, getProfileContext } from '@/entities/profile/api/profileApi';
import { getBlog } from '@/shared/api/generated/blog/blog';
import { initialPermissions, initialPerson, profilePreviewFeatures, ProfilePreviewContext, type BlogState, type LoadStatus } from './profilePreview';
import { JwtTokenService, subscribeToAuthState } from '@/shared/auth/tokenStorage';

type ProfileState = { status: LoadStatus; requestKey: string };
type ContextState = BlogState & {
    requestKey: string;
    features: typeof profilePreviewFeatures;
    permissions: typeof initialPermissions;
};

export function ProfilePreviewProvider({ children }: { children: ReactNode }) {
    const [person, setPerson] = useState(initialPerson);
    const [profileState, setProfileState] = useState<ProfileState>({ status: 'loading', requestKey: '' });
    const [contextState, setContextState] = useState<ContextState>({
        status: 'loading', blog: null, requestKey: '', features: profilePreviewFeatures, permissions: initialPermissions
    });
    const [profileRevision, setProfileRevision] = useState(0);
    const [contextRevision, setContextRevision] = useState(0);
    const profileRequestKey = String(profileRevision);
    const contextRequestKey = String(contextRevision);
    useEffect(() => subscribeToAuthState(() => {
        setPerson(initialPerson);
        setProfileState({ status: 'loading', requestKey: '' });
        setContextState({ status: 'loading', blog: null, requestKey: '', features: profilePreviewFeatures, permissions: initialPermissions });
        setProfileRevision(value => value + 1);
        setContextRevision(value => value + 1);
    }), []);
    useEffect(() => {
        if (!JwtTokenService.isAuth()) return;
        const controller = new AbortController();
        async function load() {
            try {
                const { data } = await getMyProfile(controller.signal);
                setPerson({
                    name: data.name,
                    username: null,
                    photoUrl: data.photoUrl ?? '',
                    createdAt: data.createdAt,
                    interests: data.interests ?? []
                });
                setProfileState({ status: 'ready', requestKey: profileRequestKey });
            } catch (error: unknown) {
                if (!axios.isCancel(error)) setProfileState({ status: 'error', requestKey: profileRequestKey });
            }
        }
        void load();
        return () => controller.abort();
    }, [profileRequestKey]);
    useEffect(() => {
        if (!JwtTokenService.isAuth()) return;
        const controller = new AbortController();
        async function load() {
            try {
                const { data: context } = await getProfileContext(controller.signal);
                const blog = context.blog.hasBlog && context.blog.id
                    ? (await getBlog().getApiBlogBlogBlogId(context.blog.id, { signal: controller.signal })).data
                    : null;
                setContextState({
                    status: 'ready', blog, requestKey: contextRequestKey,
                    features: context.features, permissions: context.permissions
                });
            } catch (error: unknown) {
                if (!axios.isCancel(error)) {
                    setContextState({
                        status: 'error', blog: null, requestKey: contextRequestKey,
                        features: profilePreviewFeatures, permissions: initialPermissions
                    });
                }
            }
        }
        void load();
        return () => controller.abort();
    }, [contextRequestKey]);
    const currentProfileStatus = profileState.requestKey === profileRequestKey ? profileState.status : 'loading';
    const currentContext: ContextState = contextState.requestKey === contextRequestKey
        ? contextState
        : { status: 'loading', blog: null, requestKey: '', features: profilePreviewFeatures, permissions: initialPermissions };
    return <ProfilePreviewContext.Provider value={{
        status: currentContext.status,
        blog: currentContext.blog,
        features: currentContext.features,
        permissions: currentContext.permissions,
        person,
        profileStatus: currentProfileStatus,
        updatePerson: setPerson,
        reloadProfile: () => setProfileRevision(value => value + 1),
        reloadBlog: () => setContextRevision(value => value + 1)
    }}>{children}</ProfilePreviewContext.Provider>;
}
