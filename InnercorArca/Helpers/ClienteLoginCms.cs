
using System;
using System.IO;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

/// <summary> 
/// Clase para crear objetos Login Tickets 
/// </summary> 
/// <remarks> 
/// Ver documentacion: 
/// Especificacion Tecnica del Webservice de Autenticacion y Autorizacion 
/// Version 1.0 
/// Departamento de Seguridad Informatica - AFIP 
/// </remarks> 
/// namespace 
/// 

namespace InnercorArca.V1.Helpers
{
    class LoginTicket
    {
        // Entero de 32 bits sin signo que identifica el requerimiento 
        public UInt32 UniqueId;
        // Momento en que fue generado el requerimiento 
        public DateTime GenerationTime;
        // Momento en el que exoira la solicitud 
        public DateTime ExpirationTime;
        // Identificacion del WSN para el cual se solicita el TA 
        public string Service;
        // Firma de seguridad recibida en la respuesta 
        public string Sign;
        // Token de seguridad recibido en la respuesta 
        public string Token;

        public XmlDocument XmlLoginTicketRequest = null;
        public XmlDocument XmlLoginTicketResponse = null;
        public string RutaDelCertificadoFirmante;
        public string RutaDelKey;
        public string XmlStrLoginTicketRequestTemplate = "<loginTicketRequest><header><uniqueId></uniqueId><generationTime></generationTime><expirationTime></expirationTime></header><service></service></loginTicketRequest>";

        private bool _verboseMode = true;

        // OJO! NO ES THREAD-SAFE 
        private static UInt32 _globalUniqueID = 0;

