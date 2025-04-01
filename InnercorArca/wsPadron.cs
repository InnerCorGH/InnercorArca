using InnercorArca.V1.Helpers;
using InnercorArca.V1.ModelsCOM;
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
    }

    [Guid("66666666-7777-8888-9999-666666000000")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class wsPadron : IIwsPadron
    {
        #region [VAriables Publicas]
        readonly string dllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        readonly string service = GlobalSettings.ServiceARCA.ws_sr_constancia_inscripcion.ToString();

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

        // Instancia FEAuthRequest correctamente
        internal object feAuthRequest;

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


                string urlWSAA = string.Empty;
                //Definir si variable de produccion es true o false segun la url del login
                Produccion = ModoProduccion;
                urlWSAA = Produccion ? GlobalSettings.urlWSAAProd : GlobalSettings.urlWSAAHomo;


                // Asegura que el protocolo TLS 1.2 se use siempre
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                if (!Produccion) ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                // Definir la ruta del archivo .cache
                PathCache = Path.Combine(dllPath, Path.GetFileName(pathCRT).Replace(".crt", ".cache"));
                // Crear el cliente del servicio - revisar en el archivo .cache si el token es válido y no expiró
                if (File.Exists(PathCache))
                {
                    string cache = HelpersArca.LeerCache(PathCache, service);
                    if (!string.IsNullOrEmpty(cache))
                    {
                        // Verificar si el token es válido
                        if (HelpersArca.ValidarToken(cache))
                        {
                            TkValido = HelpersArca.RecuperarTokenSign(cache);

                            //HelpersArca.SeteaAuthRequest(Produccion, ref feAuthRequest, TkValido, Convert.ToInt64(Cuit));
                            return true;
                        }
                    }
                }
                // Cargar el certificado y la clave privada directamente (sin usar .pfx)
                X509Certificate2 certificate = HelpersCert.LoadCertificateAndPrivateKey(pathCRT, pathKey);
                if (certificate == null)
                {
                    SetError(GlobalSettings.Errors.CERT_ERROR, "No se pudo cargar el certificado y la clave privada.", "Login 1");
                    return false;
                }


                if (!certificate.HasPrivateKey)
                {
                    SetError(GlobalSettings.Errors.CERT_ERROR, "El certificado no contiene clave privada.", "Login 2");
                    return false;
                }


                LoginTicket objTicketRespuesta = new LoginTicket();
                string response = objTicketRespuesta.ObtenerLoginTicketResponse(service, urlWSAA, pathCRT, pathKey, true, Produccion);

                if (string.IsNullOrEmpty(response))
                {
                    SetError(GlobalSettings.Errors.WSAA_ERROR, "No se pudo obtener el CMS.", "Login 3");
                    return false;
                }

                // Guardar el CMS en un archivo .cache
                TkValido = HelpersArca.GenerarCache(PathCache, response, service);
                //if (TkValido != null)
                //{
                //    HelpersArca.SeteaAuthRequest(Produccion, ref feAuthRequest, TkValido, Convert.ToInt64(Cuit));

                //    return true;
                //}
                return true;
            }
            catch (Exception ex)
            {
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
                    TkValido = HelpersArca.RecuperarTokenSign(HelpersArca.LeerCache(PathCache, service));

                dynamic response = null;
                bool success = false;

                if (nCuit.Length != 11)
                {
                    string cuitM = HelpersCUIT.GenerarCUIT(Convert.ToInt64(nCuit), true, "M");
                    string cuitF = HelpersCUIT.GenerarCUIT(Convert.ToInt64(nCuit), true, "F");

                    success = TryGetPersona(cuitM, ref response, service) || TryGetPersona(cuitF, ref response, service);
                }
                else
                {
                    success = TryGetPersona(nCuit, ref response, service);


                }

                if (!success)
                {

                    return false;
                }


                oContrib = HelpersPadron.MapToContribuyenteCOMAsync(response, service);
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
        private bool TryGetPersona(string cuit, ref dynamic response, string service)
        {
            try
            {


                if (service == GlobalSettings.ServiceARCA.ws_sr_padron_a5.ToString() || service == GlobalSettings.ServiceARCA.ws_sr_constancia_inscripcion.ToString())
                {
                    if (Produccion)
                    {
                        var client = new Aws.PersonaServiceA5();
                        response = client.getPersona(TkValido.Token, TkValido.Sign, Convert.ToInt64(Cuit), Convert.ToInt64(cuit));
                    }
                    else
                    {
                        var client = new Awshomo.PersonaServiceA5();
                        response = client.getPersona(TkValido.Token, TkValido.Sign, Convert.ToInt64(Cuit), Convert.ToInt64(cuit));
                    }


                    if (response.errorConstancia != null || response.errorMonotributo != null || response.errorRegimenGeneral != null)
                    {
                        string errorDesc = "";
                        if (response.errorConstancia != null)
                            errorDesc = response.errorConstancia.error[0];
                        if (response.errorMonotributo != null) errorDesc += response.errorMonotributo;
                        if (response.errorRegimenGeneral != null) errorDesc += response.errorRegimenGeneral;

                        SetError(GlobalSettings.Errors.GET_ERROR, errorDesc, "Errores Respuesta Consultar");
                        return false;
                    }



                }
                else
                {
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
                return Contribuyente.DomicilioFiscal;
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
