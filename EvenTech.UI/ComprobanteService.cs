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
    public static class ComprobanteService
    {
        // Paleta de marca (espejo de Theme, en hex para el CSS embebido).
        private const string Navy = "#242B49";
        private const string Gold = "#9D7035";
        private const string GoldSoft = "#B9A05B";
        private const string Green = "#218838";
        private const string Ink = "#212529";
        private const string Muted = "#6C757D";
        private const string Line = "#DFE3E9";
        private const string Soft = "#F6F7F9";

        public static string GenerarHtml(int reservaId)
        {
            var reserva = BLL_Reserva.GetById(reservaId);
            if (reserva == null) return null;

            BE_Cliente cliente = reserva.ClienteId > 0 ? BLL_Cliente.GetById(reserva.ClienteId) : null;
            List<BE_ReservaServicio> servicios = BLL_ReservaServicio.GetByReserva(reservaId);
            List<BE_Pago> pagos = BLL_Pago.GetByReserva(reservaId);

            decimal total = reserva.Monto;
            decimal pagado = BLL_Pago.TotalPagado(reservaId);
            decimal saldo = total - pagado;

            string estadoPago, estadoColor;
            if (total > 0 && saldo <= 0) { estadoPago = T("CMP_EST_PAGADO", "Pagado"); estadoColor = Green; }
            else if (pagado > 0) { estadoPago = T("CMP_EST_PARCIAL", "Pago parcial"); estadoColor = Gold; }
            else { estadoPago = T("CMP_EST_PENDIENTE", "Pendiente"); estadoColor = Muted; }

            // El documento correspondiente al estado (Proceso 1, paso 6): una
            // cotizacion emite un presupuesto (sin compromiso del salon); una
            // reserva emite el comprobante propiamente dicho.
            bool esPresupuesto = reserva.Estado == EstadoReserva.COTIZACION;
            string docTitulo = esPresupuesto
                ? T("CMP_TITULO_PRESUPUESTO", "Presupuesto")
                : T("CMP_TITULO", "Comprobante de Reserva");
            string docNro = esPresupuesto
                ? T("CMP_DOC_NRO_PRESUPUESTO", "Presupuesto N")
                : T("CMP_DOC_NRO", "Comprobante N");

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\">");
            sb.Append("<title>").Append(E(docTitulo))
              .Append(" #").Append(reservaId).Append("</title>");
            sb.Append("<style>")
              .Append("*{box-sizing:border-box;}")
              .Append("body{font-family:'Segoe UI',Ebrima,Arial,sans-serif;color:").Append(Ink).Append(";margin:0;background:").Append(Soft).Append(";}")
              .Append(".sheet{max-width:780px;margin:24px auto;background:#fff;border:1px solid ").Append(Line).Append(";border-radius:10px;overflow:hidden;}")
              .Append(".head{background:").Append(Navy).Append(";color:#fff;padding:24px 32px;display:flex;justify-content:space-between;align-items:flex-start;}")
              .Append(".brand{font-size:26px;font-weight:bold;letter-spacing:.5px;}")
              .Append(".brand small{display:block;font-size:12px;font-weight:normal;color:").Append(GoldSoft).Append(";letter-spacing:2px;margin-top:2px;}")
              .Append(".doc{text-align:right;font-size:13px;color:#cfd5e0;}")
              .Append(".doc b{color:#fff;font-size:15px;}")
              .Append(".body{padding:24px 32px;}")
              .Append(".grid{display:flex;gap:32px;margin-bottom:20px;}")
              .Append(".grid .col{flex:1;}")
              .Append("h2{font-size:12px;text-transform:uppercase;letter-spacing:1px;color:").Append(Gold).Append(";border-bottom:2px solid ").Append(Line).Append(";padding-bottom:6px;margin:0 0 10px;}")
              .Append(".row{font-size:14px;margin:4px 0;}")
              .Append(".row span{color:").Append(Muted).Append(";display:inline-block;min-width:90px;}")
              .Append("table{width:100%;border-collapse:collapse;margin-top:6px;font-size:14px;}")
              .Append("th{background:").Append(Navy).Append(";color:#fff;text-align:left;padding:9px 10px;font-size:12px;text-transform:uppercase;letter-spacing:.5px;}")
              .Append("td{padding:9px 10px;border-bottom:1px solid ").Append(Line).Append(";}")
              .Append("tr:nth-child(even) td{background:").Append(Soft).Append(";}")
              .Append(".num{text-align:right;white-space:nowrap;}")
              .Append(".totals{margin-top:18px;margin-left:auto;width:300px;font-size:14px;}")
              .Append(".totals .t{display:flex;justify-content:space-between;padding:6px 0;}")
              .Append(".totals .grand{border-top:2px solid ").Append(Navy).Append(";font-weight:bold;font-size:16px;padding-top:8px;}")
              .Append(".totals .saldo{color:").Append(estadoColor).Append(";font-weight:bold;}")
              .Append(".badge{display:inline-block;padding:4px 12px;border-radius:14px;font-size:12px;font-weight:bold;color:#fff;background:").Append(estadoColor).Append(";}")
              .Append(".empty{color:").Append(Muted).Append(";font-style:italic;padding:10px 0;}")
              .Append(".foot{padding:18px 32px;border-top:1px solid ").Append(Line).Append(";color:").Append(Muted).Append(";font-size:12px;text-align:center;}")
              .Append("@media print{body{background:#fff;}.sheet{border:0;margin:0;max-width:none;}}")
              .Append("</style></head><body><div class=\"sheet\">");

            // ---- Encabezado ----
            sb.Append("<div class=\"head\"><div class=\"brand\">EvenTech<small>")
              .Append(E(T("CMP_TAGLINE", "GESTION DE EVENTOS"))).Append("</small></div>");
            sb.Append("<div class=\"doc\">").Append(E(docNro))
              .Append("<b> #").Append(reservaId).Append("</b><br>")
              .Append(E(T("CMP_EMITIDO", "Emitido"))).Append(": ")
              .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).Append("<br>")
              .Append("<span class=\"badge\">").Append(E(estadoPago)).Append("</span></div></div>");

            sb.Append("<div class=\"body\">");

            // ---- Datos de cliente y evento ----
            sb.Append("<div class=\"grid\"><div class=\"col\"><h2>").Append(E(T("COL_CLIENTE", "Cliente"))).Append("</h2>");
            sb.Append("<div class=\"row\">").Append(E(cliente != null ? cliente.NombreCompleto : reserva.ClienteNombre ?? "-")).Append("</div>");
            if (cliente != null)
            {
                if (!string.IsNullOrWhiteSpace(cliente.Dni)) sb.Append("<div class=\"row\"><span>").Append(E(T("LBL_DNI", "DNI"))).Append(":</span>").Append(E(cliente.Dni)).Append("</div>");
                if (!string.IsNullOrWhiteSpace(cliente.Email)) sb.Append("<div class=\"row\"><span>").Append(E(T("LBL_EMAIL", "Email"))).Append(":</span>").Append(E(cliente.Email)).Append("</div>");
                if (!string.IsNullOrWhiteSpace(cliente.Telefono)) sb.Append("<div class=\"row\"><span>").Append(E(T("LBL_TELEFONO", "Tel"))).Append(":</span>").Append(E(cliente.Telefono)).Append("</div>");
            }
            sb.Append("</div><div class=\"col\"><h2>").Append(E(T("CMP_EVENTO", "Evento"))).Append("</h2>");
            sb.Append("<div class=\"row\"><span>").Append(E(T("COL_SALON", "Salon"))).Append(":</span>").Append(E(reserva.SalonNombre ?? "-")).Append("</div>");
            sb.Append("<div class=\"row\"><span>").Append(E(T("RES_LBL_FECHA", "Fecha del evento"))).Append(":</span>").Append(reserva.FechaEvento.ToString("yyyy-MM-dd")).Append("</div>");
            sb.Append("<div class=\"row\"><span>").Append(E(T("COL_ESTADO", "Estado"))).Append(":</span>").Append(E(Tr.Estado(reserva.Estado))).Append("</div>");
            sb.Append("</div></div>");

            // ---- Servicios ----
            sb.Append("<h2>").Append(E(T("CMP_DETALLE_SERVICIOS", "Detalle de servicios"))).Append("</h2>");
            if (servicios.Count == 0)
            {
                sb.Append("<div class=\"empty\">").Append(E(T("CMP_SIN_SERVICIOS", "Sin servicios contratados."))).Append("</div>");
            }
            else
            {
                sb.Append("<table><thead><tr><th>").Append(E(T("COL_SERVICIO", "Servicio")))
                  .Append("</th><th class=\"num\">").Append(E(T("COL_CANTIDAD", "Cantidad")))
                  .Append("</th><th class=\"num\">").Append(E(T("COL_PRECIO", "Precio")))
                  .Append("</th><th class=\"num\">").Append(E(T("COL_SUBTOTAL", "Subtotal")))
                  .Append("</th></tr></thead><tbody>");
                foreach (var s in servicios)
                    sb.Append("<tr><td>").Append(E(s.ServicioNombre))
                      .Append("</td><td class=\"num\">").Append(s.Cantidad)
                      .Append("</td><td class=\"num\">").Append(s.PrecioUnitario.ToString("N2"))
                      .Append("</td><td class=\"num\">").Append(s.Subtotal.ToString("N2"))
                      .Append("</td></tr>");
                sb.Append("</tbody></table>");
            }

            // ---- Totales ----
            sb.Append("<div class=\"totals\">");
            sb.Append("<div class=\"t grand\"><div>").Append(E(T("LBL_TOTAL", "Total"))).Append("</div><div class=\"num\">").Append(total.ToString("N2")).Append("</div></div>");
            sb.Append("<div class=\"t\"><div>").Append(E(T("LBL_PAGADO", "Pagado"))).Append("</div><div class=\"num\">").Append(pagado.ToString("N2")).Append("</div></div>");
            sb.Append("<div class=\"t saldo\"><div>").Append(E(T("LBL_SALDO", "Saldo"))).Append("</div><div class=\"num\">").Append(saldo.ToString("N2")).Append("</div></div>");
            sb.Append("</div>");

            // ---- Pagos ----
            sb.Append("<h2 style=\"margin-top:24px;\">").Append(E(T("RES_PAGOS", "Pagos de la reserva"))).Append("</h2>");
            if (pagos.Count == 0)
            {
                sb.Append("<div class=\"empty\">").Append(E(T("CMP_SIN_PAGOS", "Sin pagos registrados."))).Append("</div>");
            }
            else
            {
                sb.Append("<table><thead><tr><th>").Append(E(T("COL_FECHA", "Fecha")))
                  .Append("</th><th>").Append(E(T("COL_METODO", "Metodo")))
                  .Append("</th><th>").Append(E(T("COL_OBSERVACION", "Observacion")))
                  .Append("</th><th class=\"num\">").Append(E(T("COL_MONTO", "Monto")))
                  .Append("</th></tr></thead><tbody>");
                foreach (var p in pagos)
                    sb.Append("<tr><td>").Append(p.Fecha.ToString("yyyy-MM-dd HH:mm"))
                      .Append("</td><td>").Append(E(p.MetodoNombre))
                      .Append("</td><td>").Append(E(p.Observacion ?? ""))
                      .Append("</td><td class=\"num\">").Append(p.Monto.ToString("N2"))
                      .Append("</td></tr>");
                sb.Append("</tbody></table>");
            }

            sb.Append("</div>"); // body
            sb.Append("<div class=\"foot\">")
              .Append(E(esPresupuesto
                  ? T("CMP_PRESUPUESTO_NOTA", "Presupuesto sin compromiso de reserva. Sujeto a disponibilidad del salon al momento de confirmar.")
                  : T("CMP_GRACIAS", "Gracias por su reserva.")))
              .Append("</div>");
            sb.Append("</div></body></html>");
            return sb.ToString();
        }

        private static string E(string s) => WebUtility.HtmlEncode(s ?? "");

        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }
    }
}
