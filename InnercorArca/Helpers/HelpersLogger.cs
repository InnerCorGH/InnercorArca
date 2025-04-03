using System.IO;
using System;
using System.Data.SqlTypes;
using System.Reflection;

public static class HelpersLogger
{
   
    private static readonly object lockObj = new object();
    private static readonly string logPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"{DateTime.Now:yyyyMMdd}.log");  // Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now:yyyyMMdd}.log");
    

    public static void Escribir(string mensaje)
    {
         

        lock (lockObj)
        {
            File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {mensaje}{Environment.NewLine}");
        }
    }
}
