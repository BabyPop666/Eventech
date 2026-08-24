using System;
using System.Collections.Generic;
using System.Linq;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    // Consulta de disponibilidad (Proceso 1, paso 1): el vendedor indica fecha
    // del evento y cantidad estimada de invitados y el sistema informa que
    // salones se pueden comprometer. Si un salon no esta libre, se calcula la
    // propuesta alternativa (proxima fecha sin reserva confirmada) para poder
    // ofrecerle al cliente otra opcion de similar preferencia.
    public static class BLL_Disponibilidad
    {
        // Hasta cuantos dias hacia adelante se busca una fecha alternativa.
        public const int HorizontePropuestasDias = 60;

        public static List<BE_DisponibilidadSalon> Consultar(DateTime fecha, int capacidadRequerida)
        {
            fecha = fecha.Date;
            if (fecha < DateTime.Today) fecha = DateTime.Today;
            if (capacidadRequerida < 0) capacidadRequerida = 0;

            List<BE_Salon> salones = DAL_Salon.GetAll();
            Dictionary<int, HashSet<DateTime>> ocupadas =
                DAL_Reserva.FechasConfirmadasPorSalon(fecha, fecha.AddDays(HorizontePropuestasDias));

            var resultado = new List<BE_DisponibilidadSalon>();
            foreach (var salon in salones)
            {
                ocupadas.TryGetValue(salon.Id, out var fechasSalon);
                bool libre = fechasSalon == null || !fechasSalon.Contains(fecha);
                bool capacidadOk = salon.Capacidad >= capacidadRequerida;

                var item = new BE_DisponibilidadSalon
                {
                    SalonId = salon.Id,
                    SalonNombre = salon.Nombre,
                    Capacidad = salon.Capacidad,
                    FechaConsultada = fecha,
                    Libre = libre,
                    CapacidadSuficiente = capacidadOk
                };

                // Propuesta alternativa solo si el salon serviria pero esta
                // tomado ese dia: primera fecha posterior sin reserva firme.
                if (!libre && capacidadOk)
                {
                    for (int i = 1; i <= HorizontePropuestasDias; i++)
                    {
                        DateTime candidata = fecha.AddDays(i);
                        if (!fechasSalon.Contains(candidata))
                        {
                            item.ProximaFechaLibre = candidata;
                            break;
                        }
                    }
                }

                resultado.Add(item);
            }

            // Orden de "similar preferencia": primero lo que se puede reservar
            // tal cual, despues lo ocupado con capacidad (tiene propuesta) y al
            // final lo chico; dentro de cada grupo, capacidad mas cercana a la
            // pedida primero.
            return resultado
                .OrderByDescending(d => d.Disponible)
                .ThenByDescending(d => d.CapacidadSuficiente)
                .ThenBy(d => Math.Abs(d.Capacidad - capacidadRequerida))
                .ThenBy(d => d.SalonNombre)
                .ToList();
        }
    }
}
