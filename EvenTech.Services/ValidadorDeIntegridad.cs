using System.Collections.Generic;
using EvenTech.BE;

namespace EvenTech.Services
{
    // Mecanismo generico de digitos verificadores (T07/T08). No accede a datos:
    // solo calcula. Sirve para cualquier entidad que implemente IVerificable.
    //
    // Algoritmo del DV horizontal (DVH):
    //   Para cada atributo en posicion 'a' (1..N) y cada caracter en posicion
    //   'c' (1..M), se acumula:  codigoASCII(caracter) * c * a   (modulo primo).
    //   Asi participa el contenido del atributo, la posicion del caracter y la
    //   posicion del atributo dentro de la entidad: detecta tanto alteraciones de
    //   contenido como intercambios de posicion entre campos.
    //
    // Algoritmo del DV vertical (DVV):
    //   Sobre el conjunto de DVH (uno por registro), se acumula:
    //   DVH(i) * posicion(i)  (modulo primo). Detecta filas agregadas, quitadas o
    //   reordenadas por fuera del sistema.
    public static class ValidadorDeIntegridad
    {
        private const long MOD = 1000000007L; // primo grande para acotar el acumulado

        public static string CalcularDVH(IVerificable entidad)
        {
            string[] campos = entidad?.ObtenerCamposParaDV() ?? new string[0];
            long acc = 0;
            for (int a = 0; a < campos.Length; a++)
            {
                string valor = campos[a] ?? string.Empty;
                int posAtributo = a + 1;
                for (int c = 0; c < valor.Length; c++)
                {
                    long contribucion = ((long)valor[c]) * (c + 1) * posAtributo;
                    acc = (acc + contribucion) % MOD;
                }
            }
            return acc.ToString();
        }

        public static string CalcularDVV(IEnumerable<string> dvhsEnOrden)
        {
            long acc = 0;
            int posicion = 0;
            foreach (string dvh in dvhsEnOrden)
            {
                posicion++;
                long valor = ParseLong(dvh);
                acc = (acc + (valor * posicion) % MOD) % MOD;
            }
            return acc.ToString();
        }

        private static long ParseLong(string s)
        {
            return long.TryParse(s, out long v) ? v : 0L;
        }
    }
}
