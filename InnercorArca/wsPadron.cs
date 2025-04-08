using InnercorArca.V1.Helpers;
using InnercorArca.V1.ModelsCOM;
using Microsoft.Win32;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using static InnercorArca.V1.Helpers.InnercorArcaModels;

namespace InnercorArca.V1
{
    /// <summary>
    /// Interfaz para exponer métodos y propiedades en COM.
    /// </summary>
    [Guid("11111111-2222-3333-4444-666666555555")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IIwsPadron
    {
        [DispId(1)]
        bool Login(string pathCRT, string pathKey);

        [DispId(2)]
        bool Consultar(string nCuit, ref object oContrib);
        [DispId(3)]
        object GetContribuyente();
        [DispId(4)]
        object GetDomicilio();
        [DispId(5)]
        string GetVersion();
        [DispId(6)]
        int ErrorCode { get; }
        [DispId(7)]
        string ErrorDesc { get; }
        [DispId(8)]
        string XmlResponse { get; }
        [DispId(9)]
        string Excepcion { get; }
        [DispId(10)]
        string TraceBack { get; }
        [DispId(11)]
        bool ModoProduccion { get; set; }
        [DispId(12)]
        string Cuit { get; set; }
        [DispId(13)]
        bool HabilitaLog { get; set; }
    }

    [Guid("66666666-7777-8888-9999-666666000000")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class wsPadron : IIwsPadron
    {
        #region [VAriables Publicas]
        readonly string dllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        readonly string service = GlobalSettings.ServiceARCA.ws_sr_constancia_inscripcion.ToString();
        public bool HabilitaLog { get; set; } = false;

        public int ErrorCode { get; private set; }
        public string ErrorDesc { get; private set; } = string.Empty;
        public bool ModoProduccion { get; set; } = false;

        public string Cuit { get; set; } = string.Empty;
        public string XmlResponse { get; private set; } = string.Empty;
        public string Excepcion { get; private set; } = string.Empty;
        public string TraceBack { get; private set; } = string.Empty;

        #endregion

        #region[Declaración Variables Internas]
        internal bool Produccion { get; set; } = false;
        internal string PathCache { get; private set; } = string.Empty;
         
        // Variable estática para que persista mientras la DLL esté en uso
        private static CacheResult TkValido;
        internal ContribuyenteCOM Contribuyente { get; private set; }

        #endregion

        public wsPadron()
        {
            // Si la variable es null, inicializarla
            if (TkValido == null)
            {
                TkValido = new CacheResult();
            }
            SetError(0, "", "");

        }

        #region [Metodos ARCA]
        public bool Login(string pathCRT, string pathKey)
        {
            try
            {

                if (HabilitaLog) HelpersLogger.Escribir($"Login Verion {GetVersion()}");

                string urlWSAA = string.Empty;
                //Definir si variable de produccion es true o false segun la url del login
                Produccion = ModoProduccion;
                urlWSAA = Produccion ? GlobalSettings.urlWSAAProd : GlobalSettings.urlWSAAHomo;


                // Asegura que el protocolo TLS 1.2 se use siempre
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                if (!Produccion) ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                // Definir la ruta del archivo .cache
                PathCache = Path.Combine(dllPath, Path.GetFileName(pathCRT).Replace(".crt", ".cache"));
                if (HabilitaLog) HelpersLogger.Escribir($"Login PathCache {PathCache}");

                // Crear el cliente del servicio - revisar en el archivo .cache si el token es válido y no expiró
                if (File.Exists(PathCache))
                {
                    //string cache = HelpersArca.LeerCache(PathCache, service);
                    string cache = HelpersCache.LeerBloqueServicio(PathCache, service);

                    if (!string.IsNullOrEmpty(cache))
                    {
                        if (HabilitaLog) HelpersLogger.Escribir($"Login Cache {cache}");
                        // Verificar si el token es válido
                        if (HelpersCache.ValidarToken(cache))
                        {
                            if (HabilitaLog) HelpersLogger.Escribir($"Login Token Válido {cache}");
                            TkValido = HelpersCache.RecuperarTokenSign(cache);


                            return true;
                        }
                    }
                }
                // Cargar el certificado y la clave privada directamente (sin usar .pfx)
                X509Certificate2 certificate = HelpersCert.LoadCertificateAndPrivateKey(pathCRT, pathKey);
                if (certificate == null)
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"Login Error al cargar el certificado y la clave privada {pathCRT} {pathKey}");
                    SetError(GlobalSettings.Errors.CERT_ERROR, "No se pudo cargar el certificado y la clave privada.", "Login 1");
                    return false;
                }

                if (!certificate.HasPrivateKey)
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"Login El certificado no contiene clave privada {pathCRT} {pathKey}");
                    SetError(GlobalSettings.Errors.CERT_ERROR, "El certificado no contiene clave privada.", "Login 2");
                    return false;
                }

                LoginTicket objTicketRespuesta = new LoginTicket();
                string response = objTicketRespuesta.ObtenerLoginTicketResponse(service, urlWSAA, pathCRT, pathKey, true, Produccion);

                if (string.IsNullOrEmpty(response))
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"No se pudo obtener el CMS. Login 3");
                    SetError(GlobalSettings.Errors.WSAA_ERROR, "No se pudo obtener el CMS.", "Login 3");
                    return false;
                }

                // Guardar el CMS en un archivo .cache
                TkValido = HelpersCache.GuardarBloque(PathCache, response, service);

                if (HabilitaLog) HelpersLogger.Escribir($"Login Token Válido ");
                return true;
            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"Error Exception {ex.Message} {TraceBack} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, ex.StackTrace);
                return false;
            }
        }

        public bool Consultar(string nCuit, ref object oContrib)
        {
            try
            {
                // obtiene token y sign del archivo cache
                if (TkValido == null)
                    TkValido = HelpersCache.RecuperarTokenSign(HelpersCache.LeerBloqueServicio(PathCache, service));

                dynamic response = null;
                bool success = false;

                if (nCuit.Length > 11)
                {
                    return success;
                }

                if (nCuit.Length != 11)
                {
                    string cuitM = HelpersCUIT.GenerarCUIT(Convert.ToInt64(nCuit), true, "M");
                    string cuitF = HelpersCUIT.GenerarCUIT(Convert.ToInt64(nCuit), true, "F");

                    success = TryGetPersona(cuitM, ref response) || TryGetPersona(cuitF, ref response);
                }
                else
                {
                    success = TryGetPersona(nCuit, ref response);
                }

                if (!success)
                {
                    return false;
                }


                oContrib = HelpersPadron.MapToContribuyenteCOM(response);
                Contribuyente = (ContribuyenteCOM)oContrib;

                return true;
            }
            catch (Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, ex.StackTrace);
                return false;
            }
        }

        #endregion

        #region [Métodos Internos ] 
        private bool TryGetPersona(string cuit, ref dynamic response )
        {
            try
            {



                if (Produccion)
                {
                    var client = new Aws.PersonaServiceA5();
                    response = client.getPersona(TkValido.Token, TkValido.Sign, Convert.ToInt64(Cuit), Convert.ToInt64(cuit));
                }
                else
                {
                    //var client = new Awshomo.PersonaServiceA5();
                    //response = client.getPersona(TkValido.Token, TkValido.Sign, Convert.ToInt64(Cuit), Convert.ToInt64(cuit));
                }


                if (response.errorConstancia != null || response.errorMonotributo != null || response.errorRegimenGeneral != null)
                {
                    string errorDesc = "";
                    if (response.errorConstancia != null)
                    {
                        if (response.errorConstancia.apellido.Length > 0)

                        //if  (response.errorConstancia.error[0].Contains("La clave  no registra Apellido y/o Nombre informados"))
                        {
                            response = response.errorConstancia;
                            return true;
                        }
                        else
                        {
                            errorDesc = response.errorConstancia[0].error;
                        }
                    }

                    if (response.errorMonotributo != null) errorDesc += response.errorMonotributo;
                    if (response.errorRegimenGeneral != null) errorDesc += response.errorRegimenGeneral;

                    SetError(GlobalSettings.Errors.GET_ERROR, errorDesc, "Errores Respuesta Consultar");
                    return false;
                }




                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region [Metodos COM]


        public object GetContribuyente()
        {
            try
            {
                return Contribuyente;
            }
            catch (Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, ex.StackTrace);
                return null;
            }

        }
        public object GetDomicilio()
        {
            try
            {
                if (Contribuyente.DomicilioFiscal != null) return Contribuyente.DomicilioFiscal;
                return null;
            }
            catch (Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, ex.StackTrace);
                return null;
            }

        }

        #endregion


        public string GetVersion()
        {
            return "1.1.3";
        }
        #region[Metodos Seteo]
        private void SetError(GlobalSettings.Errors codigoError, string descError, string traceBack)
        {
            ErrorCode = (int)codigoError;

            ErrorDesc = codigoError == 0 ? "" : descError;

            TraceBack = traceBack;
        }
        #endregion


    }
}
