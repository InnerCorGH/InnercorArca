using InnercorArca.V1.ModelsCOM;
using System;
using System.Collections.Generic;
using System.EnterpriseServices.CompensatingResourceManager;
using System.Security.Cryptography;

namespace InnercorArca.V1.Helpers
{
    public static class HelpersPadron
    {

        public static int InterpretarCondicionIVA(ImpuestoCOM[] impuestos)
        {
            if (impuestos == null || impuestos.Length == 0)
                return (int)GlobalSettings.CondicionIVA.SinDatos;

            if (Array.Exists(impuestos, i => i.Codigo == "30"))
                return (int)GlobalSettings.CondicionIVA.ResponsableInscripto;

            if (Array.Exists(impuestos, i => i.Codigo == "20"))
                return (int)GlobalSettings.CondicionIVA.ResponsableMonotributo;

            return (int)GlobalSettings.CondicionIVA.IvaSujetoExento;
        }

        public static ContribuyenteCOM MapToContribuyenteCOM(dynamic datosGenerales, string service)
        {

            if (datosGenerales == null)
                return null;


            // Check if datosGenerales is of type aws.errorConstancia
            if (datosGenerales is Aws.errorConstancia)
            {
                // Handle the presence of errorConstancia 
                return new ContribuyenteCOM
                {
                    Apellido = datosGenerales.apellido,
                    NombreSimple = datosGenerales.nombre,
                    CondicionIva = ((int)GlobalSettings.CondicionIVA.ConsumidorFinal).ToString(),
                    CondicionIvaDesc = GlobalSettings.CondicionIVA.ConsumidorFinal.ToString(),
                };

            }


            var Datos = datosGenerales;
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

                    EsSucesion = datosGenerales.esSucesion,
                    FechaFallecimiento = datosGenerales.fechaFallecimiento.ToString(),
                    FechaInscripcion = datosGenerales.fechaContratoSocial.ToString(),
                    //IdCatAutonomo = datosGenerales.idCatAutonomo,
                    //IdDependencia = datosGenerales.idDependencia,
                    //SolicitarConstanciaInscripcion = datosGenerales.solicitarConstanciaInscripcion

                };

                var domiciliosList = new List<InnercorArca.V1.Aws.domicilio> { datosGenerales.domicilioFiscal };
                HelperContribuyenteCOM.CargarDomicilioFiscal(domiciliosList, contribuyenteCOM);


                #endregion
                #region [Datos  ]
                if (Datos.datosMonotributo != null)
                {


                    ///Categoria Monotributo 
                    CategoriaMonotributoCOM Cat = new CategoriaMonotributoCOM
                    {
                        Categoria = Datos.datosMonotributo.categoriaMonotributo.idCategoria.ToString(),
                        Descripcion = Datos.datosMonotributo.categoriaMonotributo.descripcionCategoria
                    };

                    CategoriaMonotributoCOM[] CAts = new CategoriaMonotributoCOM[1];
                    CAts[0] = Cat;

                    contribuyenteCOM.SetCategoriasMonotributo(CAts);


                    //IMpuestos 
                    var impuestosList = new List<Aws.impuesto>(Datos.datosMonotributo.impuesto);
                    HelperContribuyenteCOM.CargarImpuestos(impuestosList, contribuyenteCOM);

                    //Actividades
                    ActividadCOM Act = new ActividadCOM
                    {
                        Codigo = Datos.datosMonotributo.actividadMonotributista.idActividad.ToString(),
                        Descripcion = Datos.datosMonotributo.actividadMonotributista.descripcionActividad
                    };

                    ActividadCOM[] Acts = new ActividadCOM[1];
                    Acts[0] = Act;
                    contribuyenteCOM.SetActividades(Acts);

                    contribuyenteCOM.CondicionIva = ((int)GlobalSettings.CondicionIVA.ResponsableMonotributo).ToString();
                    contribuyenteCOM.CondicionIvaDesc = GlobalSettings.CondicionIVA.ResponsableMonotributo.ToString();

                }
                else
                {


                    if (Datos.datosRegimenGeneral.categoriaAutonomo != null)
                    {
                        //HelperContribuyenteCOM.CargarCategoriasMonotributo(Datos.datosRegimenGeneral.categoriaAutonomo, contribuyenteCOM);
                        contribuyenteCOM.IdCatAutonomo = Datos.datosRegimenGeneral.categoriaAutonomo.ToString();
                    }
                    //IMpuestos 
                    var impuestosList_ = new List<Aws.impuesto>(Datos.datosRegimenGeneral.impuesto);
                    var Impuestos_ = HelperContribuyenteCOM.CargarImpuestos(impuestosList_, contribuyenteCOM);


                    var actividadesList_ = new List<Aws.actividad>(Datos.datosRegimenGeneral.actividad);
                    HelperContribuyenteCOM.CargarActividades(actividadesList_, contribuyenteCOM);

                    // Interpretar la condición IVA
                    contribuyenteCOM.CondicionIva = InterpretarCondicionIVA(Impuestos_).ToString();
                    contribuyenteCOM.CondicionIvaDesc = ((GlobalSettings.CondicionIVA)Int32.Parse(contribuyenteCOM.CondicionIva)).ToString();


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
