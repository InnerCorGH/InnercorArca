using System.Runtime.InteropServices;

namespace InnercorArca.V1
{
    [ComVisible(true)]
    [Guid("99999999-8888-7777-6666-555555555555")]
    public static class VersionInfo
    {
        [DispId(1)]
        public static string GetVersionDLL()
        {
            return "1.0.0"; // Cambia esto según tu versión actual
        }
    }
}
