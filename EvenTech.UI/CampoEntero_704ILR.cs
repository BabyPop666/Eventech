using System;
using System.Globalization;
using System.Windows.Forms;

namespace EvenTech.UI
{
    // Campo numerico para cantidades enteras (invitados, unidades de un servicio).
    // El NumericUpDown estandar con DecimalPlaces = 0 NO redondea lo que se tipea: con
    // "250,5" el valor quedaba en 250,5, el campo mostraba "251" y el codigo, al pasarlo
    // a int, guardaba 250. Ademas lo interpreta con decimal.Parse, que admite el
    // separador de miles en cualquier posicion: "250.5" en es-AR se leia como 2505.
    // Este campo solo admite digitos. Al tipear, el separador decimal y cualquier otro
    // caracter se descartan; un texto pegado que no sea un entero sin signo (con
    // separadores, signo, letras o exponente) no se interpreta: se descarta y el campo
    // vuelve a mostrar el ultimo valor valido. Asi el numero que se ve es siempre el
    // que se guarda, se consulta y se contrata.
    internal class CampoEntero_704ILR : NumericUpDown
    {
        public CampoEntero_704ILR()
        {
            DecimalPlaces = 0;
            ThousandsSeparator = false;
            Hexadecimal = false;
        }

        // Tipeo: digitos ASCII y teclas de control (borrar, Ctrl+C, Ctrl+V, Ctrl+Z...).
        // El control base deja pasar tambien el separador decimal, el de miles y el signo.
        protected override void OnTextBoxKeyPress(object source_704ILR, KeyPressEventArgs e_704ILR)
        {
            base.OnTextBoxKeyPress(source_704ILR, e_704ILR);
            if (e_704ILR.Handled) return;
            if ((e_704ILR.KeyChar >= '0' && e_704ILR.KeyChar <= '9') || char.IsControl(e_704ILR.KeyChar)) return;
            e_704ILR.Handled = true;
            System.Media.SystemSounds.Beep.Play();   // la misma senal que da el control base ante una tecla invalida
        }

        // Todas las vias por las que el control base interpreta el texto editado (leer
        // Value, Enter, perder el foco, flechas, rueda) pasan por estos cuatro metodos:
        // cada uno aplica primero la lectura estricta, y la del control base ya no corre.
        protected override void ValidateEditText()
        {
            AplicarTextoEditado_704ILR();
            UpdateEditText();
        }

        protected override void UpdateEditText()
        {
            AplicarTextoEditado_704ILR();
            base.UpdateEditText();
        }

        public override void UpButton()
        {
            AplicarTextoEditado_704ILR();
            base.UpButton();
        }

        public override void DownButton()
        {
            AplicarTextoEditado_704ILR();
            base.DownButton();
        }

        // Rueda del mouse: una unidad por muesca, como las flechas. El control base aplica
        // SystemInformation.MouseWheelScrollLines incrementos por muesca (3 por defecto): una
        // muesca sobre Cantidad contrataba 4 unidades y Invitados pasaba de 40 a 43 sin tipear.
        // El desplazamiento se acumula para las ruedas de alta resolucion (menos de una muesca
        // por evento) y se conservan las excepciones de la base: con Shift o Alt, con un boton
        // del mouse apretado o con el desplazamiento por rueda desactivado el valor no cambia.
        // Handled evita que la muesca llegue ademas al contenedor.
        private const int MuescaRueda_704ILR = 120;   // WHEEL_DELTA de Windows
        private int _ruedaAcumulada_704ILR;

        protected override void OnMouseWheel(MouseEventArgs e_704ILR)
        {
            if (e_704ILR is HandledMouseEventArgs manejado_704ILR)
            {
                if (manejado_704ILR.Handled) return;
                manejado_704ILR.Handled = true;
            }
            if ((ModifierKeys & (Keys.Shift | Keys.Alt)) != 0 || MouseButtons != MouseButtons.None) return;
            if (SystemInformation.MouseWheelScrollLines == 0) return;

            // Un cambio de sentido descarta el resto acumulado: si no, la primera muesca
            // completa en el sentido contrario no cambiaba el valor.
            if (_ruedaAcumulada_704ILR != 0 && System.Math.Sign(e_704ILR.Delta) != System.Math.Sign(_ruedaAcumulada_704ILR))
                _ruedaAcumulada_704ILR = 0;
            _ruedaAcumulada_704ILR += e_704ILR.Delta;
            while (_ruedaAcumulada_704ILR >= MuescaRueda_704ILR)
            {
                _ruedaAcumulada_704ILR -= MuescaRueda_704ILR;
                UpButton();
            }
            while (_ruedaAcumulada_704ILR <= -MuescaRueda_704ILR)
            {
                _ruedaAcumulada_704ILR += MuescaRueda_704ILR;
                DownButton();
            }
        }

        // Lectura estricta del texto editado. Solo un entero sin signo cambia el valor
        // (acotado a Minimum..Maximum, como hace el control base); cualquier otro texto
        // se descarta. La marca de edicion se baja ANTES de asignar Value: el setter
        // vuelve a formatear el texto y no debe reinterpretar el que se esta aplicando.
        private void AplicarTextoEditado_704ILR()
        {
            if (!UserEdit) return;
            string texto_704ILR = (Text ?? string.Empty).Trim();
            bool entero_704ILR = texto_704ILR.Length > 0;
            foreach (char c_704ILR in texto_704ILR)
                if (c_704ILR < '0' || c_704ILR > '9') { entero_704ILR = false; break; }
            UserEdit = false;
            if (!entero_704ILR)
            {
                // El control base no reescribe un texto VACIO si el valor no cambio (deja
                // borrar el campo mientras se tipea): al confirmar, el campo quedaba en
                // blanco y seguia valiendo el numero anterior. Se vuelve a mostrar el valor
                // vigente; ChangingText marca la escritura como del control y no del usuario.
                ChangingText = true;
                Text = Value.ToString("F0", CultureInfo.CurrentCulture);
                return;
            }
            // Mas digitos de los que entran en un decimal: supera el maximo de todos modos.
            if (!decimal.TryParse(texto_704ILR, NumberStyles.None, CultureInfo.InvariantCulture, out decimal valor_704ILR))
                valor_704ILR = Maximum;
            Value = Math.Min(Math.Max(valor_704ILR, Minimum), Maximum);
        }
    }
}
