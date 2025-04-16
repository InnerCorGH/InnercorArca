using System;
using System.IO;
using System.Reflection;

public static class HelpersLogger
{

    private static readonly object lockObj = new object();
    private static readonly string logPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"{DateTime.Now:yyyyMMdd}.log");  // Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now:yyyyMMdd}.log");


    public static void Escribir(string mensaje)
    {

        try
        {
            string directorio = Path.GetDirectoryName(logPath) ?? "";

            if (string.IsNullOrWhiteSpace(directorio))
                throw new Exception($"La ruta del archivo de log no tiene un directorio válido. {directorio}");

            if (!Directory.Exists(directorio))
                throw new Exception($"El directorio del log no existe: {directorio}");

            if (!TienePermisoEscritura(directorio))
                throw new Exception($"No hay permisos de escritura en el directorio del log: {directorio}");

            if (File.Exists(logPath))
            {
                var attributes = File.GetAttributes(logPath);
                if (attributes.HasFlag(FileAttributes.ReadOnly))
                    throw new Exception($"El archivo '{logPath}' es de solo lectura.");
            }

            lock (lockObj)
            {
                using (FileStream fs = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {mensaje}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Logger] Error al escribir en el log: {ex.Message}");
            throw new Exception($"[Logger] Error al escribir en el log: {ex.Message}", ex);
        }
    }


    private static bool TienePermisoEscritura(string path)
    {
        try
        {
            string archivoTest = Path.Combine(path, Path.GetRandomFileName());
            using (FileStream fs = File.Create(archivoTest, 1, FileOptions.DeleteOnClose)) { }
            return true;
        }
        catch
        {
            return false;
        }
    }
}
