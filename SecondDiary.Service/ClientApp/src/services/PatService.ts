/**
 * Service for handling Personal Access Token operations
 */

export interface PersonalAccessTokenSummary {
  id: string;
  tokenPrefix: string;
  createdAt: string;
  isActive: boolean;
}

export interface CreatePersonalAccessTokenResponse {
  id: string;
  token: string;
  tokenPrefix: string;
  createdAt: string;
  warning: string;
}

export class PatService {
  /**
   * Creates a new Personal Access Token
   * @param authToken The authentication token for API access
   * @returns Promise resolving to the created token response
   */
  public static async createToken(authToken: string): Promise<CreatePersonalAccessTokenResponse> {
    const response: Response = await fetch('/api/PersonalAccessToken', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${authToken.trim()}`
      }
    });

    if (!response.ok) {
      const errorData = await response.text();
      throw new Error(`Failed to create PAT: ${response.status} ${response.statusText} - ${errorData}`);
    }

    return await response.json();
  }

  /**
   * Gets all Personal Access Tokens for the current user
   * @param authToken The authentication token for API access
   * @returns Promise resolving to array of token summaries
   */
  public static async getUserTokens(authToken: string): Promise<PersonalAccessTokenSummary[]> {
    const response: Response = await fetch('/api/PersonalAccessToken', {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${authToken.trim()}`
      }
    });

    if (!response.ok) {
      const errorData = await response.text();
      throw new Error(`Failed to get PATs: ${response.status} ${response.statusText} - ${errorData}`);
    }

    return await response.json();
  }

  /**
   * Revokes a Personal Access Token
   * @param tokenId The ID of the token to revoke
   * @param authToken The authentication token for API access
   * @returns Promise resolving when token is revoked
   */
  public static async revokeToken(tokenId: string, authToken: string): Promise<void> {
    const response: Response = await fetch(`/api/PersonalAccessToken/${tokenId}`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${authToken.trim()}`
      }
    });

    if (!response.ok) {
      const errorData = await response.text();
      throw new Error(`Failed to revoke PAT: ${response.status} ${response.statusText} - ${errorData}`);
    }
  }
}
