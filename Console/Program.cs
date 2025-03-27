using InnercorArca.V1.Wsfev1;
using System;
using System.Runtime.Remoting;
using System.Threading.Tasks;

namespace Console_InnercorDLL
{
    internal class Program
    {
        static Task Main()
        {


            try
            {
                TestARCA();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return Task.CompletedTask;
        }



        static void TestARCA()
        {

            Console.WriteLine("*********ARCA***********");
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
            int PtoVta = 99, TipoCbte = 201; // Factura a
            int CbteNro = 0;

            //Client.RecuperaLastCMP(PtoVta, TipoCbte, ref CbteNro);
            //Console.WriteLine($"Ultimo Nro PtoVta: {PtoVta}, TipoCbte: {TipoCbte}, Ultimo CbteNro: {CbteNro}");
            //if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");
            //Console.WriteLine($"Ult Nro: {PtoVta} {Client.GetUltimoNumero()}");

            //TipoCbte = 6; 
            //TipoCbte = 1; 

            //TipoCbte = 201; 
            //TipoCbte = 3;



            //Client.RecuperaLastCMP(PtoVta, TipoCbte, ref CbteNro);
            //Console.WriteLine($"Ultimo Nro PtoVta: {PtoVta}, TipoCbte: {TipoCbte}, Ultimo CbteNro: {CbteNro}");
            //if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");
            //Console.WriteLine($"Ult Nro: {TipoCbte} - {Client.GetUltimoNumero()}");
            //Console.WriteLine("Presione Enter para seguir...");

            string sCAE = string.Empty;
            //string sVtoCAE = string.Empty;
            //Client.CmpConsultar(TipoCbte, PtoVta, CbteNro, ref sCAE, ref sVtoCAE);
            //Console.WriteLine($"CMPConultar CAE: {sCAE}, Fecha CAE: {sVtoCAE}");
            //Console.WriteLine($"CMPConultar  GET CAE: {Client.GetNumeroCAE()}, Fecha CAE: {Client.GetVencimientoCAE()}");
            ////Console.WriteLine($"{Client.XmlResponse}");
            //if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");

            //////invocar agregafactura  
            //Client.AgregaFactura(1, 96, 33198937, CbteNro+1, CbteNro+1, "20250327", 385872, 0, 318902.47, 0, "", "", "", "PES", 1,5);
            //if (Client.ErrorCode != 0) Console.WriteLine($"Agrega Factura Error: {Client.ErrorCode} - {Client.ErrorDesc}");

            //Client.AgregaIVA(5, 385872, 66969.53);
            //if (Client.ErrorCode != 0) Console.WriteLine($"Agrega Iva Error: {Client.ErrorCode} - {Client.ErrorDesc}");

            ////Client.AgregaOpcional("20101", "0200408601000000192133");
            ////if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");
            ////Client.AgregaOpcional("27", "SCA");
            ////if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");

            ////Client.AgregaTributo( );
            ////if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");

            //Client.AgregaCompAsoc (4, 99, 35, 20256571405, "20250327");
            //if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");

            //Client.Autorizar(99, TipoCbte);
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


            string @dFchDes = "";  string @dFchHas = ""; string @dFchTop = ""; string @dFchPro = "";

            Client.CAEAConsultar(202504, 1,ref sCAE, ref dFchDes, ref @dFchHas,ref @dFchTop,ref @dFchPro);
            Console.WriteLine($"CAEAConsultar: {Client.ErrorCode} - {Client.ErrorDesc}");
            Console.WriteLine($" {sCAE} {dFchDes} { @dFchHas} {@dFchTop } {@dFchPro}");
            Console.WriteLine($"{Client.GetNumeroCAE()}");


            Client.CAEASolicitar(202504, 2, ref sCAE, ref dFchDes, ref @dFchHas, ref @dFchTop, ref @dFchPro);
            Console.WriteLine($"CAEASolicitar: {Client.ErrorCode} - {Client.ErrorDesc}");
            Console.WriteLine($" {sCAE} {dFchDes} {@dFchHas} {@dFchTop} {@dFchPro}");
            Console.WriteLine($"{Client.GetNumeroCAE()}");


        }
    }
}