        /// <summary> 
        /// Construye un Login Ticket obtenido del WSAA 
        /// </summary> 
        /// <param name="argServicio">Servicio al que se desea acceder</param> 
        /// <param name="argUrlWsaa">URL del WSAA</param> 
        /// <param name="argRutaCertX509Firmante">Ruta del certificado X509 (con clave privada) usado para firmar</param> 
        /// <param name="argVerbose">Nivel detallado de descripcion? true/false</param> 
        ///<param name="clavePFX">Clave para cifrar el PFX</param> 
        /// <remarks></remarks> 
        public string ObtenerLoginTicketResponse(string argServicio, string argUrlWsaa, string argRutaCertX509Firmante, bool argVerbose, string clavePFX)
        {

            this.RutaDelCertificadoFirmante = argRutaCertX509Firmante;
            this._verboseMode = argVerbose;
            CertificadosX509Lib.VerboseMode = argVerbose;

            string cmsFirmadoBase64;
            string loginTicketResponse;

            XmlNode xmlNodoUniqueId;
            XmlNode xmlNodoGenerationTime;
            XmlNode xmlNodoExpirationTime;
            XmlNode xmlNodoService;

            // PASO 1: Genero el Login Ticket Request 
            try
            {
                XmlLoginTicketRequest = new XmlDocument();
                XmlLoginTicketRequest.LoadXml(XmlStrLoginTicketRequestTemplate);

                xmlNodoUniqueId = XmlLoginTicketRequest.SelectSingleNode("//uniqueId");
                xmlNodoGenerationTime = XmlLoginTicketRequest.SelectSingleNode("//generationTime");
                xmlNodoExpirationTime = XmlLoginTicketRequest.SelectSingleNode("//expirationTime");
                xmlNodoService = XmlLoginTicketRequest.SelectSingleNode("//service");

                xmlNodoGenerationTime.InnerText = DateTime.Now.AddMinutes(-10).ToString("s");
                xmlNodoExpirationTime.InnerText = DateTime.Now.AddMinutes(+10).ToString("s");
                xmlNodoUniqueId.InnerText = Convert.ToString(_globalUniqueID);
                xmlNodoService.InnerText = argServicio;
                this.Service = argServicio;

                _globalUniqueID += 1;

                if (this._verboseMode)
                {
                    HelpersLogger.Escribir("***LoginTicketRequest: " + XmlLoginTicketRequest.OuterXml);
                }
            }

            catch (Exception excepcionAlGenerarLoginTicketRequest)
            {
                if (this._verboseMode) HelpersLogger.Escribir("Error al generar el LoginTicketRequest: " + excepcionAlGenerarLoginTicketRequest.Message);
                throw new Exception("***Error GENERANDO el LoginTicketRequest : " + excepcionAlGenerarLoginTicketRequest.Message);
            }

            // PASO 2: Firmo el Login Ticket Request 
            try
            {
                if (this._verboseMode)
                {
                    HelpersLogger.Escribir("***Leyendo certificado: " + RutaDelCertificadoFirmante);
                }

                X509Certificate2 certFirmante = CertificadosX509Lib.ObtieneCertificadoDesdeArchivo(RutaDelCertificadoFirmante, clavePFX);

                if (this._verboseMode)
                {
                    HelpersLogger.Escribir("***Firmando: " + XmlLoginTicketRequest.OuterXml);
                }

                // Convierto el login ticket request a bytes, para firmar 
                Encoding EncodedMsg = Encoding.UTF8;
                byte[] msgBytes = EncodedMsg.GetBytes(XmlLoginTicketRequest.OuterXml);

                // Firmo el msg y paso a Base64 
                byte[] encodedSignedCms = CertificadosX509Lib.FirmaBytesMensaje(msgBytes, certFirmante);
                cmsFirmadoBase64 = Convert.ToBase64String(encodedSignedCms);
            }

            catch (Exception excepcionAlFirmar)
            {
                if (this._verboseMode) HelpersLogger.Escribir("Error al firmar el LoginTicketRequest: " + excepcionAlFirmar.Message);
                throw new Exception("***Error FIRMANDO el LoginTicketRequest : " + excepcionAlFirmar.Message);
            }

            // PASO 3: Invoco al WSAA para obtener el Login Ticket Response 
            try
            {
                if (this._verboseMode)
                {
                    HelpersLogger.Escribir("***Llamando al WSAA en URL: " + argUrlWsaa);
                }

                InnercorArca.V1.Wsaa.LoginCMSService servicioWsaa = new InnercorArca.V1.Wsaa.LoginCMSService
                {
                    Url = argUrlWsaa
                };

                loginTicketResponse = servicioWsaa.loginCms(cmsFirmadoBase64);

                if (this._verboseMode)
                {
                    HelpersLogger.Escribir("***Argumento en el request: " + cmsFirmadoBase64);
                }
            }

            catch (Exception excepcionAlInvocarWsaa)
            {
                if (this._verboseMode) HelpersLogger.Escribir("Error al invocar el servicio WSAA: " + excepcionAlInvocarWsaa.Message);
                throw new Exception("***Error INVOCANDO al servicio WSAA : " + excepcionAlInvocarWsaa.Message);
            }


            // PASO 4: Analizo el Login Ticket Response recibido del WSAA 
            try
            {
                XmlLoginTicketResponse = new XmlDocument();
                XmlLoginTicketResponse.LoadXml(loginTicketResponse);

                this.UniqueId = UInt32.Parse(XmlLoginTicketResponse.SelectSingleNode("//uniqueId").InnerText);
                this.GenerationTime = DateTime.Parse(XmlLoginTicketResponse.SelectSingleNode("//generationTime").InnerText);
                this.ExpirationTime = DateTime.Parse(XmlLoginTicketResponse.SelectSingleNode("//expirationTime").InnerText);
                this.Sign = XmlLoginTicketResponse.SelectSingleNode("//sign").InnerText;
                this.Token = XmlLoginTicketResponse.SelectSingleNode("//token").InnerText;
            }
            catch (Exception excepcionAlAnalizarLoginTicketResponse)
            {
                if (this._verboseMode) HelpersLogger.Escribir("Error al analizar el LoginTicketResponse: " + excepcionAlAnalizarLoginTicketResponse.Message);
                throw new Exception("***Error ANALIZANDO el LoginTicketResponse : " + excepcionAlAnalizarLoginTicketResponse.Message);
            }

            return loginTicketResponse;

        }

