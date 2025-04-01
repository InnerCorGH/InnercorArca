using System;

namespace InnercorArca.V1.Helpers
{
    public class HelpersCUIT
    {
        public static string GenerarCUIT(long dni, bool esPersonaFisica, string genero = "", bool SinGuion = true )
        {
            int prefijo;

            if (esPersonaFisica)
            {
                if (string.Equals(genero, "m", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(genero, "masculino", StringComparison.OrdinalIgnoreCase))
                {
                    prefijo = 20;
                }
                else if (string.Equals(genero, "f", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(genero, "femenino", StringComparison.OrdinalIgnoreCase))
                {
                    prefijo = 27;
                }
                else
                {
                    // Si no especifica género, elegimos 20 por default
                    prefijo = 20;
                }
            }
            else
            {
                prefijo = 30; // Persona jurídica
            }

            string baseNumber = $"{prefijo}{dni:D8}";

            int[] pesos = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
            int suma = 0;

            for (int i = 0; i < pesos.Length; i++)
            {
                suma += int.Parse(baseNumber[i].ToString()) * pesos[i];
            }

            int resto = suma % 11;
            int digito = resto == 0 ? 0 : resto == 1 ? 9 : 11 - resto;

            return SinGuion ? $"{prefijo}{dni:D8}{digito}": $"{prefijo}-{dni:D8}-{digito}";
        }


    }
}
