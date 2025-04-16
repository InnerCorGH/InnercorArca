using System.ComponentModel.DataAnnotations;
using static InnercorArca.V1.ModelsCOM.CAECOM;

namespace InnercorArca.V1.ModelsCOM
{
    public class CAEACOM
    {
        #region [Modelos Metodos CAEA]


        public class InnCAEADetRequest : CAEDetRequest
        {
            [MaxLength(14, ErrorMessage = "El CAEA no puede superar los 14 caracteres.")]
            public string CAEA { get; set; }

            [MaxLength(8, ErrorMessage = "Fecha y Hora de generación del comprobante por contingencia. Formato yyyymmddhhmiss.")]
            public string CbteFchHsGen { get; set; }
            public Actividad Actividades { get; set; }
            public Periodo Periodo_ { get; set; }

        }

        public class Actividad
        {
            public int Id { get; set; }
        }
        public class Periodo
        {
            public string FchDesde { get; set; }
            public string FchHasta { get; set; }
        }
        #endregion

    }
}
