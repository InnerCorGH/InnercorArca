namespace InnercorArca.V1.ModelsCOM
{
    public class CAECOM
    {




        #region [Modelos Metodos CAE]
        public class CAEDetRequest
        {

            public int Concepto { get; set; } // 1-Producto 2-Servicio 3-Producto y Servicio
            public int DocTipo { get; set; }
            public long DocNro { get; set; }
            public long CbteDesde { get; set; }
            public long CbteHasta { get; set; }

            public string CbteFch { get; set; }
            public double ImpTotal { get; set; }

            public double ImpTotConc { get; set; }
            public double ImpNeto { get; set; }
            public double ImpOpEx { get; set; }
            public double ImpTrib { get; set; }
            public double ImpIVA { get; set; }
            public string MonId { get; set; }
            public double MonCotiz { get; set; }
            public string CantidadMismaMonedaExt { get; set; } = "N";
            public string FchServDesde { get; set; }
            public string FchServHasta { get; set; }
            public string FchVtoPago { get; set; }
            public int CondicionIvaReceptor { get; set; }
            public AlicIva[] Iva { get; set; }
            public Opcional[] Opcionales { get; set; }
            public Tributo[] Tributos { get; set; }
            public CbteAsoc[] ComprobantesAsociados { get; set; }



        }

        public class AlicIva
        {
            public double BaseImp { get; set; }
            public double Importe { get; set; }
            public int Id { get; set; }
        }

        public class Opcional
        {
            public string Id { get; set; }
            public string Valor { get; set; }
        }

        public class Tributo
        {
            public double BaseImp { get; set; }
            public double Alic { get; set; }
            public double Importe { get; set; }
            public short Id { get; set; }
            public string Desc { get; set; }
        }

        public class CbteAsoc
        {
            public int Tipo { get; set; }
            public int PtoVta { get; set; }
            public long Nro { get; set; }

            public string Cuit { get; set; }

            public string CbteFch { get; set; }


        }
        #endregion



    }
}
