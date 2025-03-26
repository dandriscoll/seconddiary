import React from 'react';
import { useMsal } from '@azure/msal-react';

export const SignOutButton: React.FC = () => {
  const { instance } = useMsal();

  const handleLogout = (): void => {
    instance.logout();
  };

  return (
    <button className="btn btn-secondary" onClick={handleLogout}>
      Sign Out
    </button>
  );
};
