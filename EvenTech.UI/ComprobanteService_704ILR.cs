using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using EvenTech.BE;
using EvenTech.BLL;

namespace EvenTech.UI
{
    // Genera el comprobante/presupuesto de una reserva como HTML imprimible
    // (Proceso 1, paso 6). Sin dependencias externas: se abre en el navegador y
    // se imprime con Ctrl+P. Usa Tr para seguir el idioma activo y la paleta de
    // marca (azul/dorado) para mantener la identidad visual.
    public static class ComprobanteService_704ILR
    {
        // Paleta de marca (espejo de Theme, en hex para el CSS embebido).
        private const string Navy_704ILR = "#242B49";
        private const string Gold_704ILR = "#9D7035";
        private const string GoldSoft_704ILR = "#B9A05B";
        private const string Green_704ILR = "#218838";
        private const string Ink_704ILR = "#212529";
        private const string Muted_704ILR = "#6C757D";
        private const string Line_704ILR = "#DFE3E9";
        private const string Soft_704ILR = "#F6F7F9";

        public static string GenerarHtml_704ILR(int reservaId_704ILR)
        {
            var reserva_704ILR = BLL_Reserva_704ILR.GetById_704ILR(reservaId_704ILR);
            if (reserva_704ILR == null) return null;

            BE_Cliente_704ILR cliente_704ILR = reserva_704ILR.ClienteId_704ILR > 0 ? BLL_Cliente_704ILR.GetById_704ILR(reserva_704ILR.ClienteId_704ILR) : null;
            List<BE_ReservaServicio_704ILR> servicios_704ILR = BLL_ReservaServicio_704ILR.GetByReserva_704ILR(reservaId_704ILR);
            List<BE_Pago_704ILR> pagos_704ILR = BLL_Pago_704ILR.GetByReserva_704ILR(reservaId_704ILR);

            decimal total_704ILR = reserva_704ILR.Monto_704ILR;
            decimal pagado_704ILR = BLL_Pago_704ILR.TotalPagado_704ILR(reservaId_704ILR);
            decimal saldo_704ILR = total_704ILR - pagado_704ILR;

            string estadoPago_704ILR, estadoColor_704ILR;
            if (total_704ILR > 0 && saldo_704ILR <= 0) { estadoPago_704ILR = T_704ILR("CMP_EST_PAGADO", "Pagado"); estadoColor_704ILR = Green_704ILR; }
            else if (pagado_704ILR > 0) { estadoPago_704ILR = T_704ILR("CMP_EST_PARCIAL", "Pago parcial"); estadoColor_704ILR = Gold_704ILR; }
            else { estadoPago_704ILR = T_704ILR("CMP_EST_PENDIENTE", "Pendiente"); estadoColor_704ILR = Muted_704ILR; }

            // El documento correspondiente al estado (Proceso 1, paso 6): una
            // cotizacion emite un presupuesto (sin compromiso del salon); una
            // reserva emite el comprobante propiamente dicho.
            bool esPresupuesto_704ILR = reserva_704ILR.Estado_704ILR == EstadoReserva_704ILR.COTIZACION;
            string docTitulo_704ILR = esPresupuesto_704ILR
                ? T_704ILR("CMP_TITULO_PRESUPUESTO", "Presupuesto")
                : T_704ILR("CMP_TITULO", "Comprobante de Reserva");
            string docNro_704ILR = esPresupuesto_704ILR
                ? T_704ILR("CMP_DOC_NRO_PRESUPUESTO", "Presupuesto N")
                : T_704ILR("CMP_DOC_NRO", "Comprobante N");

            var sb_704ILR = new StringBuilder();
            sb_704ILR.Append("<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\">");
            sb_704ILR.Append("<title>").Append(E_704ILR(docTitulo_704ILR))
              .Append(" #").Append(reservaId_704ILR).Append("</title>");
            sb_704ILR.Append("<style>")
              .Append("*{box-sizing:border-box;}")
              .Append("body{font-family:'Segoe UI',Ebrima,Arial,sans-serif;color:").Append(Ink_704ILR).Append(";margin:0;background:").Append(Soft_704ILR).Append(";}")
              .Append(".sheet{max-width:780px;margin:24px auto;background:#fff;border:1px solid ").Append(Line_704ILR).Append(";border-radius:10px;overflow:hidden;}")
              .Append(".head{background:").Append(Navy_704ILR).Append(";color:#fff;padding:24px 32px;display:flex;justify-content:space-between;align-items:flex-start;}")
              .Append(".brand{font-size:26px;font-weight:bold;letter-spacing:.5px;}")
              .Append(".brand small{display:block;font-size:12px;font-weight:normal;color:").Append(GoldSoft_704ILR).Append(";letter-spacing:2px;margin-top:2px;}")
              .Append(".doc{text-align:right;font-size:13px;color:#cfd5e0;}")
              .Append(".doc b{color:#fff;font-size:15px;}")
              .Append(".body{padding:24px 32px;}")
              .Append(".grid{display:flex;gap:32px;margin-bottom:20px;}")
              .Append(".grid .col{flex:1;}")
              .Append("h2{font-size:12px;text-transform:uppercase;letter-spacing:1px;color:").Append(Gold_704ILR).Append(";border-bottom:2px solid ").Append(Line_704ILR).Append(";padding-bottom:6px;margin:0 0 10px;}")
              .Append(".row{font-size:14px;margin:4px 0;}")
              .Append(".row span{color:").Append(Muted_704ILR).Append(";display:inline-block;min-width:90px;}")
              .Append("table{width:100%;border-collapse:collapse;margin-top:6px;font-size:14px;}")
              .Append("th{background:").Append(Navy_704ILR).Append(";color:#fff;text-align:left;padding:9px 10px;font-size:12px;text-transform:uppercase;letter-spacing:.5px;}")
              .Append("td{padding:9px 10px;border-bottom:1px solid ").Append(Line_704ILR).Append(";}")
              .Append("tr:nth-child(even) td{background:").Append(Soft_704ILR).Append(";}")
              .Append(".num{text-align:right;white-space:nowrap;}")
              .Append(".totals{margin-top:18px;margin-left:auto;width:300px;font-size:14px;}")
              .Append(".totals .t{display:flex;justify-content:space-between;padding:6px 0;}")
              .Append(".totals .grand{border-top:2px solid ").Append(Navy_704ILR).Append(";font-weight:bold;font-size:16px;padding-top:8px;}")
              .Append(".totals .saldo{color:").Append(estadoColor_704ILR).Append(";font-weight:bold;}")
              .Append(".badge{display:inline-block;padding:4px 12px;border-radius:14px;font-size:12px;font-weight:bold;color:#fff;background:").Append(estadoColor_704ILR).Append(";}")
              .Append(".empty{color:").Append(Muted_704ILR).Append(";font-style:italic;padding:10px 0;}")
              .Append(".foot{padding:18px 32px;border-top:1px solid ").Append(Line_704ILR).Append(";color:").Append(Muted_704ILR).Append(";font-size:12px;text-align:center;}")
              .Append("@media print{body{background:#fff;}.sheet{border:0;margin:0;max-width:none;}}")
              .Append("</style></head><body><div class=\"sheet\">");

            // ---- Encabezado ----
            sb_704ILR.Append("<div class=\"head\"><div class=\"brand\">EvenTech<small>")
              .Append(E_704ILR(T_704ILR("CMP_TAGLINE", "GESTION DE EVENTOS"))).Append("</small></div>");
            sb_704ILR.Append("<div class=\"doc\">").Append(E_704ILR(docNro_704ILR))
              .Append("<b> #").Append(reservaId_704ILR).Append("</b><br>")
              .Append(E_704ILR(T_704ILR("CMP_EMITIDO", "Emitido"))).Append(": ")
              .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).Append("<br>")
              .Append("<span class=\"badge\">").Append(E_704ILR(estadoPago_704ILR)).Append("</span></div></div>");

