using System;
using System.Runtime.InteropServices;

namespace InnercorArca.V1.ModelsCOM
{
    [ComVisible(true)]

    [Guid("B1C2D3E4-F5A6-4789-8123-456789ABCD66")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class CategoriaMonotributoCOM
    {
        public string Categoria { get; set; }
        public string Descripcion { get; set; }
        public decimal ImporteMensual { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }
}
