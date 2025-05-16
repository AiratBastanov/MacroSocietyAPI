using System.Security.Cryptography;
using System.Text;

namespace MacroSocietyAPI.Encryption
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    public class AesEncryptionService
    {
        private static readonly string _key = "bastanov_1234567"; // 16 символов
        private static readonly string _iv = "societyiv_123456";  // 16 символов

        public static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.IV = Encoding.UTF8.GetBytes(_iv);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

            // URL-safe Base64: заменяем + на -, / на _, убираем =
            string base64 = Convert.ToBase64String(encryptedBytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            return base64;
        }

        public static string Decrypt(string encryptedText)
        {
            try
            {
                // Восстанавливаем стандартный Base64 из URL-safe
                string base64 = encryptedText
                    .Replace('-', '+')
                    .Replace('_', '/');

                // Добавляем padding, если нужно
                int padding = 4 - (base64.Length % 4);
                if (padding != 4)
                    base64 = base64.PadRight(base64.Length + padding, '=');

                byte[] encryptedBytes = Convert.FromBase64String(base64);

                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_key);
                aes.IV = Encoding.UTF8.GetBytes(_iv);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                return null;
            }
        }
    }
}
