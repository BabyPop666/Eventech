using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;
using EvenTech.Services;

namespace EvenTech.UI
{
    // Gestion de perfiles (Composite + TreeView recursivo). Dos tarjetas:
    //  - Izquierda: permisos del perfil (arbol) + alta de perfil.
    //  - Derecha: asignacion de perfiles a usuarios (grilla con combo por fila).
    // Observa el idioma para traducir sus textos.
    public class ucPerfiles_704ILR : UserControl, IObservadorIdioma_704ILR
    {
        // Marca del nodo raiz de la rama "Perfiles incluidos" (Composite de
        // perfiles): es un titulo, no un componente seleccionable.
        private const string TagRamaPerfiles_704ILR = "PERFILES_INCLUIDOS";

        private ComboBox _cboPerfil_704ILR;
        private TreeView _tree_704ILR;
        private TreeNode _nodoPerfiles_704ILR; // rama con los demas perfiles para incluir
        private AppButton_704ILR _btnGuardar_704ILR, _btnNuevoPerfil_704ILR, _btnGuardarAsig_704ILR;
        private Label _lblError_704ILR, _lblOk_704ILR, _lblAsigTitulo_704ILR, _lblMsgAsig_704ILR;
        private DataGridView _gridUsuarios_704ILR;
        private bool _suppressAfterCheck_704ILR;

        // Ids de permisos cuyo tilde es heredado de un perfil incluido (el check
        // lo puso el sistema, no el usuario): se muestran marcados y no se
        // persisten como asignacion directa al guardar.
        private HashSet<int> _marcadosHeredados_704ILR = new HashSet<int>();

        // Cache de permisos efectivos por perfil incluido (evita repetir la
        // resolucion del Composite en cada tilde).
        private readonly Dictionary<int, List<BE_Permiso_704ILR>> _permisosPorPerfil_704ILR = new Dictionary<int, List<BE_Permiso_704ILR>>();

        public ucPerfiles_704ILR()
        {
            BackColor = Theme_704ILR.BgContent_704ILR;
            BuildUi_704ILR();
            ActualizarTextos_704ILR();
            Load += (s_704ILR, e_704ILR) =>
            {
                ConstruirArbol_704ILR();
                CargarPerfiles_704ILR();
                CargarUsuarios_704ILR();
                GestorDeIdioma_704ILR.GetInstance_704ILR.Suscribir_704ILR(this);
            };
            Disposed += (s_704ILR, e_704ILR) => GestorDeIdioma_704ILR.GetInstance_704ILR.Desuscribir_704ILR(this);
        }

        private void BuildUi_704ILR()
        {
            var root_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Padding = new Padding(Theme_704ILR.SpaceXl_704ILR)
            };
            root_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // titulo
            root_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // cuerpo (2 columnas)

            var lblTitle_704ILR = Ui_704ILR.H1_704ILR("Gestion de Perfiles");
            lblTitle_704ILR.Tag = "T:PERF_TITULO";
            lblTitle_704ILR.Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR);

            var body_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            body_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
            body_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            body_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            body_704ILR.Controls.Add(BuildCardPermisos_704ILR(), 0, 0);
            body_704ILR.Controls.Add(BuildCardAsignacion_704ILR(), 1, 0);