        /// <summary> 
        /// Construye un Login Ticket obtenido del WSAA 
        /// </summary> 
        /// <param name="argServicio">Servicio al que se desea acceder</param> 
        /// <param name="argUrlWsaa">URL del WSAA</param> 
        /// <param name="argRutaCertX509Firmante">Ruta del certificado X509 (con clave privada) usado para firmar</param> 
        /// <param name="argRutaKey">Ruta de la clave privada</param>
        /// <param name="argVerbose">Nivel detallado de descripcion? true/false</param> 
        /// <remarks></remarks> 
        public string ObtenerLoginTicketResponse(string argServicio, string argUrlWsaa, string argRutaCertX509Firmante, string argRutaKey, bool argVerbose, bool produccion)
        {

            this.RutaDelCertificadoFirmante = argRutaCertX509Firmante;
            this.RutaDelKey = argRutaKey;
            this._verboseMode = argVerbose;
            CertificadosX509Lib.VerboseMode = argVerbose;

            string cmsFirmadoBase64;
            string loginTicketResponse;

            XmlNode xmlNodoUniqueId;
            XmlNode xmlNodoGenerationTime;
            XmlNode xmlNodoExpirationTime;
            XmlNode xmlNodoService;

            // PASO 1: Genero el Login Ticket Request 
            try
            {
                XmlLoginTicketRequest = new XmlDocument();
                XmlLoginTicketRequest.LoadXml(XmlStrLoginTicketRequestTemplate);

                xmlNodoUniqueId = XmlLoginTicketRequest.SelectSingleNode("//uniqueId");
                xmlNodoGenerationTime = XmlLoginTicketRequest.SelectSingleNode("//generationTime");
                xmlNodoExpirationTime = XmlLoginTicketRequest.SelectSingleNode("//expirationTime");
                xmlNodoService = XmlLoginTicketRequest.SelectSingleNode("//service");

                xmlNodoGenerationTime.InnerText = DateTime.Now.AddMinutes(-5).ToString("s");
                xmlNodoExpirationTime.InnerText = DateTime.Now.AddMinutes(+5).ToString("s");
                xmlNodoUniqueId.InnerText = Convert.ToString(_globalUniqueID);
                xmlNodoService.InnerText = argServicio;
                this.Service = argServicio;

                _globalUniqueID += 1;

                if (this._verboseMode)
                {
                    HelpersLogger.Escribir("LoginTicketRequest: " + XmlLoginTicketRequest.OuterXml);
                }
            }

            catch (Exception excepcionAlGenerarLoginTicketRequest)
            {
                if (this._verboseMode) HelpersLogger.Escribir("Error al generar el LoginTicketRequest: " + excepcionAlGenerarLoginTicketRequest.Message);
                throw new Exception("***Error GENERANDO el LoginTicketRequest : " + excepcionAlGenerarLoginTicketRequest.Message);
            }

            // PASO 2: Firmo el Login Ticket Request 
            try
            {
                if (this._verboseMode)
                {
                    HelpersLogger.Escribir("Leyendo certificado: " + RutaDelCertificadoFirmante);
                }

                X509Certificate2 certFirmante = HelpersCert.ObtieneCertificadoDesdeArchivos(RutaDelCertificadoFirmante, RutaDelKey);

                if (this._verboseMode)
                {
                    HelpersLogger.Escribir("Firmando: " + XmlLoginTicketRequest.OuterXml);
                }

                // Convierto el login ticket request a bytes, para firmar 
                Encoding EncodedMsg = Encoding.UTF8;
                byte[] msgBytes = EncodedMsg.GetBytes(XmlLoginTicketRequest.OuterXml);

                // Firmo el msg y paso a Base64 
                byte[] encodedSignedCms = CertificadosX509Lib.FirmaBytesMensaje(msgBytes, certFirmante);
                cmsFirmadoBase64 = Convert.ToBase64String(encodedSignedCms);
            }

            catch (Exception excepcionAlFirmar)
            {
                if (this._verboseMode) HelpersLogger.Escribir("Error al firmar el LoginTicketRequest: " + excepcionAlFirmar.Message);
                throw new Exception("***Error FIRMANDO el LoginTicketRequest : " + excepcionAlFirmar.Message);
            }

            // PASO 3: Invoco al WSAA para obtener el Login Ticket Response 
            try
            {
                if (this._verboseMode)
                {
                    HelpersLogger.Escribir("Llamando al WSAA en URL: " + argUrlWsaa);
                    HelpersLogger.Escribir("Argumento en el request: " + cmsFirmadoBase64);
                }
                if (produccion)
                {
                    var servicioWsaa = new Wsaa.LoginCMSService
                    {
                        Url = argUrlWsaa
                    };
                    loginTicketResponse = servicioWsaa.loginCms(cmsFirmadoBase64);
                }
                else
                {
                    var servicioWsaa = new Wsaahomo.LoginCMSService
                    {
                        Url = argUrlWsaa
                    };
                    loginTicketResponse = servicioWsaa.loginCms(cmsFirmadoBase64);
                }

                if (this._verboseMode)
                {
                    HelpersLogger.Escribir("LoguinTicketResponse: " + loginTicketResponse);
                }
            }

            catch (Exception excepcionAlInvocarWsaa)
            {
                if (this._verboseMode) HelpersLogger.Escribir("Error al invocar el servicio WSAA: " + excepcionAlInvocarWsaa.Message);
                throw new Exception("***Error INVOCANDO al servicio WSAA : " + excepcionAlInvocarWsaa.Message);
            }


            // PASO 4: Analizo el Login Ticket Response recibido del WSAA 
            try
            {
                XmlLoginTicketResponse = new XmlDocument();
                XmlLoginTicketResponse.LoadXml(loginTicketResponse);

                this.UniqueId = UInt32.Parse(XmlLoginTicketResponse.SelectSingleNode("//uniqueId").InnerText);
                this.GenerationTime = DateTime.Parse(XmlLoginTicketResponse.SelectSingleNode("//generationTime").InnerText);
                this.ExpirationTime = DateTime.Parse(XmlLoginTicketResponse.SelectSingleNode("//expirationTime").InnerText);
                this.Sign = XmlLoginTicketResponse.SelectSingleNode("//sign").InnerText;
                this.Token = XmlLoginTicketResponse.SelectSingleNode("//token").InnerText;
            }
            catch (Exception excepcionAlAnalizarLoginTicketResponse)
            {
                if (this._verboseMode) HelpersLogger.Escribir("Error al analizar el LoginTicketResponse: " + excepcionAlAnalizarLoginTicketResponse.Message);
                throw new Exception("***Error ANALIZANDO el LoginTicketResponse : " + excepcionAlAnalizarLoginTicketResponse.Message);
            }

            return loginTicketResponse;

        }


    }