            sb_704ILR.Append("<div class=\"body\">");

            // ---- Datos de cliente y evento ----
            sb_704ILR.Append("<div class=\"grid\"><div class=\"col\"><h2>").Append(E_704ILR(T_704ILR("COL_CLIENTE", "Cliente"))).Append("</h2>");
            sb_704ILR.Append("<div class=\"row\">").Append(E_704ILR(cliente_704ILR != null ? cliente_704ILR.NombreCompleto_704ILR : reserva_704ILR.ClienteNombre_704ILR ?? "-")).Append("</div>");
            if (cliente_704ILR != null)
            {
                if (!string.IsNullOrWhiteSpace(cliente_704ILR.Dni_704ILR)) sb_704ILR.Append("<div class=\"row\"><span>").Append(E_704ILR(T_704ILR("LBL_DNI", "DNI"))).Append(":</span>").Append(E_704ILR(cliente_704ILR.Dni_704ILR)).Append("</div>");
                if (!string.IsNullOrWhiteSpace(cliente_704ILR.Email_704ILR)) sb_704ILR.Append("<div class=\"row\"><span>").Append(E_704ILR(T_704ILR("LBL_EMAIL", "Email"))).Append(":</span>").Append(E_704ILR(cliente_704ILR.Email_704ILR)).Append("</div>");
                if (!string.IsNullOrWhiteSpace(cliente_704ILR.Telefono_704ILR)) sb_704ILR.Append("<div class=\"row\"><span>").Append(E_704ILR(T_704ILR("LBL_TELEFONO", "Tel"))).Append(":</span>").Append(E_704ILR(cliente_704ILR.Telefono_704ILR)).Append("</div>");
            }
            sb_704ILR.Append("</div><div class=\"col\"><h2>").Append(E_704ILR(T_704ILR("CMP_EVENTO", "Evento"))).Append("</h2>");
            sb_704ILR.Append("<div class=\"row\"><span>").Append(E_704ILR(T_704ILR("COL_SALON", "Salon"))).Append(":</span>").Append(E_704ILR(reserva_704ILR.SalonNombre_704ILR ?? "-")).Append("</div>");
            sb_704ILR.Append("<div class=\"row\"><span>").Append(E_704ILR(T_704ILR("RES_LBL_FECHA", "Fecha del evento"))).Append(":</span>").Append(reserva_704ILR.FechaEvento_704ILR.ToString("yyyy-MM-dd")).Append("</div>");
            sb_704ILR.Append("<div class=\"row\"><span>").Append(E_704ILR(T_704ILR("COL_ESTADO", "Estado"))).Append(":</span>").Append(E_704ILR(Tr_704ILR.Estado_704ILR(reserva_704ILR.Estado_704ILR))).Append("</div>");
            sb_704ILR.Append("</div></div>");

