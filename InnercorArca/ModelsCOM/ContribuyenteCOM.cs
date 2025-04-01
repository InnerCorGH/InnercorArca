using System.Runtime.InteropServices;

namespace InnercorArca.V1.ModelsCOM
{
    [ComVisible(true)]
    [Guid("F5E8A637-9265-4B70-98F5-01537DA94B2C")]
    [ProgId("InnercorArca.ActividadCOM")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ActividadCOM
    {
        public long Codigo { get; set; }
        public string Descripcion { get; set; }
    }

    [ComVisible(true)]
    [Guid("2C8CE676-A4B1-4F23-96AE-44D2D6C76A7A")]
    [ProgId("InnercorArca.CategoriaMonotributoCOM")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class CategoriaMonotributoCOM
    {
        public int Categoria { get; set; }
        public string Descripcion { get; set; }
    }

    [ComVisible(true)]
    [Guid("3F7D447B-3E69-4413-BE19-8EE4F169F172")]
    [ProgId("InnercorArca.ImpuestoCOM")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ImpuestoCOM
    {
        public int Codigo { get; set; }
        public string Nombre { get; set; }
    }

    [ComVisible(true)]
    [Guid("E60DAA1B-3D44-4A7A-B799-118A3C8D35DA")]
    [ProgId("InnercorArca.DomicilioCOM")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class DomicilioCOM
    {
        public string CodPostal { get; set; }
        //public string DatoAdicional { get; set; }
        public string Provincia { get; set; }
        public string Direccion { get; set; }
        public string IdProvincia { get; set; }
        public string Localidad { get; set; }
        //public string TipoDatoAdicional { get; set; }
        //public string TipoDomicilio { get; set; }
    }

    [ComVisible(true)]
    [Guid("0F86B6E5-113A-45B7-90C1-6BDB19B9DA86")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IContribuyenteCOM
    {
        string Apellido { get; set; }
        string NombreSimple { get; set; }
        string Nombre { get; set; }
        string EstadoClave { get; set; }
        string TipoClave { get; set; }
        string TipoPersona { get; set; }
        string IdPersona { get; set; }
        int CondicionIva { get; set; }
        int MesCierre { get; set; }
        string NumeroDocumento { get; set; }
        string TipoDocumento { get; set; }
        string EsSucesion { get; set; }
        string FechaFallecimiento { get; set; }
        string FechaInscripcion { get; set; }
        string IdCatAutonomo { get; set; }
        string IdDependencia { get; set; }
        bool SolicitarConstanciaInscripcion { get; set; }
        string Observaciones { get; set; }

        DomicilioCOM DomicilioFiscal { get; set; }
        //void SetDomicilioFiscal(DomicilioCOM[] domicilio);
        //DomicilioCOM GetDomicilioFiscal(int nIndice);
        //int GetDomicilioFiscalCount();
        [DispId(0)]
        long GetActividad(int nIndice);
        int ActividadesCount();
        void SetActividades(ActividadCOM[] actividades);

        [DispId(1)]
        int GetCategoriaMonotributo(int nIndice);
        int CategoriasMonotributoCount();
        void SetCategoriasMonotributo(CategoriaMonotributoCOM[] categorias);

        [DispId(2)]
        int GetImpuestoCOM(int nIndice);
        int ImpuestosCount();
        void SetImpuestosCOM(ImpuestoCOM[] impuestos);
    }

    [ComVisible(true)]
    [Guid("1A9F1A3E-F450-46B2-BF6D-09B934CFAB10")]
    [ProgId("InnercorArca.ContribuyenteCOM")]
    [ClassInterface(ClassInterfaceType.None)]
    public class ContribuyenteCOM : IContribuyenteCOM
    {
        public string Apellido { get; set; }
        public string NombreSimple { get; set; }
        public string Nombre { get; set; }
        public string EstadoClave { get; set; }
        public string TipoClave { get; set; }
        public string TipoPersona { get; set; }
        public string IdPersona { get; set; }
        public int CondicionIva { get; set; }
        public string CondicionIvaDesc { get; set; }
        public int MesCierre { get; set; }
        public string NumeroDocumento { get; set; }
        public string TipoDocumento { get; set; }
        public string EsSucesion { get; set; }
        public string FechaFallecimiento { get; set; }
        public string FechaInscripcion { get; set; }
        public string IdCatAutonomo { get; set; }
        public string IdDependencia { get; set; }
        public bool SolicitarConstanciaInscripcion { get; set; }
        public string Observaciones { get; set; }
        public DomicilioCOM DomicilioFiscal { get; set; }
        private ActividadCOM[] _actividades;
        private CategoriaMonotributoCOM[] _categoriasMonotributo;
        private ImpuestoCOM[] _impuestosCOM;

        [DispId(0)]
        public long GetActividad(int nIndice)
        {
            if (_actividades == null || nIndice < 0 || nIndice >= _actividades.Length)
                return 0;
            return _actividades[nIndice].Codigo;
        }
        public int ActividadesCount()
        {
            if (_actividades == null)
                return 0;
            return _actividades.Length;
        }
        public void SetActividades(ActividadCOM[] actividades) => _actividades = actividades;

        [DispId(1)]
        public int GetCategoriaMonotributo(int nIndice)
        {
            if (_categoriasMonotributo == null || nIndice < 0 || nIndice >= _categoriasMonotributo.Length)
                return 0;
            return _categoriasMonotributo[nIndice].Categoria;
        }
        public int CategoriasMonotributoCount()
        {
            if (_categoriasMonotributo == null)
                return 0;
            return _categoriasMonotributo.Length;
        }
        public void SetCategoriasMonotributo(CategoriaMonotributoCOM[] categorias) => _categoriasMonotributo = categorias;

        [DispId(2)]
        public int GetImpuestoCOM(int nIndice)
        {
            if (_impuestosCOM == null || nIndice < 0 || nIndice >= _impuestosCOM.Length)
                return 0;
            return _impuestosCOM[nIndice].Codigo;
        }
        public int ImpuestosCount()
        {
            if (_impuestosCOM == null)
                return 0;
            return _impuestosCOM.Length;
        }
        public void SetImpuestosCOM(ImpuestoCOM[] impuestos) => _impuestosCOM = impuestos;

    }
}
