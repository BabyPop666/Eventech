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
    public class ucPerfiles : UserControl, IObservadorIdioma
    {
        // Marca del nodo raiz de la rama "Perfiles incluidos" (Composite de
        // perfiles): es un titulo, no un componente seleccionable.
        private const string TagRamaPerfiles = "PERFILES_INCLUIDOS";

        private ComboBox _cboPerfil;
        private TreeView _tree;
        private TreeNode _nodoPerfiles; // rama con los demas perfiles para incluir
        private AppButton _btnGuardar, _btnNuevoPerfil, _btnGuardarAsig;
        private Label _lblError, _lblOk, _lblAsigTitulo, _lblMsgAsig;
        private DataGridView _gridUsuarios;
        private bool _suppressAfterCheck;

        // Ids de permisos cuyo tilde es heredado de un perfil incluido (el check
        // lo puso el sistema, no el usuario): se muestran marcados y no se
        // persisten como asignacion directa al guardar.
        private HashSet<int> _marcadosHeredados = new HashSet<int>();

        // Cache de permisos efectivos por perfil incluido (evita repetir la
        // resolucion del Composite en cada tilde).
        private readonly Dictionary<int, List<BE_Permiso>> _permisosPorPerfil = new Dictionary<int, List<BE_Permiso>>();

        public ucPerfiles()
        {
            BackColor = Theme.BgContent;
            BuildUi();
            ActualizarTextos();
            Load += (s, e) =>
            {
                ConstruirArbol();
                CargarPerfiles();
                CargarUsuarios();
                GestorDeIdioma.GetInstance.Suscribir(this);
            };
            Disposed += (s, e) => GestorDeIdioma.GetInstance.Desuscribir(this);
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Padding = new Padding(Theme.SpaceXl)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // titulo
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // cuerpo (2 columnas)

            var lblTitle = Ui.H1("Gestion de Perfiles");
            lblTitle.Tag = "T:PERF_TITULO";
            lblTitle.Margin = new Padding(0, 0, 0, Theme.SpaceMd);

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            body.Controls.Add(BuildCardPermisos(), 0, 0);
            body.Controls.Add(BuildCardAsignacion(), 1, 0);

            root.Controls.Add(lblTitle, 0, 0);
            root.Controls.Add(body, 0, 1);
            Controls.Add(root);
        }

        // ---- Tarjeta izquierda: permisos del perfil + alta de perfil ----
        private Control BuildCardPermisos()
        {
            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, Theme.SpaceLg, 0),
                Padding = new Padding(Theme.SpaceLg)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Color.Transparent
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // selector + nuevo perfil
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // hint
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // arbol
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // acciones

            // Fila en TableLayoutPanel: el combo se estira y el boton queda SIEMPRE
            // visible a la derecha (antes el FlowLayoutPanel sin wrap lo recortaba).
            var fila = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Color.Transparent, Margin = new Padding(0) };
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fila.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var lblPerfil = Ui.BodyBold("Perfil:");
            lblPerfil.Tag = "T:PERF_PERFIL"; lblPerfil.Anchor = AnchorStyles.Left; lblPerfil.Margin = new Padding(0, 0, Theme.SpaceMd, 0);
            _cboPerfil = Ui.Combo(); _cboPerfil.Anchor = AnchorStyles.Left | AnchorStyles.Right; _cboPerfil.Margin = new Padding(0, 0, Theme.SpaceMd, 0);
            _cboPerfil.SelectedIndexChanged += (s, e) => CargarAsignacionesPerfil();
            _btnNuevoPerfil = Ui.Secondary("Nuevo perfil", Theme.IcoAdd);
            _btnNuevoPerfil.Tag = "T:PERF_NUEVO"; _btnNuevoPerfil.Size = new Size(150, 30); _btnNuevoPerfil.Anchor = AnchorStyles.Right; _btnNuevoPerfil.Margin = new Padding(0);
            _btnNuevoPerfil.Click += (s, e) => NuevoPerfil();
            fila.Controls.Add(lblPerfil, 0, 0); fila.Controls.Add(_cboPerfil, 1, 0); fila.Controls.Add(_btnNuevoPerfil, 2, 0);

            var lblHint = new Label
            {
                Tag = "T:PERF_HINT",
                Text = "Tilde los permisos del perfil. Marcar un grupo incluye a sus hijos.",
                Font = new Font(Theme.FontCaption, FontStyle.Italic),
                ForeColor = Theme.TextMuted, AutoSize = true, BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, Theme.SpaceSm)
            };

            _tree = new TreeView
            {
                Dock = DockStyle.Fill, CheckBoxes = true, Font = Theme.FontBody,
                BackColor = Theme.Surface, ForeColor = Theme.TextOnLight, BorderStyle = BorderStyle.None,
                ShowLines = true, HideSelection = false, ItemHeight = 26, Indent = 22
            };
            _tree.AfterCheck += Tree_AfterCheck;

            var acciones = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0, Theme.SpaceSm, 0, 0) };
            _btnGuardar = Ui.Primary("Guardar permisos", Theme.IcoSave);
            _btnGuardar.Tag = "T:PERF_GUARDAR"; _btnGuardar.Size = new Size(190, 38); _btnGuardar.Margin = new Padding(0, 0, Theme.SpaceMd, 0);
            _btnGuardar.Click += (s, e) => Guardar();
            _lblOk = new Label { AutoSize = true, Font = Theme.FontBodyBold, ForeColor = Theme.Success, Visible = false, BackColor = Color.Transparent, Anchor = AnchorStyles.Left, Margin = new Padding(0, 9, Theme.SpaceMd, 0) };
            _lblError = new Label { AutoSize = true, Font = Theme.FontBodyBold, ForeColor = Theme.Error, Visible = false, BackColor = Color.Transparent, MaximumSize = new Size(260, 0), Anchor = AnchorStyles.Left, Margin = new Padding(0, 9, 0, 0) };
            acciones.Controls.Add(_btnGuardar); acciones.Controls.Add(_lblOk); acciones.Controls.Add(_lblError);

            layout.Controls.Add(fila, 0, 0);
            layout.Controls.Add(lblHint, 0, 1);
            layout.Controls.Add(_tree, 0, 2);
            layout.Controls.Add(acciones, 0, 3);
            card.Controls.Add(layout);
            return card;
        }

        // ---- Tarjeta derecha: asignar perfiles a usuarios ----
        private Control BuildCardAsignacion()
        {
            var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(Theme.SpaceLg) };

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.Transparent };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // titulo
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // grilla
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // acciones

            _lblAsigTitulo = Ui.H2("Asignar perfil a usuarios");
            _lblAsigTitulo.Tag = "T:PERF_ASIGNAR_TITULO";
            _lblAsigTitulo.Margin = new Padding(0, 0, 0, Theme.SpaceMd);

            _gridUsuarios = new DataGridView { Dock = DockStyle.Fill };
            UiGrid.Style(_gridUsuarios, editable: true);
            _gridUsuarios.DataError += (s, e) => e.ThrowException = false; // valores de combo fuera de lista: ignorar
            _gridUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "cUsuario", HeaderText = "Usuario", FillWeight = 30, ReadOnly = true });
            var colPerfil = new DataGridViewComboBoxColumn { Name = "cPerfil", HeaderText = "Perfil", FillWeight = 33, FlatStyle = FlatStyle.Flat, DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton };
            _gridUsuarios.Columns.Add(colPerfil);
            _gridUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "cEstado", HeaderText = "Estado", FillWeight = 25, ReadOnly = true });
            // Boton-icono (candado abierto) para desbloquear: compacto + tooltip.
            var colDesbloq = new DataGridViewButtonColumn { Name = "cDesbloq", HeaderText = "", FillWeight = 12, FlatStyle = FlatStyle.Flat, UseColumnTextForButtonValue = false, ToolTipText = "Desbloquear" };
            colDesbloq.DefaultCellStyle.Font = new Font("Segoe MDL2 Assets", 10F);
            colDesbloq.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridUsuarios.Columns.Add(colDesbloq);
            _gridUsuarios.CellContentClick += GridUsuarios_CellContentClick;

            var acciones = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0, Theme.SpaceSm, 0, 0) };
            _btnGuardarAsig = Ui.Primary("Guardar asignaciones", Theme.IcoSave);
            _btnGuardarAsig.Tag = "T:PERF_GUARDAR_ASIG"; _btnGuardarAsig.Size = new Size(210, 38); _btnGuardarAsig.Margin = new Padding(0, 0, Theme.SpaceMd, 0);
            _btnGuardarAsig.Click += (s, e) => GuardarAsignaciones();
            _lblMsgAsig = new Label { AutoSize = true, Font = Theme.FontBodyBold, ForeColor = Theme.Success, Visible = false, BackColor = Color.Transparent, Anchor = AnchorStyles.Left, MaximumSize = new Size(300, 0), Margin = new Padding(0, 9, 0, 0) };
            acciones.Controls.Add(_btnGuardarAsig); acciones.Controls.Add(_lblMsgAsig);

            layout.Controls.Add(_lblAsigTitulo, 0, 0);
            layout.Controls.Add(_gridUsuarios, 0, 1);
            layout.Controls.Add(acciones, 0, 2);
            card.Controls.Add(layout);
            return card;
        }

        public void ActualizarTextos()
        {
            Tr.AplicarTags(this);
            if (_nodoPerfiles != null)
            {
                _nodoPerfiles.Text = T("PERF_INCLUIDOS", "Perfiles incluidos");
                // Re-traduce el sufijo "(heredado)" de los permisos marcados.
                _suppressAfterCheck = true;
                ActualizarHerencia();
                _suppressAfterCheck = false;
            }
            if (_gridUsuarios != null && _gridUsuarios.Columns.Count >= 4)
            {
                _gridUsuarios.Columns["cUsuario"].HeaderText = Tr.T("COL_USUARIO");
                _gridUsuarios.Columns["cPerfil"].HeaderText  = Tr.T("COL_PERFIL");
                _gridUsuarios.Columns["cEstado"].HeaderText  = Tr.T("COL_ESTADO");
                _gridUsuarios.Columns["cDesbloq"].ToolTipText = Tr.T("PERF_DESBLOQUEAR");
            }
        }

        // Desbloqueo de la cuenta de un usuario (boton de la grilla).
        private void GridUsuarios_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_gridUsuarios.Columns[e.ColumnIndex].Name != "cDesbloq") return;
            if (!(_gridUsuarios.Rows[e.RowIndex].Tag is BE_User u) || !u.Blocked) return;
            if (!Permisos.Exigir("PERFILES_GESTION", FindForm(), "desbloquear la cuenta '" + u.Username + "'")) return;
            try
            {
                BLL_User.Desbloquear(u.Id);
                CargarUsuarios();
                MensajeAsig(Tr.T("MSG_PERF_DESBLOQ"), error: false);
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Perfiles", "Desbloquear usuario");
                MensajeAsig(Tr.T("MSG_ERROR_PREFIJO") + ex.Message, error: true);
            }
        }

        // ===================== Permisos (Composite) =====================
        private void ConstruirArbol()
        {
            try
            {
                _tree.Nodes.Clear();
                List<BE_IComponentePermiso> raices = BLL_Perfil.GetArbolPermisos();
                foreach (var nodo in raices) _tree.Nodes.Add(CrearNodo(nodo));
                _tree.ExpandAll();
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Perfiles", "Construir arbol de permisos");
                MostrarError(Tr.T("MSG_ERROR_PREFIJO") + ex.Message);
            }
        }

        private TreeNode CrearNodo(BE_IComponentePermiso componente)
        {
            var node = new TreeNode(componente.Nombre) { Tag = componente.Id };
            if (componente is BE_GrupoPermisos grupo)
            {
                node.NodeFont = Theme.FontBodyBold;
                foreach (var hijo in grupo.Hijos) node.Nodes.Add(CrearNodo(hijo));
            }
            return node;
        }

        private void CargarPerfiles()
        {
            try
            {
                _cboPerfil.DataSource = BLL_Perfil.GetPerfiles();
                _cboPerfil.DisplayMember = "Nombre";
                _cboPerfil.ValueMember = "Id";
                if (_cboPerfil.Items.Count > 0) _cboPerfil.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Perfiles", "Cargar perfiles");
                MostrarError(Tr.T("MSG_ERROR_PREFIJO") + ex.Message);
            }
        }

        private void CargarAsignacionesPerfil()
        {
            if (!(_cboPerfil.SelectedValue is int perfilId)) return;
            try
            {
                HashSet<int> asignados = BLL_Perfil.GetPermisosAsignados(perfilId);
                HashSet<int> incluidos = BLL_Perfil.GetPerfilesIncluidos(perfilId);
                _suppressAfterCheck = true;
                QuitarRamaPerfiles();
                _marcadosHeredados = new HashSet<int>();
                AplicarChecks(_tree.Nodes, asignados);
                ConstruirRamaPerfiles(perfilId, incluidos);
                ActualizarHerencia();
                _suppressAfterCheck = false;
            }
            catch (Exception ex)
            {
                _suppressAfterCheck = false;
                BLL_Bitacora.RegistrarExcepcion(ex, "Perfiles", "Cargar asignaciones del perfil");
                MostrarError(Tr.T("MSG_ERROR_PREFIJO") + ex.Message);
            }
        }

        private void QuitarRamaPerfiles()
        {
            if (_nodoPerfiles != null)
            {
                _tree.Nodes.Remove(_nodoPerfiles);
                _nodoPerfiles = null;
            }
            _permisosPorPerfil.Clear();
        }

        // Refleja en el arbol principal los permisos heredados de los perfiles
        // incluidos tildados: aparecen tildados y marcados "(heredado)". El tilde
        // heredado no se persiste como asignacion directa ni se puede destildar
        // (se quita destildando el perfil incluido que lo aporta).
        private void ActualizarHerencia()
        {
            var heredados = new HashSet<int>();
            if (_nodoPerfiles != null)
            {
                foreach (TreeNode n in _nodoPerfiles.Nodes)
                {
                    if (!n.Checked || !(n.Tag is BE_Perfil p)) continue;
                    if (_permisosPorPerfil.TryGetValue(p.Id, out var permisos))
                        foreach (var permiso in permisos) heredados.Add(permiso.Id);
                }
            }

            var nuevosMarcados = new HashSet<int>();
            AplicarHerencia(_tree.Nodes, heredados, nuevosMarcados);
            _marcadosHeredados = nuevosMarcados;
        }

        private void AplicarHerencia(TreeNodeCollection nodes, HashSet<int> heredados, HashSet<int> nuevosMarcados)
        {
            foreach (TreeNode n in nodes)
            {
                if (n == _nodoPerfiles) continue; // la rama de perfiles no se marca

                if (n.Tag is int id)
                {
                    bool eraHeredado = _marcadosHeredados.Contains(id);
                    // El tilde es "directo" si lo puso el usuario (no el sistema).
                    bool directo = n.Checked && !eraHeredado;

                    if (heredados.Contains(id) && !directo)
                    {
                        n.Checked = true;
                        MarcarHeredado(n, true);
                        nuevosMarcados.Add(id);
                    }
                    else
                    {
                        if (eraHeredado) n.Checked = directo; // dejo de heredarse: se destilda
                        MarcarHeredado(n, false);
                    }
                }
                AplicarHerencia(n.Nodes, heredados, nuevosMarcados);
            }
        }

        // Marca visual del permiso heredado: sufijo "(heredado)" + color de exito.
        // El texto original se conserva en Name para poder restaurarlo.
        private void MarcarHeredado(TreeNode n, bool heredado)
        {
            if (heredado)
            {
                if (string.IsNullOrEmpty(n.Name)) n.Name = n.Text;
                n.Text = n.Name + "  " + T("PERF_HEREDADO", "(heredado)");
                n.ForeColor = Theme.Success;
            }
            else
            {
                if (!string.IsNullOrEmpty(n.Name)) n.Text = n.Name;
                n.ForeColor = _tree.ForeColor;
            }
        }

        // Rama del Composite de perfiles: lista los demas perfiles para poder
        // incluirlos dentro del seleccionado (p.ej. Gerencial contiene Vendedor
        // y hereda sus permisos). Debajo de cada perfil se muestran, a modo
        // informativo, los permisos efectivos que aportaria.
        private void ConstruirRamaPerfiles(int perfilId, HashSet<int> incluidos)
        {
            _nodoPerfiles = new TreeNode(T("PERF_INCLUIDOS", "Perfiles incluidos"))
            {
                Tag = TagRamaPerfiles,
                NodeFont = Theme.FontBodyBold
            };

            foreach (var perfil in BLL_Perfil.GetPerfiles())
            {
                if (perfil.Id == perfilId) continue; // un perfil no puede incluirse a si mismo

                var nodoPerfil = new TreeNode(perfil.Nombre) { Tag = perfil, NodeFont = Theme.FontBodyBold };
                try
                {
                    // Permisos efectivos del perfil incluido (resueltos por el
                    // Composite, inclusiones anidadas incluidas). Tag null: son
                    // informativos, no se recolectan al guardar.
                    List<BE_Permiso> permisos = BLL_Perfil.GetPermisosEfectivosDePerfil(perfil.Id);
                    _permisosPorPerfil[perfil.Id] = permisos;
                    foreach (var permiso in permisos)
                        nodoPerfil.Nodes.Add(new TreeNode(permiso.Nombre));
                }
                catch (Exception ex)
                {
                    _permisosPorPerfil[perfil.Id] = new List<BE_Permiso>();
                    BLL_Bitacora.RegistrarExcepcion(ex, "Perfiles", "Resolver permisos del perfil incluido");
                }

                nodoPerfil.Checked = incluidos.Contains(perfil.Id);
                if (nodoPerfil.Checked) PropagarHijos(nodoPerfil, true);

                _nodoPerfiles.Nodes.Add(nodoPerfil);
                if (nodoPerfil.Checked) nodoPerfil.Expand();
            }

            _tree.Nodes.Add(_nodoPerfiles);
            _nodoPerfiles.Expand();
        }

        private void AplicarChecks(TreeNodeCollection nodes, HashSet<int> asignados)
        {
            foreach (TreeNode n in nodes)
            {
                n.Checked = n.Tag is int id && asignados.Contains(id);
                AplicarChecks(n.Nodes, asignados);
            }
        }

        private void Tree_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (_suppressAfterCheck) return;
            _suppressAfterCheck = true;
            if (e.Node.Tag as string == TagRamaPerfiles)
            {
                // El titulo de la rama de perfiles no es seleccionable.
                e.Node.Checked = false;
            }
            else if (e.Node.Tag is BE_Perfil)
            {
                // (Des)incluir un perfil: sus hijos informativos lo siguen y los
                // permisos que aporta se reflejan en el arbol principal.
                PropagarHijos(e.Node, e.Node.Checked);
                if (e.Node.Checked) e.Node.Expand();
                ActualizarHerencia();
            }
            else if (e.Node.Tag == null && e.Node.Parent?.Tag is BE_Perfil)
            {
                // Los permisos mostrados bajo un perfil incluido son informativos:
                // siguen el estado del perfil, no se tildan sueltos.
                e.Node.Checked = e.Node.Parent.Checked;
            }
            else if (e.Node.Tag is int id && !e.Node.Checked && _marcadosHeredados.Contains(id))
            {
                // Un permiso heredado no se destilda a mano: se quita destildando
                // el perfil incluido que lo aporta.
                e.Node.Checked = true;
            }
            else
            {
                PropagarHijos(e.Node, e.Node.Checked);
                // El cascadeo pudo tildar/destildar permisos heredados: se
                // restablecen sus marcas y tildes.
                ActualizarHerencia();
            }
            _suppressAfterCheck = false;
        }

        private void PropagarHijos(TreeNode node, bool valor)
        {
            foreach (TreeNode hijo in node.Nodes)
            {
                hijo.Checked = valor;
                PropagarHijos(hijo, valor);
            }
        }

        private void Guardar()
        {
            // Segunda capa del control de acceso: editar la composicion de un
            // perfil redefine que puede hacer el resto del sistema.
            if (!Permisos.Exigir("PERFILES_GESTION", FindForm(), "guardar la composicion de un perfil")) return;

            _lblOk.Visible = false;
            _lblError.Visible = false;
            if (!(_cboPerfil.SelectedValue is int perfilId))
            {
                MostrarError(Tr.T("MSG_PERF_SELECCIONE"));
                return;
            }
            try
            {
                var ids = new List<int>();
                RecolectarChecked(_tree.Nodes, ids);
                // Los tildes heredados los puso el sistema: no son asignaciones
                // directas del perfil (viven en el perfil incluido que las aporta).
                ids.RemoveAll(id => _marcadosHeredados.Contains(id));

                // Perfiles incluidos tildados en la rama del Composite de perfiles.
                var incluidos = new List<int>();
                if (_nodoPerfiles != null)
                    foreach (TreeNode n in _nodoPerfiles.Nodes)
                        if (n.Checked && n.Tag is BE_Perfil p) incluidos.Add(p.Id);

                PerfilResult res = BLL_Perfil.GuardarComposicion(perfilId, ids, incluidos);
                if (res == PerfilResult.ReferenciaCircular)
                {
                    MostrarError(T("MSG_PERF_CICLO", "No se puede incluir ese perfil: generaria una referencia circular."));
                    return;
                }

                // Refresca la rama: los permisos heredados que muestran los demas
                // perfiles pueden haber cambiado con esta edicion.
                CargarAsignacionesPerfil();
                _lblOk.Text = Tr.T("MSG_PERF_OK");
                _lblOk.Visible = true;
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Perfiles", "Guardar permisos");
                MostrarError(Tr.T("MSG_ERROR_PREFIJO") + ex.Message);
            }
        }

        private void RecolectarChecked(TreeNodeCollection nodes, List<int> ids)
        {
            foreach (TreeNode n in nodes)
            {
                if (n.Checked && n.Tag is int id) ids.Add(id);
                RecolectarChecked(n.Nodes, ids);
            }
        }

        private void NuevoPerfil()
        {
            if (!Permisos.Exigir("PERFILES_GESTION", FindForm(), "crear un perfil")) return;
            using (var dlg = new frmNuevoPerfil())
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    CargarPerfiles();
                    _cboPerfil.SelectedValue = dlg.NuevoId;
                    CargarUsuarios(); // el nuevo perfil aparece en el combo de asignacion
                }
            }
        }

        private void MostrarError(string msg)
        {
            _lblError.Text = msg;
            _lblError.Visible = true;
        }

        // Devuelve la traduccion de 'clave' o, si falta, el texto por defecto dado.
        private static string T(string clave, string defecto)
        {
            string t = Tr.T(clave);
            return t == clave ? defecto : t;
        }

        // ===================== Asignacion a usuarios =====================
        private void CargarUsuarios()
        {
            try
            {
                // Opciones del combo: "(sin perfil)" (Id=0) + perfiles existentes.
                var opciones = new List<BE_Perfil> { new BE_Perfil { Id = 0, Nombre = Tr.T("PERF_SIN") } };
                opciones.AddRange(BLL_Perfil.GetPerfiles());
                var col = (DataGridViewComboBoxColumn)_gridUsuarios.Columns["cPerfil"];
                col.DataSource = opciones;
                col.ValueMember = "Id";
                col.DisplayMember = "Nombre";

                _gridUsuarios.Rows.Clear();
                foreach (var u in BLL_User.GetAll())
                {
                    int idx = _gridUsuarios.Rows.Add(u.Username);
                    var row = _gridUsuarios.Rows[idx];
                    row.Tag = u;
                    row.Cells["cPerfil"].Value = u.PerfilId ?? 0;
                    row.Cells["cEstado"].Value = u.Blocked ? Tr.T("EST_BLOQUEADO")
                                               : (u.Activo ? Tr.T("EST_ACTIVO") : Tr.T("EST_INACTIVO"));
                    // Icono de candado (desbloquear) solo para cuentas bloqueadas.
                    row.Cells["cDesbloq"].Value = u.Blocked ? Theme.IcoUnlock : "";
                    if (u.Blocked) row.Cells["cEstado"].Style.ForeColor = Theme.Error;
                }
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Perfiles", "Cargar usuarios");
                MensajeAsig(Tr.T("MSG_ERROR_PREFIJO") + ex.Message, error: true);
            }
        }

        private void GuardarAsignaciones()
        {
            try
            {
                _gridUsuarios.EndEdit();
                foreach (DataGridViewRow row in _gridUsuarios.Rows)
                {
                    if (!(row.Tag is BE_User u)) continue;
                    int val = row.Cells["cPerfil"].Value is int v ? v : 0;
                    BLL_User.AsignarPerfil(u.Id, val == 0 ? (int?)null : val);
                }
                MensajeAsig(Tr.T("MSG_PERF_ASIG_OK"), error: false);
            }
            catch (Exception ex)
            {
                BLL_Bitacora.RegistrarExcepcion(ex, "Perfiles", "Guardar asignaciones de usuarios");
                MensajeAsig(Tr.T("MSG_ERROR_PREFIJO") + ex.Message, error: true);
            }
        }

        private void MensajeAsig(string texto, bool error)
        {
            _lblMsgAsig.ForeColor = error ? Theme.Error : Theme.Success;
            _lblMsgAsig.Text = texto;
            _lblMsgAsig.Visible = true;
        }
    }
}