    /// <summary> 
    /// Libreria de utilidades para manejo de certificados 
    /// </summary> 
    /// <remarks></remarks> 
    class CertificadosX509Lib
    {

        public static bool VerboseMode = false;

        /// <summary> 
        /// Firma mensaje 
        /// </summary> 
        /// <param name="argBytesMsg">Bytes del mensaje</param> 
        /// <param name="argCertFirmante">Certificado usado para firmar</param> 
        /// <returns>Bytes del mensaje firmado</returns> 
        /// <remarks></remarks> 
        public static byte[] FirmaBytesMensaje(byte[] argBytesMsg, X509Certificate2 argCertFirmante)
        {
            try
            {
                // Pongo el mensaje en un objeto ContentInfo (requerido para construir el obj SignedCms) 
                ContentInfo infoContenido = new ContentInfo(argBytesMsg);
                SignedCms cmsFirmado = new SignedCms(infoContenido);

                // Creo objeto CmsSigner que tiene las caracteristicas del firmante 
                CmsSigner cmsFirmante = new CmsSigner(argCertFirmante)
                {
                    IncludeOption = X509IncludeOption.EndCertOnly
                };

                if (VerboseMode)
                {
                    //Console.WriteLine("***Firmando bytes del mensaje...");
                    HelpersLogger.Escribir("***Firmando bytes del mensaje...");
                }
                // Firmo el mensaje PKCS #7 
                cmsFirmado.ComputeSignature(cmsFirmante);

                if (VerboseMode)
                {
                    //Console.WriteLine("***OK mensaje firmado");
                    HelpersLogger.Escribir("***OK mensaje firmado");
                }

                // Encodeo el mensaje PKCS #7. 
                return cmsFirmado.Encode();
            }
            catch (Exception excepcionAlFirmar)
            {
                if (VerboseMode)
                {
                    HelpersLogger.Escribir("***Error al firmar: " + excepcionAlFirmar.Message);
                }
                throw new Exception("***Error al firmar: " + excepcionAlFirmar.Message);
            }
        }

        /// <summary> 
        /// Lee certificado de disco 
        /// </summary> 
        /// <param name="argArchivo">Ruta del certificado a leer.</param> 
        /// <returns>Un objeto certificado X509</returns> 
        /// <remarks></remarks> 
        public static X509Certificate2 ObtieneCertificadoDesdeArchivo(string argArchivo, string argPass)
        {
            X509Certificate2 objCert = new X509Certificate2();

            try
            {
                objCert.Import(File.ReadAllBytes(argArchivo), argPass, X509KeyStorageFlags.DefaultKeySet);
                return objCert;
            }
            catch (Exception excepcionAlImportarCertificado)
            {
                if (VerboseMode)
                {
                    HelpersLogger.Escribir("Error al importar el certificado: " + excepcionAlImportarCertificado.Message);
                }
                throw new Exception("argArchivo=" + argArchivo + " excepcion=" + excepcionAlImportarCertificado.Message + " " + excepcionAlImportarCertificado.StackTrace);

            }
        }

        /// <summary> 
        /// Lee certificado de disco 
        /// </summary> 
        /// <param name="argArchivo">Ruta del certificado a leer.</param> 
        /// <returns>Un objeto certificado X509</returns> 
        /// <remarks></remarks> 
        public static X509Certificate2 ObtieneCertificadoDesdeArchivo(string argArchivo)
        {
            X509Certificate2 objCert = new X509Certificate2();

            try
            {

                objCert.Import(File.ReadAllBytes(argArchivo), "", X509KeyStorageFlags.DefaultKeySet);
                return objCert;
            }
            catch (Exception excepcionAlImportarCertificado)
            {
                if (VerboseMode)
                {
                    HelpersLogger.Escribir("Error al importar el certificado: " + excepcionAlImportarCertificado.Message);
                }
                throw new Exception("argArchivo=" + argArchivo + " excepcion=" + excepcionAlImportarCertificado.Message + " " + excepcionAlImportarCertificado.StackTrace);

            }
        }
    }

}