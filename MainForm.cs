using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PowerShellAnalyzer
{
    public partial class MainForm : Form
    {
        // Menu kontekstowe pod prawy przycisk myszy
        private ContextMenuStrip gridContextMenu;
        private Label lblSimilarityTitle;
        private Label lblSimilarityData;
        private SimilarityManager _simManager = new SimilarityManager();

        private string apiKeyOpenAI = "";
        private string apiKeyGemini = "";
        private string apiKeyClaude = "";

        // Biblioteka - komponenty
        private TreeView tvAlbums;
        private DataGridView dgvLibraryScripts;
        private ContextMenuStrip libraryGridContextMenu;
        private ContextMenuStrip tvContextMenu;
        private int? currentSelectedAlbumId = null;

        // Umbenennen (Rename) - komponenty
        private TabPage tabRename;
        private DataGridView dgvRename;
        private Button btnSuggestNames;
        private Button btnApplyRename;
        private Button btnRemoveFromRename;

        private void LogToProtocol(string message)
        {
            if (txtProtocol.InvokeRequired)
            {
                txtProtocol.Invoke(new Action(() => LogToProtocol(message)));
                return;
            }
            txtProtocol.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            txtProtocol.ScrollToCaret();
        }

        public MainForm()
        {
            InitializeComponent();
            DatabaseHelper.InitializeDatabase();
            LoadApiKeys();
            SetupCustomUI();
            SetupRenameUI();
            LoadMockModels();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadSavedSources();
        }

        private void LoadApiKeys()
        {
            apiKeyOpenAI = DatabaseHelper.GetSetting("OpenAI");
            apiKeyGemini = DatabaseHelper.GetSetting("Gemini");
            apiKeyClaude = DatabaseHelper.GetSetting("Claude");

            if (!string.IsNullOrWhiteSpace(apiKeyOpenAI) || !string.IsNullOrWhiteSpace(apiKeyGemini) || !string.IsNullOrWhiteSpace(apiKeyClaude))
            {
                lblApiKeyStatus.Text = "Status: Schlüssel gespeichert";
                lblApiKeyStatus.ForeColor = Color.Green;
            }
            else
            {
                lblApiKeyStatus.Text = "Status: Kein Schlüssel vorhanden";
                lblApiKeyStatus.ForeColor = Color.Red;
            }
        }

        private void LoadSavedSources()
        {
            var savedSources = DatabaseHelper.GetSources();
            foreach (var source in savedSources)
            {
                if (System.IO.Directory.Exists(source))
                {
                    lstSources.Items.Add(source);
                }
            }

            if (lstSources.Items.Count > 0)
            {
                ScanFoldersForScripts();
            }
        }

        private void SetupCustomUI()
        {
            // --- 1. NAPRAWA NAZW KOLUMN I CZCIONKI TABELI ---
            dgvScripts.DefaultCellStyle.Font = new Font("Segoe UI", 9.75F);

            if (dgvScripts.Columns.Count >= 4 && !dgvScripts.Columns.Contains("Path"))
            {
                dgvScripts.Columns[0].Name = "Status";
                dgvScripts.Columns[1].Name = "Id";
                dgvScripts.Columns[2].Name = "FileName";
                dgvScripts.Columns[3].Name = "Path";
            }

            dgvScripts.CellPainting -= DgvScripts_CellPainting;
            dgvScripts.CellPainting += DgvScripts_CellPainting;

            // --- 2. DODANIE KOLUMNY CHECKBOX ---
            dgvScripts.ReadOnly = false;
            dgvScripts.EditMode = DataGridViewEditMode.EditOnEnter;

            if (!dgvScripts.Columns.Contains("Select"))
            {
                DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn
                {
                    Name = "Select",
                    HeaderText = "☑",
                    Width = 40,
                    ReadOnly = false
                };
                dgvScripts.Columns.Insert(0, checkColumn);
            }

            foreach (DataGridViewColumn col in dgvScripts.Columns)
            {
                if (col.Name != "Select") col.ReadOnly = true;
            }

            // --- 3. STYLIZACJA TABELI I ZAKŁADEK ---
            dgvScripts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvScripts.ColumnHeadersHeight = 40;
            dgvScripts.EnableHeadersVisualStyles = false;
            dgvScripts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 244, 248);
            dgvScripts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            dgvScripts.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 244, 248);

            tabControlMain.SizeMode = TabSizeMode.Fixed;
            tabControlMain.ItemSize = new Size(150, 40);

            txtDescriptionRight.BackColor = Color.FromArgb(248, 249, 250);
            txtProtocol.BackColor = Color.FromArgb(248, 249, 250);

            // --- 4. STYLIZACJA PRZYCISKÓW ---
            StyleSecondaryButton(btnCancel);
            StyleSecondaryButton(btnAddSource);
            StyleSecondaryButton(btnApiKey);
            StyleSecondaryButton(btnEditDesc);

            StylePrimaryButton(btnAnalyze);
            StylePrimaryButton(btnSaveDesc);

            // --- 5. TŁUMACZENIA NA NIEMIECKI ---
            btnAddSource.Text = "+ Quelle hinzufügen";
            btnEditDesc.Text = "Bearbeiten";
            btnSaveDesc.Text = "Speichern";
            btnAnalyze.Text = "Analyse starten";
            btnCancel.Text = "Abbrechen";
            btnApiKey.Text = "⚙ Einstellungen";

            // --- 6. MENU KONTEKSTOWE ---
            gridContextMenu = new ContextMenuStrip();
            gridContextMenu.Items.Add("Nur dieses Skript analysieren", null, ContextMenu_AnalyzeSingle);
            gridContextMenu.Items.Add("Pfad kopieren", null, ContextMenu_CopyPath);
            gridContextMenu.Items.Add(new ToolStripSeparator());
            gridContextMenu.Items.Add("Ähnliche Skripte anzeigen", null, ContextMenu_ShowSimilar);
            gridContextMenu.Items.Add("Mit ähnlichem Skript vergleichen", null, ContextMenu_CompareSimilar);
            gridContextMenu.Items.Add(new ToolStripSeparator());
            gridContextMenu.Items.Add("Skript öffnen", null, ContextMenu_OpenScript);

            // --- 7. PODPIĘCIE ZDARZEŃ ---
            btnAddSource.Click -= BtnAddSource_Click;
            btnAddSource.Click += BtnAddSource_Click;

            btnAnalyze.Click -= BtnAnalyzeGlobal_Click;
            btnAnalyze.Click += BtnAnalyzeGlobal_Click;

            txtSearch.TextChanged -= TxtSearch_TextChanged;
            txtSearch.TextChanged += TxtSearch_TextChanged;

            dgvScripts.CellMouseUp -= DgvScripts_CellMouseUp;
            dgvScripts.CellMouseUp += DgvScripts_CellMouseUp;

            dgvScripts.ColumnHeaderMouseClick -= DgvScripts_ColumnHeaderMouseClick;
            dgvScripts.ColumnHeaderMouseClick += DgvScripts_ColumnHeaderMouseClick;

            dgvScripts.SelectionChanged -= DgvScripts_SelectionChanged;
            dgvScripts.SelectionChanged += DgvScripts_SelectionChanged;

            btnEditDesc.Click -= BtnEditDesc_Click;
            btnEditDesc.Click += BtnEditDesc_Click;

            btnSaveDesc.Click -= BtnSaveDesc_Click;
            btnSaveDesc.Click += BtnSaveDesc_Click;

            // --- 8. STATUS LEGENDE HINZUFÜGEN ---
            Panel pnlTopRightInfo = new Panel
            {
                Width = 750,
                Height = 85,
                Location = new Point(pnlHeader.Width - 760, 5),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent
            };
            pnlHeader.Controls.Add(pnlTopRightInfo);

            Font boldFont = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            Font regularFont = new Font("Segoe UI", 9.75F);

            Label lblLegendTitle = new Label { Text = "Status Legende:", Font = boldFont, AutoSize = true, Location = new Point(0, 5) };

            PictureBox picWait = new PictureBox { Image = PowerShellManager.Resources.ausstehend, SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(18, 18), Location = new Point(120, 5) };
            Label lblLegendWait = new Label { Text = "Ausstehend", Font = regularFont, AutoSize = true, Location = new Point(140, 4) };

            PictureBox picOK = new PictureBox { Image = PowerShellManager.Resources.OK, SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(18, 18), Location = new Point(230, 5) };
            Label lblLegendOK = new Label { Text = "OK", Font = regularFont, AutoSize = true, Location = new Point(250, 4) };

            PictureBox picError = new PictureBox { Image = PowerShellManager.Resources.Fehler, SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(18, 18), Location = new Point(285, 5) };
            Label lblLegendError = new Label { Text = "Fehler", Font = regularFont, AutoSize = true, Location = new Point(305, 4) };

            PictureBox picDup = new PictureBox { Image = PowerShellManager.Resources.duplikat, SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(18, 18), Location = new Point(355, 5) };
            Label lblLegendDup = new Label { Text = "Duplikat", Font = regularFont, AutoSize = true, Location = new Point(375, 4) };

            pnlTopRightInfo.Controls.Add(lblLegendTitle);
            pnlTopRightInfo.Controls.Add(picWait);
            pnlTopRightInfo.Controls.Add(lblLegendWait);
            pnlTopRightInfo.Controls.Add(picOK);
            pnlTopRightInfo.Controls.Add(lblLegendOK);
            pnlTopRightInfo.Controls.Add(picError);
            pnlTopRightInfo.Controls.Add(lblLegendError);
            pnlTopRightInfo.Controls.Add(picDup);
            pnlTopRightInfo.Controls.Add(lblLegendDup);

            Button btnSearchDuplicates = new Button
            {
                Text = "Suche Duplikate",
                Location = new Point(0, 35),
                Width = 140,
                Height = 32
            };
            StyleSecondaryButton(btnSearchDuplicates);
            btnSearchDuplicates.Click += BtnSearchDuplicates_Click;
            pnlTopRightInfo.Controls.Add(btnSearchDuplicates);

            Button btnOverviewSimilar = new Button
            {
                Text = "Übersicht Duplikate",
                Location = new Point(150, 35),
                Width = 140,
                Height = 32
            };
            StyleSecondaryButton(btnOverviewSimilar);
            btnOverviewSimilar.Click += BtnOverviewSimilar_Click;
            pnlTopRightInfo.Controls.Add(btnOverviewSimilar);

            lblSimilarityTitle = new Label { Text = "Ähnlichkeit:", Font = new Font("Segoe UI", 9.75F, FontStyle.Bold), AutoSize = true, Location = new Point(300, 42) };
            lblSimilarityData = new Label { Text = "-", Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(390, 43), MaximumSize = new Size(350, 40), AutoEllipsis = true };

            pnlTopRightInfo.Controls.Add(lblSimilarityTitle);
            pnlTopRightInfo.Controls.Add(lblSimilarityData);

            btnCancel.Click -= btnCancel_Click_Handler;
            btnCancel.Click += btnCancel_Click_Handler;

            btnApiKey.Click -= BtnApiKey_Click;
            btnApiKey.Click += BtnApiKey_Click;

            SetupLibraryUI();
        }

        private void btnCancel_Click_Handler(object sender, EventArgs e)
        {
            MessageBox.Show("Aktion abgebrochen.", "Info");
        }

        private void SetupLibraryUI()
        {
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 6,
                BackColor = Color.White
            };

            splitContainer.Resize += (s, e) =>
            {
                if (splitContainer.Width > 100)
                {
                    splitContainer.SplitterDistance = (int)(splitContainer.Width * 0.40);
                }
            };
            splitContainer.SplitterDistance = (int)(1400 * 0.40);

            tabBibliothek.Controls.Add(splitContainer);

            var pnlTree = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            pnlTree.BackColor = Color.White;
            splitContainer.Panel1.Controls.Add(pnlTree);

            var lblTreeTitle = new Label { Text = "Alben", Font = new Font("Segoe UI", 9.75F, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 };
            pnlTree.Controls.Add(lblTreeTitle);

            tvAlbums = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                HideSelection = false,
                AllowDrop = true,
                ShowLines = false,
                FullRowSelect = true,
                ItemHeight = 25,
                BackColor = Color.White
            };
            pnlTree.Controls.Add(tvAlbums);
            tvAlbums.BringToFront();

            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            pnlGrid.BackColor = Color.White;
            splitContainer.Panel2.Controls.Add(pnlGrid);

            var lblGridTitle = new Label { Text = "Spezifikationen", Font = new Font("Segoe UI", 9.75F, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 };
            pnlGrid.Controls.Add(lblGridTitle);

            dgvLibraryScripts = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BorderStyle = BorderStyle.None,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 40,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };
            dgvLibraryScripts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 244, 248);
            dgvLibraryScripts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            dgvLibraryScripts.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 244, 248);
            dgvLibraryScripts.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvLibraryScripts.DefaultCellStyle.Padding = new Padding(5);

            dgvLibraryScripts.Columns.Add("Property", "Eigenschaft");
            dgvLibraryScripts.Columns["Property"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvLibraryScripts.Columns["Property"].FillWeight = 30;

            dgvLibraryScripts.Columns.Add("Value", "Wert");
            dgvLibraryScripts.Columns["Value"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvLibraryScripts.Columns["Value"].FillWeight = 70;
            dgvLibraryScripts.Columns["Value"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            pnlGrid.Controls.Add(dgvLibraryScripts);
            dgvLibraryScripts.BringToFront();

            tvContextMenu = new ContextMenuStrip();
            tvContextMenu.Items.Add("Neues Album", null, TvContext_AddAlbum);
            tvContextMenu.Items.Add("Album umbenennen", null, TvContext_RenameAlbum);
            tvContextMenu.Items.Add("Album löschen", null, TvContext_DeleteAlbum);
            tvAlbums.ContextMenuStrip = tvContextMenu;
            tvAlbums.MouseDown += TvAlbums_MouseDown;
            tvAlbums.AfterSelect += TvAlbums_AfterSelect;
            tvAlbums.ItemDrag += TvAlbums_ItemDrag;
            tvAlbums.DragEnter += TvAlbums_DragEnter;
            tvAlbums.DragDrop += TvAlbums_DragDrop;
            tvAlbums.NodeMouseDoubleClick += (s, e) =>
            {
                if (e.Node != null)
                {
                    if (e.Node.Tag is string scriptPath)
                    {
                        if (File.Exists(scriptPath))
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo("powershell_ise.exe", $"\"{scriptPath}\"") { UseShellExecute = true });
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Fehler beim Öffnen des Skripts: " + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    else
                    {
                        tvAlbums.LabelEdit = true;
                        e.Node.BeginEdit();
                    }
                }
            };
            tvAlbums.AfterLabelEdit += TvAlbums_AfterLabelEdit;

            libraryGridContextMenu = new ContextMenuStrip();
            libraryGridContextMenu.Items.Add("Skript öffnen", null, LibGridContext_OpenScript);
            libraryGridContextMenu.Items.Add(new ToolStripSeparator());
            libraryGridContextMenu.Items.Add("Skript umbenennen (virtuell)", null, LibGridContext_Rename);
            libraryGridContextMenu.Items.Add("Aus Album entfernen", null, LibGridContext_Remove);

            gridContextMenu.Items.Insert(0, new ToolStripSeparator());
            var libItem = new ToolStripMenuItem("Zur Bibliothek hinzufügen");
            gridContextMenu.Items.Insert(0, libItem);
            libItem.Click += ContextMenu_AddToLibrary;

            var renameItem = new ToolStripMenuItem("Zur Umbenennung hinzufügen");
            gridContextMenu.Items.Insert(1, renameItem);
            renameItem.Click += ContextMenu_AddToRename;

            gridContextMenu.Items.Add(new ToolStripSeparator());
            gridContextMenu.Items.Add("Skript physisch löschen", null, ContextMenu_DeleteScript);

            dgvLibraryScripts.CellPainting -= DgvLibraryScripts_CellPainting;
            dgvLibraryScripts.CellPainting += DgvLibraryScripts_CellPainting;

            LoadAlbums();
        }

        private void LoadAlbums()
        {
            tvAlbums.Nodes.Clear();
            var dt = DatabaseHelper.GetAlbums();
            var nodes = new Dictionary<int, TreeNode>();

            foreach (System.Data.DataRow row in dt.Rows)
            {
                int id = Convert.ToInt32(row["Id"]);
                string name = row["Name"].ToString();
                var node = new TreeNode(name) { Tag = id };
                nodes[id] = node;
            }

            foreach (System.Data.DataRow row in dt.Rows)
            {
                int id = Convert.ToInt32(row["Id"]);
                if (row["ParentId"] != DBNull.Value)
                {
                    int parentId = Convert.ToInt32(row["ParentId"]);
                    if (nodes.ContainsKey(parentId))
                        nodes[parentId].Nodes.Add(nodes[id]);
                    else
                        tvAlbums.Nodes.Add(nodes[id]);
                }
                else
                {
                    tvAlbums.Nodes.Add(nodes[id]);
                }
            }

            foreach (var albumId in nodes.Keys)
            {
                var scriptsDt = DatabaseHelper.GetScriptsInAlbum(albumId);
                foreach (System.Data.DataRow row in scriptsDt.Rows)
                {
                    string path = row["Path"].ToString();
                    string fileName = row["FileName"].ToString();
                    var scriptNode = new TreeNode("📄 " + fileName) { Tag = path };
                    nodes[albumId].Nodes.Add(scriptNode);
                }
            }

            tvAlbums.ExpandAll();
        }

        private void TvAlbums_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var clickedNode = tvAlbums.GetNodeAt(e.X, e.Y);
                tvAlbums.SelectedNode = clickedNode;

                if (clickedNode != null)
                {
                    if (clickedNode.Tag is int)
                    {
                        tvAlbums.ContextMenuStrip = tvContextMenu;
                    }
                    else if (clickedNode.Tag is string)
                    {
                        tvAlbums.ContextMenuStrip = libraryGridContextMenu;
                    }
                }
                else
                {
                    tvAlbums.ContextMenuStrip = tvContextMenu;
                }
            }
            else
            {
                tvAlbums.ContextMenuStrip = null;
            }
        }

        private void TvAlbums_AfterSelect(object sender, TreeViewEventArgs e)
        {
            dgvLibraryScripts.Rows.Clear();
            if (e.Node != null && e.Node.Tag != null)
            {
                if (e.Node.Tag is int albumId)
                {
                    currentSelectedAlbumId = albumId;
                    dgvLibraryScripts.Rows.Add("Typ", "Album");
                    dgvLibraryScripts.Rows.Add("Name", e.Node.Text);
                }
                else if (e.Node.Tag is string scriptPath)
                {
                    currentSelectedAlbumId = e.Node.Parent?.Tag as int?;

                    var dbInfo = DatabaseHelper.GetScriptInfo(scriptPath);
                    string desc = dbInfo.Description ?? "";
                    string recName = "-";

                    try
                    {
                        if (desc.Contains("Empfohlener Dateiname:"))
                        {
                            var parts = desc.Split(new[] { "Empfohlener Dateiname:" }, StringSplitOptions.None);
                            if (parts.Length > 1)
                            {
                                desc = parts[0].Trim();
                                recName = parts[1].Trim();
                            }
                        }
                    }
                    catch { }

                    dgvLibraryScripts.Rows.Add("Typ", "Skript");
                    dgvLibraryScripts.Rows.Add("Dateiname", e.Node.Text.Replace("📄 ", ""));
                    dgvLibraryScripts.Rows.Add("Empfohlener Dateiname", recName);
                    dgvLibraryScripts.Rows.Add("Pfad", scriptPath);
                    dgvLibraryScripts.Rows.Add("Status", dbInfo.Status);
                    dgvLibraryScripts.Rows.Add("Beschreibung", desc);

                    var simInfo = _simManager.Scripts.FirstOrDefault(s => s.Path == scriptPath);
                    if (simInfo != null && simInfo.SimilarScriptsCount > 0)
                    {
                        dgvLibraryScripts.Rows.Add("Ähnliche Skripte", $"{simInfo.SimilarScriptsCount} (Beste Übereinstimmung: {simInfo.BestSimilarityScore:P0})");
                    }
                    else
                    {
                        dgvLibraryScripts.Rows.Add("Ähnliche Skripte", "Keine");
                    }
                }
            }
            else
            {
                currentSelectedAlbumId = null;
            }
        }

        private void TvAlbums_ItemDrag(object sender, ItemDragEventArgs e)
        {
            DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void TvAlbums_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void TvAlbums_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                Point pt = tvAlbums.PointToClient(new Point(e.X, e.Y));
                TreeNode targetNode = tvAlbums.GetNodeAt(pt);
                TreeNode draggedNode = (TreeNode)e.Data.GetData(typeof(TreeNode));

                if (draggedNode != null && targetNode != draggedNode && !ContainsNode(draggedNode, targetNode))
                {
                    if (draggedNode.Tag is int draggedAlbumId)
                    {
                        int? targetId = null;
                        if (targetNode != null && targetNode.Tag is int tId)
                            targetId = tId;
                        else if (targetNode != null && targetNode.Tag is string)
                            targetId = targetNode.Parent?.Tag as int?;

                        DatabaseHelper.UpdateAlbum(draggedAlbumId, draggedNode.Text, targetId);

                        draggedNode.Remove();
                        if (targetId.HasValue && targetNode != null)
                        {
                            var destNode = (targetNode.Tag is int) ? targetNode : targetNode.Parent;
                            destNode.Nodes.Add(draggedNode);
                            destNode.Expand();
                        }
                        else
                        {
                            tvAlbums.Nodes.Add(draggedNode);
                        }
                    }
                    else if (draggedNode.Tag is string scriptPath && draggedNode.Parent != null)
                    {
                        // BEZPIECZNE POBIERANIE STARGO ID ALBUMU
                        if (draggedNode.Parent.Tag is int oldAlbumId)
                        {
                            int newAlbumId = -1;

                            if (targetNode != null && targetNode.Tag is int targetAId)
                                newAlbumId = targetAId;
                            else if (targetNode != null && targetNode.Tag is string && targetNode.Parent != null)
                                newAlbumId = (targetNode.Parent.Tag is int parentId) ? parentId : -1;

                            if (newAlbumId != -1 && newAlbumId != oldAlbumId)
                            {
                                DatabaseHelper.RemoveScriptFromAlbum(scriptPath, oldAlbumId);
                                DatabaseHelper.AddScriptToAlbum(scriptPath, newAlbumId);

                                draggedNode.Remove();
                                var destNode = (targetNode.Tag is int) ? targetNode : targetNode.Parent;
                                destNode.Nodes.Add(draggedNode);
                                destNode.Expand();
                            }
                        }
                    }
                }
            }
        }

        private bool ContainsNode(TreeNode node1, TreeNode node2)
        {
            if (node2 == null) return false;
            if (node2.Parent == null) return false;
            if (node2.Parent.Equals(node1)) return true;
            return ContainsNode(node1, node2.Parent);
        }

        private void TvAlbums_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Label))
            {
                if (e.Node.Tag is int id)
                {
                    int? parentId = e.Node.Parent?.Tag as int?;
                    DatabaseHelper.UpdateAlbum(id, e.Label, parentId);
                }
                else if (e.Node.Tag is string scriptPath)
                {
                    string newName = e.Label.Replace("📄 ", "");
                    DatabaseHelper.UpdateScriptFileName(scriptPath, newName);

                    foreach (DataGridViewRow row in dgvScripts.Rows)
                    {
                        if (row.Cells["Path"].Value?.ToString() == scriptPath)
                        {
                            row.Cells["FileName"].Value = newName;
                            break;
                        }
                    }
                    e.Node.Text = "📄 " + newName;
                }
            }
            else
            {
                e.CancelEdit = true;
            }
        }

        private void LibGridContext_OpenScript(object sender, EventArgs e)
        {
            if (tvAlbums.SelectedNode != null && tvAlbums.SelectedNode.Tag is string scriptPath)
            {
                if (File.Exists(scriptPath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo("powershell_ise.exe", $"\"{scriptPath}\"") { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Fehler beim Öffnen des Skripts: " + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void LibGridContext_Rename(object sender, EventArgs e)
        {
            if (tvAlbums.SelectedNode != null && tvAlbums.SelectedNode.Tag is string)
            {
                tvAlbums.LabelEdit = true;
                tvAlbums.SelectedNode.BeginEdit();
            }
        }

        private void LibGridContext_Remove(object sender, EventArgs e)
        {
            if (tvAlbums.SelectedNode != null && tvAlbums.SelectedNode.Tag is string scriptPath)
            {
                int albumId = (int)tvAlbums.SelectedNode.Parent.Tag;
                DatabaseHelper.RemoveScriptFromAlbum(scriptPath, albumId);
                tvAlbums.SelectedNode.Remove();
            }
        }

        private void ContextMenu_DeleteScript(object sender, EventArgs e)
        {
            if (dgvScripts.SelectedRows.Count > 0)
            {
                var row = dgvScripts.SelectedRows[0];
                var path = row.Cells["Path"].Value.ToString();

                if (MessageBox.Show($"Möchten Sie das Skript wirklich von der Festplatte löschen?\n\n{path}", "Löschen bestätigen", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    try
                    {
                        if (File.Exists(path))
                            File.Delete(path);

                        DatabaseHelper.DeleteScriptData(path);
                        dgvScripts.Rows.Remove(row);
                        LoadAlbums();
                        MessageBox.Show("Skript wurde erfolgreich gelöscht.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Löschen des Skripts:\n{ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void TvContext_AddAlbum(object sender, EventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Bitte geben Sie den Namen des neuen Albums ein:", "Neues Album", "Neues Album");
            if (!string.IsNullOrWhiteSpace(name))
            {
                int? parentId = tvAlbums.SelectedNode?.Tag as int?;
                int newId = DatabaseHelper.AddAlbum(name, parentId);

                var node = new TreeNode(name) { Tag = newId };
                if (tvAlbums.SelectedNode != null)
                {
                    tvAlbums.SelectedNode.Nodes.Add(node);
                    tvAlbums.SelectedNode.Expand();
                }
                else
                {
                    tvAlbums.Nodes.Add(node);
                }
            }
        }

        private void TvContext_RenameAlbum(object sender, EventArgs e)
        {
            if (tvAlbums.SelectedNode != null)
            {
                tvAlbums.LabelEdit = true;
                tvAlbums.SelectedNode.BeginEdit();
            }
        }

        private void TvContext_DeleteAlbum(object sender, EventArgs e)
        {
            if (tvAlbums.SelectedNode != null)
            {
                if (MessageBox.Show("Möchten Sie dieses Album wirklich löschen? Alle Unteralben werden ebenfalls gelöscht. Es werden keine Skripte von der Festplatte gelöscht.", "Album löschen", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    int id = (int)tvAlbums.SelectedNode.Tag;
                    DatabaseHelper.DeleteAlbum(id);
                    tvAlbums.SelectedNode.Remove();
                    RefreshAllScriptIcons();
                }
            }
        }

        private void DgvScripts_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == dgvScripts.Columns["Select"].Index)
            {
                bool isAnyUnchecked = false;
                foreach (DataGridViewRow row in dgvScripts.Rows)
                {
                    if (row.Cells["Select"].Value == null || !(bool)row.Cells["Select"].Value)
                    {
                        isAnyUnchecked = true;
                        break;
                    }
                }

                foreach (DataGridViewRow row in dgvScripts.Rows)
                {
                    row.Cells["Select"].Value = isAnyUnchecked;
                }
                dgvScripts.EndEdit();
            }
        }

        private void ContextMenu_AddToRename(object sender, EventArgs e)
        {
            int addedCount = 0;
            var rowsToProcess = new List<DataGridViewRow>();

            foreach (DataGridViewRow row in dgvScripts.Rows)
            {
                if (Convert.ToBoolean(row.Cells["Select"].Value))
                {
                    rowsToProcess.Add(row);
                }
            }

            if (rowsToProcess.Count == 0 && dgvScripts.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvScripts.SelectedRows)
                {
                    rowsToProcess.Add(row);
                }
            }

            foreach (DataGridViewRow row in rowsToProcess)
            {
                string id = row.Cells["Id"].Value?.ToString();
                string fileName = row.Cells["FileName"].Value?.ToString();
                string path = row.Cells["Path"].Value?.ToString();

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(path))
                {
                    bool alreadyExists = false;
                    foreach (DataGridViewRow r in dgvRename.Rows)
                    {
                        if (r.Cells["Path"] != null && r.Cells["Path"].Value?.ToString() == path)
                        {
                            alreadyExists = true;
                            break;
                        }
                    }

                    if (!alreadyExists)
                    {
                        dgvRename.Rows.Add(id, fileName, "", path);
                        addedCount++;
                    }
                }
            }

            if (addedCount > 0)
            {
                tabControlMain.SelectedTab = tabRename;
                lblStatusInfo.Text = $"{addedCount} Skripte zur Umbenennung hinzugefügt.";
            }
        }

        private async void BtnSuggestNames_Click(object sender, EventArgs e)
        {
            string model = cmbModels.Text;
            string keyToUse = "";
            if (model.StartsWith("Gemini")) keyToUse = apiKeyGemini;
            else if (model.StartsWith("OpenAI")) keyToUse = apiKeyOpenAI;

            if (string.IsNullOrEmpty(keyToUse))
            {
                MessageBox.Show("Bitte konfigurieren Sie zuerst den API-Key!", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSuggestNames.Enabled = false;
            LogToProtocol("Generiere neue Dateinamen...");

            foreach (DataGridViewRow row in dgvRename.Rows)
            {
                if (row.IsNewRow) continue;
                string currentProposed = row.Cells["ProposedName"].Value?.ToString();
                if (!string.IsNullOrEmpty(currentProposed)) continue;

                string path = row.Cells["Path"].Value.ToString();
                var dbInfo = DatabaseHelper.GetScriptInfo(path);
                string description = dbInfo.Description;

                if (string.IsNullOrWhiteSpace(description) || description.Length < 10)
                {
                    LogToProtocol($"Keine ausreichende Beschreibung für {row.Cells["CurrentName"].Value}, überspringe...");
                    continue;
                }

                try
                {
                    row.Cells["ProposedName"].Value = "⏳ Generiere...";
                    string aiName = await AIEngine.AnalyzeRenameAsync(description, keyToUse, model);

                    string cleanAiName = aiName.Replace(".ps1", "").Trim();
                    string id = row.Cells["Id"].Value.ToString();
                    string proposedName = $"{id}_{cleanAiName}.ps1";

                    row.Cells["ProposedName"].Value = proposedName;
                }
                catch (Exception ex)
                {
                    row.Cells["ProposedName"].Value = "";
                    LogToProtocol($"Fehler bei der Namensgenerierung: {ex.Message}");
                }
            }

            btnSuggestNames.Enabled = true;
            LogToProtocol("Namensvorschläge abgeschlossen. Sie können die Namen bei Bedarf manuell anpassen.");
        }

        private void BtnApplyRename_Click(object sender, EventArgs e)
        {
            int changedCount = 0;
            var rowsToRemove = new System.Collections.Generic.List<DataGridViewRow>();

            foreach (DataGridViewRow row in dgvRename.Rows)
            {
                if (row.IsNewRow) continue;
                string proposedName = row.Cells["ProposedName"].Value?.ToString();
                if (string.IsNullOrWhiteSpace(proposedName) || proposedName.StartsWith("⏳")) continue;

                if (!proposedName.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                {
                    proposedName += ".ps1";
                }

                string oldPath = row.Cells["Path"].Value.ToString();
                string oldName = row.Cells["CurrentName"].Value.ToString();

                if (oldName == proposedName)
                {
                    rowsToRemove.Add(row);
                    continue;
                }

                try
                {
                    string dir = Path.GetDirectoryName(oldPath);
                    string newPath = Path.Combine(dir, proposedName);

                    if (File.Exists(newPath))
                    {
                        LogToProtocol($"Warnung: {newPath} existiert bereits! Überspringe {oldName}.");
                        continue;
                    }

                    File.Move(oldPath, newPath);
                    DatabaseHelper.MoveScript(oldPath, newPath, proposedName);

                    changedCount++;
                    rowsToRemove.Add(row);
                }
                catch (Exception ex)
                {
                    LogToProtocol($"Fehler beim Umbenennen von {oldName}: {ex.Message}");
                }
            }

            foreach (var r in rowsToRemove)
            {
                dgvRename.Rows.Remove(r);
            }

            if (changedCount > 0)
            {
                MessageBox.Show($"{changedCount} Skripte wurden erfolgreich umbenannt.", "Umbenennen", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ScanFoldersForScripts();
                LoadAlbums();
            }
        }

        private void BtnRemoveFromRename_Click(object sender, EventArgs e)
        {
            var rowsToRemove = new System.Collections.Generic.List<DataGridViewRow>();
            foreach (DataGridViewRow row in dgvRename.SelectedRows)
            {
                if (!row.IsNewRow) rowsToRemove.Add(row);
            }

            foreach (var r in rowsToRemove)
            {
                dgvRename.Rows.Remove(r);
            }
        }

        private void ContextMenu_AddToLibrary(object sender, EventArgs e)
        {
            var rowsToProcess = new List<DataGridViewRow>();
            foreach (DataGridViewRow row in dgvScripts.Rows)
            {
                if (Convert.ToBoolean(row.Cells["Select"].Value))
                {
                    rowsToProcess.Add(row);
                }
            }

            if (rowsToProcess.Count == 0 && dgvScripts.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvScripts.SelectedRows)
                {
                    rowsToProcess.Add(row);
                }
            }

            if (rowsToProcess.Count > 0)
            {
                var dt = DatabaseHelper.GetAlbums();
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Sie müssen zuerst ein Album in der Registerkarte 'Bibliothek' erstellen.", "Keine Alben vorhanden", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Form picker = new Form { Text = "Album auswählen", Width = 300, Height = 400, StartPosition = FormStartPosition.CenterParent };
                TreeView tvPicker = new TreeView { Dock = DockStyle.Fill, HideSelection = false };
                picker.Controls.Add(tvPicker);

                var btnOk = new Button { Text = "OK", Dock = DockStyle.Bottom, Height = 40 };
                picker.Controls.Add(btnOk);

                var nodes = new Dictionary<int, TreeNode>();
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    int id = Convert.ToInt32(row["Id"]);
                    string name = row["Name"].ToString();
                    var node = new TreeNode(name) { Tag = id };
                    nodes[id] = node;
                }
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    int id = Convert.ToInt32(row["Id"]);
                    if (row["ParentId"] != DBNull.Value)
                    {
                        int parentId = Convert.ToInt32(row["ParentId"]);
                        if (nodes.ContainsKey(parentId))
                            nodes[parentId].Nodes.Add(nodes[id]);
                        else
                            tvPicker.Nodes.Add(nodes[id]);
                    }
                    else
                        tvPicker.Nodes.Add(nodes[id]);
                }
                tvPicker.ExpandAll();

                btnOk.Click += (s, args) =>
                {
                    if (tvPicker.SelectedNode != null)
                    {
                        int targetAlbumId = (int)tvPicker.SelectedNode.Tag;

                        foreach (DataGridViewRow row in rowsToProcess)
                        {
                            string path = row.Cells["Path"].Value?.ToString();
                            if (string.IsNullOrEmpty(path)) continue;

                            string fileName = row.Cells["FileName"].Value?.ToString() ?? "Unknown";
                            if (fileName.StartsWith("📘 "))
                                fileName = fileName.Substring(3);

                            string status = row.Cells["Status"].Value?.ToString() ?? "⏳";
                            var dbInfo = DatabaseHelper.GetScriptInfo(path);
                            DatabaseHelper.SaveScript(path, fileName, status, dbInfo.Description);

                            DatabaseHelper.AddScriptToAlbum(path, targetAlbumId);
                            row.Cells["Select"].Value = false;
                        }
                        MessageBox.Show("Skripte wurden zum Album hinzugefügt.", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RefreshAllScriptIcons();
                        LoadAlbums();
                        picker.Close();
                    }
                    else MessageBox.Show("Bitte wählen Sie ein Album aus.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                };

                picker.ShowDialog();
            }
        }

        private void RefreshAllScriptIcons()
        {
            foreach (DataGridViewRow row in dgvScripts.Rows)
            {
                if (row.IsNewRow) continue;
                string path = row.Cells["Path"].Value.ToString();
                string fileName = row.Cells["FileName"].Value.ToString();
                bool inLib = DatabaseHelper.IsScriptInLibrary(path);

                if (fileName.StartsWith("📘 "))
                    fileName = fileName.Substring(3);

                if (inLib)
                {
                    fileName = "📘 " + fileName;
                }
                row.Cells["FileName"].Value = fileName;
            }
        }

        private void SetupRenameUI()
        {
            tabRename = new TabPage("Umbenennen");
            tabRename.BackColor = Color.White;
            tabControlMain.TabPages.Insert(1, tabRename);

            TableLayoutPanel tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            tabRename.Controls.Add(tlp);

            Panel pnlLeftRename = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            tlp.Controls.Add(pnlLeftRename, 0, 0);

            Label lblTitle = new Label { Text = "Zu umbenennende Skripte", Font = new Font("Segoe UI", 9.75F, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 };
            pnlLeftRename.Controls.Add(lblTitle);

            dgvRename = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 40,
                EditMode = DataGridViewEditMode.EditOnEnter
            };
            dgvRename.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 244, 248);
            dgvRename.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);

            var idCol = new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", ReadOnly = true, FillWeight = 15 };
            var currentNameCol = new DataGridViewTextBoxColumn { Name = "CurrentName", HeaderText = "Aktueller Dateiname", ReadOnly = true, FillWeight = 40 };
            var proposedNameCol = new DataGridViewTextBoxColumn { Name = "ProposedName", HeaderText = "Vorgeschlagener Dateiname", FillWeight = 45 };
            var pathCol = new DataGridViewTextBoxColumn { Name = "Path", Visible = false };

            dgvRename.Columns.AddRange(new DataGridViewColumn[] { idCol, currentNameCol, proposedNameCol, pathCol });

            pnlLeftRename.Controls.Add(dgvRename);
            dgvRename.BringToFront();

            Panel pnlRightRename = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            tlp.Controls.Add(pnlRightRename, 1, 0);

            btnApplyRename = new Button { Text = "💾 Umbenennen ausführen", Dock = DockStyle.Top, Height = 40, BackColor = Color.MediumSeaGreen, ForeColor = Color.White };
            pnlRightRename.Controls.Add(btnApplyRename);

            Panel spacer = new Panel { Dock = DockStyle.Top, Height = 10 };
            pnlRightRename.Controls.Add(spacer);

            btnSuggestNames = new Button { Text = "🤖 Namen durch AI vorschlagen", Dock = DockStyle.Top, Height = 40, BackColor = Color.SteelBlue, ForeColor = Color.White };
            pnlRightRename.Controls.Add(btnSuggestNames);

            btnRemoveFromRename = new Button { Text = "Ausgewählte entfernen", Dock = DockStyle.Bottom, Height = 35, BackColor = Color.LightGray };
            pnlRightRename.Controls.Add(btnRemoveFromRename);

            btnSuggestNames.Click += BtnSuggestNames_Click;
            btnApplyRename.Click += BtnApplyRename_Click;
            btnRemoveFromRename.Click += BtnRemoveFromRename_Click;
        }

        private void LoadMockModels()
        {
            cmbModels.Items.Clear();
            cmbModels.Items.AddRange(new string[] {
                "OpenAI: gpt-4o-mini",
                "OpenAI: gpt-4o",
                "Gemini: Gemini 3.5 Flash-Lite",
                "Gemini: Gemini 3.5 Flash",
                "Gemini: Gemini 3.1 Pro"
            });

            cmbModels.SelectedIndex = 2;
        }

        private void BtnAddSource_Click(object sender, EventArgs e)
        {
            try
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Bitte wählen Sie einen Ordner mit PowerShell-Skripten aus:";
                    fbd.ShowNewFolderButton = false;

                    if (fbd.ShowDialog(this) == DialogResult.OK)
                    {
                        if (!lstSources.Items.Contains(fbd.SelectedPath))
                        {
                            lstSources.Items.Add(fbd.SelectedPath);
                            DatabaseHelper.SaveSource(fbd.SelectedPath);
                            ScanFoldersForScripts();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Öffnen des Dialogs: " + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateSimilarityLabel()
        {
            if (dgvScripts.SelectedRows.Count > 0)
            {
                string id = dgvScripts.SelectedRows[0].Cells["Id"].Value?.ToString();
                var info = _simManager.Scripts.FirstOrDefault(s => s.Id == id);
                if (info != null)
                {
                    if (info.SimilarScriptsCount > 0)
                    {
                        lblSimilarityData.Text = $"{info.BestSimilarityScore:P0} ähnlich zu ID {info.BestMatchScriptId} | {info.SimilarityGroupId} ({info.SimilarScriptsCount + 1} Treffer)";
                    }
                    else
                    {
                        lblSimilarityData.Text = "Keine ähnlichen Skripte gefunden.";
                    }
                }
                else
                {
                    lblSimilarityData.Text = "-";
                }
            }
            else
            {
                lblSimilarityData.Text = "-";
            }
        }

        private void RefreshDuplicateStatuses()
        {
            foreach (DataGridViewRow row in dgvScripts.Rows)
            {
                if (row.IsNewRow) continue;
                string id = row.Cells["Id"].Value?.ToString();
                var info = _simManager.Scripts.FirstOrDefault(s => s.Id == id);

                if (info != null && info.SimilarScriptsCount > 0)
                {
                    row.Cells["Status"].Value = "📑";
                }
            }
        }

        private void BtnOverviewSimilar_Click(object sender, EventArgs e)
        {
            if (_simManager.Scripts.Count == 0)
            {
                MessageBox.Show("Bitte warten Sie, bis die Ähnlichkeitsanalyse abgeschlossen ist.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string model = cmbModels.Text;
            string keyToUse = "";

            if (model.StartsWith("Gemini")) keyToUse = apiKeyGemini;
            else if (model.StartsWith("OpenAI")) keyToUse = apiKeyOpenAI;

            var form = new SimilarityOverviewForm(_simManager.Scripts, keyToUse, model);
            form.ShowDialog();

            if (form.HasDeletedScripts)
            {
                ScanFoldersForScripts();
                LoadAlbums();
                _simManager.Scripts.Clear();
                lblSimilarityData.Text = "Neuprüfung erforderlich.";
                MessageBox.Show("Einige Skripte wurden gelöscht. Die Analyse-Ansicht und Bibliothek wurden aktualisiert.\nBitte führen Sie die Duplikat-Suche erneut aus, um den aktuellen Status zu sehen.", "Aktualisierung", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void BtnSearchDuplicates_Click(object sender, EventArgs e)
        {
            if (dgvScripts.Rows.Count == 0)
            {
                MessageBox.Show("Keine Skripte geladen.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            LogToProtocol("Starte Suche nach Duplikaten in der Bibliothek...");
            txtProtocol.Refresh();
            lblStatusInfo.Text = "Berechne Ähnlichkeiten...";
            Application.DoEvents();

            var scriptsToAnalyze = new List<ScriptSimilarityInfo>();
            foreach (DataGridViewRow row in dgvScripts.Rows)
            {
                if (row.IsNewRow) continue;

                string id = row.Cells["Id"].Value?.ToString();
                string fileName = row.Cells["FileName"].Value?.ToString();
                string path = row.Cells["Path"].Value?.ToString();

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(path))
                {
                    scriptsToAnalyze.Add(new ScriptSimilarityInfo
                    {
                        Id = id,
                        FileName = fileName,
                        Path = path
                    });
                }
            }

            await _simManager.ComputeSimilaritiesAsync(scriptsToAnalyze);
            lblStatusInfo.Text = $"Bereit. {dgvScripts.Rows.Count} Skripte gefunden. Ähnlichkeiten berechnet.";
            RefreshDuplicateStatuses();
            UpdateSimilarityLabel();
            LogToProtocol($"Suche abgeschlossen. {_simManager.Scripts.Count(s => s.SimilarScriptsCount > 0)} Duplikate gefunden.");
            MessageBox.Show("Die Suche nach Duplikaten wurde erfolgreich abgeschlossen!", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void ScanFoldersForScripts()
        {
            int removed = DatabaseHelper.CleanMissingScripts();
            if (removed > 0) LogToProtocol($"Datenbank bereinigt: {removed} ungültige Einträge gelöscht.");

            dgvScripts.Rows.Clear();

            LogToProtocol("Lade Skripte...");
            lblStatusInfo.Text = "Scripts werden geladen...";
            var scriptsToAnalyze = new List<ScriptSimilarityInfo>();

            var loadedScripts = new List<(string file, string fileName, string status, string id, bool isNew)>();

            // Pobieramy listę folderów z UI, ale wyszukiwanie plików robimy asynchronicznie
            var folders = lstSources.Items.Cast<string>().ToList();

            await Task.Run(() =>
            {
                foreach (string folderPath in folders)
                {
                    if (Directory.Exists(folderPath))
                    {
                        try
                        {
                            string[] psFiles = Directory.GetFiles(folderPath, "*.ps1", SearchOption.AllDirectories);

                            foreach (string file in psFiles)
                            {
                                string fileName = Path.GetFileName(file);
                                var dbInfo = DatabaseHelper.GetOrInitializeScriptInfo(file, fileName);
                                string id = dbInfo.ScriptId.ToString("D4");
                                lock (loadedScripts)
                                {
                                    loadedScripts.Add((file, fileName, dbInfo.Status, id, dbInfo.IsNew));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogToProtocol($"Fehler beim Scannen von {folderPath}: {ex.Message}");
                        }
                    }
                }
            });

            var sortedScripts = loadedScripts
                .OrderByDescending(s => s.isNew || s.status == "⏳")
                .ThenBy(s => s.id)
                .ToList();

            foreach (var s in sortedScripts)
            {
                int rowIndex = dgvScripts.Rows.Add(false, s.status, s.id, s.fileName, s.file);
                if (s.isNew || s.status == "⏳")
                {
                    dgvScripts.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(230, 242, 255);
                }

                scriptsToAnalyze.Add(new ScriptSimilarityInfo
                {
                    Id = s.id,
                    FileName = s.fileName,
                    Path = s.file
                });
            }

            lblStatusInfo.Text = $"Bereit. {dgvScripts.Rows.Count} Skripte gefunden. Berechne Ähnlichkeiten...";
            LogToProtocol($"Berechne Ähnlichkeiten für {dgvScripts.Rows.Count} Skripte..."); // POPRAWIONO JĘZYK
            await _simManager.ComputeSimilaritiesAsync(scriptsToAnalyze);
            lblStatusInfo.Text = $"Bereit. {dgvScripts.Rows.Count} Skripte gefunden. Ähnlichkeiten berechnet.";
            LogToProtocol($"Ähnlichkeitsberechnung abgeschlossen. {_simManager.Scripts.Count(s => s.SimilarScriptsCount > 0)} Duplikate gefunden."); // POPRAWIONO JĘZYK
            RefreshDuplicateStatuses();
            UpdateSimilarityLabel();
            RefreshAllScriptIcons();
        }

        private async void BtnAnalyzeGlobal_Click(object sender, EventArgs e)
        {
            string model = cmbModels.Text;
            string keyToUse = "";

            if (model.StartsWith("Gemini")) keyToUse = apiKeyGemini;
            else if (model.StartsWith("OpenAI")) keyToUse = apiKeyOpenAI;
            else if (model.StartsWith("Claude")) keyToUse = apiKeyClaude; // DODANO Claude

            if (string.IsNullOrEmpty(keyToUse))
            {
                MessageBox.Show($"Bitte konfigurieren Sie zuerst den API-Key für {model.Split(':')[0]}!", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var rowsToAnalyze = new List<DataGridViewRow>();
            bool isManualSelection = false;
            foreach (DataGridViewRow row in dgvScripts.Rows)
            {
                if (Convert.ToBoolean(row.Cells["Select"].Value))
                {
                    rowsToAnalyze.Add(row);
                    isManualSelection = true;
                }
            }

            if (rowsToAnalyze.Count == 0)
            {
                string inputPin = Microsoft.VisualBasic.Interaction.InputBox(
                    "Sie haben keine einzelnen Skripte ausgewählt. Um ALLE Skripte zu analysieren, geben Sie den PIN (8203) ein:", "Sicherheitsüberprüfung", "");

                if (inputPin == "8203")
                {
                    foreach (DataGridViewRow row in dgvScripts.Rows) rowsToAnalyze.Add(row);
                }
                else
                {
                    MessageBox.Show("Falscher PIN oder abgebrochen.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            LogToProtocol($"Starte Massenanalyse für {rowsToAnalyze.Count} Skripte..."); // POPRAWIONO JĘZYK
            btnAnalyze.Enabled = false;

            foreach (DataGridViewRow row in rowsToAnalyze)
            {
                string path = row.Cells["Path"].Value.ToString();
                string fileName = row.Cells["FileName"].Value.ToString();

                string currentStatus = row.Cells["Status"].Value.ToString();
                if (isManualSelection)
                {
                    if (currentStatus == "🟢") continue;
                }
                else
                {
                    if (currentStatus == "🟢" || currentStatus == "📑") continue;
                }

                LogToProtocol($"Analysiere: {fileName}...");
                row.Cells["Status"].Value = "⏳";

                try
                {
                    string scriptContent = File.ReadAllText(path);
                    string aiResponse = await AIEngine.AnalyzeScriptAsync(scriptContent, keyToUse, model);

                    DatabaseHelper.SaveScript(path, fileName, "🟢", aiResponse);
                    row.Cells["Status"].Value = "🟢";
                    row.Cells["Select"].Value = false;
                    LogToProtocol($"Fertig: {fileName}");
                }
                catch (Exception ex)
                {
                    row.Cells["Status"].Value = "🔴";
                    LogToProtocol($"Fehler bei {fileName}: {ex.Message}");
                }

                await Task.Delay(500);
            }

            btnAnalyze.Enabled = true;
            LogToProtocol("Massenanalyse beendet.");
            MessageBox.Show("Die Analyse wurde erfolgreich beendet!", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information); // POPRAWIONO JĘZYK
        }

        private void DgvScripts_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                dgvScripts.ClearSelection();
                dgvScripts.Rows[e.RowIndex].Selected = true;
                gridContextMenu.Show(Cursor.Position);
            }
        }

        private async void ContextMenu_AnalyzeSingle(object sender, EventArgs e)
        {
            if (dgvScripts.SelectedRows.Count > 0)
            {
                DataGridViewRow row = dgvScripts.SelectedRows[0];
                string path = row.Cells["Path"].Value.ToString();
                string fileName = row.Cells["FileName"].Value.ToString();

                string model = cmbModels.Text;
                string keyToUse = "";

                if (model.StartsWith("Gemini")) keyToUse = apiKeyGemini;
                else if (model.StartsWith("OpenAI")) keyToUse = apiKeyOpenAI;
                else if (model.StartsWith("Claude")) keyToUse = apiKeyClaude;

                if (string.IsNullOrEmpty(keyToUse))
                {
                    MessageBox.Show($"Bitte konfigurieren Sie zuerst den API-Key dla {model.Split(':')[0]}!", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!File.Exists(path)) return;

                LogToProtocol($"Starte Analyse dla: {fileName} z {model}...");
                row.Cells["Status"].Value = "⏳";

                try
                {
                    string scriptContent = File.ReadAllText(path);
                    string aiResponse = await AIEngine.AnalyzeScriptAsync(scriptContent, keyToUse, model);

                    DatabaseHelper.SaveScript(path, fileName, "🟢", aiResponse);

                    row.Cells["Status"].Value = "🟢";
                    txtDescriptionRight.Text = aiResponse;
                    LogToProtocol($"Analiza dla {fileName} zakończona sukcesem.");
                }
                catch (Exception ex)
                {
                    row.Cells["Status"].Value = "🔴";
                    LogToProtocol($"Fehler bei {fileName}: {ex.Message}");
                }
            }
        }

        private void ContextMenu_EditDescription(object sender, EventArgs e)
        {
            if (dgvScripts.SelectedRows.Count > 0)
            {
                var row = dgvScripts.SelectedRows[0];
                string path = row.Cells["Path"].Value.ToString();
                string fileName = row.Cells["FileName"].Value.ToString();
                string currentStatus = row.Cells["Status"].Value.ToString();
                string currentDesc = row.Cells["Remarks"].Value?.ToString() ?? "";

                Form editForm = new Form()
                {
                    Width = 600,
                    Height = 400,
                    Text = $"Beschreibung bearbeiten: {fileName}",
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false
                };

                TextBox txtDesc = new TextBox()
                {
                    Left = 20,
                    Top = 20,
                    Width = 540,
                    Height = 280,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    Text = currentDesc,
                    Font = new Font("Segoe UI", 10)
                };

                Button btnSave = new Button()
                {
                    Text = "Speichern",
                    Left = 460,
                    Top = 315,
                    Width = 100,
                    Height = 35,
                    BackColor = Color.SteelBlue,
                    ForeColor = Color.White
                };

                btnSave.Click += (s, args) =>
                {
                    DatabaseHelper.SaveScript(path, fileName, currentStatus, txtDesc.Text);
                    row.Cells["Remarks"].Value = txtDesc.Text;
                    editForm.Close();
                };

                editForm.Controls.Add(txtDesc);
                editForm.Controls.Add(btnSave);
                editForm.ShowDialog();
            }
        }

        private void ContextMenu_OpenScript(object sender, EventArgs e)
        {
            if (dgvScripts.SelectedRows.Count > 0)
            {
                string path = dgvScripts.SelectedRows[0].Cells["Path"].Value.ToString();
                if (File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo("powershell_ise.exe", $"\"{path}\"") { UseShellExecute = true });
                }
            }
        }

        private void ContextMenu_CopyPath(object sender, EventArgs e)
        {
            if (dgvScripts.SelectedRows.Count > 0)
            {
                string path = dgvScripts.SelectedRows[0].Cells["Path"].Value?.ToString();
                if (!string.IsNullOrEmpty(path))
                {
                    Clipboard.SetText(path);
                    lblStatusInfo.Text = "Pfad in die Zwischenablage kopiert!";
                }
            }
        }

        private void ContextMenu_ShowSimilar(object sender, EventArgs e)
        {
            if (_simManager.Scripts.Count == 0)
            {
                MessageBox.Show("Ähnlichkeitsanalyse ist noch nicht abgeschlossen.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (dgvScripts.SelectedRows.Count > 0)
            {
                string id = dgvScripts.SelectedRows[0].Cells["Id"].Value?.ToString();
                var scriptData = _simManager.Scripts.FirstOrDefault(s => s.Id == id);
                if (scriptData != null && !string.IsNullOrEmpty(scriptData.SimilarityGroupId))
                {
                    string model = cmbModels.Text;
                    string keyToUse = "";

                    if (model.StartsWith("Gemini")) keyToUse = apiKeyGemini;
                    else if (model.StartsWith("OpenAI")) keyToUse = apiKeyOpenAI;

                    var form = new SimilarityOverviewForm(_simManager.Scripts.Where(s => s.SimilarityGroupId == scriptData.SimilarityGroupId).ToList(), keyToUse, model);
                    form.Text = $"Ähnliche Skripte für Gruppe {scriptData.SimilarityGroupId}";
                    form.ShowDialog();

                    if (form.HasDeletedScripts)
                    {
                        ScanFoldersForScripts();
                        LoadAlbums();
                        _simManager.Scripts.Clear();
                        lblSimilarityData.Text = "Neuprüfung erforderlich.";
                        MessageBox.Show("Einige Skripte wurden gelöscht. Die Analyse-Ansicht und Bibliothek wurden aktualisiert.\nBitte führen Sie die Duplikat-Suche erneut aus, um den aktuellen Status zu sehen.", "Aktualisierung", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Keine ähnlichen Skripte für dieses Skript gefunden.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ContextMenu_CompareSimilar(object sender, EventArgs e)
        {
            if (_simManager.Scripts.Count == 0) return;

            if (dgvScripts.SelectedRows.Count > 0)
            {
                string id = dgvScripts.SelectedRows[0].Cells["Id"].Value?.ToString();
                var scriptData = _simManager.Scripts.FirstOrDefault(s => s.Id == id);

                if (scriptData != null && !string.IsNullOrEmpty(scriptData.BestMatchScriptId))
                {
                    var matchData = _simManager.Scripts.FirstOrDefault(s => s.Id == scriptData.BestMatchScriptId);
                    if (matchData != null)
                    {
                        var diff = new DiffForm(scriptData.Id, scriptData.FileName, scriptData.Content, matchData.Id, matchData.FileName, matchData.Content, scriptData.BestSimilarityScore);
                        diff.ShowDialog();
                    }
                }
                else
                {
                    MessageBox.Show("Dieses Skript hat keine ausreichende Ähnlichkeit mit anderen loaded Skripten.", "Vergleich", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            string filterText = txtSearch.Text.ToLower();

            dgvScripts.SuspendLayout();
            dgvScripts.CurrentCell = null;

            CurrencyManager currencyManager = null;
            if (dgvScripts.DataSource != null)
            {
                currencyManager = (CurrencyManager)BindingContext[dgvScripts.DataSource];
                currencyManager?.SuspendBinding();
            }

            foreach (DataGridViewRow row in dgvScripts.Rows)
            {
                if (row.IsNewRow) continue;

                if (string.IsNullOrWhiteSpace(filterText))
                {
                    if (!row.Visible) row.Visible = true;
                    continue;
                }

                bool match = false;
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value != null && cell.Value.ToString().ToLower().Contains(filterText))
                    {
                        match = true;
                        break;
                    }
                }

                if (row.Visible != match)
                {
                    row.Visible = match;
                }
            }

            currencyManager?.ResumeBinding();
            dgvScripts.ResumeLayout();
        }

        private void StyleSecondaryButton(Button btn)
        {
            btn.BackColor = Color.FromArgb(240, 244, 248);
            btn.ForeColor = Color.Black;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
        }

        private void StylePrimaryButton(Button btn)
        {
            btn.BackColor = Color.SteelBlue;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
        }

        private void DgvScripts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvScripts.SelectedRows.Count > 0)
            {
                var id = dgvScripts.SelectedRows[0].Cells["Id"].Value?.ToString();
                var path = dgvScripts.SelectedRows[0].Cells["Path"].Value?.ToString();
                if (!string.IsNullOrEmpty(path))
                {
                    var info = DatabaseHelper.GetScriptInfo(path);
                    txtDescriptionRight.Text = info.Description;
                }
            }
        }

        private void BtnEditDesc_Click(object sender, EventArgs e)
        {
            txtDescriptionRight.ReadOnly = !txtDescriptionRight.ReadOnly;
            txtDescriptionRight.BackColor = txtDescriptionRight.ReadOnly ? Color.FromArgb(248, 249, 250) : Color.White;
        }

        private void BtnSaveDesc_Click(object sender, EventArgs e)
        {
            if (dgvScripts.SelectedRows.Count > 0)
            {
                var path = dgvScripts.SelectedRows[0].Cells["Path"].Value?.ToString();
                var fileName = dgvScripts.SelectedRows[0].Cells["FileName"].Value?.ToString();
                var status = dgvScripts.SelectedRows[0].Cells["Status"].Value?.ToString();

                if (!string.IsNullOrEmpty(path))
                {
                    DatabaseHelper.SaveScript(path, fileName, status, txtDescriptionRight.Text);
                    txtDescriptionRight.ReadOnly = true;
                    txtDescriptionRight.BackColor = Color.FromArgb(248, 249, 250);
                    MessageBox.Show("Beschreibung gespeichert.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private async void BtnApiKey_Click(object sender, EventArgs e)
        {
            using (Form f = new Form())
            {
                f.Text = "Ustawienia i Modele API";
                f.Width = 750;
                f.Height = 550;
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false;

                // Główny kontener TabControl
                TabControl tcSettings = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.75F) };
                f.Controls.Add(tcSettings);

                // --- ZAKŁADKA 1: KLUCZE I BAZA ---
                TabPage tpKeys = new TabPage("Klucze API i Baza danych");
                tpKeys.BackColor = Color.White;
                tcSettings.TabPages.Add(tpKeys);

                Label lblDb = new Label() { Text = "Datenbank:", Left = 20, Top = 25, Width = 120 };
                TextBox txtDb = new TextBox() { Left = 150, Top = 22, Width = 380, Text = DatabaseHelper.DbFileName, ReadOnly = true };
                Button btnOpenDb = new Button() { Text = "📂", Left = 540, Top = 20, Width = 40 };
                Button btnSaveDb = new Button() { Text = "💾", Left = 590, Top = 20, Width = 40 };

                bool dbPathChanged = false;
                bool copyDatabase = false;

                btnOpenDb.Click += (s, args) => {
                    using (OpenFileDialog ofd = new OpenFileDialog { Filter = "SQLite Datenbank|*.sqlite|Alle Dateien|*.*" })
                    {
                        if (ofd.ShowDialog() == DialogResult.OK) { txtDb.Text = ofd.FileName; dbPathChanged = true; copyDatabase = false; }
                    }
                };
                btnSaveDb.Click += (s, args) => {
                    using (SaveFileDialog sfd = new SaveFileDialog { Filter = "SQLite Datenbank|*.sqlite", FileName = "scripts_db.sqlite" })
                    {
                        if (sfd.ShowDialog() == DialogResult.OK) { txtDb.Text = sfd.FileName; dbPathChanged = true; copyDatabase = true; }
                    }
                };

                // Dostawcy API i ich klucze + Przyciski Test połączenia
                Label lblOpenAI = new Label() { Text = "OpenAI API-Key:", Left = 20, Top = 75, Width = 120 };
                TextBox txtOpenAI = new TextBox() { Left = 150, Top = 72, Width = 380, Text = apiKeyOpenAI, PasswordChar = '*' };
                Button btnTestOpenAI = new Button() { Text = "Test połączenia", Left = 540, Top = 70, Width = 120, Height = 28 };

                Label lblGemini = new Label() { Text = "Gemini API-Key:", Left = 20, Top = 125, Width = 120 };
                TextBox txtGemini = new TextBox() { Left = 150, Top = 122, Width = 380, Text = apiKeyGemini, PasswordChar = '*' };
                Button btnTestGemini = new Button() { Text = "Test połączenia", Left = 540, Top = 120, Width = 120, Height = 28 };

                Label lblClaude = new Label() { Text = "Claude API-Key:", Left = 20, Top = 175, Width = 120 };
                TextBox txtClaude = new TextBox() { Left = 150, Top = 172, Width = 380, Text = apiKeyClaude, PasswordChar = '*' };
                Button btnTestClaude = new Button() { Text = "Test połączenia", Left = 540, Top = 170, Width = 120, Height = 28 };

                // Funkcja pomocnicza do testowania połączenia (wykorzystuje istniejące metody Fetch)
                async Task RunConnectionTest(string provider, string key, Func<string, Task<List<string>>> fetchMethod)
                {
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        MessageBox.Show("Wprowadź klucz API przed testem!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    Cursor.Current = Cursors.WaitCursor;
                    try
                    {
                        var res = await fetchMethod(key.Trim());
                        if (res != null && res.Count > 0)
                            MessageBox.Show($"Połączenie z {provider} udane!\nAutoryzacja poprawna. Pobrano dostępną listę modeli.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show($"Autoryzacja {provider} powiodła się, ale API nie zwróciło modeli.", "Uwaga", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd połączenia z {provider}:\n{ex.Message}", "Błąd połączenia", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        Cursor.Current = Cursors.Default;
                    }
                }

                btnTestOpenAI.Click += async (s, args) => await RunConnectionTest("OpenAI", txtOpenAI.Text, FetchOpenAIModelsAsync);
                btnTestGemini.Click += async (s, args) => await RunConnectionTest("Gemini", txtGemini.Text, FetchGeminiModelsAsync);
                btnTestClaude.Click += async (s, args) => await RunConnectionTest("Claude", txtClaude.Text, FetchClaudeModelsAsync);

                tpKeys.Controls.AddRange(new Control[] {
            lblDb, txtDb, btnOpenDb, btnSaveDb,
            lblOpenAI, txtOpenAI, btnTestOpenAI,
            lblGemini, txtGemini, btnTestGemini,
            lblClaude, txtClaude, btnTestClaude
        });

                // --- ZAKŁADKA 2: ULUBIONE MODELE ---
                TabPage tpModels = new TabPage("Ulubione modele (Lista dropdown)");
                tpModels.BackColor = Color.White;
                tcSettings.TabPages.Add(tpModels);

                Panel pnlModelsTop = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(245, 245, 245) };
                Button btnFetchModels = new Button { Text = "🔄 Odśwież i pobierz wszystkie dostępne modele z API", Left = 15, Top = 10, Width = 400, Height = 30, BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                btnFetchModels.FlatAppearance.BorderSize = 0;
                pnlModelsTop.Controls.Add(btnFetchModels);
                tpModels.Controls.Add(pnlModelsTop);

                // Tabela modeli (Ulubione + Nazwa)
                DataGridView dgvModels = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    AllowUserToAddRows = false,
                    RowHeadersVisible = false,
                    BackgroundColor = Color.White,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                };

                var colFav = new DataGridViewCheckBoxColumn { Name = "IsFav", HeaderText = "★ Ulubiony", FillWeight = 25 };
                var colName = new DataGridViewTextBoxColumn { Name = "ModelName", HeaderText = "Nazwa modelu API", ReadOnly = true, FillWeight = 75 };
                dgvModels.Columns.AddRange(new DataGridViewColumn[] { colFav, colName });
                tpModels.Controls.Add(dgvModels);
                dgvModels.BringToFront();

                // Ładowanie aktualnych zapisanych ulubionych do tabeli dgvModels jako stan startowy
                var savedFavorites = DatabaseHelper.GetFavoriteModels();
                foreach (var fav in savedFavorites)
                {
                    dgvModels.Rows.Add(true, fav);
                }

                // Akcja pobierania i scalania modeli ze wszystkich aktywnych kluczy podanych w polach tekstowych
                btnFetchModels.Click += async (s, args) => {
                    btnFetchModels.Enabled = false;
                    Cursor.Current = Cursors.WaitCursor;

                    var downloadedModels = new List<string>();
                    if (!string.IsNullOrWhiteSpace(txtOpenAI.Text)) downloadedModels.AddRange(await FetchOpenAIModelsAsync(txtOpenAI.Text.Trim()));
                    if (!string.IsNullOrWhiteSpace(txtGemini.Text)) downloadedModels.AddRange(await FetchGeminiModelsAsync(txtGemini.Text.Trim()));
                    if (!string.IsNullOrWhiteSpace(txtClaude.Text)) downloadedModels.AddRange(await FetchClaudeModelsAsync(txtClaude.Text.Trim()));

                    if (downloadedModels.Count == 0)
                    {
                        MessageBox.Show("Nie pobrano żadnych modeli. Upewnij się, że wprowadziłeś poprawne klucze API.", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        btnFetchModels.Enabled = true;
                        Cursor.Current = Cursors.Default;
                        return;
                    }

                    // Zapamiętaj co było zaznaczone przed odświeżeniem
                    var currentlyChecked = new HashSet<string>();
                    foreach (DataGridViewRow r in dgvModels.Rows)
                    {
                        if (Convert.ToBoolean(r.Cells["IsFav"].Value)) currentlyChecked.Add(r.Cells["ModelName"].Value.ToString());
                    }

                    dgvModels.Rows.Clear();
                    foreach (var m in downloadedModels.Distinct())
                    {
                        bool wasFav = currentlyChecked.Contains(m) || savedFavorites.Contains(m);
                        dgvModels.Rows.Add(wasFav, m);
                    }

                    MessageBox.Show($"Pomyślnie załadowano {downloadedModels.Distinct().Count()} unikalnych modeli z aktywnych API.\nZaznacz gwiazdką te, które mają być widoczne w oknie głównym.", "Pobieranie zakończone", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    btnFetchModels.Enabled = true;
                    Cursor.Current = Cursors.Default;
                };

                // --- DOLNY PANEL Z PRZYCISKAMI ZAPISU ---
                Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.WhiteSmoke };
                Button btnOk = new Button() { Text = "Zapisz", Left = 520, Top = 12, Width = 90, Height = 35, DialogResult = DialogResult.OK };
                StylePrimaryButton(btnOk);
                Button btnCancelSet = new Button() { Text = "Anuluj", Left = 620, Top = 12, Width = 90, Height = 35, DialogResult = DialogResult.Cancel };
                StyleSecondaryButton(btnCancelSet);

                pnlBottom.Controls.AddRange(new Control[] { btnOk, btnCancelSet });
                f.Controls.Add(pnlBottom);
                pnlBottom.BringToFront();

                f.AcceptButton = btnOk;
                f.CancelButton = btnCancelSet;

                // --- PROCES ZAPISYWANIA USTAWIEŃ ---
                if (f.ShowDialog() == DialogResult.OK)
                {
                    // 1. Zmiana ścieżki bazy danych (jeśli zmodyfikowano)
                    string oldDbPath = DatabaseHelper.DbFileName;
                    if (dbPathChanged && oldDbPath != txtDb.Text)
                    {
                        if (copyDatabase)
                        {
                            try { File.Copy(oldDbPath, txtDb.Text, true); }
                            catch (Exception ex) { MessageBox.Show("Fehler beim Kopieren der Datenbank: " + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                        }
                        DatabaseHelper.ChangeDatabasePath(txtDb.Text);
                        lstSources.Items.Clear(); dgvScripts.Rows.Clear(); _simManager.Scripts.Clear(); lblSimilarityData.Text = "-";
                        LoadSavedSources(); LoadAlbums();
                    }

                    // 2. Zapis kluczy do bazy danych i zmiennych lokalnych
                    apiKeyOpenAI = txtOpenAI.Text;
                    apiKeyGemini = txtGemini.Text;
                    apiKeyClaude = txtClaude.Text;
                    DatabaseHelper.SaveSetting("OpenAI", apiKeyOpenAI);
                    DatabaseHelper.SaveSetting("Gemini", apiKeyGemini);
                    DatabaseHelper.SaveSetting("Claude", apiKeyClaude);

                    // Aktualizacja etykiety statusu na głównym oknie
                    if (!string.IsNullOrWhiteSpace(apiKeyOpenAI) || !string.IsNullOrWhiteSpace(apiKeyGemini) || !string.IsNullOrWhiteSpace(apiKeyClaude))
                    {
                        lblApiKeyStatus.Text = "Status: Schlüssel gespeichert"; lblApiKeyStatus.ForeColor = Color.Green;
                    }
                    else
                    {
                        lblApiKeyStatus.Text = "Status: Kein Schlüssel vorhanden"; lblApiKeyStatus.ForeColor = Color.Red;
                    }

                    // 3. Zapis wybranych ULUBIONYCH modeli do bazy danych
                    var selectedFavorites = new List<string>();
                    foreach (DataGridViewRow row in dgvModels.Rows)
                    {
                        if (Convert.ToBoolean(row.Cells["IsFav"].Value))
                        {
                            selectedFavorites.Add(row.Cells["ModelName"].Value.ToString());
                        }
                    }
                    DatabaseHelper.SaveFavoriteModels(selectedFavorites);

                    // 4. Przeładowanie głównego ComboBoxa na podstawie nowej listy ulubionych
                    await LoadActualModelsAsync();
                }
            }
        }

        private async Task<List<string>> FetchOpenAIModelsAsync(string apiKey)
        {
            var models = new List<string>();
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var response = await client.GetAsync("https://api.openai.com/v1/models");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataProp))
                    {
                        foreach (var model in dataProp.EnumerateArray())
                        {
                            string id = model.GetProperty("id").GetString();
                            if (id.Contains("gpt") || id.Contains("o1") || id.Contains("o3"))
                            {
                                models.Add($"OpenAI: {id}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToProtocol($"Fehler beim Laden der OpenAI-Modelle: {ex.Message}");
            }
            return models;
        }

        private async Task<List<string>> FetchGeminiModelsAsync(string apiKey)
        {
            var models = new List<string>();
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync($"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("models", out var modelsProp))
                    {
                        foreach (var model in modelsProp.EnumerateArray())
                        {
                            string name = model.GetProperty("name").GetString();
                            string cleanName = name.Replace("models/", "");

                            bool supportsText = false;
                            if (model.TryGetProperty("supportedGenerationMethods", out var methods))
                            {
                                foreach (var method in methods.EnumerateArray())
                                {
                                    if (method.GetString() == "generateContent")
                                    {
                                        supportsText = true;
                                        break;
                                    }
                                }
                            }

                            if (supportsText)
                            {
                                models.Add($"Gemini: {cleanName}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToProtocol($"Fehler beim Laden der Gemini-Modelle: {ex.Message}");
            }
            return models;
        }

        private async Task<List<string>> FetchClaudeModelsAsync(string apiKey)
        {
            var models = new List<string>();
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                var response = await client.GetAsync("https://api.anthropic.com/v1/models");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataProp))
                    {
                        foreach (var model in dataProp.EnumerateArray())
                        {
                            string id = model.GetProperty("id").GetString();
                            models.Add($"Claude: {id}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToProtocol($"Fehler beim Laden der Claude-Modelle: {ex.Message}");
            }
            return models;
        }

        private async Task LoadActualModelsAsync()
        {
            LogToProtocol("Lade konfigurierte Favoriten-Modelle...");

            // Pobranie ulubionych z bazy danych
            var favoriteModels = DatabaseHelper.GetFavoriteModels();

            // Jeśli użytkownik nie wybrał jeszcze żadnych ulubionych, załaduj listę podstawową (fallback)
            if (favoriteModels.Count == 0)
            {
                favoriteModels.AddRange(new string[] {
            "OpenAI: gpt-4o-mini",
            "OpenAI: gpt-4o",
            "Gemini: gemini-1.5-flash",
            "Gemini: gemini-1.5-pro",
            "Claude: claude-3-5-sonnet-latest"
        });
            }

            cmbModels.Items.Clear();
            foreach (var model in favoriteModels)
            {
                cmbModels.Items.Add(model);
            }

            if (cmbModels.Items.Count > 0)
            {
                cmbModels.SelectedIndex = 0;
            }
            LogToProtocol("Modellliste wurde mit Favoriten aktualisiert.");
        }

        private void DgvScripts_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvScripts.Columns["Status"].Index)
            {
                e.PaintBackground(e.CellBounds, true);

                string status = e.Value?.ToString();
                Image img = null;
                if (status == "🟢") img = PowerShellManager.Resources.OK;
                else if (status == "🔴") img = PowerShellManager.Resources.Fehler;
                else if (status == "📑") img = PowerShellManager.Resources.duplikat;
                else if (status == "⏳") img = PowerShellManager.Resources.ausstehend;

                if (img != null)
                {
                    int size = 20;
                    int x = e.CellBounds.X + (e.CellBounds.Width - size) / 2;
                    int y = e.CellBounds.Y + (e.CellBounds.Height - size) / 2;
                    e.Graphics.DrawImage(img, new Rectangle(x, y, size, size));
                }

                e.Handled = true;
            }
        }

        private void DgvLibraryScripts_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvLibraryScripts.Columns["Value"].Index)
            {
                string propName = dgvLibraryScripts.Rows[e.RowIndex].Cells["Property"].Value?.ToString();
                if (propName == "Status")
                {
                    e.PaintBackground(e.CellBounds, true);
                    string status = e.Value?.ToString();
                    Image img = null;

                    if (status == "🟢") img = PowerShellManager.Resources.OK;
                    else if (status == "🔴") img = PowerShellManager.Resources.Fehler;
                    else if (status == "📑") img = PowerShellManager.Resources.duplikat;
                    else if (status == "⏳") img = PowerShellManager.Resources.ausstehend;

                    if (img != null)
                    {
                        int size = 20;
                        int x = e.CellBounds.X + 2;
                        int y = e.CellBounds.Y + Math.Max(0, (e.CellBounds.Height - size) / 2);
                        e.Graphics.DrawImage(img, new Rectangle(x, y, size, size));
                    }
                    e.Handled = true;
                }
            }
        }

        private void dgvScripts_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0 && e.RowIndex >= 0)
            {
                dgvScripts.EndEdit();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }
    }
}