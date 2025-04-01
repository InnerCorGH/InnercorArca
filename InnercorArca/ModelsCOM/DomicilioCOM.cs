using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ModelsCOM/DomicilioCOM.cs
using System.Runtime.InteropServices;

namespace InnercorArca.V1.ModelsCOM
{
    [ComVisible(true)]
    [Guid("D0EAC3E1-3C1F-4A33-93A4-4A31E8A29A01")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class DomicilioCOM
    {
        public string CodPostal { get; set; }
        public string DatoAdicional { get; set; }
        public string DescripcionProvincia { get; set; }
        public string Direccion { get; set; }
        public int IdProvincia { get; set; }
        public string Localidad { get; set; }
        public string TipoDatoAdicional { get; set; }
        public string TipoDomicilio { get; set; }
    }
}
