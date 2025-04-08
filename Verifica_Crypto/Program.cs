using System;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace VerificadorCrypto
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Verificador de entorno de ejecución ===\n");

            // 1. ¿Existe el método CopyWithPrivateKey?
            var type = typeof(System.Security.Cryptography.X509Certificates.ECDsaCertificateExtensions);
            var method = type.GetMethod("CopyWithPrivateKey", BindingFlags.Public | BindingFlags.Static);

            Console.WriteLine("Método 'CopyWithPrivateKey' encontrado: " + (method != null ? "✅ SÍ" : "❌ NO"));

            // 2. ¿Desde dónde se está cargando la DLL?
            var assembly = type.Assembly;
            Console.WriteLine("\nAssembly: " + assembly.FullName);
            Console.WriteLine("Ubicación física: " + assembly.Location);

            // 3. Versión de .NET
            Console.WriteLine("\nVersión de .NET en uso: " + Environment.Version);
            Console.WriteLine("\nPresione una tecla para salir...");
            Console.ReadKey();
        }
    }
}
