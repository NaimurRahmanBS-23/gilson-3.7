using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Infrastructure;
using Nop.Services.Logging;

namespace Bss.Nop.Plugin.Custom.Common
{
    public class Crypto
    {
        private const string CRYPTO_KEY = "baysh0re*1";

        // Use strong encryption for passwords
        public static string EncryptPassword(string passwordStr)
        {
            return Encrypt(passwordStr, CRYPTO_KEY, true);
        }

        public static string DecryptPassword(string encryptedPassword)
        {
            return Decrypt(encryptedPassword, CRYPTO_KEY, true);
        }


        private static string Encrypt(string toEncrypt, string key, bool useHashing)
        {
            if (string.IsNullOrEmpty(toEncrypt)) return string.Empty;

            string outStr = string.Empty;
            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();

            try
            {
                byte[] keyArray;
                byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

                if (useHashing)
                {
                    keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
                    hashmd5.Clear();
                }
                else
                    keyArray = UTF8Encoding.UTF8.GetBytes(key);

                tdes.Key = keyArray;
                tdes.Mode = CipherMode.ECB;
                tdes.Padding = PaddingMode.PKCS7;

                ICryptoTransform cTransform = tdes.CreateEncryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                tdes.Clear();
                outStr = Convert.ToBase64String(resultArray, 0, resultArray.Length);
            }
            catch (Exception ex)
            {
                // Log to nop error log
                var logger = EngineContext.Current.Resolve<ILogger>();
                logger.Error("Custom Crypto Error (Encrypt): " + ex.Message, ex);
            }
            finally
            {
                hashmd5.Clear();
                tdes.Clear();
            }
            return outStr;
        }

        private static string Decrypt(string toDecrypt, string key, bool useHashing)
        {
            if (string.IsNullOrEmpty(toDecrypt)) return string.Empty;

            string outStr = string.Empty;
            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();

            try
            {
                byte[] keyArray;
                byte[] toEncryptArray = Convert.FromBase64String(toDecrypt);

                if (useHashing)
                {
                    keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
                }
                else
                    keyArray = UTF8Encoding.UTF8.GetBytes(key);

                tdes.Key = keyArray;
                tdes.Mode = CipherMode.ECB;
                tdes.Padding = PaddingMode.PKCS7;

                ICryptoTransform cTransform = tdes.CreateDecryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                outStr = UTF8Encoding.UTF8.GetString(resultArray);
            }
            catch (Exception ex)
            {
                // Log to nop error log
                var logger = EngineContext.Current.Resolve<ILogger>();
                logger.Error("Custom Crypto Error (Decrypt): " + ex.Message, ex);
            }
            finally
            {
                hashmd5.Clear();
                tdes.Clear();
            }
            return outStr;
        }

    }
}
