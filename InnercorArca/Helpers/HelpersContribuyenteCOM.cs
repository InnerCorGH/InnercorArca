using InnercorArca.V1.Aws;
using InnercorArca.V1.ModelsCOM;
using System.Collections.Generic;

public static class HelperContribuyenteCOM
{
    public static void CargarCategoriasMonotributo(List<categoria> source, ContribuyenteCOM destino)
    {
        if (source == null || source.Count == 0)
            return;

        var categorias = new CategoriaMonotributoCOM[1]; // source.Count];

        for (int i = 0; i < source.Count; i++)
        {
            categorias[i] = new CategoriaMonotributoCOM
            {
                Categoria = source[i].idCategoria,
                Descripcion = source[i].descripcionCategoria
            };
        }

        destino.SetCategoriasMonotributo(categorias);
    }

    public static ImpuestoCOM[] CargarImpuestos(List<impuesto> source, ContribuyenteCOM destino)
    {
        if (source == null || source.Count == 0)
            return null;

        var impuestos = new ImpuestoCOM[source.Count];

        for (int i = 0; i < source.Count; i++)
        {
            impuestos[i] = new ImpuestoCOM
            {
                Nombre = source[i].descripcionImpuesto,
                Codigo = source[i].idImpuesto
            };
        }

        destino.SetImpuestosCOM(impuestos);
        return impuestos;
    }

    public static void CargarActividades(List<actividad> source, ContribuyenteCOM destino)
    {
        if (source == null || source.Count == 0)
            return;

        var actividades = new ActividadCOM[source.Count];

        for (int i = 0; i < source.Count; i++)
        {
            actividades[i] = new ActividadCOM
            {
                Codigo = source[i].idActividad,
                Descripcion = source[i].descripcionActividad.ToString(),
            };
        }

        destino.SetActividades(actividades);
    }

    //public static void CargarDomicilioFiscal(List<domicilio> source, ContribuyenteCOM destino)
    //{
    //    if (source == null || source.Count == 0)
    //        return;

    //    var domicilios = new DomicilioCOM[source.Count];

    //    for (int i = 0; i < source.Count; i++)
    //    {
    //        domicilios[i] = new DomicilioCOM
    //        {

    //            CodPostal = source[i].codPostal,
    //            DatoAdicional = source[i].datoAdicional,
    //            DescripcionProvincia = source[i].descripcionProvincia,
    //            Direccion = source[i].direccion,
    //            IdProvincia = source[i].idProvincia.ToString(),
    //            Localidad = source[i].localidad,
    //            TipoDatoAdicional = source[i].tipoDatoAdicional,
    //            TipoDomicilio = source[i].tipoDomicilio
    //        };

    //    }



    //    destino.SetDomicilioFiscal(domicilios);
    //}
    public static void CargarDomicilioFiscal(List<domicilio> source, ContribuyenteCOM destino)
    {
        if (source == null || source.Count == 0)
            return;

        var domicilios = new DomicilioCOM[source.Count];

        for (int i = 0; i < source.Count; i++)
        {
            domicilios[i] = new DomicilioCOM
            {

                CodPostal = source[i].codPostal,
                //DatoAdicional = source[i].datoAdicional,
                Provincia = source[i].descripcionProvincia,
                Direccion = source[i].direccion,
                IdProvincia = source[i].idProvincia.ToString(),

                Localidad = source[i].localidad,
                //TipoDatoAdicional = source[i].tipoDatoAdicional,
                //TipoDomicilio = source[i].tipoDomicilio
            };

        }

        destino.DomicilioFiscal = domicilios[0];

        //    destino.SetDomicilioFiscal(domicilios);
    }
    }
