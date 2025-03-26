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
            int PtoVta = 99, TipoCbte = 1; // Factura B
            int CbteNro = 0;

            Client.RecuperaLastCMP(PtoVta, TipoCbte, ref  CbteNro);
            Console.WriteLine($"PtoVta: {PtoVta}, TipoCbte: {TipoCbte}, CbteNro: {CbteNro}");
            if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");
            Console.WriteLine($"Ult Nro: {Client.GetUltimoNumero()}");


            string sCAE=string.Empty;
            string sVtoCAE = string.Empty;
            Client.CmpConsultar(TipoCbte, PtoVta,CbteNro , ref sCAE,ref sVtoCAE);
            Console.WriteLine($"CAE: {sCAE}, Fecha CAE: {sVtoCAE}");
            Console.WriteLine($"GET CAE: {Client.GetNumeroCAE()}, Fecha CAE: {Client.GetVencimientoCAE()}");
            Console.WriteLine($"{Client.XmlResponse}");
            if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");

            ////invocar agregafactura 
            //Client.AgregaFactura(1, 1, 25657145, 1, 1, DateTime.Now, 100, 21, 21, 0, DateTime.Now, DateTime.Now, DateTime.Now, "PES", 1, 5);
            //if (Client.ErrorCode != 0) Console.WriteLine($"Error: {Client.ErrorCode} - {Client.ErrorDesc}");

        }
    }
}

