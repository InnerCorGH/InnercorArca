
using InnercorArca.V1.Helpers;
using System;
using System.Linq;
using static InnercorArca.V1.Helpers.InnercorArcaModels;

namespace InnercorArca.V1.Procesos
{
    public static class ArcaCAE
    {

        public static void AutorizarARCA(bool habilitaLog, string cuit, 
            CAEDetRequest caeDetRequest, double iva, CacheResult tkValido,
            int nPtoVta, int nTipCom, out object respuesta)
        {
            respuesta = null;
            string traceBack = "";
            try
            {
                if (habilitaLog) HelpersLogger.Escribir("Inicio AutorizarARCA");

                var authProd = new Wsfev1.FEAuthRequest
                {
                    Token = tkValido.Token,
                    Sign = tkValido.Sign,
                    Cuit = Convert.ToInt64(cuit)
                };

                var cabeceraProd = new Wsfev1.FECAECabRequest
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

                var detalleProd = new Wsfev1.FECAEDetRequest
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
                    ImpTrib = caeDetRequest.ImpTrib,
                    MonId = caeDetRequest.MonId,
                    MonCotiz = caeDetRequest.MonCotiz
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
                    detalleProd.Iva = caeDetRequest.Iva.Select(alicIva => (Wsfev1.AlicIva)HelpersArca.ConvertAlicIva(alicIva, true)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 4 Tributos " + caeDetRequest.Tributos?.Length);
                if (caeDetRequest.Tributos?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 4.1 " + caeDetRequest.Tributos?.Length);
                    detalleProd.ImpTrib = caeDetRequest.Tributos.Sum(t => t.Importe);
                    detalleProd.Tributos = caeDetRequest.Tributos.Select(tributo => (Wsfev1.Tributo)HelpersArca.ConvertirTributos(tributo, true)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 5 Comp Asociados " + caeDetRequest.ComprobantesAsociados?.Length);
                if (caeDetRequest.ComprobantesAsociados?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 5.1 " + caeDetRequest.ComprobantesAsociados?.Length);
                    detalleProd.CbtesAsoc = caeDetRequest.ComprobantesAsociados.Select(cbteAsoc => (Wsfev1.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, true)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 6 Opcionales " + caeDetRequest.Opcionales?.Length);
                if (caeDetRequest.Opcionales?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 6.1 Opcionales " + caeDetRequest.Opcionales?.Length);
                    detalleProd.Opcionales = caeDetRequest.Opcionales.Select(opcional => (Wsfev1.Opcional)HelpersArca.ConvertirOpcionales(opcional, true)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 7");
                var solicitud = new Wsfev1.FECAERequest
                {
                    FeCabReq = cabeceraProd,
                    FeDetReq = new[] { detalleProd }
                };


                traceBack = HelpersGlobal.SerializeObjectAXml(solicitud);
                if (habilitaLog) HelpersLogger.Escribir($"Linea 7.1 {traceBack}");

                var ws = new Wsfev1.Service();
                respuesta = ((dynamic)ws).FECAESolicitar(authProd, solicitud);

                if (habilitaLog) HelpersLogger.Escribir("Respuesta: " + HelpersGlobal.SerializeObjectAXml(respuesta));
            }
            catch (Exception ex)
            {
                if (habilitaLog) HelpersLogger.Escribir($"Error AutorizarARCA Exception {ex.Message} {ex.StackTrace}");
                throw new Exception ($"Error AutorizarARCA Exception {ex.Message} {ex.StackTrace}");
            }
        }



        public static void RegInformativoARCA(bool habilitaLog, string cuit, 
                CAEDetRequest caeDetRequest, double dIva, string sCAE, string cbteFchGen,
                CacheResult tkValido, int nPtoVta, int nTipCom, out object respuesta)
        {
            try
            {
                if (habilitaLog) HelpersLogger.Escribir("Inicio RegInformativoARCA ");

                var authProd = new Wsfev1.FEAuthRequest
                {
                    Token = tkValido.Token,
                    Sign = tkValido.Sign,
                    Cuit = Convert.ToInt64(cuit)
                };

                var cabeceraProd = new Wsfev1.FECAEACabRequest
                {
                    CantReg = 1,
                    PtoVta = nPtoVta,
                    CbteTipo = nTipCom
                };
                if (habilitaLog) HelpersLogger.Escribir("RegInformativoARCA Auth CabReq");

                
                //Convertir CAEADetRequest a FECAEDetRequest
                if (habilitaLog) HelpersLogger.Escribir($"Antes Copy {HelpersGlobal.SerializeToXml(caeDetRequest)}");

                InnCAEADetRequest InnCAEADetReq = caeDetRequest.CopyToDerived<CAEDetRequest, InnCAEADetRequest>();
                    if (habilitaLog) HelpersLogger.Escribir($"Pos Copy {HelpersGlobal.SerializeToXml(InnCAEADetReq)}");
                
                InnCAEADetReq.CAEA = sCAE;
                InnCAEADetReq.CbteFchHsGen = cbteFchGen;
                
                
                var detalleProd = new Wsfev1.FECAEADetRequest
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
                    ImpIVA = dIva,
                    ImpTrib = InnCAEADetReq.ImpTrib,
                    MonId = InnCAEADetReq.MonId,
                    MonCotiz = InnCAEADetReq.MonCotiz,
                    CAEA = InnCAEADetReq.CAEA,
                    CbteFchHsGen = InnCAEADetReq.CbteFchHsGen
                };

                if (habilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 2");
                if (InnCAEADetReq.Concepto == 2 || InnCAEADetReq.Concepto == 3)
                {
                    if (habilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 2.1");
                    detalleProd.FchServDesde = InnCAEADetReq.FchServDesde;
                    detalleProd.FchServHasta = InnCAEADetReq.FchServHasta;
                }
                if (!string.IsNullOrEmpty(InnCAEADetReq.FchVtoPago))                    detalleProd.FchVtoPago = InnCAEADetReq.FchVtoPago;
                

                if (habilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 3");
                if (InnCAEADetReq.Iva?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 3.1");
                    detalleProd.ImpIVA = InnCAEADetReq.Iva.Sum(t => t.Importe);
                    detalleProd.Iva = InnCAEADetReq.Iva.Select(alicIva => (Wsfev1.AlicIva)HelpersArca.ConvertAlicIva(alicIva, true)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 4");
                if (InnCAEADetReq.Tributos?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 4.1");
                    detalleProd.ImpTrib = InnCAEADetReq.Tributos.Sum(t => t.Importe);
                    detalleProd.Tributos = InnCAEADetReq.Tributos.Select(t => (Wsfev1.Tributo)HelpersArca.ConvertirTributos(t, true)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 5 " + InnCAEADetReq.ComprobantesAsociados?.Length);
                if (InnCAEADetReq.ComprobantesAsociados?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 5.1 " + InnCAEADetReq.ComprobantesAsociados?.Length);
                    detalleProd.CbtesAsoc = InnCAEADetReq.ComprobantesAsociados.Select(cbteAsoc => (Wsfev1.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, true)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 6");
                if (InnCAEADetReq.Opcionales?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 6.1");
                    detalleProd.Opcionales = InnCAEADetReq.Opcionales.Select(o => (Wsfev1.Opcional)HelpersArca.ConvertirOpcionales(o, true)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 7");
                var solicitud = new Wsfev1.FECAEARequest
                {
                    FeCabReq = cabeceraProd,
                    FeDetReq = new[] { detalleProd }
                };

                if (habilitaLog) HelpersLogger.Escribir(HelpersGlobal.SerializeObjectAXml(solicitud));

                var ws = new Wsfev1.Service();
                respuesta = ((dynamic)ws).FECAEARegInformativo(authProd, solicitud);
                if (habilitaLog) HelpersLogger.Escribir("Linea 8");
            }
            catch (Exception ex)
            {
                if (habilitaLog) HelpersLogger.Escribir($"ERROR RegInformativoARCA Exception: {ex.Message} {ex.StackTrace}"); 
                throw new Exception($"ERROR RegInformativoARCA Exception: {ex.Message} {ex.StackTrace}");
            }
        }

        public static Wsfev1.FEAuthRequest FEAuthRequest_Set(string Token, string Sign, long CUIT)
        {
            try
            {
                Wsfev1.FEAuthRequest FEAuthRequest = new Wsfev1.FEAuthRequest
                {
                    Token = Token,
                    Sign = Sign,
                    Cuit = (CUIT)
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

