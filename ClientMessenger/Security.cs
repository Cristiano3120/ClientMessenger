using System.IO;
using System.Security.Cryptography;
using ZstdNet;

namespace ClientMessenger
{
    internal static class Security
    {
        public static Aes Aes { get; private set; }

        static Security()
        {
            Aes = Aes.Create();
            Aes.GenerateIV();
            Aes.GenerateKey();
        }

        #region Encryption

        public static async Task<byte[]> EncryptAesAsync(byte[] dataToEncrypt)
        {
            if (dataToEncrypt == null || dataToEncrypt.Length == 0)
                return [];

            using (MemoryStream ms = new())
            {
                using (ICryptoTransform encryptor = Aes.CreateEncryptor())
                {
                    using (CryptoStream cryptoStream = new(ms, encryptor, CryptoStreamMode.Write, true))
                    {
                        await cryptoStream.WriteAsync(dataToEncrypt);
                        await cryptoStream.FlushFinalBlockAsync();
                    }
                }
                
                return ms.ToArray();
            }
        }

        public static byte[] EncryptRSA(RSAParameters publicKey, byte[] data)
        {
            using RSA rsa = RSA.Create();
            {
                rsa.ImportParameters(publicKey);
                return rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
            }
        }

        #endregion

        #region Decryption

        public static async Task<byte[]> DecryptMessageAsync(byte[] receivedData)
        {
            try
            {
                return await DecryptAesAsync(receivedData);
            }
            catch (Exception)
            {
                return receivedData;
            }
        }

        private static async Task<byte[]> DecryptAesAsync(byte[] encryptedData)
        {
            ICryptoTransform decryptor = Aes.CreateDecryptor();

            using (MemoryStream ms = new())
            {
                using (CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Write, true))
                {
                    await cs.WriteAsync(encryptedData);
                }

                return ms.ToArray();
            }   
        }

        #endregion

        #region Compress/ Decompress

        /// <summary>
        /// <c>Returns</c> the compressed data but only if the compressed data is smaller than the original
        /// </summary>
        internal static byte[] CompressData(byte[] data)
        {
            using Compressor compressor = new(new CompressionOptions(1));
            byte[] compressedData = compressor.Wrap(data);
            return compressedData.Length >= data.Length
                ? data
                : compressedData;
        }

        internal static byte[] DecompressData(byte[] data)
        {
            try
            {
                using Decompressor decompressor = new();
                return decompressor.Unwrap(data);
            }
            catch (Exception)
            {
                return data;
            }
        }

        #endregion
    }
}
