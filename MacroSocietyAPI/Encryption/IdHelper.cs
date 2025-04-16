namespace MacroSocietyAPI.Encryption
{
    public static class IdHelper
    {
        public static bool TryDecryptId(string encryptedId, out int id)
        {
            try
            {
                var decrypted = AesEncryptionService.Decrypt(encryptedId);
                return int.TryParse(decrypted, out id);
            }
            catch
            {
                id = 0;
                return false;
            }
        }
    }
}
