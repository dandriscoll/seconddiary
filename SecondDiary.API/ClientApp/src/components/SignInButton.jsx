import React from 'react';
import { useMsal } from '@azure/msal-react';
import { loginRequest } from '../authConfig';

export const SignInButton = () => {
  const { instance } = useMsal();

  const handleLogin = () => {
    instance.loginPopup(loginRequest)
      .catch(error => console.log(error));
  };

  return (
    <button className="btn btn-primary" onClick={handleLogin}>
      Sign In
    </button>
  );
};
