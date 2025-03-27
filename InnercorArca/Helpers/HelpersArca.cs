using System;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using static InnercorArca.V1.Helpers.InnercorArcaModels;

namespace InnercorArca.V1.Helpers
{
    public class HelpersArca
    {

        public static CacheResult GenerarCache(string pathCache, string cmsBase64)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(cmsBase64);

                string token = doc.SelectSingleNode("//credentials/token")?.InnerText;
                string sign = doc.SelectSingleNode("//credentials/sign")?.InnerText;
                string expiredTime = doc.SelectSingleNode("//expirationTime")?.InnerText;
                string generatedTime = doc.SelectSingleNode("//generationTime")?.InnerText;

                // Parse the expiration time string to DateTime
                DateTimeOffset expirationDateTime = DateTime.ParseExact(expiredTime, "yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture);


                // Write to the file with the desired format for expiration time
                File.WriteAllLines(pathCache, new string[]
                {
                $"{generatedTime:yyyyMMddHHmmss}",
                $"token={token}",
                $"sign={sign}",
                $"expTime={expirationDateTime.UtcDateTime:o}"
                });

                return new CacheResult
                {
                    Token = token,
                    Sign = sign,
                    ExpTime = expirationDateTime.UtcDateTime
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


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


        public static string LeerCache(string pathCache)
        {

            try
            {
                //leer archivo.cache y devolver el contenido
                return File.ReadAllText(pathCache);
            }
            catch (Exception ex)
            {
                throw new Exception($"PAth: {pathCache} - {ex.Message}") ;
            }
        }

        public static bool ValidarToken(string cache)
        {
            try
            {
                // Read cache
                string[] lines = cache.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 4)
                {
                    return false;
                }

                // Extract expiration time and parse it
                string expTimeString = lines[3].Substring(8);
                DateTimeOffset savedTimeUtc = DateTime.ParseExact(expTimeString, "o", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                //DateTimeOffset savedTimeUtc = DateTimeOffset.ParseExact(
                //            expTimeString,
                //            "yyyy-MM-ddTHH:mm:ss.fffzzz",
                //            CultureInfo.InvariantCulture,
                //            DateTimeStyles.AssumeUniversal);

                DateTime nowUtc = DateTime.UtcNow;
                TimeSpan difference = savedTimeUtc - nowUtc;

                return (difference.TotalMinutes > 0); // Si es positivo, el token sigue válido
            }catch(Exception ex)
            {
                throw ex;
            }
        }

        public static CacheResult RecuperarTokenSign(string cache)
        {
            try
            {
                // Read cache
                string[] lines = cache.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 4)
                {
                    return null;
                }

                // Extract expiration time and parse it
                return new CacheResult()
                {
                    Token = lines[1].Substring(6),
                    Sign = lines[2].Substring(5),
                    ExpTime = DateTime.Parse(lines[3].Substring(8))
                };
            }catch(Exception ex)
            {
                throw ex;
            }
        }

        // Método auxiliar para manejar la respuesta de AFIP
        public static void ProcesarRespuesta(dynamic objResp, ref int errorCode, ref string errorDesc, ref string xmlResponse)
        {
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
                    xmlResponse = SerializeToXml(objResp);
                    return;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
       
        }
        // Método auxiliar para manejar la respuesta de AFIP de una Solicitud de Factura
        public static void ProcesarRespuestaFactura(dynamic objResp, ref int errorCode, ref string errorDesc,
            ref string xmlResponse, ref string cae, ref string vtoCae, ref string result, ref string reProc, ref string observ )
        {
            try
            {
                 
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
                                cae = det.CAE; 
                                vtoCae = det.CAEFchVto;
                            }
                        }

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
                            errorCode =   obs.Code;
                            errorDesc += $" {obs.Code} {obs.Msg} " ;
                        }
                    }
                }
              
                //Procesar Events dentro de FeDetResp
                if (objResp.Events != null)
                {
                    for (int i = 0; i < objResp.Events.Length; i++)
                    {
                        var ev = objResp.FeDetResp.Events[i];
                        if (ev != null)
                        {
                            errorCode =   ev.Code;
                            errorDesc += $" {ev.Code} {ev.Msg}" ;
                        }
                    }
                }


                xmlResponse = SerializeObjectAXml(objResp);
            }
            catch (Exception ex)
            {
                errorCode = (int)Errors.EXCEPTION;
                errorDesc =$"Exception {ex.Message}";
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
        public static string SerializeToXml<T>(T obj)
        {
            try
            {
                // Crear un serializador para el tipo T
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));

                // Usar StringWriter para capturar el XML generado
                using (StringWriter stringWriter = new StringWriter())
                {
                    // Serializar el objeto
                    xmlSerializer.Serialize(stringWriter, obj);
                    return stringWriter.ToString(); // Devuelve el XML como string
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        public static string SerializeObjectAXml(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "Objeto Null");
            }

            var xmlSerializer = new XmlSerializer(obj.GetType());
            using (var stringWriter = new StringWriter())
            {
                xmlSerializer.Serialize(stringWriter, obj);
                return stringWriter.ToString();
            }
        }

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
