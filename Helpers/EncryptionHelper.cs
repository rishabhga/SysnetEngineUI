using System.Security.Cryptography;
using System.Text;

namespace ManageEngineWebApp.Helpers
{
    public static class EncryptionHelper
    {

        private static readonly string EncryptionKey = "M@n@g3Eng1n3S3cur3K3y2026!@#$";

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            byte[] clearBytes = Encoding.Unicode.GetBytes(plainText);
            using (Aes encryptor = Aes.Create())
            {
                byte[] pdb = Encoding.UTF8.GetBytes(EncryptionKey);
                using (SHA256 sha256 = SHA256.Create())
                {
                    encryptor.Key = sha256.ComputeHash(pdb);
                }
                byte[] iv = new byte[16]; // Default 0s
                Array.Copy(Encoding.UTF8.GetBytes(EncryptionKey), iv, 16);
                encryptor.IV = iv;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    plainText = Convert.ToBase64String(ms.ToArray());
                }
            }
            plainText = plainText.Replace("+", "-").Replace("/", "_").Replace("=", "");
            return plainText;
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                cipherText = cipherText.Replace("-", "+").Replace("_", "/");
                int mod4 = cipherText.Length % 4;
                if (mod4 > 0)
                {
                    cipherText += new string('=', 4 - mod4);
                }

                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                using (Aes encryptor = Aes.Create())
                {
                    byte[] pdb = Encoding.UTF8.GetBytes(EncryptionKey);
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        encryptor.Key = sha256.ComputeHash(pdb);
                    }
                    
                    byte[] iv = new byte[16];
                    Array.Copy(Encoding.UTF8.GetBytes(EncryptionKey), iv, 16);
                    encryptor.IV = iv;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }
                        cipherText = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
            }
            catch
            {
                return null;
            }
            return cipherText;
        }
    }
}
