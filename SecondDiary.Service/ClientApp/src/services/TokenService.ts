import { AccountInfo, IPublicClientApplication } from '@azure/msal-browser';
import { loginRequest } from '../authConfig';

/**
 * Service for handling authentication token operations
 */
export class TokenService {
  private static TOKEN_KEY: string = 'authToken';
  
  /**
   * Stores the authentication token in local storage
   * @param token The authentication token to store
   */
  public static storeToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
  }

  /**
   * Retrieves the authentication token from local storage
   * @returns The stored token or null if not found
   */
  public static getStoredToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  /**
   * Removes the authentication token from local storage
   */
  public static removeToken(): void {
    localStorage.removeItem(this.TOKEN_KEY);
  }

  /**
   * Refreshes the authentication token silently if possible
   * @param instance The MSAL instance
   * @param account The user account
   * @returns A promise resolving to the new token or null if refresh failed
   */
  public static async refreshToken(
    instance: IPublicClientApplication,
    account: AccountInfo
  ): Promise<string | null> {
    try {
      const response = await instance.acquireTokenSilent({
        ...loginRequest,
        account: account
      });
      
      if (response && response.accessToken) {
        this.storeToken(response.accessToken);
        return response.accessToken;
      }
      return null;
    } catch (error) {
      console.error('Token refresh failed:', error);
      this.removeToken();
      return null;
    }
  }
}