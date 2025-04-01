using System;
using System.Runtime.InteropServices;

namespace InnercorArca.V1.ModelsCOM
{
    [ComVisible(true)]
    [Guid("B1C2D3E4-F5A6-4789-8123-456789ABCDEF")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ImpuestoCOM
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        private DateTime _fechaVencimiento;
        public DateTime FechaVencimiento
        {
            get { return _fechaVencimiento; }
            set
            {
                _fechaVencimiento = value;
                Activo = _fechaVencimiento > DateTime.Now;
            }
        }
        public decimal Monto { get; set; }
        public bool Activo { get; set; }
    }
}
