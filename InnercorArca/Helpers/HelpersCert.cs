using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace InnercorArca.V1.Helpers
{
    public static class HelpersCert
    {

     
        public static X509Certificate2 ObtieneCertificadoDesdeArchivos(string rutaCrt, string rutaKey)
        {
            if (rutaCrt == null || rutaKey == null)
                throw new ArgumentNullException("Las rutas de certificado (.crt) y clave (.key) no deben ser nulas.");
            if (!File.Exists(rutaCrt))
                throw new FileNotFoundException($"No se encontró el archivo de certificado: {rutaCrt}");
            if (!File.Exists(rutaKey))
                throw new FileNotFoundException($"No se encontró el archivo de clave privada: {rutaKey}");

            try
            {
                // 1. Leer bytes del certificado (.crt)
                byte[] certBytes = File.ReadAllBytes(rutaCrt);
                // Si el certificado está en PEM (contiene encabezado BEGIN CERTIFICATE), extraer base64
                string certText = System.Text.Encoding.UTF8.GetString(certBytes);
                if (certText.Contains("-----BEGIN CERTIFICATE-----"))
                {
                    // Extraer la porción Base64 entre los marcadores PEM
                    string pemCert = certText;
                    const string beginCert = "-----BEGIN CERTIFICATE-----";
                    const string endCert = "-----END CERTIFICATE-----";
                    int beginIndex = pemCert.IndexOf(beginCert, StringComparison.Ordinal);
                    int endIndex = pemCert.IndexOf(endCert, StringComparison.Ordinal);
                    if (beginIndex >= 0 && endIndex > beginIndex)
                    {
                        string base64 = pemCert.Substring(beginIndex + beginCert.Length, endIndex - (beginIndex + beginCert.Length));
                        base64 = base64.Replace("\r", "").Replace("\n", ""); // remover saltos de línea
                        certBytes = Convert.FromBase64String(base64);
                    }
                }
                // Crear certificado X509Certificate2 (sin clave privada por ahora)
                X509Certificate2 cert = new X509Certificate2(certBytes);

                // 2. Leer la clave privada (.key) usando BouncyCastle
                AsymmetricKeyParameter privateKey;
                using (StreamReader keyReader = File.OpenText(rutaKey))
                {
                    PemReader pemReader = new PemReader(keyReader);
                    object pemObject = pemReader.ReadObject();
                    switch (pemObject)
                    {
                        case null:
                            throw new CryptographicException("El archivo .key no contiene una clave privada válida en formato PEM.");
                        case AsymmetricCipherKeyPair keyPair:
                            // Si la clave privada incluye información de la pública (par de claves)
                            privateKey = keyPair.Private;
                            break;
                        case AsymmetricKeyParameter keyParam:
                            privateKey = keyParam;
                            break;
                        default:
                            throw new CryptographicException("El formato de la clave privada no es reconocido. Asegúrese de que esté en PEM no cifrado.");
                    }
                }

                // 3. Combinar certificado y clave en un X509Certificate2
                // Usar BouncyCastle para crear un contenedor PKCS#12 en memoria
                string alias = "miCert";  // alias para la entrada, puede ser cualquier identificador
                var store = new Pkcs12StoreBuilder().Build();
                X509CertificateEntry certEntry = new X509CertificateEntry(DotNetUtilities.FromX509Certificate(cert));
                store.SetCertificateEntry(alias, certEntry);
                store.SetKeyEntry(alias, new AsymmetricKeyEntry(privateKey), new[] { certEntry });

                // Guardar el almacén PKCS#12 en memoria (sin contraseña)
                using (MemoryStream pfxStream = new MemoryStream())
                {
                    store.Save(pfxStream, new char[0] /* sin contraseña */, new SecureRandom());
                    // Crear X509Certificate2 a partir de los bytes PKCS#12
                    byte[] pfxBytes = pfxStream.ToArray();
                    // Marcar la clave como exportable y persistir clave si se desea (opcional flags)
                    X509Certificate2 certConClave = new X509Certificate2(pfxBytes, (string)null,
                                                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
                    return certConClave;
                }
            }
            catch (Exception ex)
            {
                // Envuelve cualquier excepción en una más descriptiva antes de lanzarla de nuevo
                throw new Exception($"Error al obtener X509Certificate2 desde archivos .crt y .key: {ex.Message}", ex);
            }
        }

        private static RSACryptoServiceProvider LoadPrivateKeyFromPem(string keyPath)
        {
            try
            {
                string pemKey = File.ReadAllText(keyPath);
                return DecodeRsaPrivateKey(pemKey);
            }
            catch (Exception)
            {
                return null;
            }
        }
        private static RSACryptoServiceProvider DecodeRsaPrivateKey(string pemKey)
        {
            try
            {
                string base64Key = Regex.Replace(pemKey, @"-----(BEGIN|END) (RSA )?PRIVATE KEY-----", "").Replace("\n", "").Replace("\r", "").Trim();
                byte[] privateKeyBytes = Convert.FromBase64String(base64Key);

                var rsa = new RSACryptoServiceProvider();
                rsa.ImportCspBlob(privateKeyBytes);
                return rsa;
            }
            catch (Exception)
            {
                return null;
            }
        }
        private static X509Certificate2 AttachPrivateKey(X509Certificate2 cert, RSACryptoServiceProvider privateKey)
        {
            return cert.CopyWithPrivateKey(privateKey);
        }
        public static bool CreatePfx(string crtPath, string keyPath, string pfxOutputPath, string pfxPassword, out string errorMessage)
        {

            errorMessage = string.Empty;
            try
            {
                 
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
                var certificateWithKey = HelpersCert.AttachPrivateKey(certificate, privateKey);

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

        public static X509Certificate2 LoadCertificateAndPrivateKey(string crtPath, string keyPath)
        {
            try
            {
                // Cargar el certificado (.crt) en formato PEM
                string certPem = File.ReadAllText(crtPath);
                byte[] certBytes = Convert.FromBase64String(
                    certPem.Replace("-----BEGIN CERTIFICATE-----", "")
                           .Replace("-----END CERTIFICATE-----", "")
                           .Replace("\r", "")
                           .Replace("\n", "")
                );
                X509Certificate2 certificate = new X509Certificate2(certBytes);

                // Cargar la clave privada (.key) en formato PEM
                AsymmetricKeyParameter privateKey;
                using (TextReader keyReader = new StringReader(File.ReadAllText(keyPath)))
                {
                    PemReader pemReader = new PemReader(keyReader);
                    var keyObject = pemReader.ReadObject();

                    // Verificar si es un par de claves o solo la clave privada
                    if (keyObject is AsymmetricCipherKeyPair keyPair)
                    {
                        privateKey = keyPair.Private;  // Extraer solo la clave privada
                    }
                    else if (keyObject is AsymmetricKeyParameter keyParam)
                    {
                        privateKey = keyParam;  // Usar directamente si ya es AsymmetricKeyParameter
                    }
                    else
                    {
                        throw new InvalidOperationException("Formato de clave privada no soportado.");
                    }
                }

                // Detectar tipo de clave y combinar adecuadamente
                if (privateKey is RsaPrivateCrtKeyParameters rsaKey)
                {
                    // Manejar claves RSA
                    var rsa = DotNetUtilities.ToRSA(rsaKey);
                    var certWithKey = certificate.CopyWithPrivateKey(rsa);
                    return certWithKey;
                }
                else if (privateKey is ECPrivateKeyParameters ecKey)
                {
                    // Manejar claves EC (Elliptic Curve)
                    var ecParams = new ECPublicKeyParameters("EC", ecKey.Parameters.G.Multiply(ecKey.D), ecKey.Parameters);
                    var ecDsa = ECDsa.Create(new ECParameters
                    {
                        Curve = ECCurve.CreateFromOid(new Oid(ecKey.PublicKeyParamSet.Id)),
                        D = ecKey.D.ToByteArrayUnsigned(),
                        Q = new ECPoint
                        {
                            X = ecParams.Q.AffineXCoord.GetEncoded(),
                            Y = ecParams.Q.AffineYCoord.GetEncoded()
                        }
                    });

                    var certWithKey = certificate.CopyWithPrivateKey(ecDsa);
                    return certWithKey;

                }
                else
                {
                    throw new InvalidOperationException("Tipo de clave privada no soportado.");
                }
            }
            catch (Exception)
            {
                return null;
            }
        }


    }
}