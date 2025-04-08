using InnercorArca.V1.Helpers;
using System;
using System.Runtime.InteropServices;

namespace InnercorArca.V1
{
    [ComVisible(true)]
    [Guid("12345678-1234-1234-1234-123456789012")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IQR
    {
        [DispId(1)]
        string ArchivoQR { get; set; } // Código QR del comprobante autorizado (en formato Base64).

        [DispId(2)]
        bool Generar(int nVersion, string cFecha, string nCuit, int nPtoVta, int nTipoCmp, long nNroCmp, double nImporte, string cMoneda, int nCtz, int nTipoDocRec, long nNroDocRec,
            string cTipoCodAut, double nCodAut);
        [DispId(3)]
        bool HabilitaLog { get; set; } // Habilita el log de errores.
        [DispId(4)]
        int ErrorCode { get; } // Código de error.
        [DispId(5)]
        string ErrorDesc { get; } // Descripción del error.
        [DispId(6)]
        string XmlResponse { get; } // Respuesta XML del servicio.
        [DispId(7)]
        string Excepcion { get; } // Excepción generada.
        [DispId(8)]
        string TraceBack { get; } // Traza de la excepción generada.
        [DispId(9)]
        string GetVersion();



    }

    [ComVisible(true)]
    [Guid("87654321-4321-4321-4321-210987654321")]
    [ClassInterface(ClassInterfaceType.None)]
    public class qr : IQR
    {
        #region [Variables Públicas Expuestas ]
        public string ArchivoQR { get;  set; } = string.Empty; // Código QR del comprobante autorizado (en formato Base64). 
        public bool HabilitaLog { get; set; } = false;
        public int ErrorCode { get; private set; }
        public string ErrorDesc { get; private set; } = string.Empty;
        public string XmlResponse { get; private set; } = string.Empty;
        public string Excepcion { get; private set; } = string.Empty;
        public string TraceBack { get; private set; } = string.Empty;
        #endregion

        public qr()
        {
            // Constructor

            SetError(0, "", "");
        }
        public string GetVersion()
        {
            return $"1.0.1"; // Cambia esto según tu versión actual
        }
        public bool Generar(int nVersion, string cFecha, string nCuit, int nPtoVta, int nTipoCmp, long nNroCmp, double nImporte, string cMoneda, int nCtz, int nTipoDocRec, long nNroDocRec,
            string cTipoCodAut, double nCodAut)
        {
            try
            {
                if(HabilitaLog) HelpersLogger.Escribir($"Generar QR Version {nVersion}");
                
                // Parse the date string in the format "yyyyMMdd"
                if (!DateTime.TryParseExact(cFecha, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fecha))
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"ERROR: La fecha debe estar en el formato 'yyyyMMdd'. {cFecha}");
                    SetError(GlobalSettings.Errors.FORMAT_ERROR, "La fecha debe estar en el formato 'yyyyMMdd'.", "Generar ");
                    return false;
                }
                long cuit = long.Parse(nCuit );
                cMoneda = cMoneda.Trim().Length==0? "PES":cMoneda.ToUpper().Trim();
                nCtz = nCtz == 0 ? 1 : nCtz;

                if (HabilitaLog) HelpersLogger.Escribir($"Generar {nVersion} {cFecha} {cuit} {nCuit} {nPtoVta}") ;
                // Generar el código QR del comprobante autorizado (en formato Base64).
                string QR = HelpersArca.GeneraCodigoQR(HabilitaLog, nVersion, fecha,cuit, Convert.ToInt64(nPtoVta), nTipoCmp, nNroCmp, nImporte, cMoneda, nCtz, nTipoDocRec, nNroDocRec, cTipoCodAut, nCodAut, ArchivoQR);
                if ( QR.Length==0)
                {
                    if (HabilitaLog) HelpersLogger.Escribir($"ERROR: No se pudo generar el código QR. {QR}");
                    SetError(GlobalSettings.Errors.CERT_ERROR, "No se pudo generar el código QR.", "Generar ");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                if (HabilitaLog) HelpersLogger.Escribir($"Error Exception {ex.Message} {TraceBack} {ex.StackTrace}");
                SetError(GlobalSettings.Errors.EXCEPTION, ex.Message, ex.StackTrace);
                return false;
            }
        }
        private void SetError(GlobalSettings.Errors codigoError, string descError, string traceBack)
        {
            ErrorCode = (int)codigoError;

            ErrorDesc = codigoError == 0 ? "" : descError;

            TraceBack = traceBack;
        }
    }
}
