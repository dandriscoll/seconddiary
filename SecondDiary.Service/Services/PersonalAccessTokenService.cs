using System.Security.Cryptography;
using System.Text;
using SecondDiary.Models;

namespace SecondDiary.Services
{
    public interface IPersonalAccessTokenService
    {
        Task<CreatePersonalAccessTokenResponse> CreateTokenAsync(string userId);
        Task<IEnumerable<PersonalAccessTokenSummary>> GetUserTokensAsync(string userId);
        Task<PersonalAccessToken?> ValidateTokenAsync(string token);
        Task<bool> RevokeTokenAsync(string tokenId, string userId);
        Task<PersonalAccessToken?> GetTokenAsync(string userId, string tokenId);
    }

    public class PersonalAccessTokenService : IPersonalAccessTokenService
    {
        private readonly ICosmosDbService _cosmosDbService;
        private readonly ILogger<PersonalAccessTokenService> _logger;
        private const string TokenPrefix = "p_";
        private const int TokenLength = 40; // Length of the random part

        public PersonalAccessTokenService(
            ICosmosDbService cosmosDbService,
            ILogger<PersonalAccessTokenService> logger)
        {
            _cosmosDbService = cosmosDbService;
            _logger = logger;
        }

        public async Task<CreatePersonalAccessTokenResponse> CreateTokenAsync(string userId)
        {
            // Generate a secure random token
            string tokenValue = GenerateSecureToken();
            string fullToken = $"{TokenPrefix}{tokenValue}";
            
            // Hash the token for storage
            string id = HashToken(fullToken);
            
            // Get first 8 characters for display prefix
            string displayPrefix = fullToken.Substring(0, Math.Min(12, fullToken.Length));

            PersonalAccessToken token = new PersonalAccessToken
            {
                Id = id,
                UserId = userId,
                TokenPrefix = displayPrefix,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            PersonalAccessToken createdToken = await _cosmosDbService.CreatePersonalAccessTokenAsync(token);
            
            _logger.LogInformation("Created new PAT {TokenId} for user {UserId}", 
                createdToken.Id, userId);

            return new CreatePersonalAccessTokenResponse
            {
                Id = createdToken.Id,
                Token = fullToken, // Only returned once at creation
                TokenPrefix = displayPrefix,
                CreatedAt = createdToken.CreatedAt
            };
        }

        public async Task<IEnumerable<PersonalAccessTokenSummary>> GetUserTokensAsync(string userId)
        {
            IEnumerable<PersonalAccessToken> tokens = await _cosmosDbService.GetPersonalAccessTokensAsync(userId);
            
            return tokens.Select(t => new PersonalAccessTokenSummary
            {
                Id = t.Id,
                TokenPrefix = t.TokenPrefix,
                CreatedAt = t.CreatedAt,
                IsActive = t.IsActive
            });
        }

        public async Task<PersonalAccessToken?> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token) || !token.StartsWith(TokenPrefix))
            {
                _logger.LogWarning("Invalid token format provided for validation");
                return null;
            }

            string id = HashToken(token);
            PersonalAccessToken? storedToken = await _cosmosDbService.GetPersonalAccessTokenByIdAsync(id);
            
            if (storedToken == null)
            {
                _logger.LogWarning("Token not found during validation");
                return null;
            }

            if (!storedToken.IsValid)
            {
                _logger.LogWarning("Token {TokenId} is invalid (inactive)", storedToken.Id);
                return null;
            }

            _logger.LogDebug("Token {TokenId} validated successfully for user {UserId}", storedToken.Id, storedToken.UserId);
            return storedToken;
        }

        public async Task<bool> RevokeTokenAsync(string tokenId, string userId)
        {
            try
            {
                await _cosmosDbService.DeletePersonalAccessTokenAsync(tokenId, userId);
                _logger.LogInformation("Revoked PAT {TokenId} for user {UserId}", tokenId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke PAT {TokenId} for user {UserId}", tokenId, userId);
                return false;
            }
        }

        public async Task<PersonalAccessToken?> GetTokenAsync(string userId, string tokenId)
        {
            return await _cosmosDbService.GetPersonalAccessTokenByIdAsync(tokenId);
        }

        private string GenerateSecureToken()
        {
            byte[] randomBytes = new byte[TokenLength];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            
            // Convert to base64 and make URL-safe
            string token = Convert.ToBase64String(randomBytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
                
            return token;
        }

        private string HashToken(string token)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
