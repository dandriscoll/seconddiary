using System.Security.Cryptography;
using System.Text;

namespace SecondDiary.API.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionService(IConfiguration configuration)
        {
            string encryptionKey = configuration["Encryption:Key"];
            if (string.IsNullOrEmpty(encryptionKey))
                throw new InvalidOperationException("Encryption key is not configured");

            // Convert the key to bytes and ensure it's 32 bytes (256 bits)
            _key = Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32));
            _iv = new byte[16]; // Using a fixed IV for simplicity, but in production you should use a unique IV per encryption
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            return Convert.ToBase64String(cipherBytes);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}