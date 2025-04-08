using InnercorArca.V1.Helpers;
using System;
using System.Linq;
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
                if ((caeDetRequest.Concepto == 2 || caeDetRequest.Concepto == 3))
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 2.1 Concepto " + caeDetRequest.Concepto);
                    detalleProd.FchServDesde = caeDetRequest.FchServDesde;
                    detalleProd.FchServHasta = caeDetRequest.FchServHasta;
                }

                if (!string.IsNullOrEmpty(caeDetRequest.FchVtoPago))
                {
                    detalleProd.FchVtoPago = caeDetRequest.FchVtoPago;
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 3 Iva " + caeDetRequest.Iva?.Length);
                if (caeDetRequest.Iva?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 3.1 " + caeDetRequest.Iva?.Length);
                    detalleProd.ImpIVA = caeDetRequest.Iva.Sum(t => t.Importe);
                    detalleProd.Iva = caeDetRequest.Iva.Select(alicIva => (Wsfev1Homo.AlicIva)HelpersArca.ConvertAlicIva(alicIva, false)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 4 Tributos " + caeDetRequest.Tributos?.Length);
                if (caeDetRequest.Tributos?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 4.1 " + caeDetRequest.Tributos?.Length);
                    detalleProd.ImpTrib = caeDetRequest.Tributos.Sum(t => t.Importe);
                    detalleProd.Tributos = caeDetRequest.Tributos.Select(tributo => (Wsfev1Homo.Tributo)HelpersArca.ConvertirTributos(tributo, false)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 5 Comp Asociados " + caeDetRequest.ComprobantesAsociados?.Length);
                if (caeDetRequest.ComprobantesAsociados?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 5.1 " + caeDetRequest.ComprobantesAsociados?.Length);
                    detalleProd.CbtesAsoc = caeDetRequest.ComprobantesAsociados.Select(cbteAsoc => (Wsfev1Homo.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, false)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 6 Opcionales " + caeDetRequest.Opcionales?.Length);
                if (caeDetRequest.Opcionales?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 6.1 Opcionales " + caeDetRequest.Opcionales?.Length);
                    detalleProd.Opcionales = caeDetRequest.Opcionales.Select(opcional => (Wsfev1Homo.Opcional)HelpersArca.ConvertirOpcionales(opcional, false)).ToArray();
                }


                if (habilitaLog) HelpersLogger.Escribir("Linea 7");

                var solicitud= new Wsfev1Homo.FECAERequest
                {
                    FeCabReq = cabeceraProd,
                    FeDetReq = new[] { detalleProd }
                };

                traceBack = HelpersGlobal.SerializeObjectAXml(solicitud);
                if (habilitaLog) HelpersLogger.Escribir($"Linea 7.1 {traceBack}");

                var ws = new Wsfev1Homo.Service();
                respuesta = ((dynamic)ws).FECAESolicitar(authProd, solicitud);

                if (habilitaLog) HelpersLogger.Escribir("Respuesta: " + HelpersGlobal.SerializeObjectAXml(respuesta));

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