            // ---- Servicios ----
            sb_704ILR.Append("<h2>").Append(E_704ILR(T_704ILR("CMP_DETALLE_SERVICIOS", "Detalle de servicios"))).Append("</h2>");
            if (servicios_704ILR.Count == 0)
            {
                sb_704ILR.Append("<div class=\"empty\">").Append(E_704ILR(T_704ILR("CMP_SIN_SERVICIOS", "Sin servicios contratados."))).Append("</div>");
            }
            else
            {
                sb_704ILR.Append("<table><thead><tr><th>").Append(E_704ILR(T_704ILR("COL_SERVICIO", "Servicio")))
                  .Append("</th><th class=\"num\">").Append(E_704ILR(T_704ILR("COL_CANTIDAD", "Cantidad")))
                  .Append("</th><th class=\"num\">").Append(E_704ILR(T_704ILR("COL_PRECIO", "Precio")))
                  .Append("</th><th class=\"num\">").Append(E_704ILR(T_704ILR("COL_SUBTOTAL", "Subtotal")))
                  .Append("</th></tr></thead><tbody>");
                foreach (var s_704ILR in servicios_704ILR)
                    sb_704ILR.Append("<tr><td>").Append(E_704ILR(s_704ILR.ServicioNombre_704ILR))
                      .Append("</td><td class=\"num\">").Append(s_704ILR.Cantidad_704ILR)
                      .Append("</td><td class=\"num\">").Append(s_704ILR.PrecioUnitario_704ILR.ToString("N2"))
                      .Append("</td><td class=\"num\">").Append(s_704ILR.Subtotal_704ILR.ToString("N2"))
                      .Append("</td></tr>");
                sb_704ILR.Append("</tbody></table>");
            }

