using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace InnercorArca.V1.Helpers
{
    public static class HelpersGlobal
    {
       


        public static string SerializeToXml<T>(T obj)
        {
            try
            {
                // Crear un serializador para el tipo T
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));

                // Usar StringWriter para capturar el XML generado
                using (StringWriter stringWriter = new StringWriter())
                {
                    // Serializar el objeto
                    xmlSerializer.Serialize(stringWriter, obj);
                    return stringWriter.ToString(); // Devuelve el XML como string
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        public static string SerializeObjectAXml(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "Objeto Null");
            }

            var xmlSerializer = new XmlSerializer(obj.GetType());
            using (var stringWriter = new StringWriter())
            {
                xmlSerializer.Serialize(stringWriter, obj);
                return stringWriter.ToString();
            }
        }

        /// <summary>
        /// Deserializa un XML string a un objeto del tipo especificado.
        /// </summary>
        /// <typeparam name="T">Tipo del objeto destino</typeparam>
        /// <param name="xml">Contenido XML como string</param>
        /// <param name="throwOnError">Si es true, lanza excepción en caso de error</param>
        /// <returns>Objeto deserializado o null si falla</returns>
        public static T DeserializeFromString<T>(string xml, bool throwOnError = false)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return default;

            try
            {
                var serializer = new XmlSerializer(typeof(T));
                using (var reader = new StringReader(xml))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                if (throwOnError)
                    throw new InvalidOperationException($"Error al deserializar XML a {typeof(T).Name}", ex);

                return default;
            }
        }

        public static dynamic DeserializeXmlToDynamic(string xml)
        {
            var xDoc = XDocument.Parse(xml);
            return ParseElement(xDoc.Root);
        }

        private static dynamic ParseElement(XElement element)
        {
            var expando = new ExpandoObject() as IDictionary<string, object>;

            // Atributos
            foreach (var attr in element.Attributes())
            {
                expando[$"@{attr.Name.LocalName}"] = attr.Value;
            }

            // Elementos hijos o valor
            if (element.HasElements)
            {
                foreach (var child in element.Elements())
                {
                    if (expando.ContainsKey(child.Name.LocalName))
                    {
                        // Convertir en lista si hay varios con el mismo nombre
                        var existing = expando[child.Name.LocalName];
                        if (existing is List<object> list)
                        {
                            list.Add(ParseElement(child));
                        }
                        else
                        {
                            expando[child.Name.LocalName] = new List<object> { existing, ParseElement(child) };
                        }
                    }
                    else
                    {
                        expando[child.Name.LocalName] = ParseElement(child);
                    }
                }
            }
            else
            {
                expando["#text"] = element.Value;
            }

            return expando;
        }
    }

}
