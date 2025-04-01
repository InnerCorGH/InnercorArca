using InnercorArca.V1.ModelsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnercorArca.V1.Helpers
{
    public static class HelpersPadron
    {

        public static int InterpretarCondicionIVA(List<ImpuestoCOM> impuestos)
        {
            if (impuestos == null || impuestos.Count == 0)
                return (int)GlobalSettings.CondicionIVA.SinDatos;

            // Buscamos IVA (impuesto 30)
            var iva = impuestos.FirstOrDefault(i => i.Codigo == "30"  );
            if (iva != null)
                return (int)GlobalSettings.CondicionIVA.ResponsableInscripto;

            // Buscamos Monotributo (impuesto 20)
            var mono = impuestos.FirstOrDefault(i => i.Codigo == "20" );
            if (mono != null)
                return (int) GlobalSettings.CondicionIVA.ResponsableMonotributo;

            // Si no tiene ninguno de los dos, lo marcamos como Exento / No Responsable
            return (int) GlobalSettings.CondicionIVA.IvaSujetoExento;
        }


        public static ContribuyenteCOM MapToContribuyenteCOMAsync(dynamic datosGenerales, string service)
        {

            if (datosGenerales == null)
                return null;
            var Datos = datosGenerales;


            if (service == GlobalSettings.ServiceARCA.ws_sr_padron_a13.ToString())
                datosGenerales = Datos.persona;
            else
                datosGenerales = Datos.datosGenerales;



            try
            {
                #region [Datos Persona]
                var contribuyenteCOM = new ContribuyenteCOM
                {
                    Apellido = datosGenerales.apellido,
                    Nombre = service == GlobalSettings.ServiceARCA.ws_sr_padron_a13.ToString() ? datosGenerales.razonSocial : datosGenerales.nombre,
                    NombreSimple = service == GlobalSettings.ServiceARCA.ws_sr_padron_a13.ToString() ? "" : datosGenerales.nombre,
                    IdPersona = datosGenerales.idPersona,
                    MesCierre = datosGenerales.mesCierre,

                    TipoClave = datosGenerales.tipoClave,
                    TipoPersona = datosGenerales.tipoPersona,
                    TipoDocumento = service == GlobalSettings.ServiceARCA.ws_sr_padron_a13.ToString() ? datosGenerales.tipoDocumento : "",
                    NumeroDocumento = service == GlobalSettings.ServiceARCA.ws_sr_padron_a13.ToString() ? datosGenerales.numeroDocumento : "",

                    DomicilioFiscal = new List<DomicilioCOM>(), // Initialize the list
                    Actividades = new List<ActividadCOM>(), // Initialize the list
                    CategoriasMonotributo = new List<CategoriaMonotributoCOM>(), // Initialize the list
                    ImpuestosCOM = new List<ImpuestoCOM>(),// Initialize the list
                    ActividadesCount = 0,
                    CategoriasMonotributoCount = 0,
                    ImpuestosCount = 0

                };
                //if (contribuyenteCOM.TipoClave == "CUIT") { 
                //    contribuyenteCOM.CondicionIva =  AfipApiService.ObtenerCondicionIVA(contribuyenteCOM.IdPersona).Result;
                //}


                #endregion
                #region [Datos Domicilio]

                if (service == GlobalSettings.ServiceARCA.ws_sr_padron_a13.ToString())
                {
                    if (datosGenerales.domicilio != null)
                    {
                        foreach (var dom in datosGenerales.domicilio)
                        {
                            DomicilioCOM Dom = new DomicilioCOM
                            {
                                CodPostal = dom.codigoPostal,
                                DatoAdicional = dom.datoAdicional,
                                DescripcionProvincia = dom.descripcionProvincia,
                                Direccion = dom.direccion,
                                IdProvincia = dom.idProvincia,
                                Localidad = dom.localidad,
                                TipoDatoAdicional = dom.tipoDatoAdicional,
                                TipoDomicilio = dom.tipoDomicilio
                            };
                            contribuyenteCOM.DomicilioFiscal.Add(Dom);
                        }
                    }

                }

                else
                {

                    if (datosGenerales.domicilioFiscal != null)
                    {

                        DomicilioCOM Dom = new DomicilioCOM
                        {
                            CodPostal = datosGenerales.domicilioFiscal.codPostal,
                            DatoAdicional = datosGenerales.domicilioFiscal.datoAdicional,
                            DescripcionProvincia = datosGenerales.domicilioFiscal.descripcionProvincia,
                            Direccion = datosGenerales.domicilioFiscal.direccion,
                            IdProvincia = datosGenerales.domicilioFiscal.idProvincia,
                            Localidad = datosGenerales.domicilioFiscal.localidad,
                            TipoDatoAdicional = datosGenerales.domicilioFiscal.tipoDatoAdicional,
                            TipoDomicilio = datosGenerales.domicilioFiscal.tipoDomicilio
                        };
                        contribuyenteCOM.DomicilioFiscal.Add(Dom);

                    }
                }

                #endregion
                #region [Datos Actividades - Impuestos - Categorias Monot]
                //armar el vecotr de actividades
                if (Datos.datosRegimenGeneral != null)
                {
                    foreach (var act in Datos.datosRegimenGeneral.actividad)
                    {
                        ActividadCOM actividad = new ActividadCOM
                        {
                            Codigo = act.idActividad.ToString(),
                            Descripcion = act.descripcionActividad,

                        };
                        contribuyenteCOM.Actividades.Add(actividad);
                    }
                    contribuyenteCOM.ActividadesCount = contribuyenteCOM.Actividades.Count;

                    foreach (var imp in Datos.datosRegimenGeneral.impuesto)
                    {
                        ImpuestoCOM impuesto = new ImpuestoCOM
                        {
                            Codigo = imp.idImpuesto.ToString(),
                            Descripcion = imp.descripcionImpuesto,
                        };
                        contribuyenteCOM.ImpuestosCOM.Add(impuesto);
                        

                    }
                    contribuyenteCOM.ImpuestosCount = contribuyenteCOM.ImpuestosCOM.Count;
                    contribuyenteCOM.CondicionIva = InterpretarCondicionIVA(contribuyenteCOM.ImpuestosCOM).ToString();
                }

                //armar el vector de categoriasMonotributo
                if (Datos.datosMonotributo.categoriaMonotributo != null)
                {
                    var cat = Datos.datosMonotributo.categoriaMonotributo;
                    CategoriaMonotributoCOM categoria = new CategoriaMonotributoCOM
                    {
                        Categoria = cat.idCategoria.ToString(),
                        Descripcion = cat.descripcionCategoria,

                    };
                    contribuyenteCOM.CategoriasMonotributo.Add(categoria);

                    contribuyenteCOM.CategoriasMonotributoCount = contribuyenteCOM.CategoriasMonotributo.Count;

                    contribuyenteCOM.CondicionIva = ((int)GlobalSettings.CondicionIVA.ResponsableMonotributo).ToString();
                }
                #endregion
                 
                return contribuyenteCOM;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
