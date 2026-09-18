import { useEffect, useState, type ReactNode } from 'react';
import { useLocation } from 'react-router-dom';
import { getBlog } from '@/shared/api/generated/blog/blog';
import { initialPerson, profilePreviewFeatures, ProfilePreviewContext, type BlogState } from './profilePreview';
import { JwtTokenService, subscribeToAuthState } from '@/shared/auth/tokenStorage';

// Temporary personal-profile source, to be replaced by /Profile/my and /context.
// Existing blog APIs remain live; demo edits are never sent to the server.

export function ProfilePreviewProvider({ children }: { children: ReactNode }) {
    const [person, setPerson] = useState(initialPerson);
    const [state, setState] = useState<BlogState & { requestKey: string }>({ status: 'loading', blog: null, requestKey: '' });
    const [revision, setRevision] = useState(0);
    const { pathname } = useLocation();
    const requestKey = `${pathname}:${revision}`;
    useEffect(() => subscribeToAuthState(() => {
        setPerson(initialPerson);
        setState({ status: 'loading', blog: null, requestKey: '' });
        setRevision(value => value + 1);
    }), []);
    useEffect(() => {
        let active = true;
        if (!JwtTokenService.isAuth()) return;
        async function load() {
            try {
                const api = getBlog();
                const { data } = await api.getApiBlogHasUserBlog();
                if (!active) return;
                const blog = data.hasBlog ? (await api.getApiBlogDetail()).data : null;
                if (active) setState({ status: 'ready', blog, requestKey });
            } catch {
                if (active) setState({ status: 'error', blog: null, requestKey });
            }
        }
        void load();
        return () => { active = false; };
    }, [requestKey]);
    const currentState: BlogState = state.requestKey === requestKey ? state : { status: 'loading', blog: null };
    return <ProfilePreviewContext.Provider value={{ ...currentState, person, updatePerson: setPerson, features: profilePreviewFeatures, reloadBlog: () => setRevision(value => value + 1) }}>{children}</ProfilePreviewContext.Provider>;
}
