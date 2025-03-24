import React from 'react';
import { useMsal } from '@azure/msal-react';

export const SignOutButton = () => {
  const { instance } = useMsal();

  const handleLogout = () => {
    instance.logout();
  };

  return (
    <button className="btn btn-secondary" onClick={handleLogout}>
      Sign Out
    </button>
  );
};
