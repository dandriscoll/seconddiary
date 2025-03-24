import React from 'react';
import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react';
import { SignInButton } from './components/SignInButton';
import { SignOutButton } from './components/SignOutButton';
import { ProfileContent } from './components/ProfileContent';

function App() {
  const { accounts } = useMsal();
  const isAuthenticated = accounts.length > 0;

  return (
    <div className="App">
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
        </AuthenticatedTemplate>
      </main>
    </div>
  );
}

export default App;
