using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    // Resultado de la verificacion de integridad al iniciar la aplicacion.
    public class ResultadoIntegridad
    {
        public bool Ok => Inconsistencias.Count == 0;
        public List<string> Inconsistencias { get; } = new List<string>();
    }

    // Coordina el calculo y verificacion de los digitos verificadores sobre la
    // entidad Reserva (T07/T08), usando el mecanismo generico de Services.
    public static class BLL_Integridad
    {
        private const string TablaReservas = "Reservas";

        // Recalcula el DV vertical de Reservas a partir de los DVH almacenados.
        // Se invoca tras cada alta/modificacion para mantener la linea base.
        public static void RecalcularDVVerticalReservas()
        {
            var reservas = DAL_Reserva.GetAll();
            var dvhs = new List<string>();
            // Orden estable (por Id ascendente) para que la posicion sea consistente.
            reservas.Sort((a, b) => a.Id.CompareTo(b.Id));
            foreach (var r in reservas) dvhs.Add(r.Dvh ?? string.Empty);

            string dvv = ValidadorDeIntegridad.CalcularDVV(dvhs);
            DAL_DVVertical.Upsert(TablaReservas, dvv);
        }

        // Verificacion de integridad: se ejecuta al arrancar, antes del login.
        public static ResultadoIntegridad Verificar()
        {
            var resultado = new ResultadoIntegridad();
            var reservas = DAL_Reserva.GetAll();
            reservas.Sort((a, b) => a.Id.CompareTo(b.Id));

            var dvhs = new List<string>();
            foreach (var r in reservas)
            {
                // DV horizontal: recalcular y comparar contra lo almacenado.
                string dvhCalculado = ValidadorDeIntegridad.CalcularDVH(r);
                dvhs.Add(r.Dvh ?? string.Empty);

                if (r.Dvh == null)
                    resultado.Inconsistencias.Add($"Reserva #{r.Id}: sin DV horizontal almacenado.");
                else if (r.Dvh != dvhCalculado)
                    resultado.Inconsistencias.Add($"Reserva #{r.Id}: DV horizontal no coincide (posible alteracion externa).");
            }

            // DV vertical: recalcular sobre el conjunto y comparar contra lo almacenado.
            string dvvCalculado = ValidadorDeIntegridad.CalcularDVV(dvhs);
            string dvvAlmacenado = DAL_DVVertical.Get(TablaReservas);

            if (dvvAlmacenado == null)
            {
                // Primera corrida: establecer linea base, no es una inconsistencia.
                DAL_DVVertical.Upsert(TablaReservas, dvvCalculado);
            }
            else if (dvvAlmacenado != dvvCalculado)
            {
                resultado.Inconsistencias.Add("DV vertical de Reservas no coincide (filas agregadas, quitadas o reordenadas por fuera del sistema).");
            }

            if (!resultado.Ok)
            {
                BLL_Bitacora.Registrar("Integridad", "Verificacion de integridad fallida",
                    CriticidadBitacora.Error,
                    string.Join(" | ", resultado.Inconsistencias));
            }
            return resultado;
        }
    }
}
