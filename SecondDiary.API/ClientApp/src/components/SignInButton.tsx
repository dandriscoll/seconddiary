import React from 'react';
import { useMsal } from '@azure/msal-react';
import { loginRequest } from '../authConfig';

interface SignInButtonProps {
  onTokenAcquired: (token: string) => void;
}

export const SignInButton: React.FC<SignInButtonProps> = ({ onTokenAcquired }) => {
  const { instance } = useMsal();

  const handleLogin = (): void => {
    instance.loginPopup(loginRequest)
      .then(response => {
        const token: string = response.accessToken;
        onTokenAcquired(token);
      })
      .catch(error => console.log(error));
  };

  return (
    <button className="btn btn-primary" onClick={handleLogin}>
      Sign In
    </button>
  );
};
