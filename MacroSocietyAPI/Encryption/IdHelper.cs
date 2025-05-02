namespace MacroSocietyAPI.Encryption
{
    public static class IdHelper
    {
        public static bool TryDecryptId(string encryptedId, out int id, out string error)
        {
            try
            {
                var decrypted = AesEncryptionService.Decrypt(encryptedId);
                error = null;
                return int.TryParse(decrypted, out id);
            }
            catch (Exception ex)
            {
                id = 0;
                error = ex.Message;
                return false;
            }
        }
    }

    /*public static class IdHelper
    {
        public static bool TryDecryptId(string encryptedId, out int id, out string error)
        {
            try
            {
                var decrypted = AesEncryptionService.Decrypt(encryptedId);
                error = null;
                return int.TryParse(decrypted, out id);
            }
            catch (Exception ex)
            {
                id = 0;
                error = ex.Message;
                return false;
            }
        }
    }*/
}
