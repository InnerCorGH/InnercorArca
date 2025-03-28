﻿using InnercorArca.V1.Helpers;
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
        string service = GlobalSettings.ServiceARCA.wsfe.ToString();
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
        internal InnercorArcaModels.CAEADetRequest CAEADetReq { get; set; }

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

                //Definir si variable de produccion es true o false segun la url del login
                Produccion = !(urlWSAA.ToUpper().Contains("HOMO"));

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
                string response = objTicketRespuesta.ObtenerLoginTicketResponse("wsfe", urlWSAA, pathCRT, pathKey, true, Produccion);

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

        public bool Dummy(ref string cAppServerStatus, ref string cDbServerStatus, ref string cAuthServerStatus)
        {


            try
            {
                object objDummy;
                if (Produccion)
                {
                    if (feAuthRequest == null)
                        HelpersArca.SeteaAuthRequest(Produccion, ref feAuthRequest, TkValido, Convert.ToInt64(Cuit));
                    objDummy = new Wsfev1.DummyResponse();
                }
                else
                {
                    if (feAuthRequest == null)
                        HelpersArca.SeteaAuthRequest(Produccion, ref feAuthRequest, TkValido, Convert.ToInt64(Cuit));
                    objDummy = new Wsfev1Homo.DummyResponse();
                }
                if (objDummy == null)
                {
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

                return cAppServerStatus == "OK" && cAuthServerStatus == "OK" && cDbServerStatus == "OK";
            }
            catch (Exception ex)
            {

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


                //NumeroCAE = string.Empty;
                //VencimientoCAE = DateTime.MinValue.ToString("yyyyMMdd");
                Result = string.Empty;
                //Reproc = string.Empty;

                CAEDetRequest = null;

                Neto = 0;
                Iva = 0;
            }
            catch (Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, $"Error al invocar Reset {ex.Message}", ex.StackTrace);
            }
        }

        public bool RecuperaLastCMP(int nPtoVta, int nTipCom, ref int nUltNro)
        {

            int errCode = 0;
            string errDesc = "";

            try
            {

                //obtiene token y sign del archivo cache
                if (TkValido == null)
                    TkValido = HelpersArca.RecuperarTokenSign(HelpersArca.LeerCache(PathCache, service));

                // Instancia el servicio adecuado
                object objWSFEV1;
                string xmlResponse = "";

                if (feAuthRequest == null)
                    HelpersArca.SeteaAuthRequest(Produccion, ref feAuthRequest, TkValido, Convert.ToInt64(Cuit));

                // Realiza el casting correcto según el entorno
                if (Produccion)
                {
                    objWSFEV1 = new Wsfev1.Service();
                    var wsfev1 = (Wsfev1.Service)objWSFEV1;
                    var objFERecuperaLastCbteResponse = wsfev1.FECompUltimoAutorizado((Wsfev1.FEAuthRequest)feAuthRequest, nPtoVta, nTipCom);

                    HelpersArca.ProcesarRespuesta(objFERecuperaLastCbteResponse, ref errCode, ref errDesc, ref xmlResponse);
                    if (errCode == 0) nUltNro = objFERecuperaLastCbteResponse.CbteNro;

                }
                else
                {
                    objWSFEV1 = new Wsfev1Homo.Service();
                    var wsfev1 = (Wsfev1Homo.Service)objWSFEV1;
                    var objFERecuperaLastCbteResponse = wsfev1.FECompUltimoAutorizado((Wsfev1Homo.FEAuthRequest)feAuthRequest, nPtoVta, nTipCom);

                    HelpersArca.ProcesarRespuesta(objFERecuperaLastCbteResponse, ref errCode, ref errDesc, ref xmlResponse);
                    if (errCode == 0) nUltNro = objFERecuperaLastCbteResponse.CbteNro;

                }

                SetError((GlobalSettings.Errors)errCode, errDesc, $"RecuperaLastCMP Producción {Produccion} {objWSFEV1.GetType()}");
                XmlResponse = xmlResponse;
                UltimoNumero = nUltNro;
                return true;
            }
            catch (Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, ex.Message);
                UltimoNumero = -1;
                return false;
            }
        }

        #endregion

        #region [METODOS Autorización y CONUSLTA    CAE]
        //Devuelve CAE y FECHA de VTO segun Tipo COmprobante, PtoVta y Nro OCmp
        public bool CmpConsultar(int nTipCom, int nPtoVta, long nNroCmp, ref string cNroCAE, ref string cVtoCAE)
        {
            try
            {
                // Inicialización de valores de salida
                cNroCAE = string.Empty;
                cVtoCAE = DateTime.MinValue.ToString("yyyyMMdd");

                // Configurar la solicitud con los parámetros recibidos
                object objReq;
                if (Produccion)
                    objReq = new Wsfev1.FECompConsultaReq();
                else
                    objReq = new Wsfev1Homo.FECompConsultaReq();

                dynamic req = objReq;
                req.CbteTipo = nTipCom;
                req.PtoVta = nPtoVta;
                req.CbteNro = nNroCmp;

                //obtiene token y sign del archivo cache
                if (TkValido == null)
                    TkValido = HelpersArca.RecuperarTokenSign(HelpersArca.LeerCache(PathCache, service));

                // Configurar autenticación
                object auth;
                if (Produccion)
                    auth = new Wsfev1.FEAuthRequest();
                else
                    auth = new Wsfev1Homo.FEAuthRequest();
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


                // Verificar errores en la respuesta
                if (response.Errors != null && response.Errors.Length > 0)
                {
                    int errCode = 0; string errDesc = ""; string xmlResponse = "";
                    HelpersArca.ProcesarRespuesta(response, ref errCode, ref errDesc, ref xmlResponse);
                    SetError((GlobalSettings.Errors)errCode, errDesc, "Errores Respuesta FECompConsultar");
                    XmlResponse = xmlResponse;
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

                    return false;
                }
                NumeroCAE = cNroCAE;
                VencimientoCAE = cVtoCAE;

                XmlResponse = HelpersGlobal.SerializeObjectAXml(response);
                return true;
            }
            catch (Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, ex.StackTrace);
                return false;
            }
        }

        public bool Autorizar(int nPtoVta, int nTipCom)
        {// llama al FECAESOlcicitar 

            NumeroCAE = "";
            VencimientoCAE = "";
            Result = "";
            Reproc = "";
            try
            {
                object objWSFEV1 = null;
                ////obtiene token y sign del archivo cache
                if (TkValido == null)
                    TkValido = HelpersArca.RecuperarTokenSign(HelpersArca.LeerCache(PathCache, service));


                //llama a ARCA FECAESOlicitar y manda el objeto creado
                //si devuelve error, setea el error y devuelve false
                dynamic respuesta;
                int errCode = 0;
                string errDesc = "";

                if (Produccion)
                    AutorizarARCA(nPtoVta, nTipCom, objWSFEV1, out respuesta);
                else
                    AutorizarARCA_HOMO(nPtoVta, nTipCom, objWSFEV1, out respuesta);

                // Verificar la respuesta
                string cae = ""; string vtoCae = ""; string result = ""; string reproc = ""; string xmlResponse = ""; string observ = "";
                HelpersArca.ProcesarRespuestaFactura(respuesta, ref errCode, ref errDesc, ref xmlResponse, ref cae, ref vtoCae, ref result, ref reproc, ref observ);

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

                return true;

            }
            catch (Exception ex)
            {
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
            NumeroCAE = ""; FechaDesde = ""; FechaHasta = ""; FechaTope = ""; FechaProceso = "";
            try
            {
                bool resp = HelpersArcaCAEA.MetodoCAEA(GlobalSettings.MetCAEA.CAEACONSULTAR, TkValido, PathCache, Produccion, Cuit, nPeriod, nQuince, ref cNroCAE, ref dFchDes, ref dFchHas, ref dFchTop, ref dFchPro,
                  out int errCode, out string errDesc, out string xmlResponse, out string trackBack);

                SetError((GlobalSettings.Errors)errCode, errDesc, $"Errores Respuesta FECAEAConsultar {trackBack}");

                NumeroCAE = cNroCAE;
                FechaDesde = dFchDes;
                FechaHasta = dFchHas;
                FechaTope = dFchTop;
                FechaProceso = dFchPro;
                XmlResponse = xmlResponse;
                TraceBack = trackBack;

                return resp;


            }
            catch (Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, ex.StackTrace);
                return false;
            }
        }

        public bool CAEASolicitar(int nPeriod, short nQuince, ref string cNroCAE, ref string dFchDes, ref string dFchHas, ref string dFchTop, ref string dFchPro)
        {
            NumeroCAE = ""; FechaDesde = ""; FechaHasta = ""; FechaTope = ""; FechaProceso = "";
            try
            {
                bool resp = HelpersArcaCAEA.MetodoCAEA(GlobalSettings.MetCAEA.CAEASOLICITAR, TkValido, PathCache, Produccion, Cuit, nPeriod, nQuince, ref cNroCAE, ref dFchDes, ref dFchHas, ref dFchTop, ref dFchPro,
                     out int errCode, out string errDesc, out string xmlResponse, out string trackBack);

                SetError((GlobalSettings.Errors)errCode, errDesc, $"Errores Respuesta FECAEASolicitar {trackBack}");

                NumeroCAE = cNroCAE;
                FechaDesde = dFchDes;
                FechaHasta = dFchHas;
                FechaTope = dFchTop;
                FechaProceso = dFchPro;
                XmlResponse = xmlResponse;
                TraceBack = trackBack;

                return resp;


            }
            catch (Exception ex)
            {
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
                object objWSFEV1 = null;
                ////obtiene token y sign del archivo cache
                if (TkValido == null)
                    TkValido = HelpersArca.RecuperarTokenSign(HelpersArca.LeerCache(PathCache, service));


                //llama a ARCA FECAESOlicitar y manda el objeto creado
                //si devuelve error, setea el error y devuelve false
                dynamic respuesta;
                int errCode = 0;
                string errDesc = "";

                if (Produccion)
                    RegInformativoARCA(nPtoVta, nTipCom, sCAE, objWSFEV1, out respuesta);
                else
                    RegInformativoARCA_HOMO(nPtoVta, nTipCom, sCAE, objWSFEV1, out respuesta);

                // Verificar la respuesta
                string cae = ""; string vtoCae = ""; string result = ""; string reproc = ""; string xmlResponse = ""; string observ = "";
                HelpersArca.ProcesarRespuestaFactura(respuesta, ref errCode, ref errDesc, ref xmlResponse, ref cae, ref vtoCae, ref result, ref reproc, ref observ);

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

                if (ErrorDesc.Length == 0 && observ.Length > 0) ErrorDesc = observ;

                return true;

            }
            catch (Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"{TraceBack} {ex.StackTrace}");
                return false;
            }
        }

        //este metodo agrega al objeto CAEA 
        public void CAEACbteFchHsGen(string cFchCom)
        {

            try
            {
                //Convertir CAEADetRequest a FECAEDetRequest

                if (CAEADetReq == null)
                {

                    CAEADetReq = CAEDetRequest.CopyToDerived<CAEDetRequest, CAEADetRequest>();


                }
                CAEADetReq.CbteFchHsGen = cFchCom;
            }
            catch (Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"CAEACbteFchHsGen");
            }

        }
        #endregion
        public string GetVersion()
        {
            return $"1.1.37"; // Cambia esto según tu versión actual
        }

        #region [Metodos Autorización directa ARCA]
        private void AutorizarARCA(int nPtoVta, int nTipCom, object objWSFEV1, out object respuesta)
        {
            try
            {
                TraceBack = "Linea 1";
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

                TraceBack = "Linea 2";
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
                    ImpIVA = CAEDetRequest.ImpIVA,
                    ImpTrib = CAEDetRequest.ImpTrib,
                    MonId = CAEDetRequest.MonId,
                    MonCotiz = CAEDetRequest.MonCotiz,
                    //MonCotizSpecified = true,
                    //CondicionIVAReceptorId = CAEDetRequest.CondicionIvaReceptor
                };

                if (CAEDetRequest.Concepto == 2 || CAEDetRequest.Concepto == 3)
                {
                    TraceBack = "Linea 2.1";
                    detalleProd.FchServDesde = CAEDetRequest.FchServDesde;
                    detalleProd.FchServHasta = CAEDetRequest.FchServHasta;
                    detalleProd.FchVtoPago = CAEDetRequest.FchVtoPago;
                }

                TraceBack = "Linea 3";
                if (CAEDetRequest.Iva != null && CAEDetRequest.Iva.Count() > 0)
                {
                    TraceBack = "Linea 3.1";
                    detalleProd.Iva = CAEDetRequest.Iva.Select(alicIva => (Wsfev1.AlicIva)HelpersArca.ConvertAlicIva(alicIva, Produccion)).ToArray();
                }

                TraceBack = "Linea 4";
                if (CAEDetRequest.Tributos != null && CAEDetRequest.Tributos.Count() > 0)
                {
                    TraceBack = "Linea 4.1";
                    detalleProd.ImpTrib = CAEDetRequest.Tributos?.Sum(t => t.Importe) ?? 0; //CAEDetRequest.ImpTrib;
                    detalleProd.Tributos = CAEDetRequest.Tributos.Select(tributo => (Wsfev1.Tributo)HelpersArca.ConvertirTributos(tributo, Produccion)).ToArray();
                }
                TraceBack = "Linea 5";
                if (CAEDetRequest.ComprobantesAsociados != null && CAEDetRequest.ComprobantesAsociados.Length > 0)
                {
                    TraceBack = "Linea 5.1";
                    detalleProd.CbtesAsoc = CAEDetRequest.ComprobantesAsociados.Select(cbteAsoc => (Wsfev1.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, Produccion)).ToArray();
                }

                TraceBack = "Linea 6";
                if (CAEDetRequest.Opcionales != null && CAEDetRequest.Opcionales.Length > 0)
                {
                    TraceBack = "Linea 6.1";
                    detalleProd.Opcionales = CAEDetRequest.Opcionales.Select(opcional => (Wsfev1.Opcional)HelpersArca.ConvertirOpcionales(opcional, Produccion)).ToArray();
                }

                TraceBack = "Linea 7";
                var solicitudProd = new Wsfev1.FECAERequest
                {
                    FeCabReq = cabeceraProd, //CAbecera
                    FeDetReq = new Wsfev1.FECAEDetRequest[] { detalleProd } //detalle
                };

                TraceBack =  HelpersGlobal.SerializeObjectAXml(solicitudProd);

                //invocar FECAESolicitar del  wsfev1 
                objWSFEV1 = new Wsfev1Homo.Service();
                respuesta = ((dynamic)objWSFEV1).FECAESolicitar(authProd, solicitudProd);
                TraceBack = "Linea 8";
            }
            catch (Exception ex)
            {
                TraceBack = ex.StackTrace;
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"{TraceBack} {ex.StackTrace}");
                respuesta = null;
            }
        }
        private void RegInformativoARCA(int nPtoVta, int nTipCom, string sCAE, object objWSFEV1, out object respuesta)
        {
            try
            {
                TraceBack = "Linea 1";
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
                TraceBack = "Linea 2";
                //Convertir CAEADetRequest a FECAEDetRequest
                if (CAEADetReq == null)
                {
                    CAEADetReq = CAEDetRequest.CopyToDerived<CAEDetRequest, CAEADetRequest>();
                }
                CAEADetReq.CAEA = sCAE;

                Wsfev1.FECAEADetRequest detalleProd = new Wsfev1.FECAEADetRequest
                {
                    Concepto = CAEADetReq.Concepto,
                    DocTipo = CAEADetReq.DocTipo,
                    DocNro = CAEADetReq.DocNro,
                    CbteDesde = CAEADetReq.CbteDesde,
                    CbteHasta = CAEADetReq.CbteHasta,
                    CbteFch = CAEADetReq.CbteFch,
                    ImpTotal = CAEADetReq.ImpTotal,
                    ImpTotConc = CAEADetReq.ImpTotConc,
                    ImpNeto = CAEADetReq.ImpNeto,
                    ImpOpEx = CAEADetReq.ImpOpEx,
                    ImpIVA = Iva,
                    MonCotiz = CAEADetReq.MonCotiz,
                    //MonCotizSpecified = true,
                    MonId = CAEADetReq.MonId,
                    //CanMisMonExt = CAEDetRequest.CantidadMismaMonedaExt,                    
                    //CondicionIVAReceptorId = CAEADetReq.CondicionIvaReceptor,
                    CAEA = CAEADetReq.CAEA,
                    CbteFchHsGen = CAEADetReq.CbteFchHsGen
                };
                if (CAEADetReq.Concepto == 2 || CAEADetReq.Concepto == 3)
                {

                    TraceBack = "Linea 2.1";
                    detalleProd.FchServDesde = CAEADetReq.FchServDesde;
                    detalleProd.FchServHasta = CAEADetReq.FchServHasta;
                }
                if (CAEADetReq.FchVtoPago.Length > 0) detalleProd.FchVtoPago = CAEADetReq.FchVtoPago;

                TraceBack = "Linea 3";
                if (Iva > 0)
                {
                    TraceBack = "Linea 3.1";
                    detalleProd.Iva = CAEADetReq.Iva.Select(alicIva => (Wsfev1.AlicIva)HelpersArca.ConvertAlicIva(alicIva, Produccion)).ToArray();
                }
                TraceBack = "Linea 4";
                if (CAEADetReq.ImpTrib > 0)
                {
                    TraceBack = "Linea 4.1";
                    detalleProd.ImpTrib = CAEADetReq.ImpTrib;
                    detalleProd.Tributos = CAEADetReq.Tributos.Select(tributo => (Wsfev1.Tributo)HelpersArca.ConvertirTributos(tributo, Produccion)).ToArray();
                }
                TraceBack = "Linea 5";
                if (CAEADetReq.ComprobantesAsociados != null && CAEADetReq.ComprobantesAsociados.Count() > 0)
                {
                    TraceBack = "Linea 5.1";
                    detalleProd.CbtesAsoc = CAEADetReq.ComprobantesAsociados.Select(cbteAsoc => (Wsfev1.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, Produccion)).ToArray();
                }
                TraceBack = "Linea 6";

                if (CAEADetReq.Opcionales != null && CAEADetReq.Opcionales.Count() > 0)
                {
                    TraceBack = "Linea 6.1";
                    detalleProd.Opcionales = CAEADetReq.Opcionales.Select(opcional => (Wsfev1.Opcional)HelpersArca.ConvertirOpcionales(opcional, Produccion)).ToArray();
                }


                TraceBack = "Linea 7";
                var solicitudProd = new Wsfev1.FECAEARequest
                {
                    FeCabReq = cabeceraProd, //CAbecera
                    FeDetReq = new Wsfev1.FECAEADetRequest[] { detalleProd } //detalle
                };
                TraceBack = HelpersGlobal.SerializeObjectAXml(solicitudProd);

                //invocar FECAESolicitar del wsfev1 
                objWSFEV1 = new Wsfev1.Service();
                respuesta = ((dynamic)objWSFEV1).FECAEARegInformativo(authProd, solicitudProd);
                TraceBack = "Linea 8";
            }
            catch (Exception ex)
            {
                TraceBack = ex.StackTrace;
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"{TraceBack} {ex.StackTrace}");
                respuesta = null;
            }
        }


        private void AutorizarARCA_HOMO(int nPtoVta, int nTipCom, object objWSFEV1, out object respuesta)
        {
            try
            {
                TraceBack = "Linea 1";
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
                if (CAEDetRequest.Concepto == 2 || CAEDetRequest.Concepto == 3)
                {

                    TraceBack = "Linea 2.1";
                    detalleProd.FchServDesde = CAEDetRequest.FchServDesde;
                    detalleProd.FchServHasta = CAEDetRequest.FchServHasta;
                }
                if (CAEDetRequest.FchVtoPago.Length > 0) detalleProd.FchVtoPago = CAEDetRequest.FchVtoPago;

                TraceBack = "Linea 3";
                if (Iva > 0)
                {
                    TraceBack = "Linea 3.1";
                    detalleProd.Iva = CAEDetRequest.Iva.Select(alicIva => (Wsfev1Homo.AlicIva)HelpersArca.ConvertAlicIva(alicIva, Produccion)).ToArray();
                }
                TraceBack = "Linea 4";
                if (CAEDetRequest.Tributos != null && CAEDetRequest.Tributos.Count() > 0)
                {
                    TraceBack = "Linea 4.1";
                    detalleProd.ImpTrib = CAEDetRequest. Tributos?.Sum(t => t.Importe) ?? 0; //CAEDetRequest.ImpTrib;
                    detalleProd.Tributos = CAEDetRequest.Tributos.Select(tributo => (Wsfev1Homo.Tributo)HelpersArca.ConvertirTributos(tributo, Produccion)).ToArray();
                }
                TraceBack = "Linea 5";
                if (CAEDetRequest.ComprobantesAsociados != null && CAEDetRequest.ComprobantesAsociados.Count() > 0)
                {
                    TraceBack = "Linea 5.1";
                    detalleProd.CbtesAsoc = CAEDetRequest.ComprobantesAsociados.Select(cbteAsoc => (Wsfev1Homo.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, Produccion)).ToArray();
                }
                TraceBack = "Linea 6";

                if (CAEDetRequest.Opcionales != null && CAEDetRequest.Opcionales.Count() > 0)
                {
                    TraceBack = "Linea 6.1";
                    detalleProd.Opcionales = CAEDetRequest.Opcionales.Select(opcional => (Wsfev1Homo.Opcional)HelpersArca.ConvertirOpcionales(opcional, Produccion)).ToArray();
                }


                TraceBack = "Linea 7";
                var solicitudProd = new Wsfev1Homo.FECAERequest
                {
                    FeCabReq = cabeceraProd, //CAbecera
                    FeDetReq = new Wsfev1Homo.FECAEDetRequest[] { detalleProd } //detalle
                };
                TraceBack = HelpersGlobal.SerializeObjectAXml(solicitudProd);

                //invocar FECAESolicitar del wsfev1 
                objWSFEV1 = new Wsfev1Homo.Service();
                respuesta = ((dynamic)objWSFEV1).FECAESolicitar(authProd, solicitudProd);
                TraceBack = "Linea 8";
            }
            catch (Exception ex)
            {
                TraceBack = ex.StackTrace;
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"{TraceBack} {ex.StackTrace}");
                respuesta = null;
            }
        }

        private void RegInformativoARCA_HOMO(int nPtoVta, int nTipCom, string sCAE, object objWSFEV1, out object respuesta)
        {
            try
            {
                TraceBack = "Linea 1";
                var authProd = new Wsfev1Homo.FEAuthRequest
                {
                    Token = TkValido.Token,
                    Sign = TkValido.Sign,
                    Cuit = Convert.ToInt64(Cuit)
                };

                Wsfev1Homo.FECAEACabRequest cabeceraProd = new Wsfev1Homo.FECAEACabRequest
                {
                    CantReg = 1,
                    PtoVta = nPtoVta,
                    CbteTipo = nTipCom
                };
                TraceBack = "Linea 2";
                //Convertir CAEADetRequest a FECAEDetRequest
                if (CAEADetReq == null)
                {
                    CAEADetReq = CAEDetRequest.CopyToDerived<CAEDetRequest, CAEADetRequest>();
                }
                CAEADetReq.CAEA = sCAE;

                Wsfev1Homo.FECAEADetRequest detalleProd = new Wsfev1Homo.FECAEADetRequest
                {
                    Concepto = CAEADetReq.Concepto,
                    DocTipo = CAEADetReq.DocTipo,
                    DocNro = CAEADetReq.DocNro,
                    CbteDesde = CAEADetReq.CbteDesde,
                    CbteHasta = CAEADetReq.CbteHasta,
                    CbteFch = CAEADetReq.CbteFch,
                    ImpTotal = CAEADetReq.ImpTotal,
                    ImpTotConc = CAEADetReq.ImpTotConc,
                    ImpNeto = CAEADetReq.ImpNeto,
                    ImpOpEx = CAEADetReq.ImpOpEx,
                    ImpIVA = Iva,
                    MonCotiz = CAEADetReq.MonCotiz,
                    MonCotizSpecified = true,
                    MonId = CAEADetReq.MonId,
                    //CanMisMonExt = CAEDetRequest.CantidadMismaMonedaExt,                    
                    CondicionIVAReceptorId = CAEADetReq.CondicionIvaReceptor,
                    CAEA = CAEADetReq.CAEA,
                    CbteFchHsGen = CAEADetReq.CbteFchHsGen
                };
                if (CAEADetReq.Concepto == 2 || CAEADetReq.Concepto == 3)
                {

                    TraceBack = "Linea 2.1";
                    detalleProd.FchServDesde = CAEADetReq.FchServDesde;
                    detalleProd.FchServHasta = CAEADetReq.FchServHasta;
                }
                if (CAEADetReq.FchVtoPago.Length > 0) detalleProd.FchVtoPago = CAEADetReq.FchVtoPago;

                TraceBack = "Linea 3";
                if (Iva > 0)
                {
                    TraceBack = "Linea 3.1";
                    detalleProd.Iva = CAEADetReq.Iva.Select(alicIva => (Wsfev1Homo.AlicIva)HelpersArca.ConvertAlicIva(alicIva, Produccion)).ToArray();
                }
                TraceBack = "Linea 4";
                if (CAEADetReq.ImpTrib > 0)
                {
                    TraceBack = "Linea 4.1";
                    detalleProd.ImpTrib = CAEADetReq.ImpTrib;
                    detalleProd.Tributos = CAEADetReq.Tributos.Select(tributo => (Wsfev1Homo.Tributo)HelpersArca.ConvertirTributos(tributo, Produccion)).ToArray();
                }
                TraceBack = "Linea 5";
                if (CAEADetReq.ComprobantesAsociados != null && CAEADetReq.ComprobantesAsociados.Count() > 0)
                {
                    TraceBack = "Linea 5.1";
                    detalleProd.CbtesAsoc = CAEADetReq.ComprobantesAsociados.Select(cbteAsoc => (Wsfev1Homo.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, Produccion)).ToArray();
                }
                TraceBack = "Linea 6";

                if (CAEADetReq.Opcionales != null && CAEADetReq.Opcionales.Count() > 0)
                {
                    TraceBack = "Linea 6.1";
                    detalleProd.Opcionales = CAEADetReq.Opcionales.Select(opcional => (Wsfev1Homo.Opcional)HelpersArca.ConvertirOpcionales(opcional, Produccion)).ToArray();
                }


                TraceBack = "Linea 7";
                var solicitudProd = new Wsfev1Homo.FECAEARequest
                {
                    FeCabReq = cabeceraProd, //CAbecera
                    FeDetReq = new Wsfev1Homo.FECAEADetRequest[] { detalleProd } //detalle
                };
                TraceBack = HelpersGlobal.SerializeObjectAXml(solicitudProd);

                //invocar FECAESolicitar del wsfev1 
                objWSFEV1 = new Wsfev1Homo.Service();
                respuesta = ((dynamic)objWSFEV1).FECAEARegInformativo(authProd, solicitudProd);
                TraceBack = "Linea 8";
            }
            catch (Exception ex)
            {
                TraceBack = ex.StackTrace;
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"{TraceBack} {ex.StackTrace}");
                respuesta = null;
            }
        }
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
                // Verificar que la fecha esté en el formato "yyyyMMdd"
                if (!DateTime.TryParseExact(cFchCom, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha))
                {
                    SetError(GlobalSettings.Errors.FORMAT_ERROR, "La fecha comprobante debe estar en el formato 'yyyyMMdd'.", "AgergaFactura 1");
                    return;
                }
                if (cSerDes.Length > 0 && !DateTime.TryParseExact(cSerDes, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha1))
                {
                    SetError(GlobalSettings.Errors.FORMAT_ERROR, "La fecha desde servicio debe estar en el formato 'yyyyMMdd'.", "AgregaFactura 2");
                    return;
                }
                if (cSerHas.Length > 0 && !DateTime.TryParseExact(cSerHas, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha2))
                {
                    SetError(GlobalSettings.Errors.FORMAT_ERROR, "La fecha hasta servicio debe estar en el formato 'yyyyMMdd'.", "AgregaFactura 3");
                    return;
                }
                if (cSerVto.Length > 0 && !DateTime.TryParseExact(cSerVto, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha3))
                {
                    SetError(GlobalSettings.Errors.FORMAT_ERROR, "La fecha vencimiento servicio Desde debe estar en el formato 'yyyyMMdd'.", "Agrega Factura 4");
                    return;
                }

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
            }
            catch (Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"CAEDetRequest {HelpersGlobal.SerializeObjectAXml(CAEDetRequest)}");
            }

        }

        public void AgregaIVA(int codigoAlicuota, double importeBase, double importeIVA)
        {
            try
            {
                InnercorArcaModels.AlicIva nuevaAlicuota = new InnercorArcaModels.AlicIva()
                {
                    BaseImp = Math.Abs(importeBase),
                    Id = codigoAlicuota,
                    Importe = Math.Abs(importeIVA)
                };

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
                }

                // Acumular valores en la clase
                Iva += importeIVA;
                Neto += importeBase;
            }
            catch (Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"Cantidad AliccIvas {CAEDetRequest.Iva.Count()} : {codigoAlicuota} - {importeBase} - {importeIVA} .. {ex.StackTrace}");
            }
        }

        public void AgregaOpcional(string codigo, string valor)
        {
            try
            {
                InnercorArcaModels.Opcional nuevoOpcional = new InnercorArcaModels.Opcional()
                {
                    Id = codigo,
                    Valor = valor
                };

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
                }
            }
            catch (Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"Cantidad Opcionales {CAEDetRequest.Opcionales.Count()}: {codigo} - {valor} .. {ex.StackTrace}");
            }
        }
        public void AgregaTributo(short codimp, string descri, double impbase, double alicuo, double import)
        {
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
                }
            }
            catch (Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"Cantidad Tributos {CAEDetRequest.Tributos.Count()} : {codimp} - {descri} - {impbase} - {alicuo} - {import} .. {ex.StackTrace}");
            }
        }

        public void AgregaCompAsoc(int nTipCmp, int nPtoVta, int nNroCmp, Int64 nNroCuit, string dFchCmp)
        {
            try
            {
                // Verificar que la fecha esté en el formato "yyyyMMdd"
                if (!DateTime.TryParseExact(dFchCmp, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha))
                {
                    SetError(GlobalSettings.Errors.FORMAT_ERROR, "La fecha debe estar en el formato 'yyyyMMdd'.", $"Error Fecha AgregaCompAsoc {dFchCmp}");
                    return;
                }

                InnercorArcaModels.CbteAsoc nuevoComprobanteAsociado = new InnercorArcaModels.CbteAsoc()
                {
                    Tipo = nTipCmp,
                    PtoVta = nPtoVta,
                    Nro = nNroCmp,
                    Cuit = nNroCuit.ToString(), // Convertir  Integer a string
                    CbteFch = dFchCmp//.ToString("yyyyMMdd") // Formato de fecha esperado por la API
                };

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
                }
            }
            catch (Exception ex)
            {
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, $"Cantidad Comprobantes Asociados {CAEDetRequest.ComprobantesAsociados.Count()}: {nTipCmp} - {nPtoVta} - {nNroCmp} - {nNroCuit} - {dFchCmp} .. {ex.StackTrace} ");
            }
        }
        #endregion [MÉTODOS PARA COMPLETAR EL OBJETO ]




    }
}
