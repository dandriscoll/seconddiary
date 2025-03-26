import React, { useState, useEffect } from 'react';
import ReactDOM from 'react-dom/client';
import './index.css';
import App from './App';
import { PublicClientApplication, Configuration } from '@azure/msal-browser';
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
        
        // Ensure the required properties exist
        if (!config.auth.clientId) {
          throw new Error("Client ID is missing in the configuration");
        }

        const msalConfig: Configuration = {
          auth: {
            clientId: config.auth.clientId,
            authority: config.auth.authority,
            redirectUri: config.auth.redirectUri,
          },
          cache: config.cache
        };
        
        const msalInstance = new PublicClientApplication(msalConfig);
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

const root = document.getElementById('root');
if (!root) throw new Error('Root element not found');

ReactDOM.createRoot(root).render(
  <React.StrictMode>
    <LoadingMsal />
  </React.StrictMode>
);
