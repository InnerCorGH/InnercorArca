using InnercorArca.V1.Helpers;
using System;
using System.Collections.Generic;
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
        bool CmpConsultar(int nTipCom, int nPtoVta, long nNroCmp, ref string @cNroCAE, ref string @cVtoCAE);
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
        bool AutorizarRespuesta(int nIndice, out string @cNroCAE, ref string cVtoCAE, ref string cResult, ref string cReproc);

        [DispId(20)]
        string GetVersion();

        [DispId(21)]
        string GetAppServerStatus();

        [DispId(22)]
        string GetDbServerStatus();
        [DispId(23)]
        string GetAuthServerStatus();
        [DispId(24)]
        int GetUltimoNumero();
        [DispId(25)]
        string GetNumeroCAE();
        [DispId(26)]
        string GetVencimientoCAE();
        [DispId(27)]
        string GetResultado();
        [DispId(28)]
        string GetReprocesar();


    }

    /// <summary>
    /// Implementación de wsfev1, expuesta como COM.
    /// </summary>
    [Guid("66666666-7777-8888-9999-000000000000")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class wsfev1 : IIwsfev1
    {

        readonly string dllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public int ErrorCode { get; private set; }
        public string ErrorDesc { get; private set; } = string.Empty;
        public string Cuit { get; set; } = string.Empty;
        public string URL { get; set; } = string.Empty;
        public string XmlResponse { get; private set; } = string.Empty;
        public string Excepcion { get; private set; } = string.Empty;
        public string TraceBack { get; private set; } = string.Empty;

        internal bool Produccion { get; private set; } = false;
        internal string PathCache { get; private set; } = string.Empty;

        internal string NumeroCAE { get; set; }
        internal string VencimientoCAE { get; set; }

        internal string Result { get; set; }
        internal string Reproc { get; set; }


        internal InnercorArcaModels.CAEDetRequest CAEDetRequest { get; set; }
        internal List<InnercorArcaModels.AlicIva> AlicIvas { get; set; }
        internal List<InnercorArcaModels.Opcional> Opcionales { get; set; }
        internal List<InnercorArcaModels.Tributo> Tributos { get; set; }
        internal List<InnercorArcaModels.CbteAsoc> ComprobantesAsociados { get; set; }



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

        //variables de paso Referencia que no los devuelve
        public int UltimoNumero_ { get; set; }

        public wsfev1()
        {
            // Si la variable es null, inicializarla
            if (TkValido == null)
            {
                TkValido = new CacheResult();
            }
            if (AlicIvas == null) AlicIvas = new List<InnercorArcaModels.AlicIva>(); // Initialize the list
            if (Opcionales == null) Opcionales = new List<InnercorArcaModels.Opcional>(); // Initialize the list
            if (Tributos == null) Tributos = new List<InnercorArcaModels.Tributo>(); // Initialize the list
            if (ComprobantesAsociados == null) ComprobantesAsociados = new List<InnercorArcaModels.CbteAsoc>(); // Initialize the list

        }
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
                    string cache = HelpersArca.LeerCache(PathCache);
                    if (!string.IsNullOrEmpty(cache))
                    {
                        // Verificar si el token es válido
                        if (HelpersArca.ValidarToken(cache))
                        {
                            TkValido = HelpersArca.RecuperarTokenSign(cache);

                            HelpersArca.SeteaAuthRequest(Produccion, ref feAuthRequest, TkValido, Convert.ToInt64(Cuit));
                            return true;
                        }
                    }
                }
                // Cargar el certificado y la clave privada directamente (sin usar .pfx)
                X509Certificate2 certificate = HelpersCert.LoadCertificateAndPrivateKey(pathCRT, pathKey);
                if (certificate == null)
                {
                    SetError(InnercorArcaModels.Errors.CERT_ERROR, "No se pudo cargar el certificado y la clave privada.", "Login 1");
                    return false;
                }


                if (!certificate.HasPrivateKey)
                {
                    SetError(InnercorArcaModels.Errors.CERT_ERROR, "El certificado no contiene clave privada.", "Login 2");
                    return false;
                }


                LoginTicket objTicketRespuesta = new LoginTicket();
                string response = objTicketRespuesta.ObtenerLoginTicketResponse("wsfe", urlWSAA, pathCRT, pathKey, true, Produccion);

                if (string.IsNullOrEmpty(response))
                {
                    SetError(InnercorArcaModels.Errors.WSAA_ERROR, "No se pudo obtener el CMS.", "Login 3");
                    return false;
                }

                // Guardar el CMS en un archivo .cache
                TkValido = HelpersArca.GenerarCache(PathCache, response);
                if (TkValido != null)
                {
                    HelpersArca.SeteaAuthRequest(Produccion, ref feAuthRequest, TkValido, Convert.ToInt64(Cuit));

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                SetError(InnercorArcaModels.Errors.EXCEPTION, ex.Message, ex.StackTrace);
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
                    SetError(InnercorArcaModels.Errors.EXCEPTION, "Error al crear objeto Dummy.", "Dummy 1");
                    return false;
                }

                dynamic dummy = objDummy;
                cAppServerStatus = "OK"; // dummy.AppServer??"OK";
                cAuthServerStatus = "OK";// dummy.AuthServer??"OK";
                cDbServerStatus = "OK"; // dummy.DbServer ?? "OK";

                AppServerStatus_ = cAppServerStatus;
                AuthServerStatus_ = cAuthServerStatus;
                DbServerStatus_ = cDbServerStatus;

                return cAppServerStatus == "OK" && cAuthServerStatus == "OK" && cDbServerStatus == "OK";
            }
            catch (Exception ex)
            {
                
                SetError(Errors.EXCEPTION, ex.Message, ex.StackTrace );
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


                NumeroCAE = string.Empty;
                VencimientoCAE = DateTime.MinValue.ToString("yyyyMMdd");
                Result = string.Empty;
                Reproc = string.Empty;

                CAEDetRequest = null;
                AlicIvas = null;
                Opcionales = null;
                Tributos = null;
                ComprobantesAsociados = null;

                Neto = 0;
                Iva = 0;
            }
            catch (Exception ex)
            {
                SetError(InnercorArcaModels.Errors.EXCEPTION, $"Error al invocar Reset {ex.Message}", ex.StackTrace);
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
                    TkValido = HelpersArca.RecuperarTokenSign(HelpersArca.LeerCache(PathCache));

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

                SetError((Errors)errCode, errDesc, $"RecuperaLastCMP Producción {Produccion} {objWSFEV1.GetType()}");
                XmlResponse = xmlResponse;
                UltimoNumero_ = nUltNro;
                return true;
            }
            catch (Exception ex)
            {
                SetError(InnercorArcaModels.Errors.EXCEPTION, ex.Message, ex.Message);
                UltimoNumero_ = -1;
                return false;
            }
        }


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
                    TkValido = HelpersArca.RecuperarTokenSign(HelpersArca.LeerCache(PathCache));

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
                    SetError((Errors)errCode, errDesc, "Errores Respuesta FECompConsultar");
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

                XmlResponse = HelpersArca.SerializeObjectAXml(response);
                return true;
            }
            catch (Exception ex)
            {
                SetError(InnercorArcaModels.Errors.EXCEPTION, ex.Message, ex.StackTrace);
                return false;
            }
        }



        public bool Autorizar(int nPtoVta, int nTipCom)
        {// llama al FECAESOlcicitar 


            try
            {
                object objWSFEV1 = null;
                ////obtiene token y sign del archivo cache
                if (TkValido == null)
                    TkValido = HelpersArca.RecuperarTokenSign(HelpersArca.LeerCache(PathCache));


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
                string cae = ""; DateTime vtoCae = DateTime.MinValue; string result = ""; string reproc = ""; string xmlResponse = "";
                HelpersArca.ProcesarRespuestaFactura(respuesta, ref errCode, ref errDesc, ref xmlResponse, ref cae, ref vtoCae, ref result, ref reproc);

                Result = result;
                Reproc = reproc;
                SetError((Errors)errCode, errDesc, $"Autorizar  {TraceBack}");

                XmlResponse = xmlResponse;

                return false;
            }
            catch (Exception ex)
            { 
                SetError(InnercorArcaModels.Errors.EXCEPTION, ex.Message , $"{TraceBack} {ex.StackTrace}");
                return false;
            }
        }


        public bool AutorizarRespuesta(int nIndice, out string cNroCAE, ref string cVtoCAE, ref string cResult, ref string cReproc)
        {
            cNroCAE = "";
            cVtoCAE = "";
            try
            {
                cResult = Result;
                cReproc = Reproc;
                XmlResponse = XmlResponse;


                if (Result == "A")
                {
                    cNroCAE = NumeroCAE;
                    cVtoCAE = VencimientoCAE;

                    return true;
                }
                else
                {
                    cNroCAE = "";
                    cVtoCAE = "";

                    return false;

                }
            }
            catch (Exception ex)
            { 
                SetError(InnercorArcaModels.Errors.EXCEPTION, ex.Message, ex.StackTrace);
                return false;
            }
        }


        public string GetVersion()
        {
            return $"1.1.20"; // Cambia esto según tu versión actual
        }

        #region [Metodos Autirzacion directa ARCA]
        private void AutorizarARCA(int nPtoVta, int nTipCom, object objWSFEV1, out object respuesta)
        {
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

            //Convertir CAEDetRequest a FECAEDetRequest
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
                FchServDesde = CAEDetRequest.FchServDesde,
                FchServHasta = CAEDetRequest.FchServHasta,
                FchVtoPago = CAEDetRequest.FchVtoPago,

                // Convertir listas a arrays correctamente
                Iva = AlicIvas.Select(alicIva => (Wsfev1.AlicIva)HelpersArca.ConvertAlicIva(alicIva, Produccion)).ToArray(),
                Tributos = Tributos.Select(tributo => (Wsfev1.Tributo)HelpersArca.ConvertirTributos(tributo, Produccion)).ToArray(),
                Opcionales = Opcionales.Select(opcional => (Wsfev1.Opcional)HelpersArca.ConvertirOpcionales(opcional, Produccion)).ToArray(),
                CbtesAsoc = ComprobantesAsociados.Select(cbteAsoc => (Wsfev1.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, Produccion)).ToArray()


            };

            var solicitudProd = new Wsfev1.FECAERequest
            {
                FeCabReq = cabeceraProd, //CAbecera
                FeDetReq = new Wsfev1.FECAEDetRequest[] { detalleProd } //detalle
            };

            //invocar FECAESolicitar del  wsfev1 
            objWSFEV1 = new Wsfev1Homo.Service();
            respuesta = ((dynamic)objWSFEV1).FECAESolicitar(authProd, solicitudProd);

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
                    FchServDesde = CAEDetRequest.FchServDesde,
                    FchServHasta = CAEDetRequest.FchServHasta,
                    FchVtoPago = CAEDetRequest.FchVtoPago,
                    CondicionIVAReceptorId = CAEDetRequest.CondicionIvaReceptor

                };



                TraceBack = "Linea 3";
                if (Iva > 0)
                {
                    TraceBack = "Linea 3.1";
                    detalleProd.Iva = AlicIvas.Select(alicIva => (Wsfev1Homo.AlicIva)HelpersArca.ConvertAlicIva(alicIva, Produccion)).ToArray();
                }
                TraceBack = "Linea 4";
                if (CAEDetRequest.ImpTrib > 0)
                {
                    detalleProd.ImpTrib = CAEDetRequest.ImpTrib;
                    detalleProd.Tributos = Tributos.Select(tributo => (Wsfev1Homo.Tributo)HelpersArca.ConvertirTributos(tributo, Produccion)).ToArray();
                }
                TraceBack = "Linea 5";
                if (CAEDetRequest.ComprobantesAsociados != null && CAEDetRequest.ComprobantesAsociados.Length > 0)
                {
                    detalleProd.CbtesAsoc = ComprobantesAsociados.Select(cbteAsoc => (Wsfev1Homo.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, Produccion)).ToArray();
                }
                TraceBack = "Linea 6";

                if (CAEDetRequest.Opcionales != null && CAEDetRequest.Opcionales.Length > 0)
                {
                    detalleProd.Opcionales = Opcionales.Select(opcional => (Wsfev1Homo.Opcional)HelpersArca.ConvertirOpcionales(opcional, Produccion)).ToArray();
                }
                TraceBack = "Linea 7";
                var solicitudProd = new Wsfev1Homo.FECAERequest
                {
                    FeCabReq = cabeceraProd, //CAbecera
                    FeDetReq = new Wsfev1Homo.FECAEDetRequest[] { detalleProd } //detalle
                };
                //TraceBack = HelpersArca.SerializeObjectAXml(solicitudProd);
                //invocar FECAESolicitar del wsfev1 
                objWSFEV1 = new Wsfev1Homo.Service();
                respuesta = ((dynamic)objWSFEV1).FECAESolicitar(authProd, solicitudProd);
                TraceBack = "7";
            }
            catch (Exception ex)
            {
                TraceBack = ex.StackTrace;
                SetError(InnercorArcaModels.Errors.EXCEPTION, ex.Message, $"{TraceBack} {ex.StackTrace}");
                respuesta = null;
            }
        }
        #endregion
        #region[Metodos para Setear y Devolver valores ]


        private void SetError(InnercorArcaModels.Errors errorCode, string errorDesc, string traceBack)
        {
            ErrorCode = (int)errorCode;
            ErrorDesc = errorDesc;
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
            return UltimoNumero_;
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
                    SetError(InnercorArcaModels.Errors.FORMAT_ERROR, "La fecha comprobante debe estar en el formato 'yyyyMMdd'.", "AgergaFactura 1");
                    return;
                }
                if (cSerDes.Length > 0 && !DateTime.TryParseExact(cSerDes, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha1))
                {
                    SetError(InnercorArcaModels.Errors.FORMAT_ERROR, "La fecha desde servicio debe estar en el formato 'yyyyMMdd'.","AgregaFactura 2");
                    return;
                }
                if (cSerHas.Length > 0 && !DateTime.TryParseExact(cSerHas, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha2))
                {
                    SetError(InnercorArcaModels.Errors.FORMAT_ERROR, "La fecha hasta servicio debe estar en el formato 'yyyyMMdd'.", "AgregaFactura 3");
                    return;
                }
                if (cSerVto.Length > 0 && !DateTime.TryParseExact(cSerVto, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha3))
                {
                    SetError(InnercorArcaModels.Errors.FORMAT_ERROR, "La fecha vencimiento servicio Desde debe estar en el formato 'yyyyMMdd'.", "Agrega Factura 4");
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
                SetError(InnercorArcaModels.Errors.EXCEPTION, ex.Message, $"CAEDetRequest {HelpersArca.SerializeObjectAXml(CAEDetRequest)}");
            }

        }

        public void AgregaIVA(int codigoAlicuota, double importeBase, double importeIVA)
        {
            try
            {


                InnercorArcaModels.AlicIva nuevaAlicuota = new InnercorArcaModels.AlicIva()
                {
                    BaseImp = Math.Abs(importeBase),
                    Id = (codigoAlicuota == 5) ? 5 : 4,
                    Importe = Math.Abs(importeIVA)
                };

                // Agregar la nueva alícuota a la lista de IVA
                if (nuevaAlicuota != null)
                    AlicIvas.Add((InnercorArcaModels.AlicIva)nuevaAlicuota);

                // Acumular valores en la clase
                Iva += importeIVA;
                Neto += importeBase;
            }
            catch (Exception ex)
            {
                SetError(InnercorArcaModels.Errors.EXCEPTION, ex.Message, $"Cantidad AliccIvas {AlicIvas.Count} : {codigoAlicuota} - {importeBase} - {importeIVA}");
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
                    Opcionales.Add(nuevoOpcional);
            }
            catch (Exception ex)
            { 
                SetError(InnercorArcaModels.Errors.EXCEPTION, ex.Message, $"Cantidad Opcionales {Opcionales.Count}: {codigo} - {valor}");
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
                    Tributos.Add(nuevoTributo);
            }
            catch (Exception ex)
            {
                SetError(InnercorArcaModels.Errors.EXCEPTION, ex.Message, $"Cantidad Tributos {Tributos.Count} : {codimp} - {descri} - {impbase} - {alicuo} - {import}");
            }
        }

        public void AgregaCompAsoc(int nTipCmp, int nPtoVta, int nNroCmp, Int64 nNroCuit, string dFchCmp)
        {
            try
            {
                // Verificar que la fecha esté en el formato "yyyyMMdd"
                if (!DateTime.TryParseExact(dFchCmp, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha))
                {
                    SetError(InnercorArcaModels.Errors.FORMAT_ERROR, "La fecha debe estar en el formato 'yyyyMMdd'.", $"Error Fecha AgregaCompAsoc {dFchCmp}");
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
                    ComprobantesAsociados.Add(nuevoComprobanteAsociado);
            }
            catch (Exception ex)
            { 
                SetError(InnercorArcaModels.Errors.EXCEPTION, ex.Message, $"Cantidad Comprobantes Asociados {ComprobantesAsociados.Count}: {nTipCmp} - {nPtoVta} - {nNroCmp} - {nNroCuit} - {dFchCmp} ");
            }
        }
        #endregion [MÉTODOS PARA COMPLETAR EL OBJETO ]

    }
}
