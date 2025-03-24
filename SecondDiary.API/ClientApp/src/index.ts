import React, { useState, useEffect } from 'react';
import ReactDOM from 'react-dom';
import './index.css';
import App from './App';
import { PublicClientApplication } from '@azure/msal-browser';
import { MsalProvider } from '@azure/msal-react';
import { fetchMsalConfig } from './authConfig';

// Initial loading component
const LoadingMsal: React.FC = () => {
  const [msalInstance, setMsalInstance] = useState<PublicClientApplication | null>(null);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    const initializeMsal = async (): Promise<void> => {
      try {
        const config = await fetchMsalConfig();
        const msalInstance = new PublicClientApplication(config);
        await msalInstance.initialize();
        setMsalInstance(msalInstance);
      } catch (error) {
        setError(error as Error);
        console.error('MSAL Initialization Error:', error);
      }
    };

    initializeMsal();
  }, []);

  if (error) {
    return <div className="error">Failed to initialize authentication. Please try refreshing the page.</div>;
  }

  if (!msalInstance) {
    return <div className="loading">Loading authentication...</div>;
  }

  return (
    <MsalProvider instance={msalInstance}>
      <App />
    </MsalProvider>
  );
};

ReactDOM.render(
  <React.StrictMode>
    <LoadingMsal />
  </React.StrictMode>,
  document.getElementById('root')
);
