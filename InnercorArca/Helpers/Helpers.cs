using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace InnercorArca.V1.Helpers
{
    public class Helpers
    {
        public static bool CreatePfx(string crtPath, string keyPath, string pfxOutputPath, string pfxPassword, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                // Cargar certificado desde el archivo .crt
                //var certificate = new X509Certificate2(crtPath, keyPath,
                //    X509KeyStorageFlags.MachineKeySet |
                //    X509KeyStorageFlags.PersistKeySet |
                //    X509KeyStorageFlags.Exportable);
                ////byte[] certBytes = File.ReadAllBytes(crtPath);
                ////var certificate = new X509Certificate2(certBytes);
                string pem = File.ReadAllText(crtPath);
                byte[] certBuffer = Convert.FromBase64String(
                    pem.Replace("-----BEGIN CERTIFICATE-----", "")
                       .Replace("-----END CERTIFICATE-----", "")
                       .Replace("\r", "")
                       .Replace("\n", "")
                );
                var certificate = new X509Certificate2(certBuffer);



                // Cargar la clave privada desde el archivo .key
                var privateKey = LoadPrivateKeyFromPem(keyPath);
                if (privateKey == null)
                {
                    errorMessage = "No se pudo cargar la clave privada.";
                    return false;
                }

                // Asociar la clave privada al certificado
                var certificateWithKey = AttachPrivateKey(certificate, privateKey);

                // Exportar como PFX
                byte[] pfxBytes = certificateWithKey.Export(X509ContentType.Pfx, pfxPassword);
                File.WriteAllBytes(pfxOutputPath, pfxBytes);

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private static RSACryptoServiceProvider LoadPrivateKeyFromPem(string keyPath)
        {
            string pemKey = File.ReadAllText(keyPath);
            return DecodeRsaPrivateKey(pemKey);
        }

        private static RSACryptoServiceProvider DecodeRsaPrivateKey(string pemKey)
        {
            string base64Key = Regex.Replace(pemKey, @"-----(BEGIN|END) (RSA )?PRIVATE KEY-----", "").Replace("\n", "").Replace("\r", "").Trim();
            byte[] privateKeyBytes = Convert.FromBase64String(base64Key);

            var rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(privateKeyBytes);
            return rsa;
        }

        private static X509Certificate2 AttachPrivateKey(X509Certificate2 cert, RSACryptoServiceProvider privateKey)
        {
            return cert.CopyWithPrivateKey(privateKey);
        }




    }

}