            root_704ILR.Controls.Add(lblTitle_704ILR, 0, 0);
            root_704ILR.Controls.Add(body_704ILR, 0, 1);
            Controls.Add(root_704ILR);
        }

        // ---- Tarjeta izquierda: permisos del perfil + alta de perfil ----
        private Control BuildCardPermisos_704ILR()
        {
            var card_704ILR = new CardPanel_704ILR
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, Theme_704ILR.SpaceLg_704ILR, 0),
                Padding = new Padding(Theme_704ILR.SpaceLg_704ILR)
            };

            var layout_704ILR = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Color.Transparent
            };
            layout_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // selector + nuevo perfil
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // hint
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // arbol
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // acciones

            // Fila en TableLayoutPanel: el combo se estira y el boton queda SIEMPRE
            // visible a la derecha (antes el FlowLayoutPanel sin wrap lo recortaba).
            var fila_704ILR = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Color.Transparent, Margin = new Padding(0) };
            fila_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fila_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fila_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fila_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var lblPerfil_704ILR = Ui_704ILR.BodyBold_704ILR("Perfil:");
            lblPerfil_704ILR.Tag = "T:PERF_PERFIL"; lblPerfil_704ILR.Anchor = AnchorStyles.Left; lblPerfil_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceMd_704ILR, 0);
            _cboPerfil_704ILR = Ui_704ILR.Combo_704ILR(); _cboPerfil_704ILR.Anchor = AnchorStyles.Left | AnchorStyles.Right; _cboPerfil_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceMd_704ILR, 0);
            _cboPerfil_704ILR.SelectedIndexChanged += (s_704ILR, e_704ILR) => CargarAsignacionesPerfil_704ILR();
            _btnNuevoPerfil_704ILR = Ui_704ILR.Secondary_704ILR("Nuevo perfil", Theme_704ILR.IcoAdd_704ILR);
            _btnNuevoPerfil_704ILR.Tag = "T:PERF_NUEVO"; _btnNuevoPerfil_704ILR.Size = new Size(150, 30); _btnNuevoPerfil_704ILR.Anchor = AnchorStyles.Right; _btnNuevoPerfil_704ILR.Margin = new Padding(0);
            _btnNuevoPerfil_704ILR.Click += (s_704ILR, e_704ILR) => NuevoPerfil_704ILR();
            fila_704ILR.Controls.Add(lblPerfil_704ILR, 0, 0); fila_704ILR.Controls.Add(_cboPerfil_704ILR, 1, 0); fila_704ILR.Controls.Add(_btnNuevoPerfil_704ILR, 2, 0);

            var lblHint_704ILR = new Label
            {
                Tag = "T:PERF_HINT",
                Text = "Tilde los permisos del perfil. Marcar un grupo incluye a sus hijos.",
                Font = new Font(Theme_704ILR.FontCaption_704ILR, FontStyle.Italic),
                ForeColor = Theme_704ILR.TextMuted_704ILR, AutoSize = true, BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceSm_704ILR)
            };

            _tree_704ILR = new TreeView
            {
                Dock = DockStyle.Fill, CheckBoxes = true, Font = Theme_704ILR.FontBody_704ILR,
                BackColor = Theme_704ILR.Surface_704ILR, ForeColor = Theme_704ILR.TextOnLight_704ILR, BorderStyle = BorderStyle.None,
                ShowLines = true, HideSelection = false, ItemHeight = 26, Indent = 22
            };
            _tree_704ILR.AfterCheck += Tree_AfterCheck_704ILR;

            var acciones_704ILR = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0, Theme_704ILR.SpaceSm_704ILR, 0, 0) };
            _btnGuardar_704ILR = Ui_704ILR.Primary_704ILR("Guardar permisos", Theme_704ILR.IcoSave_704ILR);
            _btnGuardar_704ILR.Tag = "T:PERF_GUARDAR"; _btnGuardar_704ILR.Size = new Size(190, 38); _btnGuardar_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceMd_704ILR, 0);
            _btnGuardar_704ILR.Click += (s_704ILR, e_704ILR) => Guardar_704ILR();
            _lblOk_704ILR = new Label { AutoSize = true, Font = Theme_704ILR.FontBodyBold_704ILR, ForeColor = Theme_704ILR.Success_704ILR, Visible = false, BackColor = Color.Transparent, Anchor = AnchorStyles.Left, Margin = new Padding(0, 9, Theme_704ILR.SpaceMd_704ILR, 0) };
            _lblError_704ILR = new Label { AutoSize = true, Font = Theme_704ILR.FontBodyBold_704ILR, ForeColor = Theme_704ILR.Error_704ILR, Visible = false, BackColor = Color.Transparent, MaximumSize = new Size(260, 0), Anchor = AnchorStyles.Left, Margin = new Padding(0, 9, 0, 0) };
            acciones_704ILR.Controls.Add(_btnGuardar_704ILR); acciones_704ILR.Controls.Add(_lblOk_704ILR); acciones_704ILR.Controls.Add(_lblError_704ILR);

            layout_704ILR.Controls.Add(fila_704ILR, 0, 0);
            layout_704ILR.Controls.Add(lblHint_704ILR, 0, 1);
            layout_704ILR.Controls.Add(_tree_704ILR, 0, 2);
            layout_704ILR.Controls.Add(acciones_704ILR, 0, 3);
            card_704ILR.Controls.Add(layout_704ILR);
            return card_704ILR;
        }

        // ---- Tarjeta derecha: asignar perfiles a usuarios ----
        private Control BuildCardAsignacion_704ILR()
        {
            var card_704ILR = new CardPanel_704ILR { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(Theme_704ILR.SpaceLg_704ILR) };

            var layout_704ILR = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.Transparent };
            layout_704ILR.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // titulo
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // grilla
            layout_704ILR.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // acciones

            _lblAsigTitulo_704ILR = Ui_704ILR.H2_704ILR("Asignar perfil a usuarios");
            _lblAsigTitulo_704ILR.Tag = "T:PERF_ASIGNAR_TITULO";
            _lblAsigTitulo_704ILR.Margin = new Padding(0, 0, 0, Theme_704ILR.SpaceMd_704ILR);

            _gridUsuarios_704ILR = new DataGridView { Dock = DockStyle.Fill };
            UiGrid_704ILR.Style_704ILR(_gridUsuarios_704ILR, editable_704ILR: true);
            _gridUsuarios_704ILR.DataError += (s_704ILR, e_704ILR) => e_704ILR.ThrowException = false; // valores de combo fuera de lista: ignorar
            _gridUsuarios_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cUsuario", HeaderText = "Usuario", FillWeight = 30, ReadOnly = true });
            var colPerfil_704ILR = new DataGridViewComboBoxColumn { Name = "cPerfil", HeaderText = "Perfil", FillWeight = 33, FlatStyle = FlatStyle.Flat, DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton };
            _gridUsuarios_704ILR.Columns.Add(colPerfil_704ILR);
            _gridUsuarios_704ILR.Columns.Add(new DataGridViewTextBoxColumn { Name = "cEstado", HeaderText = "Estado", FillWeight = 25, ReadOnly = true });
            // Boton-icono (candado abierto) para desbloquear: compacto + tooltip.
            var colDesbloq_704ILR = new DataGridViewButtonColumn { Name = "cDesbloq", HeaderText = "", FillWeight = 12, FlatStyle = FlatStyle.Flat, UseColumnTextForButtonValue = false, ToolTipText = "Desbloquear" };
            colDesbloq_704ILR.DefaultCellStyle.Font = new Font("Segoe MDL2 Assets", 10F);
            colDesbloq_704ILR.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridUsuarios_704ILR.Columns.Add(colDesbloq_704ILR);
            _gridUsuarios_704ILR.CellContentClick += GridUsuarios_CellContentClick_704ILR;

            var acciones_704ILR = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0, Theme_704ILR.SpaceSm_704ILR, 0, 0) };
            _btnGuardarAsig_704ILR = Ui_704ILR.Primary_704ILR("Guardar asignaciones", Theme_704ILR.IcoSave_704ILR);
            _btnGuardarAsig_704ILR.Tag = "T:PERF_GUARDAR_ASIG"; _btnGuardarAsig_704ILR.Size = new Size(210, 38); _btnGuardarAsig_704ILR.Margin = new Padding(0, 0, Theme_704ILR.SpaceMd_704ILR, 0);
            _btnGuardarAsig_704ILR.Click += (s_704ILR, e_704ILR) => GuardarAsignaciones_704ILR();
            _lblMsgAsig_704ILR = new Label { AutoSize = true, Font = Theme_704ILR.FontBodyBold_704ILR, ForeColor = Theme_704ILR.Success_704ILR, Visible = false, BackColor = Color.Transparent, Anchor = AnchorStyles.Left, MaximumSize = new Size(300, 0), Margin = new Padding(0, 9, 0, 0) };
            acciones_704ILR.Controls.Add(_btnGuardarAsig_704ILR); acciones_704ILR.Controls.Add(_lblMsgAsig_704ILR);

            layout_704ILR.Controls.Add(_lblAsigTitulo_704ILR, 0, 0);
            layout_704ILR.Controls.Add(_gridUsuarios_704ILR, 0, 1);
            layout_704ILR.Controls.Add(acciones_704ILR, 0, 2);
            card_704ILR.Controls.Add(layout_704ILR);
            return card_704ILR;
        }

        public void ActualizarTextos_704ILR()
        {
            Tr_704ILR.AplicarTags_704ILR(this);
            if (_nodoPerfiles_704ILR != null)
            {
                _nodoPerfiles_704ILR.Text = T_704ILR("PERF_INCLUIDOS", "Perfiles incluidos");
                // Re-traduce el sufijo "(heredado)" de los permisos marcados.
                _suppressAfterCheck_704ILR = true;
                ActualizarHerencia_704ILR();
                _suppressAfterCheck_704ILR = false;
            }
            if (_gridUsuarios_704ILR != null && _gridUsuarios_704ILR.Columns.Count >= 4)
            {
                _gridUsuarios_704ILR.Columns["cUsuario"].HeaderText = Tr_704ILR.T_704ILR("COL_USUARIO");
                _gridUsuarios_704ILR.Columns["cPerfil"].HeaderText  = Tr_704ILR.T_704ILR("COL_PERFIL");
                _gridUsuarios_704ILR.Columns["cEstado"].HeaderText  = Tr_704ILR.T_704ILR("COL_ESTADO");
                _gridUsuarios_704ILR.Columns["cDesbloq"].ToolTipText = Tr_704ILR.T_704ILR("PERF_DESBLOQUEAR");
            }
        }

        // Desbloqueo de la cuenta de un usuario (boton de la grilla).
        private void GridUsuarios_CellContentClick_704ILR(object sender_704ILR, DataGridViewCellEventArgs e_704ILR)
        {
            if (e_704ILR.RowIndex < 0 || e_704ILR.ColumnIndex < 0) return;
            if (_gridUsuarios_704ILR.Columns[e_704ILR.ColumnIndex].Name != "cDesbloq") return;
            if (!(_gridUsuarios_704ILR.Rows[e_704ILR.RowIndex].Tag is BE_User_704ILR u_704ILR) || !u_704ILR.Blocked_704ILR) return;
            if (!Permisos_704ILR.Exigir_704ILR("PERFILES_GESTION", FindForm(), "desbloquear la cuenta '" + u_704ILR.Username_704ILR + "'")) return;
            try
            {
                BLL_User_704ILR.Desbloquear_704ILR(u_704ILR.Id_704ILR);
                CargarUsuarios_704ILR();
                MensajeAsig_704ILR(Tr_704ILR.T_704ILR("MSG_PERF_DESBLOQ"), error_704ILR: false);
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Perfiles", "Desbloquear usuario");
                MensajeAsig_704ILR(Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.Message, error_704ILR: true);
            }
        }

        // ===================== Permisos (Composite) =====================
        private void ConstruirArbol_704ILR()
        {
            try
            {
                _tree_704ILR.Nodes.Clear();
                List<BE_IComponentePermiso_704ILR> raices_704ILR = BLL_Perfil_704ILR.GetArbolPermisos_704ILR();
                foreach (var nodo_704ILR in raices_704ILR) _tree_704ILR.Nodes.Add(CrearNodo_704ILR(nodo_704ILR));
                _tree_704ILR.ExpandAll();
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Perfiles", "Construir arbol de permisos");
                MostrarError_704ILR(Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.Message);
            }
        }

        private TreeNode CrearNodo_704ILR(BE_IComponentePermiso_704ILR componente_704ILR)
        {
            var node_704ILR = new TreeNode(componente_704ILR.Nombre_704ILR) { Tag = componente_704ILR.Id_704ILR };
            if (componente_704ILR is BE_GrupoPermisos_704ILR grupo_704ILR)
            {
                node_704ILR.NodeFont = Theme_704ILR.FontBodyBold_704ILR;
                foreach (var hijo_704ILR in grupo_704ILR.Hijos_704ILR) node_704ILR.Nodes.Add(CrearNodo_704ILR(hijo_704ILR));
            }
            return node_704ILR;
        }

        private void CargarPerfiles_704ILR()
        {
            try
            {
                _cboPerfil_704ILR.DataSource = BLL_Perfil_704ILR.GetPerfiles_704ILR();
                _cboPerfil_704ILR.DisplayMember = "Nombre_704ILR";
                _cboPerfil_704ILR.ValueMember = "Id_704ILR";
                if (_cboPerfil_704ILR.Items.Count > 0) _cboPerfil_704ILR.SelectedIndex = 0;
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Perfiles", "Cargar perfiles");
                MostrarError_704ILR(Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.Message);
            }
        }

        private void CargarAsignacionesPerfil_704ILR()
        {
            if (!(_cboPerfil_704ILR.SelectedValue is int perfilId_704ILR)) return;
            try
            {
                HashSet<int> asignados_704ILR = BLL_Perfil_704ILR.GetPermisosAsignados_704ILR(perfilId_704ILR);
                HashSet<int> incluidos_704ILR = BLL_Perfil_704ILR.GetPerfilesIncluidos_704ILR(perfilId_704ILR);
                _suppressAfterCheck_704ILR = true;
                QuitarRamaPerfiles_704ILR();
                _marcadosHeredados_704ILR = new HashSet<int>();
                AplicarChecks_704ILR(_tree_704ILR.Nodes, asignados_704ILR);
                ConstruirRamaPerfiles_704ILR(perfilId_704ILR, incluidos_704ILR);
                ActualizarHerencia_704ILR();
                _suppressAfterCheck_704ILR = false;
            }
            catch (Exception ex_704ILR)
            {
                _suppressAfterCheck_704ILR = false;
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Perfiles", "Cargar asignaciones del perfil");
                MostrarError_704ILR(Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.Message);
            }
        }

        private void QuitarRamaPerfiles_704ILR()
        {
            if (_nodoPerfiles_704ILR != null)
            {
                _tree_704ILR.Nodes.Remove(_nodoPerfiles_704ILR);
                _nodoPerfiles_704ILR = null;
            }
            _permisosPorPerfil_704ILR.Clear();
        }

        // Refleja en el arbol principal los permisos heredados de los perfiles
        // incluidos tildados: aparecen tildados y marcados "(heredado)". El tilde
        // heredado no se persiste como asignacion directa ni se puede destildar
        // (se quita destildando el perfil incluido que lo aporta).
        private void ActualizarHerencia_704ILR()
        {
            var heredados_704ILR = new HashSet<int>();
            if (_nodoPerfiles_704ILR != null)
            {
                foreach (TreeNode n_704ILR in _nodoPerfiles_704ILR.Nodes)
                {
                    if (!n_704ILR.Checked || !(n_704ILR.Tag is BE_Perfil_704ILR p_704ILR)) continue;
                    if (_permisosPorPerfil_704ILR.TryGetValue(p_704ILR.Id_704ILR, out var permisos_704ILR))
                        foreach (var permiso_704ILR in permisos_704ILR) heredados_704ILR.Add(permiso_704ILR.Id_704ILR);
                }
            }

            var nuevosMarcados_704ILR = new HashSet<int>();
            AplicarHerencia_704ILR(_tree_704ILR.Nodes, heredados_704ILR, nuevosMarcados_704ILR);
            _marcadosHeredados_704ILR = nuevosMarcados_704ILR;
        }

        private void AplicarHerencia_704ILR(TreeNodeCollection nodes_704ILR, HashSet<int> heredados_704ILR, HashSet<int> nuevosMarcados_704ILR)
        {
            foreach (TreeNode n_704ILR in nodes_704ILR)
            {
                if (n_704ILR == _nodoPerfiles_704ILR) continue; // la rama de perfiles no se marca

                if (n_704ILR.Tag is int id_704ILR)
                {
                    bool eraHeredado_704ILR = _marcadosHeredados_704ILR.Contains(id_704ILR);
                    // El tilde es "directo" si lo puso el usuario (no el sistema).
                    bool directo_704ILR = n_704ILR.Checked && !eraHeredado_704ILR;

                    if (heredados_704ILR.Contains(id_704ILR) && !directo_704ILR)
                    {
                        n_704ILR.Checked = true;
                        MarcarHeredado_704ILR(n_704ILR, true);
                        nuevosMarcados_704ILR.Add(id_704ILR);
                    }
                    else
                    {
                        if (eraHeredado_704ILR) n_704ILR.Checked = directo_704ILR; // dejo de heredarse: se destilda
                        MarcarHeredado_704ILR(n_704ILR, false);
                    }
                }
                AplicarHerencia_704ILR(n_704ILR.Nodes, heredados_704ILR, nuevosMarcados_704ILR);
            }
        }

        // Marca visual del permiso heredado: sufijo "(heredado)" + color de exito.
        // El texto original se conserva en Name para poder restaurarlo.
        private void MarcarHeredado_704ILR(TreeNode n_704ILR, bool heredado_704ILR)
        {
            if (heredado_704ILR)
            {
                if (string.IsNullOrEmpty(n_704ILR.Name)) n_704ILR.Name = n_704ILR.Text;
                n_704ILR.Text = n_704ILR.Name + "  " + T_704ILR("PERF_HEREDADO", "(heredado)");
                n_704ILR.ForeColor = Theme_704ILR.Success_704ILR;
            }
            else
            {
                if (!string.IsNullOrEmpty(n_704ILR.Name)) n_704ILR.Text = n_704ILR.Name;
                n_704ILR.ForeColor = _tree_704ILR.ForeColor;
            }
        }

        // Rama del Composite de perfiles: lista los demas perfiles para poder
        // incluirlos dentro del seleccionado (p.ej. Gerencial contiene Vendedor
        // y hereda sus permisos). Debajo de cada perfil se muestran, a modo
        // informativo, los permisos efectivos que aportaria.
        private void ConstruirRamaPerfiles_704ILR(int perfilId_704ILR, HashSet<int> incluidos_704ILR)
        {
            _nodoPerfiles_704ILR = new TreeNode(T_704ILR("PERF_INCLUIDOS", "Perfiles incluidos"))
            {
                Tag = TagRamaPerfiles_704ILR,
                NodeFont = Theme_704ILR.FontBodyBold_704ILR
            };

            foreach (var perfil_704ILR in BLL_Perfil_704ILR.GetPerfiles_704ILR())
            {
                if (perfil_704ILR.Id_704ILR == perfilId_704ILR) continue; // un perfil no puede incluirse a si mismo

                var nodoPerfil_704ILR = new TreeNode(perfil_704ILR.Nombre_704ILR) { Tag = perfil_704ILR, NodeFont = Theme_704ILR.FontBodyBold_704ILR };
                try
                {
                    // Permisos efectivos del perfil incluido (resueltos por el
                    // Composite, inclusiones anidadas incluidas). Tag null: son
                    // informativos, no se recolectan al guardar.
                    List<BE_Permiso_704ILR> permisos_704ILR = BLL_Perfil_704ILR.GetPermisosEfectivosDePerfil_704ILR(perfil_704ILR.Id_704ILR);
                    _permisosPorPerfil_704ILR[perfil_704ILR.Id_704ILR] = permisos_704ILR;
                    foreach (var permiso_704ILR in permisos_704ILR)
                        nodoPerfil_704ILR.Nodes.Add(new TreeNode(permiso_704ILR.Nombre_704ILR));
                }
                catch (Exception ex_704ILR)
                {
                    _permisosPorPerfil_704ILR[perfil_704ILR.Id_704ILR] = new List<BE_Permiso_704ILR>();
                    BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Perfiles", "Resolver permisos del perfil incluido");
                }

                nodoPerfil_704ILR.Checked = incluidos_704ILR.Contains(perfil_704ILR.Id_704ILR);
                if (nodoPerfil_704ILR.Checked) PropagarHijos_704ILR(nodoPerfil_704ILR, true);

                _nodoPerfiles_704ILR.Nodes.Add(nodoPerfil_704ILR);
                if (nodoPerfil_704ILR.Checked) nodoPerfil_704ILR.Expand();
            }

            _tree_704ILR.Nodes.Add(_nodoPerfiles_704ILR);
            _nodoPerfiles_704ILR.Expand();
        }

        private void AplicarChecks_704ILR(TreeNodeCollection nodes_704ILR, HashSet<int> asignados_704ILR)
        {
            foreach (TreeNode n_704ILR in nodes_704ILR)
            {
                n_704ILR.Checked = n_704ILR.Tag is int id_704ILR && asignados_704ILR.Contains(id_704ILR);
                AplicarChecks_704ILR(n_704ILR.Nodes, asignados_704ILR);
            }
        }

        private void Tree_AfterCheck_704ILR(object sender_704ILR, TreeViewEventArgs e_704ILR)
        {
            if (_suppressAfterCheck_704ILR) return;
            _suppressAfterCheck_704ILR = true;
            if (e_704ILR.Node.Tag as string == TagRamaPerfiles_704ILR)
            {
                // El titulo de la rama de perfiles no es seleccionable.
                e_704ILR.Node.Checked = false;
            }
            else if (e_704ILR.Node.Tag is BE_Perfil_704ILR)
            {
                // (Des)incluir un perfil: sus hijos informativos lo siguen y los
                // permisos que aporta se reflejan en el arbol principal.
                PropagarHijos_704ILR(e_704ILR.Node, e_704ILR.Node.Checked);
                if (e_704ILR.Node.Checked) e_704ILR.Node.Expand();
                ActualizarHerencia_704ILR();
            }
            else if (e_704ILR.Node.Tag == null && e_704ILR.Node.Parent?.Tag is BE_Perfil_704ILR)
            {
                // Los permisos mostrados bajo un perfil incluido son informativos:
                // siguen el estado del perfil, no se tildan sueltos.
                e_704ILR.Node.Checked = e_704ILR.Node.Parent.Checked;
            }
            else if (e_704ILR.Node.Tag is int id_704ILR && !e_704ILR.Node.Checked && _marcadosHeredados_704ILR.Contains(id_704ILR))
            {
                // Un permiso heredado no se destilda a mano: se quita destildando
                // el perfil incluido que lo aporta.
                e_704ILR.Node.Checked = true;
            }
            else
            {
                PropagarHijos_704ILR(e_704ILR.Node, e_704ILR.Node.Checked);
                // El cascadeo pudo tildar/destildar permisos heredados: se
                // restablecen sus marcas y tildes.
                ActualizarHerencia_704ILR();
            }
            _suppressAfterCheck_704ILR = false;
        }

        private void PropagarHijos_704ILR(TreeNode node_704ILR, bool valor_704ILR)
        {
            foreach (TreeNode hijo_704ILR in node_704ILR.Nodes)
            {
                hijo_704ILR.Checked = valor_704ILR;
                PropagarHijos_704ILR(hijo_704ILR, valor_704ILR);
            }
        }

        private void Guardar_704ILR()
        {
            // Segunda capa del control de acceso: editar la composicion de un
            // perfil redefine que puede hacer el resto del sistema.
            if (!Permisos_704ILR.Exigir_704ILR("PERFILES_GESTION", FindForm(), "guardar la composicion de un perfil")) return;

            _lblOk_704ILR.Visible = false;
            _lblError_704ILR.Visible = false;
            if (!(_cboPerfil_704ILR.SelectedValue is int perfilId_704ILR))
            {
                MostrarError_704ILR(Tr_704ILR.T_704ILR("MSG_PERF_SELECCIONE"));
                return;
            }
            try
            {
                var ids_704ILR = new List<int>();
                RecolectarChecked_704ILR(_tree_704ILR.Nodes, ids_704ILR);
                // Los tildes heredados los puso el sistema: no son asignaciones
                // directas del perfil (viven en el perfil incluido que las aporta).
                ids_704ILR.RemoveAll(id_704ILR => _marcadosHeredados_704ILR.Contains(id_704ILR));

                // Perfiles incluidos tildados en la rama del Composite de perfiles.
                var incluidos_704ILR = new List<int>();
                if (_nodoPerfiles_704ILR != null)
                    foreach (TreeNode n_704ILR in _nodoPerfiles_704ILR.Nodes)
                        if (n_704ILR.Checked && n_704ILR.Tag is BE_Perfil_704ILR p_704ILR) incluidos_704ILR.Add(p_704ILR.Id_704ILR);

                PerfilResult_704ILR res_704ILR = BLL_Perfil_704ILR.GuardarComposicion_704ILR(perfilId_704ILR, ids_704ILR, incluidos_704ILR);
                if (res_704ILR == PerfilResult_704ILR.ReferenciaCircular)
                {
                    MostrarError_704ILR(T_704ILR("MSG_PERF_CICLO", "No se puede incluir ese perfil: generaria una referencia circular."));
                    return;
                }

                // Refresca la rama: los permisos heredados que muestran los demas
                // perfiles pueden haber cambiado con esta edicion.
                CargarAsignacionesPerfil_704ILR();
                _lblOk_704ILR.Text = Tr_704ILR.T_704ILR("MSG_PERF_OK");
                _lblOk_704ILR.Visible = true;
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Perfiles", "Guardar permisos");
                MostrarError_704ILR(Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.Message);
            }
        }

        private void RecolectarChecked_704ILR(TreeNodeCollection nodes_704ILR, List<int> ids_704ILR)
        {
            foreach (TreeNode n_704ILR in nodes_704ILR)
            {
                if (n_704ILR.Checked && n_704ILR.Tag is int id_704ILR) ids_704ILR.Add(id_704ILR);
                RecolectarChecked_704ILR(n_704ILR.Nodes, ids_704ILR);
            }
        }

        private void NuevoPerfil_704ILR()
        {
            if (!Permisos_704ILR.Exigir_704ILR("PERFILES_GESTION", FindForm(), "crear un perfil")) return;
            using (var dlg_704ILR = new frmNuevoPerfil_704ILR())
            {
                if (dlg_704ILR.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    CargarPerfiles_704ILR();
                    _cboPerfil_704ILR.SelectedValue = dlg_704ILR.NuevoId_704ILR;
                    CargarUsuarios_704ILR(); // el nuevo perfil aparece en el combo de asignacion
                }
            }
        }

        private void MostrarError_704ILR(string msg_704ILR)
        {
            _lblError_704ILR.Text = msg_704ILR;
            _lblError_704ILR.Visible = true;
        }

        // Devuelve la traduccion de 'clave' o, si falta, el texto por defecto dado.
        private static string T_704ILR(string clave_704ILR, string defecto_704ILR)
        {
            string t_704ILR = Tr_704ILR.T_704ILR(clave_704ILR);
            return t_704ILR == clave_704ILR ? defecto_704ILR : t_704ILR;
        }

        // ===================== Asignacion a usuarios =====================
        private void CargarUsuarios_704ILR()
        {
            try
            {
                // Opciones del combo: "(sin perfil)" (Id=0) + perfiles existentes.
                var opciones_704ILR = new List<BE_Perfil_704ILR> { new BE_Perfil_704ILR { Id_704ILR = 0, Nombre_704ILR = Tr_704ILR.T_704ILR("PERF_SIN") } };
                opciones_704ILR.AddRange(BLL_Perfil_704ILR.GetPerfiles_704ILR());
                var col_704ILR = (DataGridViewComboBoxColumn)_gridUsuarios_704ILR.Columns["cPerfil"];
                col_704ILR.DataSource = opciones_704ILR;
                col_704ILR.ValueMember = "Id_704ILR";
                col_704ILR.DisplayMember = "Nombre_704ILR";

                _gridUsuarios_704ILR.Rows.Clear();
                foreach (var u_704ILR in BLL_User_704ILR.GetAll_704ILR())
                {
                    int idx_704ILR = _gridUsuarios_704ILR.Rows.Add(u_704ILR.Username_704ILR);
                    var row_704ILR = _gridUsuarios_704ILR.Rows[idx_704ILR];
                    row_704ILR.Tag = u_704ILR;
                    row_704ILR.Cells["cPerfil"].Value = u_704ILR.PerfilId_704ILR ?? 0;
                    row_704ILR.Cells["cEstado"].Value = u_704ILR.Blocked_704ILR ? Tr_704ILR.T_704ILR("EST_BLOQUEADO")
                                               : (u_704ILR.Activo_704ILR ? Tr_704ILR.T_704ILR("EST_ACTIVO") : Tr_704ILR.T_704ILR("EST_INACTIVO"));
                    // Icono de candado (desbloquear) solo para cuentas bloqueadas.
                    row_704ILR.Cells["cDesbloq"].Value = u_704ILR.Blocked_704ILR ? Theme_704ILR.IcoUnlock_704ILR : "";
                    if (u_704ILR.Blocked_704ILR) row_704ILR.Cells["cEstado"].Style.ForeColor = Theme_704ILR.Error_704ILR;
                }
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Perfiles", "Cargar usuarios");
                MensajeAsig_704ILR(Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.Message, error_704ILR: true);
            }
        }

        private void GuardarAsignaciones_704ILR()
        {
            try
            {
                _gridUsuarios_704ILR.EndEdit();
                foreach (DataGridViewRow row_704ILR in _gridUsuarios_704ILR.Rows)
                {
                    if (!(row_704ILR.Tag is BE_User_704ILR u_704ILR)) continue;
                    int val_704ILR = row_704ILR.Cells["cPerfil"].Value is int v_704ILR ? v_704ILR : 0;
                    BLL_User_704ILR.AsignarPerfil_704ILR(u_704ILR.Id_704ILR, val_704ILR == 0 ? (int?)null : val_704ILR);
                }
                MensajeAsig_704ILR(Tr_704ILR.T_704ILR("MSG_PERF_ASIG_OK"), error_704ILR: false);
            }
            catch (Exception ex_704ILR)
            {
                BLL_Bitacora_704ILR.RegistrarExcepcion_704ILR(ex_704ILR, "Perfiles", "Guardar asignaciones de usuarios");
                MensajeAsig_704ILR(Tr_704ILR.T_704ILR("MSG_ERROR_PREFIJO") + ex_704ILR.Message, error_704ILR: true);
            }
        }

        private void MensajeAsig_704ILR(string texto_704ILR, bool error_704ILR)
        {
            _lblMsgAsig_704ILR.ForeColor = error_704ILR ? Theme_704ILR.Error_704ILR : Theme_704ILR.Success_704ILR;
            _lblMsgAsig_704ILR.Text = texto_704ILR;
            _lblMsgAsig_704ILR.Visible = true;
        }
    }
}
