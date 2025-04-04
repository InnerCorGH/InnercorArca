using InnercorArca.V1.Helpers;
using System;
using System.IO;
using System.Linq;
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
    [Guid("11111111-2222-3333-4444-555555555555")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IIwsfev1
    {
        [DispId(1)]
        bool Login(string pathCRT, string pathKey, string urlWSAA);

        [DispId(2)]
        int ErrorCode { get; }

        [DispId(3)]
        string ErrorDesc { get; }

        [DispId(4)]
        string Cuit { get; set; }

        [DispId(5)]
        string URL { get; set; }

        [DispId(6)]
        string XmlResponse { get; }
        [DispId(7)]
        string Excepcion { get; }
        [DispId(8)]
        string TraceBack { get; }
        [DispId(9)]
        bool Dummy(ref string cAppServerStatus, ref string cDbServerStatus, ref string cAuthServerStatus);
        [DispId(10)]
        void Reset();
        [DispId(11)]
        bool RecuperaLastCMP(int nPtoVta, int nTipCom, ref int nUltNro);
        [DispId(12)]
        bool CmpConsultar(int nTipCom, int nPtoVta, long nNroCmp, ref string cNroCAE, ref string cVtoCAE);
        [DispId(13)]
        void AgregaFactura(int nConcep, int nTipDoc, long nNroDoc, long nNroDes, long nNroHas, string cFchCom, double nImpTot, double nImpCon, double nImpNet, double nImpOpc, string cSerDes, string cSerHas, string cSerVto, string cMoneda, double nCotiza, int nCondIvaRec);
        [DispId(14)]
        void AgregaIVA(int codigoAlicuota, double importeBase, double importeIVA);

        [DispId(15)]
        void AgregaOpcional(string codigo, string valor);
        [DispId(16)]
        void AgregaTributo(short codimp, string descri, double impbase, double alicuo, double import);

        [DispId(17)]
        void AgregaCompAsoc(int nTipCmp, int nPtoVta, int nNroCmp, Int64 nNroCuit, string dFchCmp);

        [DispId(18)]
        bool Autorizar(int nPtoVta, int nTipCom);
        [DispId(19)]
        void AutorizarRespuesta(int nIndice, out string cNroCAE, ref string cVtoCAE, ref string cResult, ref string cReproc);
        [DispId(20)]
        string AutorizarRespuestaObs(int nIndice);

        [DispId(21)]
        string GetVersion();

        [DispId(22)]
        string GetAppServerStatus();

        [DispId(23)]
        string GetDbServerStatus();
        [DispId(24)]
        string GetAuthServerStatus();
        [DispId(25)]
        int GetUltimoNumero();
        [DispId(26)]
        string GetNumeroCAE();
        [DispId(27)]
        string GetVencimientoCAE();
        [DispId(28)]
        string GetResultado();
        [DispId(29)]
        string GetReprocesar();

        [DispId(30)]
        bool CAEAConsultar(int nPeriod, short nQuince, ref string cNroCAE, ref string dFchDes, ref string dFchHas, ref string dFchTop, ref string dFchPro);
        [DispId(31)]
        bool HabilitaLog { get; set; }
        [DispId(32)]
        string CbteFchGen { get; set; }

    }

    /// <summary>
    /// Implementación de wsfev1, expuesta como COM.
    /// </summary>
    [Guid("66666666-7777-8888-9999-000000000000")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class wsfev1 : IIwsfev1
    {
        #region [Declaración Variables Públicas]
        readonly string dllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        readonly string service = GlobalSettings.ServiceARCA.wsfe.ToString();
        public bool HabilitaLog { get; set; } = false;
        public int ErrorCode { get; private set; }
        public string ErrorDesc { get; private set; } = string.Empty;
        public string Cuit { get; set; } = string.Empty;
        public string URL { get; set; } = string.Empty;
        public string XmlResponse { get; private set; } = string.Empty;
        public string Excepcion { get; private set; } = string.Empty;
        public string TraceBack { get; private set; } = string.Empty;
        //variables de paso Referencia que no los devuelve
        public int UltimoNumero { get; set; }

        public string Observaciones { get; set; }

        public string CbteFchGen { get; set; } = string.Empty; // Fecha de generación del comprobante (en formato "yyyyMMdd"). Se usa para la consulta de CAEA y se devuelve en la respuesta de CAEA.
        #endregion

        #region[Declaración Variables Internas]
        internal bool Produccion { get; private set; } = false;
        internal string PathCache { get; private set; } = string.Empty;

        internal string NumeroCAE { get; set; }
        internal string VencimientoCAE { get; set; }

        internal string Result { get; set; }
        internal string Reproc { get; set; }

        internal string FechaDesde { get; set; }
        internal string FechaHasta { get; set; }
        internal string FechaTope { get; set; }
        internal string FechaProceso { get; set; }

        internal InnercorArcaModels.CAEDetRequest CAEDetRequest { get; set; }
        internal InnercorArcaModels.InnCAEADetRequest InnCAEADetReq { get; set; }

        internal double Neto { get; set; }
        internal double Iva { get; set; }

        // Instancia FEAuthRequest correctamente
        internal object feAuthRequest;

        // Variable estática para que persista mientras la DLL esté en uso
        private static CacheResult TkValido;


        // Variables para almacenar los estados de los servidores
        internal string AppServerStatus_ { get; private set; } = string.Empty;
        internal string DbServerStatus_ { get; private set; } = string.Empty;
        internal string AuthServerStatus_ { get; private set; } = string.Empty;
        #endregion

        public wsfev1()
        {
            // Si la variable es null, inicializarla
            if (TkValido == null)
            {
                TkValido = new CacheResult();
            }
            SetError(0, "", "");

        }


        #region [METODOS Consulta ARCA]
        public bool Login(string pathCRT, string pathKey, string urlWSAA)
        {
            try
            {
                if (HabilitaLog) HelpersLogger.Escribir($"Login Versión {GetVersion()}");
                //Definir si variable de produccion es true o false segun la url del login
                Produccion = !(urlWSAA.ToUpper().Contains("HOMO"));

                // Asegura que el protocolo TLS 1.2 se use siempre
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                if (!Produccion) ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                if (HabilitaLog) HelpersLogger.Escribir($"Login Versión {Produccion}");

                // Definir la ruta del archivo .cache
                PathCache = Path.Combine(dllPath, Path.GetFileName(pathCRT).Replace(".crt", ".cache"));
                if (HabilitaLog) HelpersLogger.Escribir($"Login PathCache {PathCache}");

                // Crear el cliente del servicio - revisar en el archivo .cache si el token es válido y no expiró
                if (File.Exists(PathCache))
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"Login Cache existe {PathCache} {service} {urlWSAA}");
                    string cache = HelpersCache.LeerBloqueServicio(PathCache, service);
                    if (!string.IsNullOrEmpty(cache))
                    {
                        if (HabilitaLog) HelpersLogger.Escribir($"Login Cache {cache}");
                        // Verificar si el token es válido
                        if (HelpersCache.ValidarToken(cache))
                        {
                            if (HabilitaLog) HelpersLogger.Escribir($"Login Token Válido {cache}");
                            TkValido = HelpersCache.RecuperarTokenSign(cache);

                            HelpersArca.SeteaAuthRequest(Produccion, ref feAuthRequest, TkValido, Convert.ToInt64(Cuit));
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
                string response = objTicketRespuesta.ObtenerLoginTicketResponse("wsfe", urlWSAA, pathCRT, pathKey, true, Produccion);
                if (string.IsNullOrEmpty(response))
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"No se pudo obtener el CMS. Login 3");
                    SetError(GlobalSettings.Errors.WSAA_ERROR, "No se pudo obtener el CMS.", "Login 3");
                    return false;
                }

                // Guardar el CMS en un archivo .cache
                TkValido = HelpersCache.GuardarBloque(PathCache, response, service);
                if (TkValido != null)
                {
                    HelpersArca.SeteaAuthRequest(Produccion, ref feAuthRequest, TkValido, Convert.ToInt64(Cuit));
                    if (HabilitaLog) HelpersLogger.Escribir($"Setea Auth REquest {HelpersGlobal.SerializeToXml(TkValido)}");
                    return true;
                }


                if (HabilitaLog) HelpersLogger.Escribir($"Login Token Válido ");
                return false;
            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"Error Exception {ex.Message} {TraceBack} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, ex.StackTrace);
                return false;
            }
        }

        public bool Dummy(ref string cAppServerStatus, ref string cDbServerStatus, ref string cAuthServerStatus)
        {

            if (HabilitaLog) HelpersLogger.Escribir("Inicio Dummy");
            try
            {
                object objDummy;
                if (Produccion)
                {
                    if (feAuthRequest == null)
                        HelpersArca.SeteaAuthRequest(Produccion, ref feAuthRequest, TkValido, Convert.ToInt64(Cuit));
                    if (HabilitaLog) HelpersLogger.Escribir($"Dummy {feAuthRequest.GetType()}");

                    objDummy = new Wsfev1.DummyResponse();
                    if (HabilitaLog) HelpersLogger.Escribir($"Dummy {objDummy.GetType()}");
                }
                else
                {
                    if (feAuthRequest == null)
                        HelpersArca.SeteaAuthRequest(Produccion, ref feAuthRequest, TkValido, Convert.ToInt64(Cuit));
                    if (HabilitaLog) HelpersLogger.Escribir($"Dummy {feAuthRequest.GetType()}");

                    objDummy = new Wsfev1Homo.DummyResponse();
                    if (HabilitaLog) HelpersLogger.Escribir($"Dummy {objDummy.GetType()}");
                }
                if (objDummy == null)
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"Dummy Error al crear objeto Dummy");
                    SetError(GlobalSettings.Errors.EXCEPTION, "Error al crear objeto Dummy.", "Dummy 1");
                    return false;
                }

                dynamic dummy = objDummy;
                cAppServerStatus = dummy.AppServer ?? "OK";
                cAuthServerStatus = dummy.AuthServer ?? "OK";
                cDbServerStatus = dummy.DbServer ?? "OK";

                AppServerStatus_ = cAppServerStatus;
                AuthServerStatus_ = cAuthServerStatus;
                DbServerStatus_ = cDbServerStatus;

                if (HabilitaLog) HelpersLogger.Escribir($"Dummy {cAppServerStatus} {cAuthServerStatus} {cDbServerStatus}");

                return cAppServerStatus == "OK" && cAuthServerStatus == "OK" && cDbServerStatus == "OK";
            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"Error Exception {ex.Message} {TraceBack} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, ex.StackTrace);
                return false;
            }
        }

        public void Reset()
        {
            try
            {            //Limpia los variables globales

                XmlResponse = string.Empty;
                ErrorCode = 0;
                ErrorDesc = "";
                Excepcion = string.Empty;
                TraceBack = string.Empty;

                Result = string.Empty;

                CAEDetRequest = null;
                InnCAEADetReq = null;
                CbteFchGen = "";

                Neto = 0;
                Iva = 0;
            }
            catch (Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, $"Error al invocar Reset {ex.Message}", ex.StackTrace);
            }
        }

        #endregion

        #region [METODOS Autorización y CONUSLTA  ]

        public bool RecuperaLastCMP(int nPtoVta, int nTipCom, ref int nUltNro)
        {
            if (HabilitaLog) HelpersLogger.Escribir("Inicio RecuperaLastCMP");
            int errCode = 0;
            string errDesc = "";

            try
            {

                //obtiene token y sign del archivo cache
                if (TkValido == null)
                    TkValido = HelpersCache.RecuperarTokenSign(HelpersCache.LeerBloqueServicio(PathCache, service));
                if (HabilitaLog) HelpersLogger.Escribir($"RecuperaLastCMP Token Válido");
                // Instancia el servicio adecuado
                object objWSFEV1;
                string xmlResponse = "";

                if (feAuthRequest == null)
                    HelpersArca.SeteaAuthRequest(Produccion, ref feAuthRequest, TkValido, Convert.ToInt64(Cuit));
                if (HabilitaLog) HelpersLogger.Escribir($"RecuperaLastCMP {feAuthRequest.GetType()}");
                // Realiza el casting correcto según el entorno
                if (Produccion)
                {
                    objWSFEV1 = new Wsfev1.Service();
                    var wsfev1 = (Wsfev1.Service)objWSFEV1;
                    var objFERecuperaLastCbteResponse = wsfev1.FECompUltimoAutorizado((Wsfev1.FEAuthRequest)feAuthRequest, nPtoVta, nTipCom);
                    if (HabilitaLog) HelpersLogger.Escribir($"RecuperaLastCMP {HelpersGlobal.SerializeObjectAXml(objFERecuperaLastCbteResponse)}");

                    HelpersArca.ProcesarRespuesta(HabilitaLog, objFERecuperaLastCbteResponse, ref errCode, ref errDesc, ref xmlResponse);
                    if (errCode == 0) nUltNro = objFERecuperaLastCbteResponse.CbteNro;
                    if (HabilitaLog) HelpersLogger.Escribir($"ErrorCode {errCode} UltNro {nUltNro}");
                }
                else
                {
                    objWSFEV1 = new Wsfev1Homo.Service();
                    var wsfev1 = (Wsfev1Homo.Service)objWSFEV1;
                    var objFERecuperaLastCbteResponse = wsfev1.FECompUltimoAutorizado((Wsfev1Homo.FEAuthRequest)feAuthRequest, nPtoVta, nTipCom);
                    if (HabilitaLog) HelpersLogger.Escribir($"RecuperaLastCMP {HelpersGlobal.SerializeObjectAXml(objFERecuperaLastCbteResponse)}");

                    HelpersArca.ProcesarRespuesta(HabilitaLog, objFERecuperaLastCbteResponse, ref errCode, ref errDesc, ref xmlResponse);
                    if (errCode == 0) nUltNro = objFERecuperaLastCbteResponse.CbteNro;
                    if (HabilitaLog) HelpersLogger.Escribir($"ErrorCode {errCode} UltNro {nUltNro}");
                }


                XmlResponse = xmlResponse;
                UltimoNumero = nUltNro;
                if (HabilitaLog) HelpersLogger.Escribir($"RecuperaLastCMP {nUltNro} {XmlResponse}");

                return true;
            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"Error Exception {ex.Message} {TraceBack} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, ex.Message);
                UltimoNumero = -1;
                return false;
            }
        }

        //Devuelve CAE y FECHA de VTO segun Tipo COmprobante, PtoVta y Nro OCmp
        public bool CmpConsultar(int nTipCom, int nPtoVta, long nNroCmp, ref string cNroCAE, ref string cVtoCAE)
        {
            if (HabilitaLog) HelpersLogger.Escribir("Inicio CmpConsultar");
            try
            {
                // Inicialización de valores de salida
                cNroCAE = string.Empty;
                cVtoCAE = DateTime.MinValue.ToString("yyyyMMdd");
                if (HabilitaLog) HelpersLogger.Escribir($"CmpConsultar {nTipCom} {nPtoVta} {nNroCmp}");
                // Configurar la solicitud con los parámetros recibidos
                object objReq;
                if (Produccion)
                    objReq = new Wsfev1.FECompConsultaReq();
                else
                    objReq = new Wsfev1Homo.FECompConsultaReq();

                if (HabilitaLog) HelpersLogger.Escribir($"CmpConsultar {objReq.GetType()}");
                dynamic req = objReq;
                req.CbteTipo = nTipCom;
                req.PtoVta = nPtoVta;
                req.CbteNro = nNroCmp;

                //obtiene token y sign del archivo cache
                if (TkValido == null)
                    TkValido = HelpersCache.RecuperarTokenSign(HelpersCache.LeerBloqueServicio(PathCache, service));
                if (HabilitaLog) HelpersLogger.Escribir($"CmpConsultar Token");

                // Configurar autenticación
                object auth;
                if (Produccion)
                    auth = new Wsfev1.FEAuthRequest();
                else
                    auth = new Wsfev1Homo.FEAuthRequest();
                if (HabilitaLog) HelpersLogger.Escribir($"CmpConsultar {auth.GetType()}");

                dynamic authData = auth;
                authData.Token = TkValido.Token;
                authData.Sign = TkValido.Sign;
                authData.Cuit = Convert.ToInt64(Cuit); // Reemplazar con el CUIT correcto

                // Ejecutar consulta
                object objWSFEV1;
                if (Produccion)
                {
                    objWSFEV1 = new Wsfev1.Service();
                }
                else
                    objWSFEV1 = new Wsfev1Homo.Service();


                dynamic response = ((dynamic)objWSFEV1).FECompConsultar(authData, req);
                if (HabilitaLog) HelpersLogger.Escribir($"CmpConsultar {HelpersGlobal.SerializeObjectAXml(response)}");

                // Verificar errores en la respuesta
                if (response.Errors != null && response.Errors.Length > 0)
                {
                    int errCode = 0; string errDesc = ""; string xmlResponse = "";
                    HelpersArca.ProcesarRespuesta(HabilitaLog, response, ref errCode, ref errDesc, ref xmlResponse);
                    SetError((GlobalSettings.Errors)errCode, errDesc, "Errores Respuesta FECompConsultar");
                    XmlResponse = xmlResponse;
                    if (HabilitaLog) HelpersLogger.Escribir($"CmpConsultar Error {errDesc} {xmlResponse}");
                    return false;
                }


                // Extraer el CAE y su fecha de vencimiento
                cNroCAE = response.ResultGet.CodAutorizacion;
                if (DateTime.TryParseExact(response.ResultGet.FchVto, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fechaVencimiento))
                {
                    cVtoCAE = fechaVencimiento.ToString("yyyyMMdd");
                }
                else
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"CmpConsultar Error al parsear fecha de vencimiento {response.ResultGet.FchVto}");
                    return false;
                }
                NumeroCAE = cNroCAE;
                VencimientoCAE = cVtoCAE;

                XmlResponse = HelpersGlobal.SerializeObjectAXml(response);
                if (HabilitaLog) HelpersLogger.Escribir($"CmpConsultar {cNroCAE} {cVtoCAE} {XmlResponse}");

                return true;
            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"Error Exception {ex.Message} {TraceBack} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, ex.StackTrace);
                return false;
            }
        }
        #endregion
        #region [Métodos Autorizacion y Consulta CAE ]
        public bool Autorizar(int nPtoVta, int nTipCom)
        {// llama al FECAESOlcicitar 
            if (HabilitaLog) HelpersLogger.Escribir("Inicio Autorizar");
            NumeroCAE = "";
            VencimientoCAE = "";
            Result = "";
            Reproc = "";
            try
            {
                object objWSFEV1 = null;
                ////obtiene token y sign del archivo cache
                if (TkValido == null)
                    TkValido = HelpersCache.RecuperarTokenSign(HelpersCache.LeerBloqueServicio(PathCache, service));     
                if (HabilitaLog) HelpersLogger.Escribir($"Autorizar Token Válido");

                //llama a ARCA FECAESOlicitar y manda el objeto creado
                //si devuelve error, setea el error y devuelve false
                dynamic respuesta;
                int errCode = 0;
                string errDesc = "";

                if (Produccion)
                    AutorizarARCA(nPtoVta, nTipCom, objWSFEV1, out respuesta);
                else
                    AutorizarARCA_HOMO(nPtoVta, nTipCom, objWSFEV1, out respuesta);
                if (HabilitaLog) HelpersLogger.Escribir("Autorización ARCA");
                // Verificar la respuesta
                string cae = ""; string vtoCae = ""; string result = ""; string reproc = ""; string xmlResponse = ""; string observ = ""; string eventDesc = "";
                HelpersArca.ProcesarRespuestaFactura(HabilitaLog, respuesta, ref errCode, ref errDesc, ref xmlResponse, ref cae, ref vtoCae, ref result, ref reproc, ref observ, ref eventDesc,
                    GlobalSettings.TipoInformeARCA.CAE);

                //Setear Valores a Devolver publicos
                Result = result;
                Reproc = reproc;
                NumeroCAE = cae;
                VencimientoCAE = vtoCae;
                Observaciones = observ;
                TraceBack = $"Autorizar {TraceBack}";
                XmlResponse = xmlResponse;
                ErrorCode = errCode;
                ErrorDesc = errDesc;
                Excepcion = eventDesc;

                if (HabilitaLog) HelpersLogger.Escribir($"Autorizar {result} {reproc} {cae} {vtoCae} {xmlResponse}");
                return true;

            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"Error Exception {ex.Message} {TraceBack} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"{TraceBack} {ex.StackTrace}");
                return false;
            }
        }

        public void AutorizarRespuesta(int nIndice, out string cNroCAE, ref string cVtoCAE, ref string cResult, ref string cReproc)
        {

            cNroCAE = NumeroCAE;
            cVtoCAE = VencimientoCAE;
            cResult = Result;
            cReproc = Reproc;

        }

        public string AutorizarRespuestaObs(int nIndice)
        {
            return Observaciones;

        }
        #endregion

        #region [METODOS  CAEA]
        public bool CAEAConsultar(int nPeriod, short nQuince, ref string cNroCAE, ref string dFchDes, ref string dFchHas, ref string dFchTop, ref string dFchPro)
        {
            if (HabilitaLog) HelpersLogger.Escribir("Inicio CAEAConsultar");
            NumeroCAE = ""; FechaDesde = ""; FechaHasta = ""; FechaTope = ""; FechaProceso = "";
            try
            {
                bool resp = HelpersArcaCAEA.MetodoCAEA(GlobalSettings.MetCAEA.CAEACONSULTAR, TkValido, PathCache, Produccion, Cuit, nPeriod, nQuince, ref cNroCAE, ref dFchDes, ref dFchHas, ref dFchTop, ref dFchPro,
                  out int errCode, out string errDesc, out string xmlResponse, out string trackBack, HabilitaLog);

                SetError((GlobalSettings.Errors)errCode, errDesc, $"Errores Respuesta FECAEAConsultar {trackBack}");

                NumeroCAE = cNroCAE;
                FechaDesde = dFchDes;
                FechaHasta = dFchHas;
                FechaTope = dFchTop;
                FechaProceso = dFchPro;
                XmlResponse = xmlResponse;
                TraceBack = trackBack;
                if (HabilitaLog) HelpersLogger.Escribir($"CAEA Consultar {cNroCAE} {dFchDes} {dFchHas} {dFchTop} {dFchPro} {xmlResponse}");
                return resp;


            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"Error CAEAConsultar Exception {ex.Message} {TraceBack} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, ex.StackTrace);
                return false;
            }
        }

        public bool CAEASolicitar(int nPeriod, short nQuince, ref string cNroCAE, ref string dFchDes, ref string dFchHas, ref string dFchTop, ref string dFchPro)
        {
            NumeroCAE = ""; FechaDesde = ""; FechaHasta = ""; FechaTope = ""; FechaProceso = "";
            try
            {
                if (HabilitaLog) HelpersLogger.Escribir("Inicio CAEASolicitar");
                bool resp = HelpersArcaCAEA.MetodoCAEA(GlobalSettings.MetCAEA.CAEASOLICITAR, TkValido, PathCache, Produccion, Cuit, nPeriod, nQuince, ref cNroCAE, ref dFchDes, ref dFchHas, ref dFchTop, ref dFchPro,
                     out int errCode, out string errDesc, out string xmlResponse, out string trackBack, HabilitaLog);

                SetError((GlobalSettings.Errors)errCode, errDesc, $"Errores Respuesta FECAEASolicitar {trackBack}");

                NumeroCAE = cNroCAE;
                FechaDesde = dFchDes;
                FechaHasta = dFchHas;
                FechaTope = dFchTop;
                FechaProceso = dFchPro;
                XmlResponse = xmlResponse;
                TraceBack = trackBack;

                if (HabilitaLog) HelpersLogger.Escribir($"CAEA Solicitar {cNroCAE} {dFchDes} {dFchHas} {dFchTop} {dFchPro} {xmlResponse}");
                return resp;


            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"Error CAEASolicitar Exception {ex.Message} {TraceBack} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, ex.StackTrace);
                return false;
            }
        }

        public bool CAEAInformar(int nPtoVta, int nTipCom, string sCAE)
        {
            //FECAEARegInformativo
            NumeroCAE = "";
            VencimientoCAE = "";
            Result = "";
            Reproc = "";
            try
            {
                if (HabilitaLog) HelpersLogger.Escribir("Inicio CAEAInformar");
                object objWSFEV1 = null;
                ////obtiene token y sign del archivo cache
                if (TkValido == null)
                    TkValido=HelpersCache.RecuperarTokenSign(HelpersCache.LeerBloqueServicio(PathCache, service)); 

                if (HabilitaLog) HelpersLogger.Escribir("TkValido CAEAInformar");

                //llama a ARCA FECAESOlicitar y manda el objeto creado
                //si devuelve error, setea el error y devuelve false
                dynamic respuesta;
                int errCode = 0;
                string errDesc = "";

                //if (Produccion)
                RegInformativoARCA(nPtoVta, nTipCom, sCAE, objWSFEV1, out respuesta);
                //else
                //    RegInformativoARCA_HOMO(nPtoVta, nTipCom, sCAE, objWSFEV1, out respuesta);

                // Verificar la respuesta
                string cae = ""; string vtoCae = ""; string result = ""; string reproc = ""; string xmlResponse = ""; string observ = ""; string eventDesc = "";
                HelpersArca.ProcesarRespuestaFactura(HabilitaLog, respuesta, ref errCode, ref errDesc, ref xmlResponse, ref cae, ref vtoCae, ref result,
                    ref reproc, ref observ, ref eventDesc, GlobalSettings.TipoInformeARCA.CAEA);

                //Setear Valores a Devolver publicos
                Result = result;
                Reproc = reproc;
                NumeroCAE = cae;
                VencimientoCAE = vtoCae;
                Observaciones = observ;
                TraceBack = $"CAEAINformar {TraceBack}";
                XmlResponse = xmlResponse;
                ErrorCode = errCode;
                ErrorDesc = errDesc;
                Excepcion = eventDesc;

                if (ErrorDesc.Length == 0 && observ.Length > 0) ErrorDesc = observ;

                if (ErrorDesc.Length > 0) if (HabilitaLog) HelpersLogger.Escribir($"Error CAEAINformar {ErrorDesc}");
                return true;

            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"Error CAEAINformar Exception {ex.Message} {TraceBack} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"{TraceBack} {ex.StackTrace}");
                return false;
            }
        }

        //este metodo agrega al objeto CAEA 
        public void CAEACbteFchHsGen(string cFchCom)
        {

            try
            {
                ////Convertir CAEADetRequest a FECAEDetRequest

                CbteFchGen = cFchCom;
                if (HabilitaLog) HelpersLogger.Escribir($"CAEACbteFchHsGen");
            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"Error CAEACbteFchHsGen Exception {ex.Message} {TraceBack} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"CAEACbteFchHsGen");
            }

        }
        #endregion
        public string GetVersion()
        {
            return $"1.2.4"; // Cambia esto según tu versión actual
        }

        #region [Metodos Autorización directa ARCA]
        private void AutorizarARCA(int nPtoVta, int nTipCom, object objWSFEV1, out object respuesta)
        {
            if (HabilitaLog) HelpersLogger.Escribir($"Inicio AutorizarARCA {nPtoVta} {nTipCom}");
            try
            {
                if (HabilitaLog) HelpersLogger.Escribir("Linea 1");
                var authProd = new Wsfev1.FEAuthRequest
                {
                    Token = TkValido.Token,
                    Sign = TkValido.Sign,
                    Cuit = Convert.ToInt64(Cuit)
                };

                Wsfev1.FECAECabRequest cabeceraProd = new Wsfev1.FECAECabRequest
                {
                    CantReg = 1,
                    PtoVta = nPtoVta,
                    CbteTipo = nTipCom,
                };

                if (HabilitaLog)
                {
                    HelpersLogger.Escribir($"Linea 2 Cabecera {cabeceraProd.CbteTipo} {cabeceraProd.PtoVta} {cabeceraProd.CantReg}");
                    HelpersLogger.Escribir($"Linea 2 {HelpersGlobal.SerializeObjectAXml( CAEDetRequest)}");
                }
                // Convertir CAEDetRequest a FECAEDetRequest
                Wsfev1.FECAEDetRequest detalleProd = new Wsfev1.FECAEDetRequest
                {
                    Concepto = CAEDetRequest.Concepto,
                    DocTipo = CAEDetRequest.DocTipo,
                    DocNro = CAEDetRequest.DocNro,
                    CbteDesde = CAEDetRequest.CbteDesde,
                    CbteHasta = CAEDetRequest.CbteHasta,
                    CbteFch = CAEDetRequest.CbteFch,
                    ImpTotal = CAEDetRequest.ImpTotal,
                    ImpTotConc = CAEDetRequest.ImpTotConc,
                    ImpNeto = CAEDetRequest.ImpNeto,
                    ImpOpEx = CAEDetRequest.ImpOpEx,
                    ImpIVA = Iva,
                    ImpTrib = CAEDetRequest.ImpTrib,
                    MonId = CAEDetRequest.MonId,
                    MonCotiz = CAEDetRequest.MonCotiz,
                    //MonCotizSpecified = true,
                    //CondicionIVAReceptorId = CAEDetRequest.CondicionIvaReceptor
                };

                if (CAEDetRequest.Concepto == 2 || CAEDetRequest.Concepto == 3)
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"Linea 2.1 Concepto {CAEDetRequest.Concepto} {CAEDetRequest.DocTipo} {CAEDetRequest.DocNro}");
                    detalleProd.FchServDesde = CAEDetRequest.FchServDesde;
                    detalleProd.FchServHasta = CAEDetRequest.FchServHasta;
                }
                if (CAEDetRequest.FchVtoPago.Length > 0) detalleProd.FchVtoPago = CAEDetRequest.FchVtoPago;

                if (HabilitaLog) HelpersLogger.Escribir($"Linea 3 Iva {CAEDetRequest.Iva?.Count()} ");
                if (CAEDetRequest.Iva != null && CAEDetRequest.Iva.Count() > 0)
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"Linea 3.1 {CAEDetRequest.Iva?.Count()} ");
                    detalleProd.ImpIVA = CAEDetRequest.Iva?.Sum(t => t.Importe) ?? 0;
                    detalleProd.Iva = CAEDetRequest.Iva.Select(alicIva => (Wsfev1.AlicIva)HelpersArca.ConvertAlicIva(alicIva, Produccion)).ToArray();
                }

                if (HabilitaLog) HelpersLogger.Escribir($"Linea 4 Tributos {CAEDetRequest.Tributos?.Count()} ");
                if (CAEDetRequest.Tributos != null && CAEDetRequest.Tributos.Count() > 0)
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"Linea 4.1 {CAEDetRequest.Tributos?.Count()} ");
                    detalleProd.ImpTrib = CAEDetRequest.Tributos?.Sum(t => t.Importe) ?? 0; //CAEDetRequest.ImpTrib;
                    detalleProd.Tributos = CAEDetRequest.Tributos.Select(tributo => (Wsfev1.Tributo)HelpersArca.ConvertirTributos(tributo, Produccion)).ToArray();
                }

                if (HabilitaLog) HelpersLogger.Escribir($"Linea 5 Comp Asociados {CAEDetRequest.ComprobantesAsociados?.Count()} ");
                if (CAEDetRequest.ComprobantesAsociados != null && CAEDetRequest.ComprobantesAsociados.Length > 0)
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"Linea 5.1 {CAEDetRequest.ComprobantesAsociados?.Count()} ");
                    detalleProd.CbtesAsoc = CAEDetRequest.ComprobantesAsociados.Select(cbteAsoc => (Wsfev1.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, Produccion)).ToArray();
                }

                if (HabilitaLog) HelpersLogger.Escribir($"Linea 6 Opcionales {CAEDetRequest.Opcionales?.Count()} ");
                if (CAEDetRequest.Opcionales != null && CAEDetRequest.Opcionales.Length > 0)
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"Linea 6.1 Opcionales {CAEDetRequest.Opcionales?.Count()} ");
                    detalleProd.Opcionales = CAEDetRequest.Opcionales.Select(opcional => (Wsfev1.Opcional)HelpersArca.ConvertirOpcionales(opcional, Produccion)).ToArray();
                }

                if (HabilitaLog) HelpersLogger.Escribir($"Linea 7 ");
                var solicitudProd = new Wsfev1.FECAERequest
                {
                    FeCabReq = cabeceraProd, //CAbecera
                    FeDetReq = new Wsfev1.FECAEDetRequest[] { detalleProd } //detalle
                };

                TraceBack = HelpersGlobal.SerializeObjectAXml(solicitudProd);
                if (HabilitaLog) HelpersLogger.Escribir($"Linea 7.1 {TraceBack}");

                //invocar FECAESolicitar del  wsfev1 
                objWSFEV1 = new Wsfev1.Service();
                respuesta = ((dynamic)objWSFEV1).FECAESolicitar(authProd, solicitudProd);

                TraceBack = $"Respuesta: {HelpersGlobal.SerializeObjectAXml(respuesta)}";

            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"Error AutorizarARCA Exception {ex.Message} {TraceBack} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"{TraceBack} {ex.StackTrace}");
                respuesta = null;
            }
        }
        private void RegInformativoARCA(int nPtoVta, int nTipCom, string sCAE, object objWSFEV1, out object respuesta)
        {
            try
            {

                if (HabilitaLog) HelpersLogger.Escribir("Inicio RegInformativoARCA ");
                var authProd = new Wsfev1.FEAuthRequest
                {
                    Token = TkValido.Token,
                    Sign = TkValido.Sign,
                    Cuit = Convert.ToInt64(Cuit)
                };

                Wsfev1.FECAEACabRequest cabeceraProd = new Wsfev1.FECAEACabRequest
                {
                    CantReg = 1,
                    PtoVta = nPtoVta,
                    CbteTipo = nTipCom
                };
                if (HabilitaLog) HelpersLogger.Escribir("RegInformativoARCA Auth CabReq");

                //Convertir CAEADetRequest a FECAEDetRequest
                if (HabilitaLog) HelpersLogger.Escribir($"Antes Copy {HelpersGlobal.SerializeToXml(CAEDetRequest)}");
                if (InnCAEADetReq == null)
                {
                    InnCAEADetReq = CAEDetRequest.CopyToDerived<CAEDetRequest, InnCAEADetRequest>();
                    if (HabilitaLog) HelpersLogger.Escribir($"Pos Copy {HelpersGlobal.SerializeToXml(InnCAEADetReq)}");
                }
                InnCAEADetReq.CAEA = sCAE;
                InnCAEADetReq.CbteFchHsGen = CbteFchGen;

                Wsfev1.FECAEADetRequest detalleProd = new Wsfev1.FECAEADetRequest
                {
                    Concepto = InnCAEADetReq.Concepto,
                    DocTipo = InnCAEADetReq.DocTipo,
                    DocNro = InnCAEADetReq.DocNro,
                    CbteDesde = InnCAEADetReq.CbteDesde,
                    CbteHasta = InnCAEADetReq.CbteHasta,
                    CbteFch = InnCAEADetReq.CbteFch,
                    ImpTotal = InnCAEADetReq.ImpTotal,
                    ImpTotConc = InnCAEADetReq.ImpTotConc,
                    ImpNeto = InnCAEADetReq.ImpNeto,
                    ImpOpEx = InnCAEADetReq.ImpOpEx,
                    ImpIVA = InnCAEADetReq.ImpIVA,
                    MonCotiz = InnCAEADetReq.MonCotiz,
                    //MonCotizSpecified = true,
                    MonId = InnCAEADetReq.MonId,
                    //CanMisMonExt = CAEDetRequest.CantidadMismaMonedaExt,                    
                    //CondicionIVAReceptorId = CAEADetReq.CondicionIvaReceptor,
                    CAEA = InnCAEADetReq.CAEA,
                    CbteFchHsGen = InnCAEADetReq.CbteFchHsGen
                };

                if (HabilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 2");
                if (InnCAEADetReq.Concepto == 2 || InnCAEADetReq.Concepto == 3)
                {
                    if (HabilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 2.1");
                    detalleProd.FchServDesde = InnCAEADetReq.FchServDesde;
                    detalleProd.FchServHasta = InnCAEADetReq.FchServHasta;
                }
                if (InnCAEADetReq.FchVtoPago.Length > 0) detalleProd.FchVtoPago = InnCAEADetReq.FchVtoPago;

                if (HabilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 3");
                if (InnCAEADetReq.Iva != null && InnCAEADetReq.Iva.Count() > 0)
                {

                    if (HabilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 3.1");
                    detalleProd.ImpIVA = InnCAEADetReq.Iva?.Sum(t => t.Importe) ?? 0;
                    detalleProd.Iva = InnCAEADetReq.Iva.Select(alicIva => (Wsfev1.AlicIva)HelpersArca.ConvertAlicIva(alicIva, Produccion)).ToArray();

                }
                if (HabilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 4");
                if (InnCAEADetReq.Tributos != null && InnCAEADetReq.Tributos.Count() > 0)

                {
                    if (HabilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 4.1");
                    detalleProd.ImpTrib = InnCAEADetReq.Tributos?.Sum(t => t.Importe) ?? 0;
                    detalleProd.Tributos = InnCAEADetReq.Tributos.Select(tributo => (Wsfev1.Tributo)HelpersArca.ConvertirTributos(tributo, Produccion)).ToArray();
                }
                if (HabilitaLog) HelpersLogger.Escribir($"RegInformativoARCA Linea 5 {InnCAEADetReq.ComprobantesAsociados.Count()}");
                if (InnCAEADetReq.ComprobantesAsociados != null && InnCAEADetReq.ComprobantesAsociados.Count() > 0)
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"Linea 5.1 {InnCAEADetReq.ComprobantesAsociados.Count()}");
                    detalleProd.CbtesAsoc = InnCAEADetReq.ComprobantesAsociados.Select(cbteAsoc => (Wsfev1.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, Produccion)).ToArray();
                }
                if (HabilitaLog) HelpersLogger.Escribir("Linea 6");
                if (InnCAEADetReq.Opcionales != null && InnCAEADetReq.Opcionales.Count() > 0)
                {
                    if (HabilitaLog) HelpersLogger.Escribir("Linea 6.1");
                    detalleProd.Opcionales = InnCAEADetReq.Opcionales.Select(opcional => (Wsfev1.Opcional)HelpersArca.ConvertirOpcionales(opcional, Produccion)).ToArray();
                }


                if (HabilitaLog) HelpersLogger.Escribir("Linea 7");
                var solicitudProd = new Wsfev1.FECAEARequest
                {
                    FeCabReq = cabeceraProd, //CAbecera
                    FeDetReq = new Wsfev1.FECAEADetRequest[] { detalleProd } //detalle
                };
                if (HabilitaLog) HelpersLogger.Escribir(HelpersGlobal.SerializeObjectAXml(solicitudProd));

                //invocar FECAESolicitar del wsfev1 
                objWSFEV1 = new Wsfev1.Service();
                respuesta = ((dynamic)objWSFEV1).FECAEARegInformativo(authProd, solicitudProd);
                if (HabilitaLog) HelpersLogger.Escribir("Linea 8");
            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"ERROR: {ex.Message} {TraceBack} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"{TraceBack} {ex.StackTrace}");
                respuesta = null;
            }
        }


        private void AutorizarARCA_HOMO(int nPtoVta, int nTipCom, object objWSFEV1, out object respuesta)
        {
            if (HabilitaLog) HelpersLogger.Escribir("Inicio AutorizarARCA_HOMO");
            try
            {
                var authProd = new Wsfev1Homo.FEAuthRequest
                {
                    Token = TkValido.Token,
                    Sign = TkValido.Sign,
                    Cuit = Convert.ToInt64(Cuit)
                };

                Wsfev1Homo.FECAECabRequest cabeceraProd = new Wsfev1Homo.FECAECabRequest
                {
                    CantReg = 1,
                    PtoVta = nPtoVta,
                    CbteTipo = nTipCom
                };
                TraceBack = "Linea 2";
                if (HabilitaLog) HelpersLogger.Escribir($"auth {HelpersGlobal.SerializeToXml(authProd)} cabeceraProd {HelpersGlobal.SerializeToXml(cabeceraProd)} ");

                //Convertir CAEDetRequest a FECAEDetRequest
                Wsfev1Homo.FECAEDetRequest detalleProd = new Wsfev1Homo.FECAEDetRequest
                {
                    Concepto = CAEDetRequest.Concepto,
                    DocTipo = CAEDetRequest.DocTipo,
                    DocNro = CAEDetRequest.DocNro,
                    CbteDesde = CAEDetRequest.CbteDesde,
                    CbteHasta = CAEDetRequest.CbteHasta,
                    CbteFch = CAEDetRequest.CbteFch,
                    ImpTotal = CAEDetRequest.ImpTotal,
                    ImpTotConc = CAEDetRequest.ImpTotConc,
                    ImpNeto = CAEDetRequest.ImpNeto,
                    ImpOpEx = CAEDetRequest.ImpOpEx,
                    ImpIVA = Iva,
                    MonCotiz = CAEDetRequest.MonCotiz,
                    MonCotizSpecified = true,
                    MonId = CAEDetRequest.MonId,
                    //CanMisMonExt = CAEDetRequest.CantidadMismaMonedaExt,                    
                    CondicionIVAReceptorId = CAEDetRequest.CondicionIvaReceptor,

                };

                if (HabilitaLog) HelpersLogger.Escribir($"Linea 2 ");
                if (CAEDetRequest.Concepto == 2 || CAEDetRequest.Concepto == 3)
                {

                    if (HabilitaLog) HelpersLogger.Escribir($"Linea 2.1 Concepto {CAEDetRequest.Concepto} {CAEDetRequest.DocTipo} {CAEDetRequest.DocNro}");
                    detalleProd.FchServDesde = CAEDetRequest.FchServDesde;
                    detalleProd.FchServHasta = CAEDetRequest.FchServHasta;
                }
                if (CAEDetRequest.FchVtoPago.Length > 0) detalleProd.FchVtoPago = CAEDetRequest.FchVtoPago;

                if (HabilitaLog) HelpersLogger.Escribir($"Linea 3 {CAEDetRequest.Iva?.Count()} ");
                if (CAEDetRequest.Iva != null && CAEDetRequest.Iva.Count() > 0)
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"Linea 3.1 {CAEDetRequest.Iva?.Count()} ");
                    detalleProd.ImpIVA = CAEDetRequest.Iva?.Sum(t => t.Importe) ?? 0;
                    detalleProd.Iva = CAEDetRequest.Iva.Select(alicIva => (Wsfev1Homo.AlicIva)HelpersArca.ConvertAlicIva(alicIva, Produccion)).ToArray();
                }

                if (HabilitaLog) HelpersLogger.Escribir($"Linea 4 {CAEDetRequest.Tributos?.Count()} ");
                if (CAEDetRequest.Tributos != null && CAEDetRequest.Tributos.Count() > 0)
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"Linea 4.1 {CAEDetRequest.Tributos?.Count()} ");
                    detalleProd.ImpTrib = CAEDetRequest.Tributos?.Sum(t => t.Importe) ?? 0;
                    detalleProd.Tributos = CAEDetRequest.Tributos.Select(tributo => (Wsfev1Homo.Tributo)HelpersArca.ConvertirTributos(tributo, Produccion)).ToArray();
                }

                if (HabilitaLog) HelpersLogger.Escribir($"Linea 5 {CAEDetRequest.ComprobantesAsociados?.Count()} ");
                if (CAEDetRequest.ComprobantesAsociados != null && CAEDetRequest.ComprobantesAsociados.Count() > 0)
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"Linea 5.1 {CAEDetRequest.ComprobantesAsociados?.Count()}");
                    detalleProd.CbtesAsoc = CAEDetRequest.ComprobantesAsociados.Select(cbteAsoc => (Wsfev1Homo.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, Produccion)).ToArray();
                }

                if (HabilitaLog) HelpersLogger.Escribir($"Linea 6 Opcionales {CAEDetRequest.Opcionales?.Count()} ");
                if (CAEDetRequest.Opcionales != null && CAEDetRequest.Opcionales.Count() > 0)
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"Linea 6.1 Opcionales {CAEDetRequest.Opcionales?.Count()} ");
                    detalleProd.Opcionales = CAEDetRequest.Opcionales.Select(opcional => (Wsfev1Homo.Opcional)HelpersArca.ConvertirOpcionales(opcional, Produccion)).ToArray();
                }


                var solicitudProd = new Wsfev1Homo.FECAERequest
                {
                    FeCabReq = cabeceraProd, //CAbecera
                    FeDetReq = new Wsfev1Homo.FECAEDetRequest[] { detalleProd } //detalle
                };
                TraceBack = HelpersGlobal.SerializeObjectAXml(solicitudProd);
                if (HabilitaLog) HelpersLogger.Escribir($"Linea 7 {TraceBack}");

                //invocar FECAESolicitar del wsfev1 
                objWSFEV1 = new Wsfev1Homo.Service();
                respuesta = ((dynamic)objWSFEV1).FECAESolicitar(authProd, solicitudProd);

                TraceBack = HelpersGlobal.SerializeObjectAXml(respuesta);
                if (HabilitaLog) HelpersLogger.Escribir($"Linea 8 {TraceBack}");
            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"Error Exception:{ex.Message} {TraceBack} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"{TraceBack} {ex.StackTrace}");
                respuesta = null;
            }
        }

        //private void RegInformativoARCA_HOMO(int nPtoVta, int nTipCom, string sCAE, object objWSFEV1, out object respuesta)
        //{
        //    try
        //    {
        //        //TraceBack = "Linea 1";
        //        //var authProd = new Wsfev1Homo.FEAuthRequest
        //        //{
        //        //    Token = TkValido.Token,
        //        //    Sign = TkValido.Sign,
        //        //    Cuit = Convert.ToInt64(Cuit)
        //        //};

        //        //Wsfev1Homo.FECAEACabRequest cabeceraProd = new Wsfev1Homo.FECAEACabRequest
        //        //{
        //        //    CantReg = 1,
        //        //    PtoVta = nPtoVta,
        //        //    CbteTipo = nTipCom
        //        //};
        //        //TraceBack = "Linea 2";
        //        ////Convertir CAEADetRequest a FECAEDetRequest
        //        //if (CAEADetReq == null)
        //        //{
        //        //    CAEADetReq = CAEDetRequest.CopyToDerived<CAEDetRequest, CAEADetRequest>();
        //        //}
        //        //CAEADetReq.CAEA = sCAE;

        //        //Wsfev1Homo.FECAEADetRequest detalleProd = new Wsfev1Homo.FECAEADetRequest
        //        //{
        //        //    Concepto = CAEADetReq.Concepto,
        //        //    DocTipo = CAEADetReq.DocTipo,
        //        //    DocNro = CAEADetReq.DocNro,
        //        //    CbteDesde = CAEADetReq.CbteDesde,
        //        //    CbteHasta = CAEADetReq.CbteHasta,
        //        //    CbteFch = CAEADetReq.CbteFch,
        //        //    ImpTotal = CAEADetReq.ImpTotal,
        //        //    ImpTotConc = CAEADetReq.ImpTotConc,
        //        //    ImpNeto = CAEADetReq.ImpNeto,
        //        //    ImpOpEx = CAEADetReq.ImpOpEx,
        //        //    ImpIVA = Iva,
        //        //    MonCotiz = CAEADetReq.MonCotiz,
        //        //    MonCotizSpecified = true,
        //        //    MonId = CAEADetReq.MonId,
        //        //    //CanMisMonExt = CAEDetRequest.CantidadMismaMonedaExt,                    
        //        //    CondicionIVAReceptorId = CAEADetReq.CondicionIvaReceptor,
        //        //    CAEA = CAEADetReq.CAEA,
        //        //    CbteFchHsGen = CAEADetReq.CbteFchHsGen
        //        //};
        //        //if (CAEADetReq.Concepto == 2 || CAEADetReq.Concepto == 3)
        //        //{

        //        //    TraceBack = "Linea 2.1";
        //        //    detalleProd.FchServDesde = CAEADetReq.FchServDesde;
        //        //    detalleProd.FchServHasta = CAEADetReq.FchServHasta;
        //        //}
        //        //if (CAEADetReq.FchVtoPago.Length > 0) detalleProd.FchVtoPago = CAEADetReq.FchVtoPago;

        //        //TraceBack = "Linea 3";
        //        //if (Iva > 0)
        //        //{
        //        //    TraceBack = "Linea 3.1";
        //        //    detalleProd.Iva = CAEADetReq.Iva.Select(alicIva => (Wsfev1Homo.AlicIva)HelpersArca.ConvertAlicIva(alicIva, Produccion)).ToArray();
        //        //}
        //        //TraceBack = "Linea 4";
        //        //if (CAEADetReq.ImpTrib > 0)
        //        //{
        //        //    TraceBack = "Linea 4.1";
        //        //    detalleProd.ImpTrib = CAEADetReq.ImpTrib;
        //        //    detalleProd.Tributos = CAEADetReq.Tributos.Select(tributo => (Wsfev1Homo.Tributo)HelpersArca.ConvertirTributos(tributo, Produccion)).ToArray();
        //        //}
        //        //TraceBack = "Linea 5";
        //        //if (CAEADetReq.ComprobantesAsociados != null && CAEADetReq.ComprobantesAsociados.Count() > 0)
        //        //{
        //        //    TraceBack = "Linea 5.1";
        //        //    detalleProd.CbtesAsoc = CAEADetReq.ComprobantesAsociados.Select(cbteAsoc => (Wsfev1Homo.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, Produccion)).ToArray();
        //        //}
        //        //TraceBack = "Linea 6";

        //        //if (CAEADetReq.Opcionales != null && CAEADetReq.Opcionales.Count() > 0)
        //        //{
        //        //    TraceBack = "Linea 6.1";
        //        //    detalleProd.Opcionales = CAEADetReq.Opcionales.Select(opcional => (Wsfev1Homo.Opcional)HelpersArca.ConvertirOpcionales(opcional, Produccion)).ToArray();
        //        //}


        //        //TraceBack = "Linea 7";
        //        //var solicitudProd = new Wsfev1Homo.FECAEARequest
        //        //{
        //        //    FeCabReq = cabeceraProd, //CAbecera
        //        //    FeDetReq = new Wsfev1Homo.FECAEADetRequest[] { detalleProd } //detalle
        //        //};
        //        //TraceBack = HelpersGlobal.SerializeObjectAXml(solicitudProd);

        //        ////invocar FECAESolicitar del wsfev1 
        //        //objWSFEV1 = new Wsfev1Homo.Service();
        //        //respuesta = ((dynamic)objWSFEV1).FECAEARegInformativo(authProd, solicitudProd);
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        TraceBack = ex.StackTrace;
        //        SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"{TraceBack} {ex.StackTrace}");
        //        respuesta = null;
        //    }
        //}
        #endregion
        #region[Metodos para Setear y Devolver valores ]


        private void SetError(GlobalSettings.Errors codigoError, string descError, string traceBack)
        {
            ErrorCode = (int)codigoError;

            ErrorDesc = codigoError == 0 ? "" : descError;

            TraceBack = traceBack;
        }

        public string GetAppServerStatus()
        {
            return AppServerStatus_;
        }
        public string GetDbServerStatus()
        {
            return DbServerStatus_;
        }
        public string GetAuthServerStatus()
        {
            return AuthServerStatus_;
        }

        public int GetUltimoNumero()
        {
            return UltimoNumero;
        }

        public string GetFechaDesde()
        {
            return FechaDesde;
        }
        public string GetFechaHasta()
        {
            return FechaHasta;
        }
        public string GetFechaTope()
        {
            return FechaTope;
        }
        public string GetFechaProceso()
        {
            return FechaProceso;
        }
        public string GetNumeroCAE()
        {
            return NumeroCAE;
        }
        public string GetVencimientoCAE()
        {
            return VencimientoCAE;
        }
        public string GetResultado()
        {
            return Result;
        }
        public string GetReprocesar()
        {
            return Reproc;
        }
        #endregion

        #region [MÉTODOS PARA COMPLETAR EL OBJETO ]
        public void AgregaFactura(int nConcep, int nTipDoc, long nNroDoc, long nNroDes, long nNroHas, string cFchCom, double nImpTot, double nImpCon, double nImpNet, double nImpOpc, string cSerDes,
            string cSerHas, string cSerVto, string cMoneda, double nCotiza, int nCondIvaRec)
        {
            try
            {
                if (HabilitaLog) HelpersLogger.Escribir($"Inicio AgregaFactura");
                // Verificar que la fecha esté en el formato "yyyyMMdd"
                if (!DateTime.TryParseExact(cFchCom, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha))
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"ERROR: La fecha comprobante debe estar en el formato 'yyyyMMdd'. {cFchCom} {cSerDes} {cSerHas} {cSerVto}");
                    SetError(GlobalSettings.Errors.FORMAT_ERROR, "La fecha comprobante debe estar en el formato 'yyyyMMdd'.", "AgergaFactura 1");
                    return;
                }
                if (cSerDes.Trim().Length > 0 && !DateTime.TryParseExact(cSerDes.Trim(), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha1))
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"ERROR: La fecha desde servicio debe estar en el formato 'yyyyMMdd'. {cFchCom} {cSerDes} {cSerHas} {cSerVto}");
                    SetError(GlobalSettings.Errors.FORMAT_ERROR, "La fecha desde servicio debe estar en el formato 'yyyyMMdd'.", "AgregaFactura 2");
                    return;
                }
                if (cSerHas.Trim().Length > 0 && !DateTime.TryParseExact(cSerHas.Trim(), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha2))
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"ERROR: La fecha hasta servicio debe estar en el formato 'yyyyMMdd'. {cFchCom} {cSerDes} {cSerHas} {cSerVto}");
                    SetError(GlobalSettings.Errors.FORMAT_ERROR, "La fecha hasta servicio debe estar en el formato 'yyyyMMdd'.", "AgregaFactura 3");
                    return;
                }
                if (cSerVto.Trim().Length > 0 && !DateTime.TryParseExact(cSerVto.Trim(), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha3))
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"ERROR: La fecha vencimiento servicio debe estar en el formato 'yyyyMMdd'. {cFchCom} {cSerDes} {cSerHas} {cSerVto}");
                    SetError(GlobalSettings.Errors.FORMAT_ERROR, "La fecha vencimiento servicio Desde debe estar en el formato 'yyyyMMdd'.", "Agrega Factura 4");
                    return;
                }
                if (HabilitaLog) HelpersLogger.Escribir($"AgregaFactura Pos Validaciones Fecha");
                CAEDetRequest = new InnercorArcaModels.CAEDetRequest()
                {
                    Concepto = nConcep,
                    DocTipo = nTipDoc,
                    DocNro = nNroDoc,
                    CbteDesde = nNroDes,
                    CbteHasta = nNroHas,
                    CbteFch = cFchCom, //.ToString("yyyyMMdd"),
                    ImpTotal = nImpTot,
                    ImpTotConc = nImpCon,
                    ImpNeto = nImpNet,
                    ImpOpEx = nImpOpc,
                    ImpTrib = 0.00,
                    ImpIVA = nImpCon,
                    MonId = cMoneda,
                    MonCotiz = nCotiza,
                    CantidadMismaMonedaExt = "N",
                    FchServDesde = cSerDes, //.ToString("yyyyMMdd"),
                    FchServHasta = cSerHas, //.ToString("yyyyMMdd"),
                    FchVtoPago = cSerVto, //.ToString("yyyyMMdd"),
                    CondicionIvaReceptor = nCondIvaRec
                };
                TraceBack = "AgregaFactura";
                if (HabilitaLog) HelpersLogger.Escribir($"{TraceBack} ");
            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"ERROR Exception CAEDetRequest: {ex.Message} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"CAEDetRequest {HelpersGlobal.SerializeObjectAXml(CAEDetRequest)}");
            }

        }

        public void AgregaIVA(int codigoAlicuota, double importeBase, double importeIVA)
        {
            if (HabilitaLog) HelpersLogger.Escribir($"Inicia AgregaIVA ");
            try
            {
                InnercorArcaModels.AlicIva nuevaAlicuota = new InnercorArcaModels.AlicIva()
                {
                    BaseImp = Math.Abs(importeBase),
                    Id = codigoAlicuota,
                    Importe = Math.Abs(importeIVA)
                };
                if (HabilitaLog) HelpersLogger.Escribir($"AgregaIVA Agregar la nueva alícuota a la lista de IVA {codigoAlicuota} - {importeBase} - {importeIVA}");
                // Agregar la nueva alícuota a la lista de IVA
                if (nuevaAlicuota != null)
                {
                    if (CAEDetRequest.Iva == null)
                        CAEDetRequest.Iva = new InnercorArcaModels.AlicIva[0]; // Initialize the array

                    // Create a new array with the new size
                    var newArray = new InnercorArcaModels.AlicIva[CAEDetRequest.Iva.Length + 1];
                    // Copy the existing elements to the new array
                    Array.Copy(CAEDetRequest.Iva, newArray, CAEDetRequest.Iva.Length);
                    // Add the new element to the new array
                    newArray[newArray.Length - 1] = nuevaAlicuota;
                    // Assign the new array back to the property
                    CAEDetRequest.Iva = newArray;
                    if (HabilitaLog) HelpersLogger.Escribir($"AgregaIVA  lista de IVA {CAEDetRequest.Iva.Length}");
                }

                // Acumular valores en la clase
                Iva += importeIVA;
                Neto += importeBase;

                TraceBack = $"AgregaIVA {CAEDetRequest.Iva.Count()}";
                if (HabilitaLog) HelpersLogger.Escribir($"{TraceBack}");
            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"ERROR Exception: {ex.Message} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"Cantidad AliccIvas {CAEDetRequest.Iva.Count()} : {codigoAlicuota} - {importeBase} - {importeIVA} .. {ex.StackTrace}");
            }
        }

        public void AgregaOpcional(string codigo, string valor)
        {
            if (HabilitaLog) HelpersLogger.Escribir($"Inicia AgregaOpcional {codigo} - {valor}");
            try
            {
                InnercorArcaModels.Opcional nuevoOpcional = new InnercorArcaModels.Opcional()
                {
                    Id = codigo,
                    Valor = valor
                };

                if (HabilitaLog) HelpersLogger.Escribir($"AgregaOpcional Agregar el nuevo opcional a la lista de opcionales {codigo} - {valor}");
                // Verificación adicional antes de agregar 
                if (nuevoOpcional != null)
                {
                    if (CAEDetRequest.Opcionales == null)
                        CAEDetRequest.Opcionales = new InnercorArcaModels.Opcional[0]; // Initialize the array

                    // Create a new array with the new size
                    var newArray = new InnercorArcaModels.Opcional[CAEDetRequest.Opcionales.Length + 1];
                    // Copy the existing elements to the new array
                    Array.Copy(CAEDetRequest.Opcionales, newArray, CAEDetRequest.Opcionales.Length);
                    // Add the new element to the new array
                    newArray[newArray.Length - 1] = nuevoOpcional;
                    // Assign the new array back to the property
                    CAEDetRequest.Opcionales = newArray;
                    if (HabilitaLog) HelpersLogger.Escribir($"AgregaOpcional lista de opcionales {CAEDetRequest.Opcionales.Length}");
                }
                TraceBack = $"AgregaOpcional {CAEDetRequest.Opcionales.Count()}";
                if (HabilitaLog) HelpersLogger.Escribir(TraceBack);
            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"ERROR Exception: {ex.Message} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"Cantidad Opcionales {CAEDetRequest.Opcionales.Count()}: {codigo} - {valor} .. {ex.StackTrace}");
            }
        }
        public void AgregaTributo(short codimp, string descri, double impbase, double alicuo, double import)
        {
            if (HabilitaLog) HelpersLogger.Escribir($"Inicia AgregaTributo {codimp} - {descri} - {impbase} - {alicuo} - {import}");
            try
            {
                InnercorArcaModels.Tributo nuevoTributo = new InnercorArcaModels.Tributo()
                {
                    Id = codimp,
                    Desc = descri,
                    BaseImp = Math.Round(impbase, 2),
                    Alic = Math.Round(alicuo, 2),
                    Importe = Math.Round(import, 2)
                };
                if (HabilitaLog) HelpersLogger.Escribir($"AgregaTributo Agregar el nuevo tributo a la lista de tributos {codimp} - {descri} - {impbase} - {alicuo} - {import}");
                // Agregar el tributo a la lista de tributos del servicio 
                if (nuevoTributo != null)
                {
                    if (CAEDetRequest.Tributos == null)
                        CAEDetRequest.Tributos = new InnercorArcaModels.Tributo[0]; // Initialize the array

                    // Create a new array with the new size
                    var newArray = new InnercorArcaModels.Tributo[CAEDetRequest.Tributos.Length + 1];
                    // Copy the existing elements to the new array
                    Array.Copy(CAEDetRequest.Tributos, newArray, CAEDetRequest.Tributos.Length);
                    // Add the new element to the new array
                    newArray[newArray.Length - 1] = nuevoTributo;
                    // Assign the new array back to the property
                    CAEDetRequest.Tributos = newArray;
                    if (HabilitaLog) HelpersLogger.Escribir($"AgregaTributo lista de tributos {CAEDetRequest.Tributos.Length}");
                }
                TraceBack = $"AgregaTributo {CAEDetRequest.Tributos.Count()}";
                if (HabilitaLog) HelpersLogger.Escribir(TraceBack);
            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"ERROR Exception: {ex.Message} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"Cantidad Tributos {CAEDetRequest.Tributos.Count()} : {codimp} - {descri} - {impbase} - {alicuo} - {import} .. {ex.StackTrace}");
            }
        }

        public void AgregaCompAsoc(int nTipCmp, int nPtoVta, int nNroCmp, Int64 nNroCuit, string dFchCmp)
        {
            try
            {
                if (HabilitaLog) HelpersLogger.Escribir($"Inicia AgregaCompAsoc {nTipCmp} - {nPtoVta} - {nNroCmp} - {nNroCuit} - {dFchCmp}");
                // Verificar que la fecha esté en el formato "yyyyMMdd"
                if (!DateTime.TryParseExact(dFchCmp, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha))
                {
                    SetError(GlobalSettings.Errors.FORMAT_ERROR, "La fecha debe estar en el formato 'yyyyMMdd'.", $"Error Fecha AgregaCompAsoc {dFchCmp}");
                    return;
                }
                if (HabilitaLog) HelpersLogger.Escribir($"AgregaCompAsoc Pos Validaciones Fecha");

                InnercorArcaModels.CbteAsoc nuevoComprobanteAsociado = new InnercorArcaModels.CbteAsoc()
                {
                    Tipo = nTipCmp,
                    PtoVta = nPtoVta,
                    Nro = nNroCmp,
                    Cuit = nNroCuit.ToString(), // Convertir  Integer a string
                    CbteFch = dFchCmp//.ToString("yyyyMMdd") // Formato de fecha esperado por la API
                };
                if (HabilitaLog) HelpersLogger.Escribir($"AgregaCompAsoc Agregar el nuevo comprobante asociado a la lista de comprobantes asociados {nTipCmp} - {nPtoVta} - {nNroCmp} - {nNroCuit} - {dFchCmp}");
                // Agregar el comprobante asociado a la lista correspondiente dentro del servicio 
                if (nuevoComprobanteAsociado != null)
                {
                    if (CAEDetRequest.ComprobantesAsociados == null)
                        CAEDetRequest.ComprobantesAsociados = new InnercorArcaModels.CbteAsoc[0]; // Initialize the array

                    // Create a new array with the new size
                    var newArray = new InnercorArcaModels.CbteAsoc[CAEDetRequest.ComprobantesAsociados.Length + 1];
                    // Copy the existing elements to the new array
                    Array.Copy(CAEDetRequest.ComprobantesAsociados, newArray, CAEDetRequest.ComprobantesAsociados.Length);
                    // Add the new element to the new array
                    newArray[newArray.Length - 1] = nuevoComprobanteAsociado;
                    // Assign the new array back to the property
                    CAEDetRequest.ComprobantesAsociados = newArray;
                    if (HabilitaLog) HelpersLogger.Escribir($"AgregaCompAsoc lista de comprobantes asociados {CAEDetRequest.ComprobantesAsociados.Length}");
                }
                TraceBack = $"AgregaCompAsoc {CAEDetRequest.ComprobantesAsociados.Count()}";
                if (HabilitaLog) HelpersLogger.Escribir(TraceBack);
            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"ERROR Exception: {ex.Message} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"Cantidad Comprobantes Asociados {CAEDetRequest.ComprobantesAsociados.Count()}: {nTipCmp} - {nPtoVta} - {nNroCmp} - {nNroCuit} - {dFchCmp} .. {ex.StackTrace} ");
            }
        }
        #endregion [MÉTODOS PARA COMPLETAR EL OBJETO ]




    }
}
