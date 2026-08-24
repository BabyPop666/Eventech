using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    // Resultado de la verificacion de integridad al iniciar la aplicacion.
    public class ResultadoIntegridad_704ILR
    {
        public bool Ok_704ILR => Inconsistencias_704ILR.Count == 0;
        public List<string> Inconsistencias_704ILR { get; } = new List<string>();
    }

    // Coordina el calculo y verificacion de los digitos verificadores sobre la
    // entidad Reserva (T07/T08), usando el mecanismo generico de Services.
    public static class BLL_Integridad_704ILR
    {
        private const string TablaReservas_704ILR = "Reservas";

        // Recalcula el DV horizontal de TODAS las reservas y luego el vertical.
        // Util tras corregir datos corruptos o tras una migracion que cambia los
        // campos que entran al DV (deja la linea base consistente para que la
        // verificacion al arranque no falle). Devuelve cuantas reservas proceso.
        // Es una accion administrativa: queda registrada en bitacora.
        public static int RecalcularTodo_704ILR()
        {
            var reservas_704ILR = DAL_Reserva_704ILR.GetAll_704ILR();
            foreach (var r_704ILR in reservas_704ILR)
                DAL_Reserva_704ILR.UpdateDvh_704ILR(r_704ILR.Id_704ILR, ValidadorDeIntegridad_704ILR.CalcularDVH_704ILR(r_704ILR));
            RecalcularDVVerticalReservas_704ILR();

            BLL_Bitacora_704ILR.Registrar_704ILR("Integridad", "Recalculo de linea base",
                CriticidadBitacora_704ILR.Advertencia,
                $"Se recalculo el DVH de {reservas_704ILR.Count} reserva(s) y el DVV del conjunto");
            return reservas_704ILR.Count;
        }

        // Recalcula el DV vertical de Reservas a partir de los DVH almacenados.
        // Se invoca tras cada alta/modificacion para mantener la linea base.
        public static void RecalcularDVVerticalReservas_704ILR()
        {
            var reservas_704ILR = DAL_Reserva_704ILR.GetAll_704ILR();
            var dvhs_704ILR = new List<string>();
            // Orden estable (por Id ascendente) para que la posicion sea consistente.
            reservas_704ILR.Sort((a_704ILR, b_704ILR) => a_704ILR.Id_704ILR.CompareTo(b_704ILR.Id_704ILR));
            foreach (var r_704ILR in reservas_704ILR) dvhs_704ILR.Add(r_704ILR.Dvh_704ILR ?? string.Empty);

            string dvv_704ILR = ValidadorDeIntegridad_704ILR.CalcularDVV_704ILR(dvhs_704ILR);
            DAL_DVVertical_704ILR.Upsert_704ILR(TablaReservas_704ILR, dvv_704ILR);
        }

        // Verificacion de integridad: se ejecuta al arrancar, antes del login.
        public static ResultadoIntegridad_704ILR Verificar_704ILR()
        {
            var resultado_704ILR = new ResultadoIntegridad_704ILR();
            var reservas_704ILR = DAL_Reserva_704ILR.GetAll_704ILR();
            reservas_704ILR.Sort((a_704ILR, b_704ILR) => a_704ILR.Id_704ILR.CompareTo(b_704ILR.Id_704ILR));

            var dvhs_704ILR = new List<string>();
            foreach (var r_704ILR in reservas_704ILR)
            {
                // DV horizontal: recalcular y comparar contra lo almacenado.
                string dvhCalculado_704ILR = ValidadorDeIntegridad_704ILR.CalcularDVH_704ILR(r_704ILR);
                dvhs_704ILR.Add(r_704ILR.Dvh_704ILR ?? string.Empty);

                if (r_704ILR.Dvh_704ILR == null)
                    resultado_704ILR.Inconsistencias_704ILR.Add($"Reserva #{r_704ILR.Id_704ILR}: sin DV horizontal almacenado.");
                else if (r_704ILR.Dvh_704ILR != dvhCalculado_704ILR)
                    resultado_704ILR.Inconsistencias_704ILR.Add($"Reserva #{r_704ILR.Id_704ILR}: DV horizontal no coincide (posible alteracion externa).");
            }

            // DV vertical: recalcular sobre el conjunto y comparar contra lo almacenado.
            string dvvCalculado_704ILR = ValidadorDeIntegridad_704ILR.CalcularDVV_704ILR(dvhs_704ILR);
            string dvvAlmacenado_704ILR = DAL_DVVertical_704ILR.Get_704ILR(TablaReservas_704ILR);

            if (dvvAlmacenado_704ILR == null)
            {
                // Primera corrida: establecer linea base, no es una inconsistencia.
                DAL_DVVertical_704ILR.Upsert_704ILR(TablaReservas_704ILR, dvvCalculado_704ILR);
            }
            else if (dvvAlmacenado_704ILR != dvvCalculado_704ILR)
            {
                resultado_704ILR.Inconsistencias_704ILR.Add("DV vertical de Reservas no coincide (filas agregadas, quitadas o reordenadas por fuera del sistema).");
            }

            if (!resultado_704ILR.Ok_704ILR)
            {
                BLL_Bitacora_704ILR.Registrar_704ILR("Integridad", "Verificacion de integridad fallida",
                    CriticidadBitacora_704ILR.Error,
                    string.Join(" | ", resultado_704ILR.Inconsistencias_704ILR));
            }
            return resultado_704ILR;
        }
    }
}
