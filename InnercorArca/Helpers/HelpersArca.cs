using InnercorArca.V1.ModelsCOM;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Services.Description;
using System.Xml;
using System.Xml.Serialization;
using static InnercorArca.V1.Helpers.InnercorArcaModels;

namespace InnercorArca.V1.Helpers
{
    public class HelpersArca
    {
        // Método para establecer la solicitud de autenticación

        public static void SeteaAuthRequest(bool Produccion, ref object feAuthRequest, CacheResult tkValido, long cuit)
        {
            try
            {
                if (Produccion)
                {
                    if (feAuthRequest == null)
                    {
                        feAuthRequest = HelpersArca.FEAuthRequest_Set(tkValido.Token, tkValido.Sign, cuit);
                    }
                }
                else
                {
                    if (feAuthRequest == null)
                    {
                        feAuthRequest = HelpersArca.FEAuthRequest_Set_HOMO(tkValido.Token, tkValido.Sign, cuit);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // Método auxiliar para manejar la respuesta de AFIP
        public static void ProcesarRespuesta(bool habilitaLog, dynamic objResp, ref int errorCode, ref string errorDesc, ref string xmlResponse)
        {
            if (habilitaLog) HelpersLogger.Escribir("ProcesarRespuesta ");
            try
            {
                if (objResp.Errors != null)
                {
                    for (int i = 0; i < objResp.Errors.Length; i++)
                    {
                        var error = objResp.Errors[i];
                        if (error != null)
                        {
                            errorCode = error.Code;
                            errorDesc = " " + error.Msg;
                        }
                    }
                    xmlResponse = HelpersGlobal.SerializeToXml(objResp);

                    if (habilitaLog) HelpersLogger.Escribir($"Errors {xmlResponse}");
                    return;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        // Método auxiliar para manejar la respuesta de AFIP de una Solicitud de Factura
        public static void ProcesarRespuestaFactura(bool habilitaLog, dynamic objResp, ref int errorCode, ref string errorDesc,
            ref string xmlResponse, ref string cae, ref string vtoCae, ref string result, ref string reProc, ref string observ, ref string eventDesc, GlobalSettings.TipoInformeARCA tipoInformeARCA )
        {
            try
            {
               
                if (habilitaLog) HelpersLogger. Escribir("ProcesarRespuestaFactura");
               

                if (objResp.FeDetResp != null) //Solicitud procesada correctament
                {

                    result = objResp.FeDetResp[0].Resultado;
                    //si es rechazado el resultado de la solicitud
                    if (objResp.FeDetResp[0].Resultado == "R")
                    {
                        if (objResp.FeDetResp[0].Observaciones != null)
                        {
                            //errorCode = objResp.FeDetResp[0].Observaciones[0].Code;
                            //errorDesc = objResp.FeDetResp[0].Observaciones[0].Msg;

                            observ = objResp.FeDetResp[0].Observaciones[0].Msg;
                            if (habilitaLog) HelpersLogger.Escribir($"Observ {observ}");
                        }
                        reProc = objResp.FeDetResp[0].Resultado;
                    }
                    else//Solicitud procesada correctamente
                    {

                        for (int i = 0; i < objResp.FeDetResp.Length; i++)
                        {
                            var det = objResp.FeDetResp[i];
                            if (det != null)
                            {
                                if (tipoInformeARCA == GlobalSettings.TipoInformeARCA.CAE)
                                {
                                    cae = det.CAE;
                                    vtoCae = det.CAEFchVto;
                                }
                                else
                                {
                                    cae = det.CAEA;
                                    vtoCae = det.CbteFch;
                                }
                            }

                        }

                        if (habilitaLog) HelpersLogger.Escribir($"CAE {cae} VtoCAE {vtoCae}");
                    }
                }

                //Procesar Errors dentro de FeDetResp
                if (objResp.Errors != null)
                {
                    for (int i = 0; i < objResp.Errors.Length; i++)
                    {
                        var obs = objResp.Errors[i];
                        if (obs != null)
                        {
                            errorCode = obs.Code;
                            errorDesc += $" {obs.Code} {obs.Msg} ";
                        }
                    }

                    if (habilitaLog) HelpersLogger.Escribir($"Errors {errorDesc}");
                }

                //Procesar Events dentro de FeDetResp
                if (objResp.Events != null)
                {
                    for (int i = 0; i < objResp.Events.Length; i++)
                    {
                        var ev = objResp.Events[i];
                        if (ev != null)
                        {
                            //errorCode = ev.Code;
                            eventDesc += $" {ev.Code} {ev.Msg}";
                        }
                    }

                    if (habilitaLog) HelpersLogger.Escribir($"Events {eventDesc}");
                }


                xmlResponse = HelpersGlobal.SerializeObjectAXml(objResp);

                if (habilitaLog) HelpersLogger.Escribir($"xmlResponse {xmlResponse}");
            }
            catch (Exception ex)
            {
                errorCode = (int)GlobalSettings.Errors.EXCEPTION;
                errorDesc = $"Exception {ex.Message}";

                if (habilitaLog) HelpersLogger.Escribir($"Exception {errorDesc}");
            }
        }
        private static Wsfev1.FEAuthRequest FEAuthRequest_Set(string Token, string Sign, long CUIT)
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
        private static Wsfev1Homo.FEAuthRequest FEAuthRequest_Set_HOMO(string Token, string Sign, long CUIT)
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


        public static void InstanciaServicio(bool produccion, ref object objWSFEV1, ref object auth, ref object objReq)
        {
            // Instancia del servicio WSFEv1 (producción o homologación)


            if (produccion)
            {
                if (objWSFEV1 == null) objWSFEV1 = new Wsfev1.Service();
                if (objReq == null) objReq = new Wsfev1.FECompConsultaReq();
                if (auth == null) auth = new Wsfev1.FEAuthRequest();
            }
            else
            {
                if (objWSFEV1 == null) objWSFEV1 = new Wsfev1Homo.Service();
                if (objReq == null) objReq = new Wsfev1Homo.FECompConsultaReq();
                if (auth == null) auth = new Wsfev1Homo.FEAuthRequest();
            }
        }
        public static void InstanciaServicio(bool produccion, ref object objWSFEV1, ref object auth)
        {
            // Instancia del servicio WSFEv1 (producción o homologación)


            if (produccion)
            {
                if (objWSFEV1 == null) objWSFEV1 = new Wsfev1.Service();
                if (auth == null) auth = new Wsfev1.FEAuthRequest();
            }
            else
            {
                if (objWSFEV1 == null) objWSFEV1 = new Wsfev1Homo.Service();
                if (auth == null) auth = new Wsfev1Homo.FEAuthRequest();
            }
        }
        // Método genérico para serializar cualquier objeto a XML

        public static object ConvertAlicIva(InnercorArca.V1.Helpers.InnercorArcaModels.AlicIva alicIva, bool produccion)
        {
            if (produccion)
            {
                return new Wsfev1.AlicIva
                {
                    BaseImp = alicIva.BaseImp ,
                    Importe = alicIva.Importe ,
                    Id = alicIva.Id
                };
            }
            else
            {
                return new Wsfev1Homo.AlicIva
                {
                    BaseImp = alicIva.BaseImp,
                    Importe = alicIva.Importe,
                    Id = alicIva.Id
                };
            }
        }

        public static object ConvertirTributos(InnercorArca.V1.Helpers.InnercorArcaModels.Tributo tributo, bool produccion)
        {
            if (produccion)
            {
                return new Wsfev1.Tributo
                {
                    BaseImp = tributo.BaseImp,
                    Alic = tributo.Alic,
                    Importe = tributo.Importe,
                    Id = tributo.Id,
                    Desc = tributo.Desc
                };
            }
            else
            {
                return new Wsfev1Homo.Tributo
                {
                    BaseImp = tributo.BaseImp,
                    Alic = tributo.Alic,
                    Importe = tributo.Importe,
                    Id = tributo.Id,
                    Desc = tributo.Desc
                };
            }
        }

        public static object ConvertirOpcionales(InnercorArca.V1.Helpers.InnercorArcaModels.Opcional opcional, bool produccion)
        {
            if (produccion)
            {
                return new Wsfev1.Opcional
                {
                    Id = opcional.Id,
                    Valor = opcional.Valor
                };
            }
            else
            {
                return new Wsfev1Homo.Opcional
                {
                    Id = opcional.Id,
                    Valor = opcional.Valor
                };
            }
        }

        public static object ConvertirCompAsoc(InnercorArca.V1.Helpers.InnercorArcaModels.CbteAsoc cbteAsoc, bool produccion)
        {
            if (produccion)
            {
                return new Wsfev1.CbteAsoc
                {
                    Tipo = cbteAsoc.Tipo,
                    PtoVta = cbteAsoc.PtoVta,
                    Nro = cbteAsoc.Nro,
                    Cuit = cbteAsoc.Cuit,
                    CbteFch = cbteAsoc.CbteFch
                };
            }
            else
            {
                return new Wsfev1Homo.CbteAsoc
                {
                    Tipo = cbteAsoc.Tipo,
                    PtoVta = cbteAsoc.PtoVta,
                    Nro = cbteAsoc.Nro,
                    Cuit = cbteAsoc.Cuit,
                    CbteFch = cbteAsoc.CbteFch
                };
            }
        }

  
     
    }
}
