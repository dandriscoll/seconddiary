import React, { useState, useEffect } from 'react';
import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react';
import { SignInButton } from './components/SignInButton';
import { SignOutButton } from './components/SignOutButton';
import { ProfileContent } from './components/ProfileContent';
import './index.less'; // Updated to use the master stylesheet

const App: React.FC = () => {
  const { accounts } = useMsal();
  const isAuthenticated = accounts.length > 0;
  const [thought, setThought] = useState('');
  const [context, setContext] = useState('');
  const [isPosting, setIsPosting] = useState(false);
  const [theme, setTheme] = useState<'light' | 'dark'>('light');

  // Detect system theme preference
  useEffect(() => {
    // Check for system dark mode preference
    const darkModeMediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    
    // Set initial theme based on system preference
    setTheme(darkModeMediaQuery.matches ? 'dark' : 'light');
    
    // Listen for changes in the system theme
    const handleThemeChange = (e: MediaQueryListEvent) => {
      setTheme(e.matches ? 'dark' : 'light');
    };
    
    darkModeMediaQuery.addEventListener('change', handleThemeChange);
    
    // Clean up event listener
    return () => {
      darkModeMediaQuery.removeEventListener('change', handleThemeChange);
    };
  }, []);

  // Apply theme class to body
  useEffect(() => {
    document.body.classList.remove('light-theme', 'dark-theme');
    document.body.classList.add(`${theme}-theme`);
  }, [theme]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!thought.trim()) return;
    
    setIsPosting(true);
    
    try {
      // Replace with your actual API endpoint
      const response = await fetch('/api/diary', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ thought, context }),
      });
      
      if (response.ok) {
        // Clear form on successful submission
        setThought('');
        setContext('');
      } else {
        console.error('Failed to post entry');
      }
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
            <SignOutButton />
          </div>
        ) : (
          <SignInButton />
        )}
      </header>

      <main className="App-main">
        <UnauthenticatedTemplate>
          <p>Welcome to your diary application! Please sign in to continue.</p>
        </UnauthenticatedTemplate>

        <AuthenticatedTemplate>
          <div className="profile-container">
            <ProfileContent />
            <p>You're now signed in and can access your diary.</p>
          </div>
          
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
