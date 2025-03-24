// TypeScript interfaces for the configurations
interface AuthConfig {
  clientId: string;
  tenantId: string;
}

interface MsalConfig {
  auth: {
    clientId?: string;
    authority?: string;
    redirectUri: string;
  };
  cache: {
    cacheLocation: "localStorage" | "sessionStorage";
    storeAuthStateInCookie: boolean;
  };
}

interface LoginRequest {
  scopes: string[];
}

interface GraphConfig {
  graphMeEndpoint: string;
}

// Function to fetch auth configuration from the backend
export async function fetchMsalConfig(): Promise<MsalConfig> {
  try {
    const response = await fetch('/api/auth/config');
    if (!response.ok) {
      throw new Error('Failed to fetch auth configuration');
    }
    const config: AuthConfig = await response.json();
    
    return {
      auth: {
        clientId: config.clientId,
        authority: `https://login.microsoftonline.com/${config.tenantId}`,
        redirectUri: window.location.origin,
      },
      cache: {
        cacheLocation: "sessionStorage",
        storeAuthStateInCookie: false,
      }
    };
  } catch (error) {
    console.error('Error fetching auth configuration:', error);
    throw error;
  }
}

// Base MSAL configuration without sensitive information
export const msalConfig: MsalConfig = {
  auth: {
    // clientId and authority will be populated from the backend
    redirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: "sessionStorage",
    storeAuthStateInCookie: false,
  }
};

// Add scopes for the ID token to be used at Microsoft identity platform endpoints.
export const loginRequest: LoginRequest = {
  scopes: ["User.Read"]
};

// Add endpoints for MS Graph API services you want to use.
export const graphConfig: GraphConfig = {
  graphMeEndpoint: "https://graph.microsoft.com/v1.0/me"
};
