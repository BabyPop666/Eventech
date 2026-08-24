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
    public static class ValidadorDeIntegridad_704ILR
    {
        private const long MOD_704ILR = 1000000007L; // primo grande para acotar el acumulado

        public static string CalcularDVH_704ILR(IVerificable_704ILR entidad_704ILR)
        {
            string[] campos_704ILR = entidad_704ILR?.ObtenerCamposParaDV_704ILR() ?? new string[0];
            long acc_704ILR = 0;
            for (int a_704ILR = 0; a_704ILR < campos_704ILR.Length; a_704ILR++)
            {
                string valor_704ILR = campos_704ILR[a_704ILR] ?? string.Empty;
                int posAtributo_704ILR = a_704ILR + 1;
                for (int c_704ILR = 0; c_704ILR < valor_704ILR.Length; c_704ILR++)
                {
                    long contribucion_704ILR = ((long)valor_704ILR[c_704ILR]) * (c_704ILR + 1) * posAtributo_704ILR;
                    acc_704ILR = (acc_704ILR + contribucion_704ILR) % MOD_704ILR;
                }
            }
            return acc_704ILR.ToString();
        }

        public static string CalcularDVV_704ILR(IEnumerable<string> dvhsEnOrden_704ILR)
        {
            long acc_704ILR = 0;
            int posicion_704ILR = 0;
            foreach (string dvh_704ILR in dvhsEnOrden_704ILR)
            {
                posicion_704ILR++;
                long valor_704ILR = ParseLong_704ILR(dvh_704ILR);
                acc_704ILR = (acc_704ILR + (valor_704ILR * posicion_704ILR) % MOD_704ILR) % MOD_704ILR;
            }
            return acc_704ILR.ToString();
        }

        private static long ParseLong_704ILR(string s_704ILR)
        {
            return long.TryParse(s_704ILR, out long v_704ILR) ? v_704ILR : 0L;
        }
    }
}
