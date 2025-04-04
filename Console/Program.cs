using System;
using System.Threading.Tasks;

namespace Console_InnercorDLL
{
    internal class Program
    {
        static Task Main()
        {


            try
            {

                //TestARCA_CAEA();
                TestARCA_wsfev1();

                //TestARCA_wsPadron();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        static void TestARCA_wsPadron()
        {

            Console.WriteLine("*********ARCA WSPadron***********");
            string pathCrt = @"K:\Trabajo.Innercor\DLL vs\InnercorSRL_20240205.crt";
            string pathKey = @"K:\Trabajo.Innercor\DLL vs\InnercorSRL_20240205.key";
            //string pathCrt = @"K:\Trabajo.Innercor\DLL vs\AndresCastigliano_20241226_homo.crt";
            //string pathKey = @"K:\Trabajo.Innercor\DLL vs\AndresCastigliano_20241226_homo.key";

            var Client = new InnercorArca.V1.wsPadron()
            {
                Cuit = "33710525809",
                ModoProduccion = true
            };

            bool isAuthenticated = Client.Login(pathCrt, pathKey);

            if (isAuthenticated)
            {
                Console.WriteLine("Login exitoso.");
            }
            else
            {
                Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");
                return;
            }
            ;

            object oCont = null;
             //Client.Consultar("25657140", ref oCont); //Monotirbuto 
            //Client.Consultar("33710525809", ref oCont); // Resp Inscripto
            //Client.Consultar("44433967", ref oCont);//Cons final
            Client.Consultar("27046394106", ref oCont); // Cons final
            //Client.Consultar("59302332", ref oCont); //COnsumidor FInal
//            Client.Consultar("30669387128", ref oCont);//Exento
//Client.Consultar("47711304", ref oCont);//Exento
//Client.Consultar("6533114", ref oCont);//Fallecido 

            if (Client.ErrorCode != 0)
                Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");
            else
            {
                object oCont2 = Client.GetContribuyente();
                if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");
                PrintProperties(oCont2);

                //Console.WriteLine( oCont2.ActividadesCount());
                

                object oContD = Client.GetDomicilio();
                PrintProperties(oContD);

            }
        }

        static void PrintProperties(object obj)
        {
            if (obj == null)
            {
                Console.WriteLine("Object is null");
                return;
            }

            var properties = obj.GetType().GetProperties();
            foreach (var property in properties)
            {
                Console.WriteLine($"{property.Name}: {property.GetValue(obj)}");
            }
        }
        static void TestARCA_CAEA()
        {
            
                 Console.WriteLine("*********ARCA WSFEV1 CAEA***********");
            string pathCrt = @"K:\Trabajo.Innercor\DLL vs\MUSICALISIMOSA_20240926.crt";
            string pathKey = @"K:\Trabajo.Innercor\DLL vs\MUSICALISIMOSA_20240926.key";
            string urlWSAA = "https://wsaa.afip.gov.ar/ws/services/LoginCms"; // O producción
            var Client = new InnercorArca.V1.wsfev1()
            {
                Cuit = "30586954853",
                HabilitaLog=true,
            };
            bool isAuthenticated = Client.Login(pathCrt, pathKey, urlWSAA);

            if (isAuthenticated)
            {
                Console.WriteLine("Login exitoso.");
            }
            else
            {
                Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");
            }
            // Implementación real de la llamada al servicio
            string sServerStatus = "";
            string sDbServerStatus = "";
            string sAuthServer = "";
            bool Dumm = Client.Dummy(ref sServerStatus, ref sDbServerStatus, ref sAuthServer);
            Console.WriteLine($"Servicio {Dumm}");
            Console.WriteLine($"Server Status {sServerStatus}");
            Console.WriteLine($"DbServer Status {sDbServerStatus}");
            Console.WriteLine($"AuthServer Status {sAuthServer}");

            Client.Reset();

            // Recuperar el último comprobante autorizado
            int PtoVta = 92, TipoCbte = 7; // Factura a
            int CbteNro = 0; string sCAE  ;

            Client.RecuperaLastCMP(PtoVta, TipoCbte, ref CbteNro);

            Console.WriteLine($"Ultimo Nro PtoVta: {PtoVta}, TipoCbte: {TipoCbte}, Ultimo CbteNro: {CbteNro}");


            //string @dFchDes = "";  string @dFchHas = ""; string @dFchTop = ""; string @dFchPro = "";

            //Client.CAEAConsultar(202504, 1,ref sCAE, ref dFchDes, ref @dFchHas,ref @dFchTop,ref @dFchPro);
            //Console.WriteLine($"CAEAConsultar: {Client.ErrorCode} - {Client.ErrorDesc} {Client.TraceBack}");
            //Console.WriteLine($" {sCAE} {dFchDes} { @dFchHas} {@dFchTop } {@dFchPro}");
            //Console.WriteLine($"{Client.GetNumeroCAE()}");


            //Client.CAEASolicitar(202504, 2, ref sCAE, ref dFchDes, ref @dFchHas, ref @dFchTop, ref @dFchPro);
            //Console.WriteLine($"CAEASolicitar: {Client.ErrorCode} - {Client.ErrorDesc} {Client.TraceBack}");
            //Console.WriteLine($" {sCAE} {dFchDes} {@dFchHas} {@dFchTop} {@dFchPro}");
            //Console.WriteLine($"{Client.GetNumeroCAE()}");
            sCAE = "35093740997920"; CbteNro = Client.GetUltimoNumero();
            ////invocar agregafactura  
            Client.AgregaFactura(1, 96, 23825364, CbteNro + 1, CbteNro + 1, "20250317", 3160.70, 0, 2612.15, 0, "", "", "", "PES", 1, 0);
            if (Client.ErrorCode != 0) Console.WriteLine($"Agrega Factura Error: {Client.ErrorCode} - {Client.ErrorDesc}");

            Client.AgregaIVA(5, 2612.15, 548.55);
            if (Client.ErrorCode != 0) Console.WriteLine($"Agrega Iva Error: {Client.ErrorCode} - {Client.ErrorDesc}");

            Client.AgregaCompAsoc(6, 55, 14222,long.Parse( Client.Cuit ), "20241014");
            if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");
            Client.AgregaCompAsoc(6, 53, 12130, long.Parse(Client.Cuit), "20250125");
            if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");
            Client.AgregaCompAsoc(6, 53, 12131, long.Parse(Client.Cuit), "20241014");
            if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");

            Client.CAEACbteFchHsGen("20250317120000");

            Client.CAEAInformar(PtoVta, TipoCbte, sCAE);
            Console.WriteLine($"CAEAInformar: {Client.ErrorCode} - {Client.ErrorDesc}");
        }
        static void TestARCA_wsfev1()
        {

            Console.WriteLine("*********ARCA WSFEV1***********");
            string pathCrt = @"K:\Trabajo.Innercor\DLL vs\AndresCastigliano_20241226_homo.crt";
            string pathKey = @"K:\Trabajo.Innercor\DLL vs\AndresCastigliano_20241226_homo.key";
            string urlWSAA = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms"; // O producción
            var Client = new InnercorArca.V1.wsfev1()
            {
                Cuit = "20256571405"
            };
            bool isAuthenticated = Client.Login(pathCrt, pathKey, urlWSAA);

            if (isAuthenticated)
            {
                Console.WriteLine("Login exitoso.");
            }
            else
            {
                Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");
            }
                    ;

            // Implementación real de la llamada al servicio
            string sServerStatus = "";
            string sDbServerStatus = "";
            string sAuthServer = "";
            bool Dumm = Client.Dummy(ref sServerStatus, ref sDbServerStatus, ref sAuthServer);
            Console.WriteLine($"Servicio {Dumm}");
            Console.WriteLine($"Server Status {sServerStatus}");
            Console.WriteLine($"DbServer Status {sDbServerStatus}");
            Console.WriteLine($"AuthServer Status {sAuthServer}");

            //Client.Reset();
            // Recuperar el último comprobante autorizado
            int PtoVta = 90, TipoCbte = 1; // Factura a
            int CbteNro = 0;

            Client.RecuperaLastCMP(PtoVta, TipoCbte, ref CbteNro);
            Console.WriteLine($"Ultimo Nro PtoVta: {PtoVta}, TipoCbte: {TipoCbte}, Ultimo CbteNro: {CbteNro}");
            if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");
            Console.WriteLine($"Ult Nro {TipoCbte}: {PtoVta} {Client.GetUltimoNumero()}");

            //TipoCbte = 6; 
            //TipoCbte = 1; 

            //TipoCbte = 201; 
            //TipoCbte = 3;



            //Client.RecuperaLastCMP(PtoVta, TipoCbte, ref CbteNro);
            //Console.WriteLine($"Ultimo Nro PtoVta: {PtoVta}, TipoCbte: {TipoCbte}, Ultimo CbteNro: {CbteNro}");
            //if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");
            //Console.WriteLine($"Ult Nro: {TipoCbte} - {Client.GetUltimoNumero()}");
            //Console.WriteLine("Presione Enter para seguir...");

            //string sCAE = string.Empty;
            //string sVtoCAE = string.Empty;
            //Client.CmpConsultar(TipoCbte, PtoVta, CbteNro, ref sCAE, ref sVtoCAE);
            //Console.WriteLine($"CMPConultar CAE: {sCAE}, Fecha CAE: {sVtoCAE}");
            //Console.WriteLine($"CMPConultar  GET CAE: {Client.GetNumeroCAE()}, Fecha CAE: {Client.GetVencimientoCAE()}");
            ////Console.WriteLine($"{Client.XmlResponse}");
            //if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");

            ////invocar agregafactura  
            //Client.AgregaFactura(1, 80, 27242686085, 1, 1, "20250328", 1167864.53, 0, 938043.80, 0, "", "", "", "PES", 1, 1);
            //if (Client.ErrorCode != 0) Console.WriteLine($"Agrega Factura Error: {Client.ErrorCode} - {Client.ErrorDesc}");

            //Client.AgregaIVA(5, 938043.80, 196989.20);
            //if (Client.ErrorCode != 0) Console.WriteLine($"Agrega Iva Error: {Client.ErrorCode} - {Client.ErrorDesc}");

            ////Client.AgregaOpcional("20101", "0200408601000000192133");
            ////if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");
            ////Client.AgregaOpcional("27", "SCA");
            ////if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");

            //Client.AgregaTributo(7, "Percepción Tucumán", 938043.80, 3.50, 32831.53);
            //if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");

            //Client.AgregaCompAsoc (4, 99, 35, 20256571405, "20250327");
            //if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");

            //Client.Autorizar(PtoVta, TipoCbte);
            //if (Client.ErrorCode != 0) Console.WriteLine($"Autorizar Error: {Client.ErrorCode} - {Client.ErrorDesc}");
            //Console.WriteLine($"Trace {Client.TraceBack}");
            //Console.WriteLine($"{Client.XmlResponse}");

            //Console.WriteLine(Client.GetNumeroCAE());
            //Console.WriteLine(Client.GetVencimientoCAE());

            //string VtoCAE = Client.GetVencimientoCAE();
            //string Resultado = Client.GetResultado();
            //string Reprocesar = Client.GetReprocesar();
            //Client.AutorizarRespuesta(0, out string CAE, ref VtoCAE, ref Resultado, ref Reprocesar);
            //Console.WriteLine ($"{CAE} {VtoCAE} {Resultado} {Reprocesar}");

            //string obs= Client.AutorizarRespuestaObs(0);
            //Console.WriteLine($"Obs: {obs}");



        }
    }
}

