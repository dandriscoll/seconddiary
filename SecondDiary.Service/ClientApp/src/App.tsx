import React, { useState, useEffect, useCallback, useRef } from 'react';
import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react';
import { AccountInfo, InteractionRequiredAuthError } from '@azure/msal-browser';
import { ProfileContent } from './components/ProfileContent';
import { SystemPromptEditor } from './components/SystemPromptEditor';
import { EmailSettings } from './components/EmailSettings';
import { PersonalAccessTokenManager } from './components/PersonalAccessTokenManager';
import { MarkdownContent } from './components/MarkdownContent';
import { loginRequest } from './authConfig';
import { isMobile } from 'react-device-detect';
import './index.less';

const App: React.FC = () => {
  const { instance, accounts } = useMsal();
  const isAuthenticated: boolean = accounts.length > 0;
  const [thought, setThought] = useState<string>('');
  const [context, setContext] = useState<string>('');
  const [isPosting, setIsPosting] = useState<boolean>(false);
  const [isGettingRecommendation, setIsGettingRecommendation] = useState<boolean>(false);
  const [theme, setTheme] = useState<'light' | 'dark'>('light');
  const [token, setToken] = useState<string | null>(null);
  const [tokenLoading, setTokenLoading] = useState<boolean>(false);
  const [showSystemPrompt, setShowSystemPrompt] = useState<boolean>(false);
  const [showEmailSettings, setShowEmailSettings] = useState<boolean>(false);
  const [showPATManager, setShowPATManager] = useState<boolean>(false);
  const [userEmail, setUserEmail] = useState<string>('');
  const [recommendationData, setRecommendationData] = useState<string | null>(null);
  const [isRecording, setIsRecording] = useState<boolean>(false);
  const recognitionRef = useRef<SpeechRecognition | null>(null);

  // Get active account
  const activeAccount: AccountInfo | null = accounts.length > 0 ? accounts[0] : null;

  // Function to acquire token silently
  const acquireToken = useCallback(async (): Promise<string | null> => {
    if (!activeAccount) return null;
    
    setTokenLoading(true);
    
    try {
      // Explicitly request ID token in addition to access token
      const tokenRequest = {
        ...loginRequest,
        account: activeAccount,
        scopes: ['user.read', 'openid', 'profile'],
        forceRefresh: true
      };
      
      const response = await instance.acquireTokenSilent(tokenRequest);
      setTokenLoading(false);
          
      // Try to use ID token if available
      if (response.idToken && response.idToken.includes('.')) {
        console.log('Using ID token from popup instead of access token');
        return response.idToken;
      }
      
      return response.accessToken;
    } catch (error) {
      // If silent acquisition fails due to interaction required
      if (error instanceof InteractionRequiredAuthError) {
        try {
          // Fallback to popup with explicit request for id_token
          const tokenRequest = {
            ...loginRequest,
            scopes: ['user.read', 'openid', 'profile']
          };
          const response = await instance.acquireTokenPopup(tokenRequest);
          setTokenLoading(false);
          
          // Try to use ID token if available
          if (response.idToken && response.idToken.includes('.')) {
            console.log('Using ID token from popup instead of access token');
            return response.idToken;
          }
          
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
        
        // Extract email from token
        if (token) {
          try {
            // JWT tokens are in the format: header.payload.signature
            const payload = token.split('.')[1];
            // Decode the base64 encoded payload
            const decodedPayload = JSON.parse(atob(payload));
            // Get the email claim - could be in different fields depending on the token
            const email = decodedPayload.email || decodedPayload.preferred_username || '';
            setUserEmail(email);
          } catch (error) {
            console.error('Error extracting email from token:', error);
          }
        }
      } else {
        setToken(null);
        setUserEmail('');
      }
    };
    
    getTokenForPromptEditor();
  }, [isAuthenticated, acquireToken]);

  // Toggle system prompt visibility
  const toggleSystemPrompt = (): void => {
    setShowSystemPrompt(prev => !prev);
  };

  // Toggle email settings visibility
  const toggleEmailSettings = (): void => {
    setShowEmailSettings(prev => !prev);
  };

  // Toggle PAT manager visibility
  const togglePATManager = (): void => {
    setShowPATManager(prev => !prev);
  };

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

    const toLocalISOString = (date = new Date()): string => {
      const pad = (num: number) => String(num).padStart(2, '0');
      const year = date.getFullYear();
      const month = pad(date.getMonth() + 1);
      const day = pad(date.getDate());
      const hour = pad(date.getHours());
      const minute = pad(date.getMinutes());
      const second = pad(date.getSeconds());
      const millisecond = String(date.getMilliseconds()).padStart(3, '0');
    
      // Calculate timezone offset in minutes and convert it to hours and minutes.
      const timezoneOffset = -date.getTimezoneOffset(); // offset in minutes (reverse sign)
      const sign = timezoneOffset >= 0 ? '+' : '-';
      const offsetHour = pad(Math.floor(Math.abs(timezoneOffset) / 60));
      const offsetMinute = pad(Math.abs(timezoneOffset) % 60);
    
      return `${year}-${month}-${day}T${hour}:${minute}:${second}.${millisecond}${sign}${offsetHour}:${offsetMinute}`;
    }
    
    const handleSubmit = async (e: React.FormEvent): Promise<void> => {
      e.preventDefault();
      
      if (!thought.trim()) return;
      
      setIsPosting(true);
      
      try {
        // Get a fresh JWT token before making the API request
        const currentToken: string | null = await acquireToken();
        if (!currentToken) throw new Error('Failed to acquire token');
        
        const response: Response = await fetch('/api/diary', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${currentToken.trim()}`,
            'X-Entry-Date': toLocalISOString()
          },
          body: JSON.stringify({ thought, context })
        });
        
        if (!response.ok) {
          const errorData = await response.text();
          console.error('API error response:', errorData);
          throw new Error(`API error: ${response.status} ${response.statusText}`);
        }
        
        // Clear form on successful submission
        setThought('');
        setContext('');
      } catch (error) {
        console.error('Error posting entry:', error);
      } finally {
        setIsPosting(false);
      }
    };

    const handleGetRecommendation = async (): Promise<void> => {
      setIsGettingRecommendation(true);
    
      try {
        const currentToken: string | null = await acquireToken();
        if (!currentToken) throw new Error('Failed to acquire token');
    
        const response: Response = await fetch('/api/diary/recommendation', {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${currentToken.trim()}`
          }
        });
    
        if (!response.ok) {
          const errorData = await response.text();
          console.error('API error response:', errorData);
          throw new Error(`API error: ${response.status} ${response.statusText}`);
        }
    
        setRecommendationData(await response.text());
        // Handle recommendation data as needed (e.g., display to user)
      } catch (error) {
        console.error('Error fetching recommendation:', error);
      } finally {
        setIsGettingRecommendation(false);
      }
    };    

    const handleSpeechRecognition = (): void => {
      if (!('webkitSpeechRecognition' in window || 'SpeechRecognition' in window)) {
        console.error('Speech recognition is not supported in this browser.');
        return;
      }

      if (!recognitionRef.current) {
        const SpeechRecognition = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
        const recognition = new SpeechRecognition() as SpeechRecognition;
        recognition.lang = 'en-US';
        recognition.interimResults = false;
        recognition.maxAlternatives = 1;

        recognition.onstart = () => {
          setIsRecording(true);
        };

        recognition.onresult = (event: SpeechRecognitionEvent) => {
          const spokenText = event.results[0][0].transcript;
          setThought(prev => `${prev} ${spokenText}`.trim());
        };

        recognition.onerror = (event: SpeechRecognitionErrorEvent) => {
          console.error('Speech recognition error:', event.error);
          setIsRecording(false);
        };

        recognition.onend = () => {
          setIsRecording(false);
        };

        recognitionRef.current = recognition;
      }

      const recognition = recognitionRef.current as SpeechRecognition;

      if (isRecording) {
        recognition.stop();
      } else {
        recognition.start();
      }
    };
  
  return (
    <div className={`App ${theme}-theme`}>
      <header className="App-header">
        <div className="header-container">
          <div className="header-left">
            <h1>Second Diary</h1>
            {isAuthenticated && !tokenLoading && token && (
              <div className="header-profile">
                <ProfileContent />
              </div>
            )}
          </div>
          <div className="header-controls">
            {isAuthenticated ? (
              <>
                {tokenLoading ? (
                  <span className="loading-token">Loading...</span>
                ) : token && (
                  <>
                    <button 
                      onClick={toggleSystemPrompt}
                      className="btn btn-outline-secondary me-2"
                    >
                      {showSystemPrompt ? 'Hide System Prompt' : 'Show System Prompt'}
                    </button>
                    
                    <button 
                      onClick={toggleEmailSettings}
                      className="btn btn-outline-secondary me-2"
                    >
                      {showEmailSettings ? 'Hide Email Settings' : 'Show Email Settings'}
                    </button>
                    
                    <button 
                      onClick={togglePATManager}
                      className="btn btn-outline-secondary me-2"
                    >
                      {showPATManager ? 'Hide PAT Manager' : 'Manage PATs'}
                    </button>
                  </>
                )}
                <button className="btn btn-secondary" onClick={handleSignOut}>
                  Sign Out
                </button>
              </>
            ) : (
              <button className="btn btn-primary" onClick={handleLogin}>
                Sign In
              </button>
            )}
          </div>
        </div>
      </header>

      <main className="App-main">
        <UnauthenticatedTemplate>
          <p>Welcome to your thought diary! Please sign in to continue.</p>
        </UnauthenticatedTemplate>

        <AuthenticatedTemplate>
          {token ? (
            <>
              {showSystemPrompt && <SystemPromptEditor token={token} />}
              {showEmailSettings && <EmailSettings token={token} userEmail={userEmail} />}
              {showPATManager && <PersonalAccessTokenManager token={token} />}
            </>
          ) : !tokenLoading && (
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
                {isMobile && (
                  <button 
                    type="button" 
                    onClick={handleSpeechRecognition} 
                    className={`btn ${isRecording ? 'btn-danger' : 'btn-secondary'} speech-button`}
                  >
                    {isRecording ? 'Stop speaking' : 'Speak your thought'}
                  </button>
                )}
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
                disabled={isPosting || isGettingRecommendation || !thought.trim()}
                className="post-button full-width-button"
              >
                {isPosting ? 'Posting...' : 'Post'}
              </button>

              <button
                type="button"
                onClick={handleGetRecommendation}
                disabled={isPosting || isGettingRecommendation || !!thought.trim()}
                className="recommendation-button full-width-button"
              >
                {isGettingRecommendation ? 'Loading Recommendation...' : 'Get Recommendation'}
              </button>
              <div className="recommendation-section">
                {recommendationData && (
                  <div className="recommendation-container">
                    <h3>Your Recommendation</h3>
                    <div className="recommendation-content">
                      <MarkdownContent content={recommendationData} />
                    </div>
                  </div>
                )}
              </div>
            </form>
          </div>
        </AuthenticatedTemplate>
      </main>
    </div>
  );
};

export default App;
