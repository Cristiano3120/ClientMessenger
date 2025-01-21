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

        public static byte[] EncryptAes(byte[] dataToEncrypt)
        {
            if (dataToEncrypt == null || dataToEncrypt.Length == 0)
                throw new ArgumentException("Data to encrypt cannot be null or empty", nameof(dataToEncrypt));

            using (ICryptoTransform encryptor = Aes.CreateEncryptor())
            {
                return encryptor.TransformFinalBlock(dataToEncrypt, 0, dataToEncrypt.Length);
            }
        }

        public static byte[] EncryptRSA(RSAParameters publicKey, byte[] data)
        {
            using var rsa = RSA.Create();
            {
                rsa.ImportParameters(publicKey);
                return rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
            }
        }

        #endregion

        #region Decryption

        public static byte[] DecryptMessage(byte[] receivedData)
        {
            try
            {
                return DecryptAes(receivedData);
            }
            catch (Exception)
            {
                return receivedData;
            }
        }

        private static byte[] DecryptAes(byte[] encryptedData)
        {
            var decryptor = Aes.CreateDecryptor();

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
            {
                cs.Write(encryptedData, 0, encryptedData.Length);
            }

            return ms.ToArray();
        }

        #endregion

        #region Compress/ Decompress

        /// <summary>
        /// <c>Returns</c> the compressed data but only if the compressed data is smaller than the original
        /// </summary>
        internal static byte[] CompressData(byte[] data)
        {
            using var compressor = new Compressor(new CompressionOptions(1));
            var compressedData = compressor.Wrap(data);
            return compressedData.Length >= data.Length
                ? data
                : compressedData;
        }

        internal static byte[] DecompressData(byte[] data)
        {
            try
            {
                using var decompressor = new Decompressor();
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
