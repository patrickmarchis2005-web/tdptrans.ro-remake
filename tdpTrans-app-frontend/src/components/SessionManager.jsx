import { useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { logoutUser } from '../api/authApi';
import {
  clearStoredUser,
  getLastActivityAt,
  getStoredUser,
  markSessionActivity,
} from '../utils/session';

const publicPaths = new Set(['/login', '/signup', '/recover-password', '/']);

const SessionManager = () => {
  const location = useLocation();
  const navigate = useNavigate();

  useEffect(() => {
    const trackedEvents = ['click', 'keydown', 'mousemove', 'scroll', 'touchstart'];

    const onActivity = () => {
      if (getStoredUser()) {
        markSessionActivity();
      }
    };

    trackedEvents.forEach((eventName) => window.addEventListener(eventName, onActivity, { passive: true }));

    const intervalId = window.setInterval(() => {
      const user = getStoredUser();
      if (!user) {
        return;
      }

      const expiresAt = new Date(user.sessionExpiresAtUtc).getTime();
      const idleTimeoutMs = (user.sessionIdleTimeoutSeconds ?? 0) * 1000;
      const lastActivityAt = getLastActivityAt();
      const isAbsoluteExpiryReached = Number.isFinite(expiresAt) && Date.now() >= expiresAt;
      const isIdleExpiryReached = idleTimeoutMs > 0 && Date.now() - lastActivityAt >= idleTimeoutMs;

      if (!isAbsoluteExpiryReached && !isIdleExpiryReached) {
        return;
      }

      void logoutUser();
      clearStoredUser();

      if (!publicPaths.has(location.pathname)) {
        navigate('/login', {
          replace: true,
          state: {
            message: isIdleExpiryReached
              ? 'Sesiunea a expirat din cauza inactivitatii.'
              : 'Sesiunea a expirat. Autentifica-te din nou.',
          },
        });
      }
    }, 15000);

    return () => {
      trackedEvents.forEach((eventName) => window.removeEventListener(eventName, onActivity));
      window.clearInterval(intervalId);
    };
  }, [location.pathname, navigate]);

  return null;
};

export default SessionManager;
