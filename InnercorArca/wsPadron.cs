using InnercorArca.V1.Helpers;
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
    }

    [Guid("66666666-7777-8888-9999-666666000000")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class wsPadron : IIwsPadron
    {
        #region [VAriables Publicas]
        readonly string dllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        readonly  string service = GlobalSettings.ServiceARCA.ws_sr_constancia_inscripcion.ToString();
        public int ErrorCode { get; private set; }
        public string ErrorDesc { get; private set; } = string.Empty; 
        public bool ModoProduccion { get; set; } = false;

        public string Cuit { get; set; } = string.Empty;
        public string XmlResponse { get; private set; } = string.Empty;
        public string Excepcion { get; private set; } = string.Empty;
        public string TraceBack { get; private set; } = string.Empty;

        public dynamic Contribuyente { get; private set; }

        #endregion

        #region[Declaración Variables Internas]
        internal bool Produccion { get; private set; } = false;
        internal string PathCache { get; private set; } = string.Empty;

        // Instancia FEAuthRequest correctamente
        internal object feAuthRequest;

        // Variable estática para que persista mientras la DLL esté en uso
        private static CacheResult TkValido;
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
                            TkValido = HelpersArca.RecuperarTokenSign(cache );

                            HelpersArca.SeteaAuthRequest(Produccion, ref feAuthRequest, TkValido, Convert.ToInt64(Cuit));
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
                if (TkValido != null)
                {
                    HelpersArca.SeteaAuthRequest(Produccion, ref feAuthRequest, TkValido, Convert.ToInt64(Cuit));

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, ex.StackTrace);
                return false;
            }
        }

        //OContrib
        // feafip.contribuyente
        //     
        public bool Consultar(string nCuit, ref object oContrib)
        {
            try
            {
                ////obtiene token y sign del archivo cache
                if (TkValido == null)
                    TkValido = HelpersArca.RecuperarTokenSign(HelpersArca.LeerCache(PathCache, service));
                dynamic response;

                if (Produccion)
                {
                    var client = new Aws.PersonaServiceA5();
                    response = client.getPersona(TkValido.Token, TkValido.Sign, Convert.ToInt64(Cuit), Convert.ToInt64( nCuit));
                }
                else
                {
                    var client = new Awshomo.PersonaServiceA5();
                    response = client.getPersona(TkValido.Token, TkValido.Sign, Convert.ToInt64(Cuit), Convert.ToInt64(nCuit));

                }

                // Verificar errores en la respuesta
                //if (response.Errors != null && response.Errors.Length > 0)
                //{
                //    int errCode = 0; string errDesc = ""; string xmlResponse = "";
                //    HelpersArca.ProcesarRespuesta(response, ref errCode, ref errDesc, ref xmlResponse);
                //    SetError((Errors)errCode, errDesc, "Errores Respuesta FECompConsultar");
                //    XmlResponse = xmlResponse;
                //    return false;
                //}

                // Log the raw XML response
                //Console.WriteLine(response.datosGenerales.ToString());

                // Deserializar response.persona a un objeto dinámico
                oContrib = response.datosGenerales;
                Contribuyente = oContrib;

                return true;
            }
            catch ( Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, ex.StackTrace);
                return false;
            }
        }



        private void SetError(GlobalSettings.Errors codigoError, string descError, string traceBack)
        {
            ErrorCode = (int)codigoError;

            ErrorDesc = codigoError == 0 ? "" : descError;

            TraceBack = traceBack;
        }
    }
}
