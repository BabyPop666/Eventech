using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EvenTech.BE;
using EvenTech.BLL;

namespace EvenTech.UI
{
    // Gestion de perfiles (T04, patron Composite). Muestra el arbol de permisos
    // en un TreeView construido recursivamente. Al tildar un grupo, se propaga
    // recursivamente a sus hijos (permiso heredado).
    public class ucPerfiles : UserControl
    {
        private ComboBox _cboPerfil;
        private TreeView _tree;
        private Button _btnGuardar;
        private Label _lblError, _lblOk;
        private bool _suppressAfterCheck;

        public ucPerfiles()
        {
            BackColor = Theme.BgContent;
            BuildUi();
            Load += (s, e) => { ConstruirArbol(); CargarPerfiles(); };
        }

        private void BuildUi()
        {
            var lblTitle = new Label
            {
                Text = "Gestion de Perfiles",
                Font = new Font("Ebrima", 18F, FontStyle.Bold),
                ForeColor = Theme.TextOnLight,
                AutoSize = true,
                Location = new Point(10, 10)
            };

            var lblPerfil = new Label
            {
                Text = "Perfil:",
                Font = new Font("Ebrima", 11F),
                ForeColor = Theme.TextOnLight,
                AutoSize = true,
                Location = new Point(12, 56)
            };
            _cboPerfil = new ComboBox
            {
                Location = new Point(70, 52),
                Size = new Size(260, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Ebrima", 11F)
            };
            _cboPerfil.SelectedIndexChanged += (s, e) => CargarAsignacionesPerfil();

            var lblHint = new Label
            {
                Text = "Tilde los permisos del perfil. Marcar un grupo incluye a sus hijos.",
                Font = new Font("Ebrima", 9F, FontStyle.Italic),
                ForeColor = Color.DimGray,
                AutoSize = true,
                Location = new Point(12, 90)
            };

            _tree = new TreeView
            {
                Location = new Point(12, 115),
                Size = new Size(560, 420),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right,
                CheckBoxes = true,
                Font = new Font("Ebrima", 10.5F),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ShowLines = true,
                HideSelection = false
            };
            _tree.AfterCheck += Tree_AfterCheck;

            _btnGuardar = new Button
            {
                Text = "Guardar permisos",
                Font = Theme.FontButton,
                BackColor = Theme.AccentButton,
                ForeColor = Theme.TextOnDark,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(180, 36),
                Location = new Point(12, 545),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Cursor = Cursors.Hand
            };
            _btnGuardar.FlatAppearance.BorderSize = 0;
            _btnGuardar.Click += (s, e) => Guardar();

            _lblOk = new Label
            {
                AutoSize = true, Font = new Font("Ebrima", 10F, FontStyle.Bold),
                ForeColor = Color.SeaGreen, Location = new Point(205, 555), Visible = false
            };
            _lblError = new Label
            {
                AutoSize = true, Font = new Font("Ebrima", 10F, FontStyle.Bold),
                ForeColor = Color.Firebrick, Location = new Point(205, 555), Visible = false,
                MaximumSize = new Size(360, 0)
            };

            Controls.Add(lblTitle);
            Controls.Add(lblPerfil);
            Controls.Add(_cboPerfil);
            Controls.Add(lblHint);
            Controls.Add(_tree);
            Controls.Add(_btnGuardar);
            Controls.Add(_lblOk);
            Controls.Add(_lblError);
        }

        private void ConstruirArbol()
        {
            try
            {
                _tree.Nodes.Clear();
                List<BE_IComponentePermiso> raices = BLL_Perfil.GetArbolPermisos();
                foreach (var nodo in raices)
                    _tree.Nodes.Add(CrearNodo(nodo));
                _tree.ExpandAll();
            }
            catch (Exception ex)
            {
                MostrarError("Error cargando permisos: " + ex.Message);
            }
        }

        // Construccion recursiva del TreeView a partir del arbol Composite.
        private TreeNode CrearNodo(BE_IComponentePermiso componente)
        {
            var node = new TreeNode(componente.Nombre) { Tag = componente.Id };
            if (componente is BE_GrupoPermisos grupo)
            {
                node.NodeFont = new Font("Ebrima", 10.5F, FontStyle.Bold);
                foreach (var hijo in grupo.Hijos)
                    node.Nodes.Add(CrearNodo(hijo));
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
                MostrarError("Error cargando perfiles: " + ex.Message);
            }
        }

        private void CargarAsignacionesPerfil()
        {
            if (!(_cboPerfil.SelectedValue is int perfilId)) return;
            try
            {
                HashSet<int> asignados = BLL_Perfil.GetPermisosAsignados(perfilId);
                _suppressAfterCheck = true;
                AplicarChecks(_tree.Nodes, asignados);
                _suppressAfterCheck = false;
            }
            catch (Exception ex)
            {
                _suppressAfterCheck = false;
                MostrarError("Error cargando asignaciones: " + ex.Message);
            }
        }

        private void AplicarChecks(TreeNodeCollection nodes, HashSet<int> asignados)
        {
            foreach (TreeNode n in nodes)
            {
                n.Checked = n.Tag is int id && asignados.Contains(id);
                AplicarChecks(n.Nodes, asignados);
            }
        }

        // Al tildar/destildar un nodo, propagar recursivamente a los hijos.
        private void Tree_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (_suppressAfterCheck) return;
            _suppressAfterCheck = true;
            PropagarHijos(e.Node, e.Node.Checked);
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
            _lblOk.Visible = false;
            _lblError.Visible = false;
            if (!(_cboPerfil.SelectedValue is int perfilId))
            {
                MostrarError("Seleccione un perfil.");
                return;
            }
            try
            {
                var ids = new List<int>();
                RecolectarChecked(_tree.Nodes, ids);
                BLL_Perfil.GuardarAsignaciones(perfilId, ids);
                _lblOk.Text = $"Permisos guardados ({ids.Count} componentes).";
                _lblOk.Visible = true;
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo guardar: " + ex.Message);
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

        private void MostrarError(string msg)
        {
            _lblError.Text = msg;
            _lblError.Visible = true;
        }
    }
}
