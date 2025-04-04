using System;
using static InnercorArca.V1.Helpers.InnercorArcaModels;

namespace InnercorArca.V1.Helpers
{
    public static class HelpersArcaCAEA
    {

        public static bool MetodoCAEA(GlobalSettings.MetCAEA accion, CacheResult tkValido, string pathCache, bool produccion, string Cuit,
            int nPeriod, short nQuince, ref string cNroCAE, ref string dFchDes, ref string dFchHas, ref string dFchTop, ref string dFchPro,
             out int errCode, out string errDesc, out string xmlResponse, out string trackBack, bool habilitaLog)
        {
            try
            {
                //obtiene token y sign del archivo cache
                if (tkValido == null)
                    tkValido = HelpersCache.RecuperarTokenSign(HelpersCache.LeerBloqueServicio(pathCache, GlobalSettings.ServiceARCA.wsfe.ToString()));

                // Configurar autenticación
                object auth;
                // Configurar la solicitud con los parámetros recibidos
                object objWSFEV1;

                if (produccion)
                {
                    objWSFEV1 = new Wsfev1.Service();
                    auth = new Wsfev1.FEAuthRequest();
                }
                else
                {
                    auth = new Wsfev1Homo.FEAuthRequest();
                    objWSFEV1 = new Wsfev1Homo.Service();

                }
                dynamic authData = auth;
                authData.Token = tkValido.Token;
                authData.Sign = tkValido.Sign;
                authData.Cuit = Convert.ToInt64(Cuit); // Reemplazar con el CUIT correcto


                dynamic response;
                if (produccion)
                {
                    if (GlobalSettings.MetCAEA.CAEASOLICITAR == accion)
                        response = ((dynamic)objWSFEV1).FECAEASolicitar((Wsfev1.FEAuthRequest)auth, nPeriod, nQuince);
                    else
                        response = ((dynamic)objWSFEV1).FECAEAConsultar((Wsfev1.FEAuthRequest)auth, nPeriod, nQuince);
                }


                else
                {
                    if (GlobalSettings.MetCAEA.CAEASOLICITAR == accion)
                        response = ((dynamic)objWSFEV1).FECAEASolicitar((Wsfev1Homo.FEAuthRequest)auth, nPeriod, nQuince);
                    else
                        response = ((dynamic)objWSFEV1).FECAEAConsultar((Wsfev1Homo.FEAuthRequest)auth, nPeriod, nQuince);


                }
                // Verificar errores en la respuesta
                if (response.Errors != null && response.Errors.Length > 0)
                {
                    errCode = 0; errDesc = ""; xmlResponse = "";
                    HelpersArca.ProcesarRespuesta(habilitaLog, response, ref errCode, ref errDesc, ref xmlResponse);
                    trackBack = $"Error Método CAEA {accion}";

                    return false;
                }

                errCode = 0;
                errDesc = null;
                xmlResponse = HelpersGlobal.SerializeObjectAXml(response);
                trackBack = "Método CAEA";

                // Extraer el CAE y las fechas                
                return ExtractCAEADetails(response, ref cNroCAE, ref dFchDes, ref dFchHas, ref dFchTop, ref dFchPro);
            }
            catch (Exception ex)
            {
                errCode = (int)GlobalSettings.Errors.EXCEPTION;
                errDesc = ex.Message;
                trackBack = ex.StackTrace;
                xmlResponse = null;
                return false;
            }
        }


        private static bool ExtractCAEADetails(dynamic response, ref string cNroCAE, ref string dFchDes, ref string dFchHas, ref string dFchTop, ref string dFchPro)
        {
            try
            {
                cNroCAE = response.ResultGet.CAEA;
                if (DateTime.TryParseExact(response.ResultGet.FchVigDesde, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha1))
                {
                    dFchDes = fecha1.ToString("yyyyMMdd");
                }
                else
                {
                    return false;
                }
                if (DateTime.TryParseExact(response.ResultGet.FchVigHasta, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha2))
                {
                    dFchHas = fecha2.ToString("yyyyMMdd");
                }
                else
                {
                    return false;
                }
                if (DateTime.TryParseExact(response.ResultGet.FchTopeInf, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha3))
                {
                    dFchTop = fecha3.ToString("yyyyMMdd");
                }
                else
                {
                    return false;
                }
                if (DateTime.TryParseExact(response.ResultGet.FchProceso, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out DateTime fecha4))
                {
                    dFchPro = fecha4.ToString("yyyyMMdd");
                }
                else
                {
                    return false;
                }



                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
