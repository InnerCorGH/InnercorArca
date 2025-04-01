using System;
using System.Collections.Generic;



// ModelsCOM/ContribuyenteCOM.cs
using System.Runtime.InteropServices;

namespace InnercorArca.V1.ModelsCOM
{
    [ComVisible(true)]
    [Guid("8B5A9D3C-55F7-4B66-9A75-8C3C8F12F672")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ContribuyenteCOM
    {
        public string Apellido { get; set; }
        public string NombreSimple { get; set; }
        public string RazonSocial { get; set; }
        public string Nombre { get; set; }
        public string EstadoClave { get; set; }
        public string TipoClave { get; set; }
        public string TipoPersona { get; set; }
        public long IdPersona { get; set; } 
        public string Dependencia { get; set; }
        public string CondicionIva { get; set; }
        
        public int MesCierre { get; set; }  

        public string NumeroDocumento { get; set; }
        public string TipoDocumento { get; set; }
        public List<DomicilioCOM> DomicilioFiscal { get; set; }

        public List<ActividadCOM> Actividades { get; set; }

        public int ActividadesCount { get; set; }

        public List<CategoriaMonotributoCOM> CategoriasMonotributo { get; set; }
        public int CategoriasMonotributoCount { get; set; }

        public List<ImpuestoCOM> ImpuestosCOM { get; set; }
        public int ImpuestosCount { get; set; }
 
    }
}
