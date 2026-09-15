using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using EvenTech.BE;
using EvenTech.BLL;
using EvenTech.Services;

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

            // El documento correspondiente al estado (Proceso 1, paso 6): el
            // comprobante es el de una reserva CONFIRMADA, que es la unica que
            // compromete el salon (RN-03). Mientras la operacion sigue abierta
            // —COTIZACION o PENDIENTE— lo que se emite es un presupuesto, sin
            // compromiso, tal como lo describe el proceso de negocio.
            bool esPresupuesto_704ILR = reserva_704ILR.Estado_704ILR != EstadoReserva_704ILR.CONFIRMADA;
            string docTitulo_704ILR = esPresupuesto_704ILR
                ? T_704ILR("CMP_TITULO_PRESUPUESTO", "Presupuesto")
                : T_704ILR("CMP_TITULO", "Comprobante de Reserva");
            string docNro_704ILR = esPresupuesto_704ILR
                ? T_704ILR("CMP_DOC_NRO_PRESUPUESTO", "Presupuesto N")
                : T_704ILR("CMP_DOC_NRO", "Comprobante N");

            // El atributo lang sigue al idioma activo (Observer de idiomas): los
            // textos del documento salen traducidos, y declararlo siempre "es" era
            // incoherente para el navegador y los lectores de pantalla.
            string lang_704ILR = (GestorDeIdioma_704ILR.GetInstance_704ILR.IdiomaActual_704ILR ?? "es").ToLowerInvariant();

            var sb_704ILR = new StringBuilder();
            sb_704ILR.Append("<!DOCTYPE html><html lang=\"").Append(E_704ILR(lang_704ILR)).Append("\"><head><meta charset=\"utf-8\">");
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
              .Append(".grid .col{flex:1;min-width:0;}")
              .Append("h2{font-size:12px;text-transform:uppercase;letter-spacing:1px;color:").Append(Gold_704ILR).Append(";border-bottom:2px solid ").Append(Line_704ILR).Append(";padding-bottom:6px;margin:0 0 10px;}")
              // Rotulo y valor: el rotulo reserva un ancho comun y siempre deja aire
              // antes del valor ("Fecha del evento:" no entraba en 90px y se pegaba).
              // Los textos libres (nombre, correo, servicio, observacion) pueden no
              // tener espacios: se parten dentro de su caja para que la hoja no pierda
              // columnas. Fechas e importes no se parten.
              .Append(".row{font-size:14px;margin:4px 0;overflow-wrap:anywhere;word-break:break-word;}")
              .Append(".row span{color:").Append(Muted_704ILR).Append(";display:inline-block;min-width:120px;padding-right:8px;}")
              .Append("table{width:100%;border-collapse:collapse;margin-top:6px;font-size:14px;}")
              .Append("th{background:").Append(Navy_704ILR).Append(";color:#fff;text-align:left;padding:9px 10px;font-size:12px;text-transform:uppercase;letter-spacing:.5px;}")
              .Append("td{padding:9px 10px;border-bottom:1px solid ").Append(Line_704ILR).Append(";}")
              .Append("tr:nth-child(even) td{background:").Append(Soft_704ILR).Append(";}")
              .Append(".num{text-align:right;white-space:nowrap;}")
              .Append(".txt{overflow-wrap:anywhere;word-break:break-word;}")
              // Celda del metodo de pago con una palabra larga: se parte (txt) y declara un
              // ancho. En el reparto automatico de la tabla una columna con ancho declarado
              // crece antes que las demas: la palabra queda entera mientras la hoja tenga
              // lugar (hasta 14em) y se parte dentro de su celda cuando no lo tiene. Con
              // "txt" sola la Observacion se llevaba el espacio y partia hasta "Transferencia".
              .Append(".met{width:14em;}")
              // La Observacion que sigue a esa celda conserva un ancho minimo. Su encabezado no
              // siempre la sostiene ("Note" en ingles es corto): sin minimo, la columna del metodo
              // se llevaba el espacio y a 560 px la Observacion quedaba en 53-68 px, partiendo
              // palabras comunes letra por letra. Con 8em las palabras quedan enteras y nada sale
              // de la hoja; con metodos de palabras cortas (sin "met") la regla no aplica.
              .Append(".met+.txt{min-width:8em;}")
              .Append(".nw{white-space:nowrap;}")
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
            // Las fechas del documento (emision, evento y pagos) van en gregoriano con separadores
            // invariantes, como las de la bitacora: con la cultura de la estacion th-TH emitia 2569 y
            // fi-FI 01.15. Los importes siguen con el formato de la cultura (N2).
            sb_704ILR.Append("<div class=\"head\"><div class=\"brand\">EvenTech<small>")
              .Append(E_704ILR(T_704ILR("CMP_TAGLINE", "GESTIÓN DE EVENTOS"))).Append("</small></div>");
            sb_704ILR.Append("<div class=\"doc\">").Append(E_704ILR(docNro_704ILR))
              .Append("<b> #").Append(reservaId_704ILR).Append("</b><br>")
              .Append(E_704ILR(T_704ILR("CMP_EMITIDO", "Emitido"))).Append(": ")
              .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)).Append("<br>")
              .Append("<span class=\"badge\">").Append(E_704ILR(estadoPago_704ILR)).Append("</span></div></div>");

            sb_704ILR.Append("<div class=\"body\">");

            // ---- Datos de cliente y evento ----
            sb_704ILR.Append("<div class=\"grid\"><div class=\"col\"><h2>").Append(E_704ILR(T_704ILR("COL_CLIENTE", "Cliente"))).Append("</h2>");
            sb_704ILR.Append("<div class=\"row\">").Append(E_704ILR(cliente_704ILR != null ? cliente_704ILR.NombreCompleto_704ILR : reserva_704ILR.ClienteNombre_704ILR ?? "-")).Append("</div>");
            if (cliente_704ILR != null)
            {
                if (!string.IsNullOrWhiteSpace(cliente_704ILR.Dni_704ILR)) sb_704ILR.Append("<div class=\"row\"><span>").Append(E_704ILR(T_704ILR("LBL_DNI", "DNI"))).Append(":</span>").Append(E_704ILR(cliente_704ILR.Dni_704ILR)).Append("</div>");
                if (!string.IsNullOrWhiteSpace(cliente_704ILR.Email_704ILR)) sb_704ILR.Append("<div class=\"row\"><span>").Append(E_704ILR(T_704ILR("LBL_EMAIL", "Email"))).Append(":</span>").Append(E_704ILR(ContactoLegible_704ILR(cliente_704ILR.Email_704ILR))).Append("</div>");
                if (!string.IsNullOrWhiteSpace(cliente_704ILR.Telefono_704ILR)) sb_704ILR.Append("<div class=\"row\"><span>").Append(E_704ILR(T_704ILR("LBL_TELEFONO", "Tel"))).Append(":</span>").Append(E_704ILR(ContactoLegible_704ILR(cliente_704ILR.Telefono_704ILR))).Append("</div>");
            }
            sb_704ILR.Append("</div><div class=\"col\"><h2>").Append(E_704ILR(T_704ILR("CMP_EVENTO", "Evento"))).Append("</h2>");
            sb_704ILR.Append("<div class=\"row\"><span>").Append(E_704ILR(T_704ILR("COL_SALON", "Salón"))).Append(":</span>").Append(E_704ILR(reserva_704ILR.SalonNombre_704ILR ?? "-")).Append("</div>");
            sb_704ILR.Append("<div class=\"row\"><span>").Append(E_704ILR(T_704ILR("RES_LBL_FECHA", "Fecha del evento"))).Append(":</span>").Append(reserva_704ILR.FechaEvento_704ILR.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append("</div>");
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
                    sb_704ILR.Append("<tr><td class=\"txt\">").Append(E_704ILR(s_704ILR.ServicioNombre_704ILR))
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
                  .Append("</th><th>").Append(E_704ILR(T_704ILR("COL_METODO", "Método")))
                  .Append("</th><th>").Append(E_704ILR(T_704ILR("COL_OBSERVACION", "Observación")))
                  .Append("</th><th class=\"num\">").Append(E_704ILR(T_704ILR("COL_MONTO", "Monto")))
                  .Append("</th></tr></thead><tbody>");
                // El nombre del metodo es texto libre (catalogo de hasta 50 caracteres o una
                // traduccion editable). Con palabras cortas la celda es la comun: su palabra
                // mas larga fija el ancho de la columna y no se parte. Una palabra larga, sin
                // partirse, fijaba el ancho minimo de la tabla y la hoja recortaba Observacion
                // y Monto: esa celda se parte con prioridad de ancho (ClaseCeldaMetodo_704ILR).
                foreach (var p_704ILR in pagos_704ILR)
                {
                    string metodo_704ILR = frmReservaPagos_704ILR.TextoMetodo_704ILR(p_704ILR.MetodoNombre_704ILR);
                    sb_704ILR.Append("<tr><td class=\"nw\">").Append(p_704ILR.Fecha_704ILR.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
                      .Append("</td><td").Append(ClaseCeldaMetodo_704ILR(metodo_704ILR)).Append('>').Append(E_704ILR(metodo_704ILR))
                      .Append("</td><td class=\"txt\">").Append(E_704ILR(p_704ILR.Observacion_704ILR ?? ""))
                      .Append("</td><td class=\"num\">").Append(p_704ILR.Monto_704ILR.ToString("N2"))
                      .Append("</td></tr>");
                }
                sb_704ILR.Append("</tbody></table>");
            }

            sb_704ILR.Append("</div>"); // body
            sb_704ILR.Append("<div class=\"foot\">")
              .Append(E_704ILR(esPresupuesto_704ILR
                  ? T_704ILR("CMP_PRESUPUESTO_NOTA", "Presupuesto sin compromiso de reserva. Sujeto a disponibilidad del salón al momento de confirmar.")
                  : T_704ILR("CMP_GRACIAS", "Gracias por su reserva.")))
              .Append("</div>");
            sb_704ILR.Append("</div></body></html>");
            return sb_704ILR.ToString();
        }

        // Un contacto que quedo cifrado con la clave de otra instalacion (la base se restauro en otra
        // PC y la lectura devuelve el paquete "ENC:..." tal cual) no se imprime como si fuera el dato
        // del cliente: se aclara que no se puede leer, con el mismo criterio que Email aplica al
        // destinatario (CryptoService_704ILR.EstaProtegido_704ILR).
        private static string ContactoLegible_704ILR(string valor_704ILR)
            => CryptoService_704ILR.EstaProtegido_704ILR(valor_704ILR.Trim())
                ? T_704ILR("CMP_DATO_ILEGIBLE", "(no se puede leer en este equipo)")
                : valor_704ILR;

        private static string E_704ILR(string s_704ILR) => WebUtility.HtmlEncode(s_704ILR ?? "");

        // Palabra mas larga (en caracteres) con la que la celda del metodo de pago se emite
        // comun. Hasta 15 caracteres de ancho corriente, tambien en mayusculas, la palabra entra
        // entera en su columna aun con la hoja a 560 px e importes de ocho cifras; los nombres
        // de fabrica llegan a 13 ("Transferencia").
        private const int LargoPalabraMetodo_704ILR = 15;

        // Atributo de clase de la celda del metodo: ninguno si todas sus palabras son cortas
        // (la celda de siempre, que nunca parte "Transferencia" ni "MercadoPago"); "txt met" si
        // alguna supera LargoPalabraMetodo_704ILR (se parte dentro de su celda, con prioridad
        // de ancho sobre la Observacion). Un espacio de no separacion no corta la palabra: el
        // navegador tampoco pasa de linea en el.
        private static string ClaseCeldaMetodo_704ILR(string metodo_704ILR)
        {
            int largo_704ILR = 0;
            foreach (char c_704ILR in metodo_704ILR ?? "")
            {
                bool separa_704ILR = char.IsWhiteSpace(c_704ILR) && c_704ILR != '\u00A0' && c_704ILR != '\u2007' && c_704ILR != '\u202F';
                largo_704ILR = separa_704ILR ? 0 : largo_704ILR + 1;
                if (largo_704ILR > LargoPalabraMetodo_704ILR) return " class=\"txt met\"";
            }
            return "";
        }

        private static string T_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string t_704ILR = Tr_704ILR.T_704ILR(clave_704ILR);
            return t_704ILR == clave_704ILR ? defecto_704ILR : t_704ILR;
        }
    }
}
