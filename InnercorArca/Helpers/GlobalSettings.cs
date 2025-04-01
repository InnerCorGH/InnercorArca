using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnercorArca.V1.Helpers
{
    public static class GlobalSettings
    {

        public static string urlWSAAHomo= "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";
        public static string urlWSAAProd = "https://wsaa.afip.gov.ar/ws/services/LoginCms";


        public enum Errors : int
        {
            CERT_ERROR = unchecked((int)100), // 0x8004xxxx (personalizado)
            CONNECTION_ERROR = unchecked((int)200),
            INVALID_PARAM = unchecked((int)300),
            WSAA_ERROR = unchecked((int)400),
            EXCEPTION = unchecked((int)500),
            FORMAT_ERROR = unchecked((int)600),
            GET_ERROR = unchecked((int)700),
        }

        public enum ServiceARCA : int
        {
            wsfe = 1,
            ws_sr_padron_a4,
            ws_sr_padron_a5,
            ws_sr_padron_a13,
            ws_sr_padron_a10,
            ws_sr_padron_a100,
            ws_sr_constancia_inscripcion,
            ws_sr_padron_rut
        }

        public enum MetCAEA : int
        {
            CAEACONSULTAR = 1,
            CAEASOLICITAR = 2,
            CAEAINFORMAR = 3,
        }
        public enum CondicionIVA : int
        {
            ResponsableInscripto = 1,
            NoResponsable = 2,
            IvaSujetoExento = 4,
            ConsumidorFinal = 5,
            ResponsableMonotributo = 6,
            SinDatos

        }
    }
}
