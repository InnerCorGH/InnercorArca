using InnercorArca.V1.ModelsCOM;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Web.Services.Description;
using System.Xml;
using System.Xml.Serialization;
using ZXing;
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
                        feAuthRequest = Procesos.ArcaCAE.FEAuthRequest_Set(tkValido.Token, tkValido.Sign, cuit);
                    }
                }
                else
                {
                    if (feAuthRequest == null)
                    {
                        feAuthRequest = Procesos.ArcaCAEHOMO.FEAuthRequest_Set(tkValido.Token, tkValido.Sign, cuit);
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
            ref string xmlResponse, ref string cae, ref string vtoCae, ref string result, ref string reProc, ref string observ, ref string eventDesc, GlobalSettings.TipoInformeARCA tipoInformeARCA)
        {
            try
            {

                if (habilitaLog) HelpersLogger.Escribir("ProcesarRespuestaFactura");


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

        public static string GeneraCodigoQR(bool habilitaLOG, int nVersion, DateTime fecha, long nCuit, long ptovta, int tipoComp, long nroComp, double importe, string sMoneda, double cot, int tipoDoc,
            long nNroDocRec, string cTipoCodAut, double nCodAut, string pathImg) // , double idMov, string dllPath)
        {
            try
            {
                //string sturl = @"https://www.afip.gob.ar/fe/qr/";

                ////{"ver":1,"fecha":"2020-10-13","cuit":30000000007,"ptoVta":10,"tipoCmp":1,"nroCmp":94,"importe":12100,"moneda":"DOL","ctz":65,"tipoDocRec":80,"nroDocRec":20000000001,"tipoCodAut":"E","codAut":70417054367476}
                //string jSon ="{ver:"+ nVersion+" ,fecha:" + fecha.ToString("yyyy-MM-dd") + ",cuit: " + cuit + ",ptoVta:" + ptovta.ToString() +
                //        ",tipoCmp:" + tipoComp + ",nroCmp:" + nroComp.ToString() + ",importe:" + importe.ToString("#############00") + ",moneda:" + moneda + ",ctz:" + cot +
                //        ",tipoDocRec:" + tipoDoc + ",nroDocRec:" + nroDocRec + ",tipoCodAut:" + cTipoCodAut +",codAut:" + codAut + "}";
                //string DATOS_CMP_BASE_64 =HelpersGlobal.Base64Encode(jSon);

                //string value = sturl + "?p=" + DATOS_CMP_BASE_64;


                var datosQR = new
                {
                    ver = nVersion,
                    fecha = fecha.ToString("yyyy-MM-dd"),
                    cuit =nCuit,
                    ptoVta = ptovta,
                    tipoCmp = tipoComp,
                    nroCmp = nroComp,
                    importe = double.Parse(importe.ToString("#############.00")),
                    moneda = sMoneda,
                    ctz = cot,
                    tipoDocRec = tipoDoc,
                    nroDocRec = nNroDocRec,
                    tipoCodAut = cTipoCodAut,
                    codAut = nCodAut
                };
                if (habilitaLOG) HelpersLogger.Escribir($"Datos QR {datosQR}");

                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(datosQR);

                byte[] bytes = Encoding.UTF8.GetBytes(json);
                string base64 = Convert.ToBase64String(bytes);

                // Convertir a base64 URL-safe (AFIP)
                string base64UrlSafe = base64
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .TrimEnd('=');

                string value = $"https://www.afip.gob.ar/fe/qr/?p={base64UrlSafe}";

                if (habilitaLOG) HelpersLogger.Escribir($"URL QR {value}");


                // Generar el codigo, este metodo retorna una bitmap
                try
                { 
                    var writer = new BarcodeWriter() // Si un barcodeWriter para generar un codigo QR (O.O)
                    {
                        Format = BarcodeFormat.QR_CODE, //setearle el tipo de codigo que generara.
                        Options = new ZXing.Common.EncodingOptions()
                        {
                            Height = 138,//300,
                            Width = 138,//300,
                            Margin = 1, // el margen que tendra el codigo con el restro de la imagen
                        },
                    };
                    if (habilitaLOG) HelpersLogger.Escribir($"Generado ImgQR {value}");

                    Bitmap bitmap = writer.Write(value);
                    if (habilitaLOG) HelpersLogger.Escribir($"Bitmap {bitmap}");

                    //dejarlo donde está el path de la dll
                    string strPathIMG = pathImg;

                    ////// guardar el bitmap con el formato deseado y la locacion deseada
                    bitmap.Save(String.Format(strPathIMG, System.Drawing.Imaging.ImageFormat.Jpeg));
                    if (habilitaLOG) HelpersLogger.Escribir($"Bitmap guardado {strPathIMG}");
                }
                catch (Exception we)
                {
                    value = "";
                    if (habilitaLOG) HelpersLogger.Escribir($"Error generando QR {we.Message}");
                    throw new Exception("Error generando QR " + we.Message.ToString());

                }

                return value;
            }
            catch (Exception EX)
            {

                throw new Exception("Genera QRCode " + EX.Message.ToString());
            }
        }




        // Método genérico para serializar cualquier objeto a XML
        public static object ConvertAlicIva(InnercorArca.V1.Helpers.InnercorArcaModels.AlicIva alicIva, bool produccion)
        {
            if (produccion)
            {
                return new Wsfev1.AlicIva
                {
                    BaseImp = alicIva.BaseImp,
                    Importe = alicIva.Importe,
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
