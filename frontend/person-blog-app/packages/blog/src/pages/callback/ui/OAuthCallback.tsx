// src/pages/OAuthCallback.tsx
import { getAndClearOAuthReturnUrl, JwtTokenService } from '@/shared/auth/tokenStorage';
import { useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';

const OAuthCallback = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  useEffect(() => {
    const code = searchParams.get('code');
    const state = searchParams.get('state');
    const error = searchParams.get('error');

    if (error) {
      console.error('OAuth error:', error);
      JwtTokenService.redirectToAuth(searchParams.get('return_url') || '/');
      return;
    }

    if (!code || !state) {
      JwtTokenService.redirectToAuth(searchParams.get('return_url') || '/');
      return;
    }
    const handleCallback = async () => {
      try {
        await JwtTokenService.exchangeCodeForTokens(code, state);
        // Успех: редирект на лавную или return_url
        const returnUrl = getAndClearOAuthReturnUrl();
        navigate(returnUrl, { replace: true });
      } catch (err) {
        console.error('Token exchange failed:', err);
        JwtTokenService.cleanAuth();
        JwtTokenService.redirectToAuth(searchParams.get('return_url') || '/')
      }
    };

    handleCallback();
  }, [searchParams, navigate]);

  return <div>Обработка входа...</div>;
};

export default OAuthCallback;
