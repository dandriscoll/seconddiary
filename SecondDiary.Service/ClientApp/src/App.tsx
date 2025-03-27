import React, { useState, useEffect, useCallback } from 'react';
import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react';
import { AccountInfo, InteractionRequiredAuthError } from '@azure/msal-browser';
import { ProfileContent } from './components/ProfileContent';
import { SystemPromptEditor } from './components/SystemPromptEditor';
import { loginRequest } from './authConfig';
import './index.less';

const App: React.FC = () => {
  const { instance, accounts } = useMsal();
  const isAuthenticated: boolean = accounts.length > 0;
  const [thought, setThought] = useState<string>('');
  const [context, setContext] = useState<string>('');
  const [isPosting, setIsPosting] = useState<boolean>(false);
  const [theme, setTheme] = useState<'light' | 'dark'>('light');
  const [token, setToken] = useState<string | null>(null);
  const [tokenLoading, setTokenLoading] = useState<boolean>(false);

  // Get active account
  const activeAccount: AccountInfo | null = accounts.length > 0 ? accounts[0] : null;

  // Function to acquire token silently
  const acquireToken = useCallback(async (): Promise<string | null> => {
    if (!activeAccount) return null;
    
    setTokenLoading(true);
    
    try {
      const response = await instance.acquireTokenSilent({
        ...loginRequest,
        account: activeAccount
      })
      
      setTokenLoading(false);
      return response.accessToken;
    } catch (error) {
      // If silent acquisition fails due to interaction required
      if (error instanceof InteractionRequiredAuthError) {
        try {
          // Fallback to popup
          const response = await instance.acquireTokenPopup(loginRequest);
          setTokenLoading(false);
          return response.accessToken;
        } catch (popupError) {
          console.error('Error during popup token acquisition:', popupError);
          setTokenLoading(false);
          return null;
        }
      }
      console.error('Error acquiring token silently:', error);
      setTokenLoading(false);
      return null;
    }
  }, [instance, activeAccount]);

  // Get token for SystemPromptEditor when authenticated state changes
  useEffect(() => {
    const getTokenForPromptEditor = async (): Promise<void> => {
      if (isAuthenticated) {
        const token: string | null = await acquireToken();
        setToken(token);
      } else {
        setToken(null);
      }
    };
    
    getTokenForPromptEditor();
  }, [isAuthenticated, acquireToken]);

  // SignInButton functionality
  const handleLogin = (): void => {
    instance.loginPopup(loginRequest)
      .catch(error => console.error('Login failed:', error));
  };

  // SignOutButton functionality - clears token from MSAL cache without logging out of AAD
  const handleSignOut = (): void => {
    setToken(null);
    
    // Clear token from MSAL cache for current account only
    if (activeAccount) {
      const logoutRequest = {
        account: activeAccount,
        postLogoutRedirectUri: window.location.origin,
        onRedirectNavigate: () => false // prevents redirect
      };
      
      // This clears the token cache for the current account without navigating to AAD logout
      instance.logoutRedirect(logoutRequest)
        .catch((error: Error) => {
          console.error('Error clearing token cache:', error);
        });
    }
  };

  // Detect system theme preference
  useEffect(() => {
    // Check for system dark mode preference
    const darkModeMediaQuery: MediaQueryList = window.matchMedia('(prefers-color-scheme: dark)');
    
    // Set initial theme based on system preference
    setTheme(darkModeMediaQuery.matches ? 'dark' : 'light');
    
    // Listen for changes in the system theme
    const handleThemeChange = (e: MediaQueryListEvent): void => setTheme(e.matches ? 'dark' : 'light');
    
    darkModeMediaQuery.addEventListener('change', handleThemeChange);
    
    // Clean up event listener
    return () => darkModeMediaQuery.removeEventListener('change', handleThemeChange);
  }, []);

  // Apply theme class to body
  useEffect(() => {
    document.body.classList.remove('light-theme', 'dark-theme');
    document.body.classList.add(`${theme}-theme`);
  }, [theme]);

  const handleSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    
    if (!thought.trim()) return;
    
    setIsPosting(true);
    
    try {
      // Get a fresh token before making the API request
      const currentToken: string | null = await acquireToken();
      if (!currentToken) throw new Error('Failed to acquire token');
      
      // Replace with your actual API endpoint
      const response: Response = await fetch('/api/diary', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${currentToken}`
        },
        body: JSON.stringify({ thought, context }),
      });
      
      if (response.ok) {
        // Clear form on successful submission
        setThought('');
        setContext('');
      } else console.error('Failed to post entry');
    } catch (error) {
      console.error('Error posting entry:', error);
    } finally {
      setIsPosting(false);
    }
  };

  return (
    <div className={`App ${theme}-theme`}>
      <header className="App-header">
        <h1>Second Diary</h1>
        {isAuthenticated ? (
          <div className="authenticated-container">
            <button className="btn btn-secondary" onClick={handleSignOut}>
              Sign Out
            </button>
          </div>
        ) : (
          <button className="btn btn-primary" onClick={handleLogin}>
            Sign In
          </button>
        )}
      </header>

      <main className="App-main">
        <UnauthenticatedTemplate>
          <p>Welcome to your thought diary! Please sign in to continue.</p>
        </UnauthenticatedTemplate>

        <AuthenticatedTemplate>
          <div className="profile-container">
            <ProfileContent />
          </div>
          {tokenLoading ? (
            <p>Loading authentication token...</p>
          ) : token ? (
            <SystemPromptEditor token={token} />
          ) : (
            <p>Token is missing. Please try signing in again.</p>
          )}
          <div className="diary-entry-form">
            <h2>Create New Entry</h2>
            <form onSubmit={handleSubmit} className="responsive-form">
              <div className="form-group">
                <label htmlFor="thought">Thought (Required)</label>
                <textarea
                  id="thought"
                  value={thought}
                  onChange={(e) => setThought(e.target.value)}
                  placeholder="Enter your thought here..."
                  rows={4}
                  required
                  disabled={isPosting}
                  className="full-width-input"
                />
              </div>
              <div className="form-group">
                <label htmlFor="context">Context (Optional)</label>
                <textarea
                  id="context"
                  value={context}
                  onChange={(e) => setContext(e.target.value)}
                  placeholder="Enter optional context for this thought..."
                  rows={2}
                  disabled={isPosting}
                  className="full-width-input"
                />
              </div>
              <button 
                type="submit" 
                disabled={isPosting || !thought.trim()}
                className="post-button full-width-button"
              >
                {isPosting ? 'Posting...' : 'Post'}
              </button>
            </form>
          </div>
        </AuthenticatedTemplate>
      </main>
    </div>
  );
};

export default App;
