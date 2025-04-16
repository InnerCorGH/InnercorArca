using InnercorArca.V1.Helpers;
using System;
using System.Linq;
using static InnercorArca.V1.ModelsCOM.CacheResultCOM;
using static InnercorArca.V1.ModelsCOM.CAEACOM;
using static InnercorArca.V1.ModelsCOM.CAECOM;

namespace InnercorArca.V1.Procesos
{
    public static class ArcaCAE
    {
        /// <summary>        
                //llama a ARCA FECAESOlicitar y manda el objeto creado 
        /// </summary>
        public static void AutorizarARCA(bool habilitaLog, string cuit,
            CAEDetRequest caeDetRequest, double iva, CacheResult tkValido,
            int nPtoVta, int nTipCom, bool isProduction, out object respuesta)
        {
            respuesta = null;
            string traceBack = "";
            try
            {
                if (habilitaLog) HelpersLogger.Escribir("Inicio AutorizarARCA");

                // Determine the appropriate service and object types
                dynamic auth;
                if (isProduction)
                    auth = new Wsfev1.FEAuthRequest();
                else
                    auth = new Wsfev1Homo.FEAuthRequest();

                auth.Token = tkValido.Token;
                auth.Sign = tkValido.Sign;
                auth.Cuit = Convert.ToInt64(cuit);

                dynamic cabecera;
                if (isProduction)
                    cabecera = new Wsfev1.FECAECabRequest();
                else
                    cabecera = new Wsfev1Homo.FECAECabRequest();

                cabecera.CantReg = 1;
                cabecera.PtoVta = nPtoVta;
                cabecera.CbteTipo = nTipCom;

                traceBack = "Linea 2";
                if (habilitaLog)
                {
                    HelpersLogger.Escribir($"auth {HelpersGlobal.SerializeToXml(auth)} cabecera {HelpersGlobal.SerializeToXml(cabecera)} ");
                }

                dynamic detalle;
                if (isProduction)
                    detalle = new Wsfev1.FECAEDetRequest();
                else
                    detalle = new Wsfev1Homo.FECAEDetRequest();

                detalle.Concepto = caeDetRequest.Concepto;
                detalle.DocTipo = caeDetRequest.DocTipo;
                detalle.DocNro = caeDetRequest.DocNro;
                detalle.CbteDesde = caeDetRequest.CbteDesde;
                detalle.CbteHasta = caeDetRequest.CbteHasta;
                detalle.CbteFch = caeDetRequest.CbteFch;
                detalle.ImpTotal = HelpersGlobal.GetDecimales(caeDetRequest.ImpTotal);
                detalle.ImpTotConc = HelpersGlobal.GetDecimales(caeDetRequest.ImpTotConc);
                detalle.ImpNeto = HelpersGlobal.GetDecimales(caeDetRequest.ImpNeto);
                detalle.ImpOpEx = HelpersGlobal.GetDecimales(caeDetRequest.ImpOpEx);
                detalle.ImpIVA = HelpersGlobal.GetDecimales(iva);
                detalle.MonCotiz = HelpersGlobal.GetDecimales(caeDetRequest.MonCotiz);
                detalle.MonCotizSpecified = true;
                detalle.MonId = caeDetRequest.MonId;
                detalle.CondicionIVAReceptorId = caeDetRequest.CondicionIvaReceptor;
                detalle.CanMisMonExt = caeDetRequest.CantidadMismaMonedaExt;

                if ((caeDetRequest.Concepto == 2 || caeDetRequest.Concepto == 3))
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 2.1 Concepto " + caeDetRequest.Concepto);
                    detalle.FchServDesde = caeDetRequest.FchServDesde;
                    detalle.FchServHasta = caeDetRequest.FchServHasta;
                }

                if (!string.IsNullOrEmpty(caeDetRequest.FchVtoPago))
                {
                    detalle.FchVtoPago = caeDetRequest.FchVtoPago;
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 3 Iva " + caeDetRequest.Iva?.Length);
                if (caeDetRequest.Iva?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 3.1 " + caeDetRequest.Iva?.Length);
                    detalle.ImpIVA = HelpersGlobal.GetDecimales(caeDetRequest.Iva.Sum(t => t.Importe));
                    if (isProduction)
                        detalle.Iva = caeDetRequest.Iva.Select(alicIva => (Wsfev1.AlicIva)HelpersArca.ConvertAlicIva(alicIva, true)).ToArray();
                    else
                        detalle.Iva = caeDetRequest.Iva.Select(alicIva => (Wsfev1Homo.AlicIva)HelpersArca.ConvertAlicIva(alicIva, false)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 4 Tributos " + caeDetRequest.Tributos?.Length);
                if (caeDetRequest.Tributos?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 4.1 " + caeDetRequest.Tributos?.Length);
                    detalle.ImpTrib = HelpersGlobal.GetDecimales(caeDetRequest.Tributos.Sum(t => t.Importe));
                    if (isProduction)
                        detalle.Tributos = caeDetRequest.Tributos.Select(tributo => (Wsfev1.Tributo)HelpersArca.ConvertirTributos(tributo, true)).ToArray();
                    else
                        detalle.Tributos = caeDetRequest.Tributos.Select(tributo => (Wsfev1Homo.Tributo)HelpersArca.ConvertirTributos(tributo, false)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 5 Comp Asociados " + caeDetRequest.ComprobantesAsociados?.Length);
                if (caeDetRequest.ComprobantesAsociados?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 5.1 " + caeDetRequest.ComprobantesAsociados?.Length);
                    if (isProduction)
                        detalle.CbtesAsoc = caeDetRequest.ComprobantesAsociados.Select(cbteAsoc => (Wsfev1.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, true)).ToArray();
                    else
                        detalle.CbtesAsoc = caeDetRequest.ComprobantesAsociados.Select(cbteAsoc => (Wsfev1Homo.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, false)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 6 Opcionales " + caeDetRequest.Opcionales?.Length);
                if (caeDetRequest.Opcionales?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 6.1 Opcionales " + caeDetRequest.Opcionales?.Length);
                    if (isProduction)
                        detalle.Opcionales = caeDetRequest.Opcionales.Select(opcional => (Wsfev1.Opcional)HelpersArca.ConvertirOpcionales(opcional, true)).ToArray();
                    else
                        detalle.Opcionales = caeDetRequest.Opcionales.Select(opcional => (Wsfev1Homo.Opcional)HelpersArca.ConvertirOpcionales(opcional, false)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 7");

                dynamic solicitud;
                if (isProduction)
                {
                    solicitud = new Wsfev1.FECAERequest();
                    solicitud.FeDetReq = new[] { (Wsfev1.FECAEDetRequest)detalle };
                }

                else
                {
                    solicitud = new Wsfev1Homo.FECAERequest();
                    solicitud.FeDetReq = new[] { (Wsfev1Homo.FECAEDetRequest)detalle };
                }

                solicitud.FeCabReq = cabecera;



                traceBack = HelpersGlobal.SerializeObjectAXml(solicitud);
                if (habilitaLog) HelpersLogger.Escribir($"Linea 7.1 {traceBack}");

                dynamic ws;
                if (isProduction)
                    ws = new Wsfev1.Service();
                else
                    ws = new Wsfev1Homo.Service();
                respuesta = ((dynamic)ws).FECAESolicitar(auth, solicitud);

                if (habilitaLog) HelpersLogger.Escribir("Respuesta: " + HelpersGlobal.SerializeObjectAXml(respuesta));
            }
            catch (Exception ex)
            {
                if (habilitaLog) HelpersLogger.Escribir($"Error AutorizarARCA Exception {ex.Message} {traceBack} {ex.StackTrace}");
                throw new Exception($"Error AutorizarARCA Exception {ex.Message} {ex.StackTrace}");
            }
        }

        public static dynamic FEAuthRequest_Set(string Token, string Sign, long CUIT, bool isProduction)
        {
            try
            {

                dynamic FEAuthRequest;
                if (isProduction)
                    FEAuthRequest = new Wsfev1.FEAuthRequest();
                else
                    FEAuthRequest = new Wsfev1Homo.FEAuthRequest();
                FEAuthRequest.Token = Token;
                FEAuthRequest.Sign = Sign;
                FEAuthRequest.Cuit = CUIT;

                return FEAuthRequest;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static void RegInformativoARCA(bool habilitaLog, string cuit,
            CAEDetRequest caeDetRequest, double dIva, string sCAE, string cbteFchGen,
            CacheResult tkValido, int nPtoVta, int nTipCom, bool isProduction, out object respuesta)
        {
            try
            {
                if (habilitaLog) HelpersLogger.Escribir("Inicio RegInformativoARCA ");

                dynamic auth;
                if (isProduction)
                    auth = new Wsfev1.FEAuthRequest();
                else
                    auth = new Wsfev1Homo.FEAuthRequest();

                auth.Token = tkValido.Token;
                auth.Sign = tkValido.Sign;
                auth.Cuit = Convert.ToInt64(cuit);

                dynamic cabecera;
                if (isProduction)
                    cabecera = new Wsfev1.FECAECabRequest();
                else
                    cabecera = new Wsfev1Homo.FECAECabRequest();

                cabecera.CantReg = 1;
                cabecera.PtoVta = nPtoVta;
                cabecera.CbteTipo = nTipCom;

                if (habilitaLog) HelpersLogger.Escribir("RegInformativoARCA Auth CabReq");


                //Convertir CAEADetRequest a FECAEDetRequest
                if (habilitaLog) HelpersLogger.Escribir($"Antes Copy {HelpersGlobal.SerializeToXml(caeDetRequest)}");

                InnCAEADetRequest InnCAEADetReq = caeDetRequest.CopyToDerived<CAEDetRequest, InnCAEADetRequest>();
                if (habilitaLog) HelpersLogger.Escribir($"Pos Copy {HelpersGlobal.SerializeToXml(InnCAEADetReq)}");

                InnCAEADetReq.CAEA = sCAE;
                InnCAEADetReq.CbteFchHsGen = cbteFchGen;


                dynamic detalle;
                if (isProduction)
                    detalle = new Wsfev1.FECAEDetRequest();
                else
                    detalle = new Wsfev1Homo.FECAEDetRequest();


                detalle.Concepto = InnCAEADetReq.Concepto;
                detalle.DocTipo = InnCAEADetReq.DocTipo;
                detalle.DocNro = InnCAEADetReq.DocNro;
                detalle.CbteDesde = InnCAEADetReq.CbteDesde;
                detalle.CbteHasta = InnCAEADetReq.CbteHasta;
                detalle.CbteFch = InnCAEADetReq.CbteFch;
                detalle.ImpTotal = HelpersGlobal.GetDecimales(InnCAEADetReq.ImpTotal);
                detalle.ImpTotConc = HelpersGlobal.GetDecimales(InnCAEADetReq.ImpTotConc);
                detalle.ImpNeto = HelpersGlobal.GetDecimales(InnCAEADetReq.ImpNeto);
                detalle.ImpOpEx = HelpersGlobal.GetDecimales(InnCAEADetReq.ImpOpEx);
                detalle.ImpIVA = HelpersGlobal.GetDecimales(dIva);
                detalle.ImpTrib = HelpersGlobal.GetDecimales(InnCAEADetReq.ImpTrib);
                detalle.MonId = InnCAEADetReq.MonId;
                detalle.MonCotiz = HelpersGlobal.GetDecimales(caeDetRequest.MonCotiz);
                detalle.MonCotizSpecified = true;
                detalle.MonId = caeDetRequest.MonId;
                detalle.CondicionIVAReceptorId = caeDetRequest.CondicionIvaReceptor;
                detalle.CanMisMonExt = caeDetRequest.CantidadMismaMonedaExt;


                if (habilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 2");
                if (InnCAEADetReq.Concepto == 2 || InnCAEADetReq.Concepto == 3)
                {
                    if (habilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 2.1");
                    detalle.FchServDesde = InnCAEADetReq.FchServDesde;
                    detalle.FchServHasta = InnCAEADetReq.FchServHasta;
                }
                if (!string.IsNullOrEmpty(InnCAEADetReq.FchVtoPago)) detalle.FchVtoPago = InnCAEADetReq.FchVtoPago;


                if (habilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 3");
                if (InnCAEADetReq.Iva?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 3.1");
                    detalle.ImpIVA = HelpersGlobal.GetDecimales(InnCAEADetReq.Iva.Sum(t => t.Importe));
                    if (isProduction)
                        detalle.Iva = InnCAEADetReq.Iva.Select(alicIva => (Wsfev1.AlicIva)HelpersArca.ConvertAlicIva(alicIva, true)).ToArray();
                    else
                        detalle.Iva = InnCAEADetReq.Iva.Select(alicIva => (Wsfev1Homo.AlicIva)HelpersArca.ConvertAlicIva(alicIva, false)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 4");
                if (InnCAEADetReq.Tributos?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 4.1");
                    detalle.ImpTrib = HelpersGlobal.GetDecimales(InnCAEADetReq.Tributos.Sum(t => t.Importe));
                    if (isProduction)
                        detalle.Tributos = InnCAEADetReq.Tributos.Select(t => (Wsfev1.Tributo)HelpersArca.ConvertirTributos(t, true)).ToArray();
                    else
                        detalle.Tributos = InnCAEADetReq.Tributos.Select(t => (Wsfev1Homo.Tributo)HelpersArca.ConvertirTributos(t, false)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("RegInformativoARCA Linea 5 " + InnCAEADetReq.ComprobantesAsociados?.Length);
                if (InnCAEADetReq.ComprobantesAsociados?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 5.1 " + InnCAEADetReq.ComprobantesAsociados?.Length);
                    if (isProduction)
                        detalle.CbtesAsoc = InnCAEADetReq.ComprobantesAsociados.Select(cbteAsoc => (Wsfev1.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, true)).ToArray();
                    else
                        detalle.CbtesAsoc = InnCAEADetReq.ComprobantesAsociados.Select(cbteAsoc => (Wsfev1Homo.CbteAsoc)HelpersArca.ConvertirCompAsoc(cbteAsoc, false)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 6");
                if (InnCAEADetReq.Opcionales?.Length > 0)
                {
                    if (habilitaLog) HelpersLogger.Escribir("Linea 6.1");
                    if (isProduction)
                        detalle.Opcionales = InnCAEADetReq.Opcionales.Select(o => (Wsfev1.Opcional)HelpersArca.ConvertirOpcionales(o, true)).ToArray();
                    else
                        detalle.Opcionales = InnCAEADetReq.Opcionales.Select(o => (Wsfev1Homo.Opcional)HelpersArca.ConvertirOpcionales(o, false)).ToArray();
                }

                if (habilitaLog) HelpersLogger.Escribir("Linea 7");
                dynamic solicitud;
                if (isProduction)
                {
                    solicitud = new Wsfev1.FECAERequest();
                    solicitud.FeDetReq = new[] { (Wsfev1.FECAEDetRequest)detalle };
                }
                else
                {
                    solicitud = new Wsfev1Homo.FECAERequest();
                    solicitud.FeDetReq = new[] { (Wsfev1Homo.FECAEDetRequest)detalle };
                }
                solicitud.FeCabReq = cabecera;




                if (habilitaLog) HelpersLogger.Escribir(HelpersGlobal.SerializeObjectAXml(solicitud));

                dynamic ws;
                if (isProduction)
                    ws = new Wsfev1.Service();
                else
                    ws = new Wsfev1Homo.Service();
                respuesta = ((dynamic)ws).FECAESolicitar(auth, solicitud);

                if (habilitaLog) HelpersLogger.Escribir($"Linea 8 {HelpersGlobal.SerializeObjectAXml(respuesta)}");
            }
            catch (Exception ex)
            {
                if (habilitaLog) HelpersLogger.Escribir($"ERROR RegInformativoARCA Exception: {ex.Message} {ex.StackTrace}");
                throw new Exception($"ERROR RegInformativoARCA Exception: {ex.Message} {ex.StackTrace}");
            }
        }

    }
}
