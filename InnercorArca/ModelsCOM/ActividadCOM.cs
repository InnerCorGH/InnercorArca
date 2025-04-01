using System;
using System.Runtime.InteropServices;

namespace InnercorArca.V1.ModelsCOM
{
    [ComVisible(true)]
    [Guid("A1B2C3D4-E5F6-7890-1234-56789ABCDEF0")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ActividadCOM
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
    }
}
