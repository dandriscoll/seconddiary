using SecondDiary.Services;

namespace SecondDiary.Tests.Services
{
    public class EncryptionServiceTests
    {
        private readonly IEncryptionService _encryptionService;
        private readonly string _testKey = "ThisIsA32ByteTestKeyForEncryption!";

        public EncryptionServiceTests()
        {
            // Setup mock configuration
            Mock<IConfiguration> mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(c => c["Encryption:Key"]).Returns(_testKey);

            // Create the service with mock configuration
            _encryptionService = new EncryptionService(mockConfiguration.Object);
        }

        [Fact]
        public void Encrypt_WithValidInput_ReturnsEncryptedString()
        {
            // Arrange
            string plainText = "This is a test message";

            // Act
            string encryptedText = _encryptionService.Encrypt(plainText);

            // Assert
            Assert.NotNull(encryptedText);
            Assert.NotEmpty(encryptedText);
            Assert.NotEqual(plainText, encryptedText);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Encrypt_WithEmptyOrNullInput_ReturnsSameValue(string input)
        {
            // Act
            string result = _encryptionService.Encrypt(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Decrypt_WithValidInput_ReturnsOriginalString()
        {
            // Arrange
            string originalText = "This is a test message";
            string encryptedText = _encryptionService.Encrypt(originalText);

            // Act
            string decryptedText = _encryptionService.Decrypt(encryptedText);

            // Assert
            Assert.Equal(originalText, decryptedText);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Decrypt_WithEmptyOrNullInput_ReturnsSameValue(string input)
        {
            // Act
            string result = _encryptionService.Decrypt(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void EncryptionService_WithMissingKey_ThrowsException()
        {
            // Arrange
            Mock<IConfiguration> mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(c => c["Encryption:Key"]).Returns((string?)null);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new EncryptionService(mockConfiguration.Object));
        }
    }
}