            // ---- Totales ----
            sb_704ILR.Append("<div class=\"totals\">");
            sb_704ILR.Append("<div class=\"t grand\"><div>").Append(E_704ILR(T_704ILR("LBL_TOTAL", "Total"))).Append("</div><div class=\"num\">").Append(total_704ILR.ToString("N2")).Append("</div></div>");
            sb_704ILR.Append("<div class=\"t\"><div>").Append(E_704ILR(T_704ILR("LBL_PAGADO", "Pagado"))).Append("</div><div class=\"num\">").Append(pagado_704ILR.ToString("N2")).Append("</div></div>");
            sb_704ILR.Append("<div class=\"t saldo\"><div>").Append(E_704ILR(T_704ILR("LBL_SALDO", "Saldo"))).Append("</div><div class=\"num\">").Append(saldo_704ILR.ToString("N2")).Append("</div></div>");
            sb_704ILR.Append("</div>");

            // ---- Pagos ----
            sb_704ILR.Append("<h2 style=\"margin-top:24px;\">").Append(E_704ILR(T_704ILR("RES_PAGOS", "Pagos de la reserva"))).Append("</h2>");
            if (pagos_704ILR.Count == 0)
            {
                sb_704ILR.Append("<div class=\"empty\">").Append(E_704ILR(T_704ILR("CMP_SIN_PAGOS", "Sin pagos registrados."))).Append("</div>");
            }
            else
            {
                sb_704ILR.Append("<table><thead><tr><th>").Append(E_704ILR(T_704ILR("COL_FECHA", "Fecha")))
                  .Append("</th><th>").Append(E_704ILR(T_704ILR("COL_METODO", "Metodo")))
                  .Append("</th><th>").Append(E_704ILR(T_704ILR("COL_OBSERVACION", "Observacion")))
                  .Append("</th><th class=\"num\">").Append(E_704ILR(T_704ILR("COL_MONTO", "Monto")))
                  .Append("</th></tr></thead><tbody>");
                foreach (var p_704ILR in pagos_704ILR)
                    sb_704ILR.Append("<tr><td>").Append(p_704ILR.Fecha_704ILR.ToString("yyyy-MM-dd HH:mm"))
                      .Append("</td><td>").Append(E_704ILR(p_704ILR.MetodoNombre_704ILR))
                      .Append("</td><td>").Append(E_704ILR(p_704ILR.Observacion_704ILR ?? ""))
                      .Append("</td><td class=\"num\">").Append(p_704ILR.Monto_704ILR.ToString("N2"))
                      .Append("</td></tr>");
                sb_704ILR.Append("</tbody></table>");
            }

            sb_704ILR.Append("</div>"); // body
            sb_704ILR.Append("<div class=\"foot\">")
              .Append(E_704ILR(esPresupuesto_704ILR
                  ? T_704ILR("CMP_PRESUPUESTO_NOTA", "Presupuesto sin compromiso de reserva. Sujeto a disponibilidad del salon al momento de confirmar.")
                  : T_704ILR("CMP_GRACIAS", "Gracias por su reserva.")))
              .Append("</div>");
            sb_704ILR.Append("</div></body></html>");
            return sb_704ILR.ToString();
        }

        private static string E_704ILR(string s_704ILR) => WebUtility.HtmlEncode(s_704ILR ?? "");

        private static string T_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string t_704ILR = Tr_704ILR.T_704ILR(clave_704ILR);
            return t_704ILR == clave_704ILR ? defecto_704ILR : t_704ILR;
        }
    }
}
