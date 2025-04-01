using InnercorArca.V1.Aws;
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

            if (Array.Exists(impuestos, i => i.Codigo == 30))
                return (int)GlobalSettings.CondicionIVA.ResponsableInscripto;

            if (Array.Exists(impuestos, i => i.Codigo == 20))
                return (int)GlobalSettings.CondicionIVA.ResponsableMonotributo;

            return (int)GlobalSettings.CondicionIVA.IvaSujetoExento;
        }

        public static ContribuyenteCOM MapToContribuyenteCOM(dynamic datosGenerales )
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
                    CondicionIva = (int)GlobalSettings.CondicionIVA.ConsumidorFinal,
                    CondicionIvaDesc = GlobalSettings.CondicionIVA.ConsumidorFinal.ToString(),
                    TipoDocumento="NO DISPONIBLE",
                    NumeroDocumento="NO DISPONIBLE",
                    DomicilioFiscal = new DomicilioCOM(),
                    IdPersona= datosGenerales.idPersona.ToString()
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
                    Nombre = datosGenerales.razonSocial ?? "",
                    NombreSimple = datosGenerales.nombre,
                    IdPersona = datosGenerales.idPersona.ToString(),
                    MesCierre = datosGenerales.mesCierre,

                    TipoClave = datosGenerales.tipoClave,
                    EstadoClave = datosGenerales.estadoClave,
                    TipoPersona = datosGenerales.tipoPersona,

                    EsSucesion = datosGenerales.esSucesion,
                    FechaFallecimiento = datosGenerales.fechaFallecimiento.ToString("yyyyMMdd"),
                    FechaInscripcion = datosGenerales.fechaContratoSocial.ToString("yyyyMMdd"),
                    //IdCatAutonomo = datosGenerales.idCatAutonomo,
                    IdDependencia = datosGenerales.dependencia,
                    //SolicitarConstanciaInscripcion = datosGenerales.solicitarConstanciaInscripcion

                };
                #endregion
                #region[Documento Tipo y Nro ]
                if (contribuyenteCOM.TipoPersona == "FISICA")
                {
                    // Check if datosGenerales contains the property tipoDocumento
                    if (datosGenerales is IDictionary<string, object> dict && dict.ContainsKey("tipoDocumento"))
                    {
                        contribuyenteCOM.TipoDocumento = datosGenerales.tipoDocumento;
                    }
                    else
                    {
                        contribuyenteCOM.TipoDocumento = "NO DISPONIBLE";
                    }
                    // Check if datosGenerales contains the property tipoDocumento
                    if (datosGenerales is IDictionary<string, object> dict1 && dict1.ContainsKey("numeroDocumento"))
                    {
                        contribuyenteCOM.NumeroDocumento = datosGenerales.numeroDocumento.ToString();
                    }
                    else
                    {
                        contribuyenteCOM.NumeroDocumento = "NO DISPONIBLE";
                    }
                }
                else
                {
                    contribuyenteCOM.TipoDocumento = datosGenerales.tipoClave;
                    contribuyenteCOM.NumeroDocumento = datosGenerales.idPersona.ToString();
                }
                #endregion

                #region [Domicilio Fiscal]
                if (datosGenerales.domicilioFiscal != null)
                {
                    var domiciliosList = new List<Aws.domicilio> { datosGenerales.domicilioFiscal };
                    HelperContribuyenteCOM.CargarDomicilioFiscal(domiciliosList, contribuyenteCOM);
                }
                #endregion

                #region [Datos Regimen IVA ]
                if (Datos.datosMonotributo != null)
                {
                    ///Categoria Monotributo 
                    CategoriaMonotributoCOM Cat = new CategoriaMonotributoCOM
                    {
                        Categoria = Datos.datosMonotributo.categoriaMonotributo.idCategoria,
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
                        Codigo = Datos.datosMonotributo.actividadMonotributista.idActividad,
                        Descripcion = Datos.datosMonotributo.actividadMonotributista.descripcionActividad
                    };

                    ActividadCOM[] Acts = new ActividadCOM[1];
                    Acts[0] = Act;
                    contribuyenteCOM.SetActividades(Acts);

                    contribuyenteCOM.CondicionIva = (int)GlobalSettings.CondicionIVA.ResponsableMonotributo;
                    contribuyenteCOM.CondicionIvaDesc = GlobalSettings.CondicionIVA.ResponsableMonotributo.ToString();

                }
                else
                {


                    if (Datos.datosRegimenGeneral != null)
                    {
                        if (Datos.datosRegimenGeneral.categoriaAutonomo != null) 
                            contribuyenteCOM.IdCatAutonomo = Datos.datosRegimenGeneral.categoriaAutonomo.ToString(); 
                        //IMpuestos 
                        var impuestosList_ = new List<Aws.impuesto>(Datos.datosRegimenGeneral.impuesto);
                        var Impuestos_ = HelperContribuyenteCOM.CargarImpuestos(impuestosList_, contribuyenteCOM);


                        var actividadesList_ = new List<Aws.actividad>(Datos.datosRegimenGeneral.actividad);
                        HelperContribuyenteCOM.CargarActividades(actividadesList_, contribuyenteCOM);

                        // Interpretar la condición IVA
                        contribuyenteCOM.CondicionIva = InterpretarCondicionIVA(Impuestos_);
                        contribuyenteCOM.CondicionIvaDesc = ((GlobalSettings.CondicionIVA)contribuyenteCOM.CondicionIva).ToString();
                    }
                    else
                    {
                        contribuyenteCOM.CondicionIva =(int) GlobalSettings.CondicionIVA.ConsumidorFinal;
                        contribuyenteCOM.CondicionIvaDesc = GlobalSettings.CondicionIVA.ConsumidorFinal .ToString();

                    }

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
