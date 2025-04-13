using System.Security.Cryptography;
using System.Text;

namespace MacroSocietyAPI.Encryption
{
    public class AesEncryptionService
    {
        private static readonly string _key = "bastanov_1234567"; // 16 символов 
        private static readonly string _iv = "societyiv_123456";  // 16 символов

        public static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.IV = Encoding.UTF8.GetBytes(_iv);
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            using var encryptor = aes.CreateEncryptor();
            byte[] inputBuffer = Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted = encryptor.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);
            return Convert.ToBase64String(encrypted);
        }

        public static string Decrypt(string encryptedText)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.IV = Encoding.UTF8.GetBytes(_iv);
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            using var decryptor = aes.CreateDecryptor();
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            return Encoding.UTF8.GetString(decrypted);
        }
    }
}
