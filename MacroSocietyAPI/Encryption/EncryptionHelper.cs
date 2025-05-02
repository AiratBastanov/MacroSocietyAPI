using System.Security.Cryptography;
using System.Text;

namespace MacroSocietyAPI.Encryption
{
    public class AesEncryptionService
    {
        private static readonly string _key = "bastanov_1234567"; // 16 символов
        private static readonly string _iv = "societyiv_123456";   // 16 символов

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

            // Стандартный Base64 с удалением переносов
            return Convert.ToBase64String(encrypted)
                .Replace("\r", "")
                .Replace("\n", "");
        }

        public static string Decrypt(string encryptedText)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_key);
                aes.IV = Encoding.UTF8.GetBytes(_iv);
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                using var decryptor = aes.CreateDecryptor();
                byte[] encryptedBytes = Convert.FromBase64String(FixUrlSafeBase64(encryptedText));
                byte[] decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

                return Encoding.UTF8.GetString(decrypted);
            }
            catch (FormatException ex)
            {
                throw new Exception("Invalid encrypted data format.", ex);
            }
            catch (CryptographicException ex)
            {
                throw new Exception("Decryption failed due to invalid key, IV, or data.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error during decryption.", ex);
            }
        }

        private static string FixUrlSafeBase64(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Encrypted text is empty.");

            // Заменяем безопасные символы обратно на стандартные
            string fixedBase64 = input.Replace('-', '+').Replace('_', '/');

            // Добавляем недостающие символы "=" в конец
            int padding = 4 - (fixedBase64.Length % 4);
            if (padding != 4)
            {
                fixedBase64 = fixedBase64.PadRight(fixedBase64.Length + padding, '=');
            }

            return fixedBase64;
        }
    }
}
