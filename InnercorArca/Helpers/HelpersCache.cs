using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static InnercorArca.V1.Helpers.InnercorArcaModels;
using System.Xml;
using System.IO;

namespace InnercorArca.V1.Helpers
{
    public static class HelpersCache
    {
        public static string LeerBloqueServicio(string pathCache, string service)
        {
            if (!File.Exists(pathCache))
                return null;

            string[] lines = File.ReadAllLines(pathCache);
            for (int i = 0; i < lines.Length; i += 4)
            {
                if (i + 3 >= lines.Length)
                    break;

                string[] firstLineParts = lines[i].Split('!');
                if (firstLineParts.Length >= 2 && firstLineParts[0].Equals(service, StringComparison.OrdinalIgnoreCase))
                {
                    return string.Join(Environment.NewLine, lines.Skip(i).Take(4));
                }
            }
            return null;
        }

        public static bool ValidarToken(string bloque)
        {
            if (string.IsNullOrWhiteSpace(bloque))
                return false;

            string[] lines = bloque.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 4)
                return false;

            string expTimeString = lines[3].Substring(8);
            DateTimeOffset savedTimeUtc = DateTimeOffset.ParseExact(expTimeString, "o", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

            return savedTimeUtc > DateTimeOffset.UtcNow;
        }

        public static CacheResult RecuperarTokenSign(string bloque)
        {
            if (string.IsNullOrWhiteSpace(bloque))
                return null;

            string[] lines = bloque.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 4)
                return null;

            return new CacheResult
            {
                Token = lines[1].Substring(6),
                Sign = lines[2].Substring(5),
                ExpTime = DateTime.Parse(lines[3].Substring(8), null, DateTimeStyles.AdjustToUniversal)
            };
        }

        public static CacheResult GuardarBloque(string pathCache, string cmsBase64, string service)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(cmsBase64);

                string token = doc.SelectSingleNode("//credentials/token")?.InnerText;
                string sign = doc.SelectSingleNode("//credentials/sign")?.InnerText;
                string expiredTime = doc.SelectSingleNode("//expirationTime")?.InnerText;
                string generatedTime = doc.SelectSingleNode("//generationTime")?.InnerText;

                DateTimeOffset expirationDateTime = DateTime.ParseExact(expiredTime, "yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
                DateTimeOffset generatedDateTime = DateTime.ParseExact(generatedTime, "yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture);

                var newLines = new string[]
                {
            $"{service}!{generatedDateTime:yyyyMMddHHmmss}",
            $"token={token}",
            $"sign={sign}",
            $"expTime={expirationDateTime.UtcDateTime:o}"
                };

                List<string> allLines = File.Exists(pathCache)
                    ? File.ReadAllLines(pathCache).ToList()
                    : new List<string>();

                bool replaced = false;
                for (int i = 0; i < allLines.Count; i += 4)
                {
                    if (i + 3 >= allLines.Count)
                        break;

                    string[] firstLineParts = allLines[i].Split('!');
                    if (firstLineParts[0].Equals(service, StringComparison.OrdinalIgnoreCase))
                    {
                        allLines[i] = newLines[0];
                        allLines[i + 1] = newLines[1];
                        allLines[i + 2] = newLines[2];
                        allLines[i + 3] = newLines[3];
                        replaced = true;
                        break;
                    }
                }

                if (!replaced)
                {
                    allLines.AddRange(newLines);
                }

                File.WriteAllLines(pathCache, allLines);

                return new CacheResult
                {
                    Token = token,
                    Sign = sign,
                    ExpTime = expirationDateTime.UtcDateTime
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar bloque del servicio {service}: {ex.Message}", ex);
            }
        }

    }
}
