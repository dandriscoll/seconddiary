import React, { useState, useEffect } from 'react';
import { PatService, PersonalAccessTokenSummary, CreatePersonalAccessTokenResponse } from '../services/PatService';

interface PersonalAccessTokenManagerProps {
  token: string;
}

export const PersonalAccessTokenManager: React.FC<PersonalAccessTokenManagerProps> = ({ token }) => {
  const [pats, setPats] = useState<PersonalAccessTokenSummary[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [creating, setCreating] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [newToken, setNewToken] = useState<CreatePersonalAccessTokenResponse | null>(null);
  const [copySuccess, setCopySuccess] = useState<boolean>(false);

  // Load existing PATs on component mount
  useEffect(() => {
    loadPats();
  }, [token]);

  const loadPats = async (): Promise<void> => {
    try {
      setLoading(true);
      setError(null);
      const tokens: PersonalAccessTokenSummary[] = await PatService.getUserTokens(token);
      setPats(tokens);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load tokens');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateToken = async (): Promise<void> => {
    try {
      setCreating(true);
      setError(null);
      setNewToken(null);
      const response: CreatePersonalAccessTokenResponse = await PatService.createToken(token);
      setNewToken(response);
      await loadPats(); // Refresh the list
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create token');
    } finally {
      setCreating(false);
    }
  };

  const handleRevokeToken = async (tokenId: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to revoke this token? This action cannot be undone.')) {
      return;
    }

    try {
      setError(null);
      await PatService.revokeToken(tokenId, token);
      await loadPats(); // Refresh the list
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to revoke token');
    }
  };

  const handleCopyToken = async (): Promise<void> => {
    if (!newToken) return;

    try {
      await navigator.clipboard.writeText(newToken.token);
      setCopySuccess(true);
      setTimeout(() => setCopySuccess(false), 2000);
    } catch (err) {
      setError('Failed to copy token to clipboard');
    }
  };

  const handleDismissNewToken = (): void => {
    setNewToken(null);
    setCopySuccess(false);
  };

  const formatDate = (dateString: string): string => {
    const date: Date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  };

  return (
    <div className="pat-manager">
      <div className="pat-manager-header">
        <h3>Personal Access Tokens</h3>
        <p className="pat-description">
          Personal Access Tokens can be used to authenticate API requests without using your main account credentials.
        </p>
      </div>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}

      {/* New Token Display */}
      {newToken && (
        <div className="alert alert-warning" role="alert">
          <h5>New Token Created</h5>
          <p className="mb-2"><strong>Warning:</strong> {newToken.warning}</p>
          <div className="input-group mb-2">
            <input
              type="text"
              className="form-control"
              value={newToken.token}
              readOnly
              style={{ fontFamily: 'monospace', fontSize: '0.9em' }}
            />
            <button
              className="btn btn-outline-secondary"
              type="button"
              onClick={handleCopyToken}
            >
              {copySuccess ? 'Copied!' : 'Copy'}
            </button>
          </div>
          <button
            className="btn btn-sm btn-secondary"
            onClick={handleDismissNewToken}
          >
            I've copied the token
          </button>
        </div>
      )}

      {/* Create New Token Button */}
      <div className="pat-create-section mb-4">
        <button
          className="btn btn-primary"
          onClick={handleCreateToken}
          disabled={creating || loading}
        >
          {creating ? 'Creating...' : 'Create New Token'}
        </button>
      </div>

      {/* Existing Tokens List */}
      <div className="pat-list-section">
        <h4>Existing Tokens</h4>
        
        {loading ? (
          <div className="text-center py-3">
            <div className="spinner-border" role="status">
              <span className="visually-hidden">Loading...</span>
            </div>
          </div>
        ) : pats.length === 0 ? (
          <div className="alert alert-info">
            No Personal Access Tokens found. Create one to get started.
          </div>
        ) : (
          <div className="table-responsive">
            <table className="table table-striped">
              <thead>
                <tr>
                  <th>Token Prefix</th>
                  <th>Created</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {pats.map((pat: PersonalAccessTokenSummary) => (
                  <tr key={pat.id}>
                    <td>
                      <code>{pat.tokenPrefix}...</code>
                    </td>
                    <td>{formatDate(pat.createdAt)}</td>
                    <td>
                      <span className={`badge ${pat.isActive ? 'bg-success' : 'bg-secondary'}`}>
                        {pat.isActive ? 'Active' : 'Revoked'}
                      </span>
                    </td>
                    <td>
                      {pat.isActive ? (
                        <button
                          className="btn btn-sm btn-outline-danger"
                          onClick={() => handleRevokeToken(pat.id)}
                        >
                          Revoke
                        </button>
                      ) : (
                        <span className="text-muted">—</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};
