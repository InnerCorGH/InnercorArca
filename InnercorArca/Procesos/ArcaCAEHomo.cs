using InnercorArca.V1.Helpers;
using System;
using static InnercorArca.V1.Helpers.InnercorArcaModels;

namespace InnercorArca.V1.Procesos
{
    public static class ArcaCAEHOMO
    {
        public static void AutorizarARCA(bool habilitaLog, string cuit,  
            CAEDetRequest caeDetRequest, double iva, CacheResult tkValido,
            int nPtoVta, int nTipCom, out object respuesta)
        {
            respuesta = null;
            string traceBack = "";
            try
            {
                if (habilitaLog) HelpersLogger.Escribir("Inicio AutorizarARCA_HOMO");

                var authProd = new Wsfev1Homo.FEAuthRequest
                {
                    Token = tkValido.Token,
                    Sign = tkValido.Sign,
                    Cuit = Convert.ToInt64(cuit)
                };

                var cabeceraProd = new Wsfev1Homo.FECAECabRequest
                {
                    CantReg = 1,
                    PtoVta = nPtoVta,
                    CbteTipo = nTipCom
                };

                traceBack = "Linea 2";
                if (habilitaLog)
                {
                    HelpersLogger.Escribir($"auth {HelpersGlobal.SerializeToXml(authProd)} cabeceraProd {HelpersGlobal.SerializeToXml(cabeceraProd)} ");
                }

                var detalleProd = new Wsfev1Homo.FECAEDetRequest
                {
                    Concepto = caeDetRequest.Concepto,
                    DocTipo = caeDetRequest.DocTipo,
                    DocNro = caeDetRequest.DocNro,
                    CbteDesde = caeDetRequest.CbteDesde,
                    CbteHasta = caeDetRequest.CbteHasta,
                    CbteFch = caeDetRequest.CbteFch,
                    ImpTotal = caeDetRequest.ImpTotal,
                    ImpTotConc = caeDetRequest.ImpTotConc,
                    ImpNeto = caeDetRequest.ImpNeto,
                    ImpOpEx = caeDetRequest.ImpOpEx,
                    ImpIVA = iva,
                    MonCotiz = caeDetRequest.MonCotiz,
                    MonCotizSpecified = true,
                    MonId = caeDetRequest.MonId,
                    CondicionIVAReceptorId = caeDetRequest.CondicionIvaReceptor,
                    CanMisMonExt = caeDetRequest.CantidadMismaMonedaExt,
                };

                if (habilitaLog)
                {
                    HelpersLogger.Escribir("Detalle generado");
                }

                var solicitudProd = new Wsfev1Homo.FECAERequest
                {
                    FeCabReq = cabeceraProd,
                    FeDetReq = new[] { detalleProd }
                };

                traceBack = HelpersGlobal.SerializeObjectAXml(solicitudProd);
                if (habilitaLog) HelpersLogger.Escribir($"Linea 7 {traceBack}");

                var ws = new Wsfev1Homo.Service();
                respuesta = ((dynamic)ws).FECAESolicitar(authProd, solicitudProd);

                traceBack = HelpersGlobal.SerializeObjectAXml(respuesta);
                if (habilitaLog) HelpersLogger.Escribir($"Linea 8 {traceBack}");
            }
            catch (Exception ex)
            {
                if (habilitaLog) HelpersLogger.Escribir($"Error AutorizarARCA_HOMO Exception:{ex.Message} {traceBack} {ex.StackTrace}");
                throw new Exception($"Error AutorizarARCA_HOMO Exception {ex.Message} {ex.StackTrace}"); 
            }
        }

        public static Wsfev1Homo.FEAuthRequest FEAuthRequest_Set (string Token, string Sign, long CUIT)
        {
            try
            {
                Wsfev1Homo.FEAuthRequest FEAuthRequest = new Wsfev1Homo.FEAuthRequest
                {
                    Token = Token,
                    Sign = Sign,
                    Cuit = CUIT
                };

                return FEAuthRequest;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
