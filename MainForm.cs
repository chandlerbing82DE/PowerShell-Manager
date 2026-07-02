using System.Diagnostics;

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
        private string apiKeyClaude = ""; // Przygotowane na przyszłość

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
            // Wpisuje tekst do dolnego okna protokołu z aktualną godziną
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
            // NOWOŚĆ: Ładujemy zapisane foldery zaraz po starcie!
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

        // Nowa metoda do ładowania przy starcie
        private void LoadSavedSources()
        {
            var savedSources = DatabaseHelper.GetSources();
            foreach (var source in savedSources)
            {
                if (System.IO.Directory.Exists(source)) // Sprawdzamy czy folder nadal istnieje na dysku
                {
                    lstSources.Items.Add(source);
                }
            }

            // Jeśli załadowaliśmy jakieś foldery, od razu skanujemy skrypty
            if (lstSources.Items.Count > 0)
            {
                ScanFoldersForScripts();
            }
        }

        private void SetupCustomUI()
        {
            // --- 1. NAPRAWA NAZW KOLUMN I CZCIONKI TABELI ---
            // Wymuszamy Segoe UI dla całej tabeli
            dgvScripts.DefaultCellStyle.Font = new Font("Segoe UI", 9.75F);

            if (dgvScripts.Columns.Count >= 4 && !dgvScripts.Columns.Contains("Path"))
            {
                dgvScripts.Columns[0].Name = "Status";
                dgvScripts.Columns[1].Name = "Id";
                dgvScripts.Columns[2].Name = "FileName";
                dgvScripts.Columns[3].Name = "Path";
            }

            // Podłączamy zdarzenie do malowania ikon
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

            // Zabezpieczenie reszty kolumn przed edycją (tylko CheckBox jest klikalny)
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

            // Usunięto "Beschreibung bearbeiten" - mamy to na stałe po prawej stronie!
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

            // --- 8. STATUS LEGENDE HINZUFÜGEN (Legenda na górze po prawej) ---
            Panel pnlTopRightInfo = new Panel
            {
                Width = 750,
                Height = 85,
                Location = new Point(pnlHeader.Width - 760, 5),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent
            };
            pnlHeader.Controls.Add(pnlTopRightInfo);

            // Definiujemy czcionkę raz, aby użyć jej w labelach
            Font boldFont = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            Font regularFont = new Font("Segoe UI", 9.75F);

            // Pierwszy wiersz: Legenda Statusów
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

            // Drugi wiersz: Przyciski i Ähnlichkeit (Podobieństwo)
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

            // Tymczasowe dla nienapisanych jeszcze funkcji:
            btnCancel.Click -= (s, e) => MessageBox.Show("Aktion abgebrochen.", "Info");
            btnCancel.Click += (s, e) => MessageBox.Show("Aktion abgebrochen.", "Info");

            btnApiKey.Click -= BtnApiKey_Click;
            btnApiKey.Click += BtnApiKey_Click;

            SetupLibraryUI();
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

            // Proporcja 40% (Albumy) do 60% (Spezifikationen)
            splitContainer.Resize += (s, e) =>
            {
                if (splitContainer.Width > 100)
                {
                    splitContainer.SplitterDistance = (int)(splitContainer.Width * 0.40);
                }
            };
            // Wymuś raz zanim kontrolka się pojawi, na domyślnej szerokości ekranu (1400)
            splitContainer.SplitterDistance = (int)(1400 * 0.40);

            tabBibliothek.Controls.Add(splitContainer);

            // Lewy panel (Drzewo)
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

            // Prawy panel (Skrypty)
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
            dgvLibraryScripts.Columns["Property"].FillWeight = 30; // 30% z dostepnej szerokoci panela (60%) 

            dgvLibraryScripts.Columns.Add("Value", "Wert");
            dgvLibraryScripts.Columns["Value"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvLibraryScripts.Columns["Value"].FillWeight = 70; // 70% detalu
            dgvLibraryScripts.Columns["Value"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            pnlGrid.Controls.Add(dgvLibraryScripts);
            dgvLibraryScripts.BringToFront();

            // Menu kontekstowe drzewa
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

            // Menu kontekstowe tabeli biblioteki (teraz w drzewie)
            libraryGridContextMenu = new ContextMenuStrip();
            libraryGridContextMenu.Items.Add("Skript öffnen", null, LibGridContext_OpenScript);
            libraryGridContextMenu.Items.Add(new ToolStripSeparator());
            libraryGridContextMenu.Items.Add("Skript umbenennen (virtuell)", null, LibGridContext_Rename);
            libraryGridContextMenu.Items.Add("Aus Album entfernen", null, LibGridContext_Remove);

            // Menu kontekstowe glownej tabeli (Analyse) 
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

            // Pierwszy przebieg: Tworzenie wszystkich węzłów
            foreach (System.Data.DataRow row in dt.Rows)
            {
                int id = Convert.ToInt32(row["Id"]);
                string name = row["Name"].ToString();
                var node = new TreeNode(name) { Tag = id };
                nodes[id] = node;
            }

            // Drugi przebieg: Budowanie drzewa
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

            // Trzeci przebieg: Skrypty do węzłów
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

        // --- TREEVIEW EVENTS ---

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
                        tvAlbums.ContextMenuStrip = tvContextMenu; // Menu albumu
                    }
                    else if (clickedNode.Tag is string)
                    {
                        tvAlbums.ContextMenuStrip = libraryGridContextMenu; // Menu skryptu
                    }
                }
                else
                {
                    tvAlbums.ContextMenuStrip = tvContextMenu;
                }
            }
            else
            {
                tvAlbums.ContextMenuStrip = null; // Brak menu przy innym kliknięciu
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
                    if (draggedNode.Tag is int draggedAlbumId) // Moving Album
                    {
                        int? targetId = null;
                        if (targetNode != null && targetNode.Tag is int tId)
                            targetId = tId;
                        else if (targetNode != null && targetNode.Tag is string)
                            targetId = targetNode.Parent?.Tag as int?; // If dropped on script, use parent album

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
                    else if (draggedNode.Tag is string scriptPath && draggedNode.Parent != null) // Moving Script
                    {
                        int oldAlbumId = (int)draggedNode.Parent.Tag;
                        int newAlbumId = -1;

                        if (targetNode != null && targetNode.Tag is int targetAId)
                            newAlbumId = targetAId;
                        else if (targetNode != null && targetNode.Tag is string && targetNode.Parent != null)
                            newAlbumId = (int)targetNode.Parent.Tag;

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

                    // Update grid if found
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
                        LoadAlbums(); // Refresh library
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
                if (MessageBox.Show("Möchten Sie dieses Album wirklich löschen? Alle Unteralben będą równieź usunięte. Es werden keine Skripte von der Festplatte gelöscht.", "Album löschen", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    int id = (int)tvAlbums.SelectedNode.Tag;
                    DatabaseHelper.DeleteAlbum(id);
                    tvAlbums.SelectedNode.Remove();
                    RefreshAllScriptIcons(); // Because some scripts might have lost their library assigned status
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
                dgvScripts.EndEdit(); // Force apply changes
            }
        }

        // --- GRID EVENTS AND DATA ---

        private void ContextMenu_AddToRename(object sender, EventArgs e)
        {
            int addedCount = 0;

            // Collect rows to process: first check if any rows have been checked.
            var rowsToProcess = new List<DataGridViewRow>();
            foreach (DataGridViewRow row in dgvScripts.Rows)
            {
                if (Convert.ToBoolean(row.Cells["Select"].Value))
                {
                    rowsToProcess.Add(row);
                }
            }

            // Fallback: If no rows are checked, use standard multiselect
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
                    // Check if already in the rename grid
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
                var dbInfo = DatabaseHelper.GetScriptInfo(path); // Has Status, Description
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

                    // Cleanup and format string (ID_Name.ps1)
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
                ScanFoldersForScripts(); // Refresh analyze tab
                LoadAlbums(); // Refresh library
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
                // Quick album picker dialog
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

                            // Skrypt musi istnieć w tabeli Scripts, aby INNER JOIN zadziałał w bibliotece
                            string fileName = row.Cells["FileName"].Value?.ToString() ?? "Unknown";
                            if (fileName.StartsWith("📘 "))
                                fileName = fileName.Substring(3);

                            string status = row.Cells["Status"].Value?.ToString() ?? "⏳";
                            var dbInfo = DatabaseHelper.GetScriptInfo(path);
                            DatabaseHelper.SaveScript(path, fileName, status, dbInfo.Description);

                            DatabaseHelper.AddScriptToAlbum(path, targetAlbumId);
                            row.Cells["Select"].Value = false; // Odznacz checkbox po dodaniu
                        }
                        MessageBox.Show("Skripte wurden zum Album hinzugefügt.", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RefreshAllScriptIcons();
                        LoadAlbums(); // <-- odswiezamy cale drzewo zeby zaladowaly sie wezly skryptow
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

                // Clean current icon
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
                "Gemini: Gemini 2.5 Flash-Lite",
                "Gemini: Gemini 2.5 Flash",
                "Gemini: Gemini 2.5 Pro",
                "Gemini: Gemini 3 Flash Preview"
            });

            // Domyślnie ustawiamy pozycję na index 3 (Gemini 2.5 Flash) - najlepszy stosunek ceny do jakości do analizy kodu! 😎
            cmbModels.SelectedIndex = 2;
        }

        // --- OBSŁUGA PLIKÓW I ŹRÓDEŁ ---

        private void BtnAddSource_Click(object sender, EventArgs e)
        {
            try
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    // Tłumaczenie na niemiecki
                    fbd.Description = "Bitte wählen Sie einen Ordner mit PowerShell-Skripten aus:";
                    fbd.ShowNewFolderButton = false;

                    // 'this' wymusza, by okienko było ZAWSZE na wierzchu naszej aplikacji
                    if (fbd.ShowDialog(this) == DialogResult.OK)
                    {
                        if (!lstSources.Items.Contains(fbd.SelectedPath))
                        {
                            lstSources.Items.Add(fbd.SelectedPath);

                            // NOWOŚĆ: Zapisujemy ścieżkę do bazy SQLite
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
            txtProtocol.Refresh(); // <-- Dodano odświeżenie kontrolki, aby tekst od razu się ukazał
            lblStatusInfo.Text = "Berechne Ähnlichkeiten...";
            Application.DoEvents(); // Pozwala na zrenderowanie teksu z paska stanu przed wielką pętlą

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

            foreach (string folderPath in lstSources.Items)
            {
                if (Directory.Exists(folderPath))
                {
                    // Szukamy plików .ps1, w tym w podfolderach
                    string[] psFiles = Directory.GetFiles(folderPath, "*.ps1", SearchOption.AllDirectories);

                    foreach (string file in psFiles)
                    {
                        string fileName = Path.GetFileName(file);
                        var dbInfo = DatabaseHelper.GetOrInitializeScriptInfo(file, fileName);
                        string id = dbInfo.ScriptId.ToString("D4");
                        loadedScripts.Add((file, fileName, dbInfo.Status, id, dbInfo.IsNew));
                    }
                }
            }

            var sortedScripts = loadedScripts
                .OrderByDescending(s => s.isNew || s.status == "⏳")
                .ThenBy(s => s.id)
                .ToList();

            foreach (var s in sortedScripts)
            {
                int rowIndex = dgvScripts.Rows.Add(false, s.status, s.id, s.fileName, s.file);
                if (s.isNew || s.status == "⏳")
                {
                    dgvScripts.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(230, 242, 255); // Lekki niebieski
                }

                scriptsToAnalyze.Add(new ScriptSimilarityInfo
                {
                    Id = s.id,
                    FileName = s.fileName,
                    Path = s.file
                });
            }

            lblStatusInfo.Text = $"Bereit. {dgvScripts.Rows.Count} Skripte gefunden. Berechne Ähnlichkeiten...";
            LogToProtocol($"Berechne Ähnlichkeiten dla {dgvScripts.Rows.Count} Skryptów...");
            await _simManager.ComputeSimilaritiesAsync(scriptsToAnalyze);
            lblStatusInfo.Text = $"Bereit. {dgvScripts.Rows.Count} Skripte gefunden. Ähnlichkeiten berechnet.";
            LogToProtocol($"Ähnlichkeitsberechnung zakończona. {_simManager.Scripts.Count(s => s.SimilarScriptsCount > 0)} Duplikate znalezione.");
            RefreshDuplicateStatuses();
            UpdateSimilarityLabel();
            RefreshAllScriptIcons();
        }

        // --- GLOBALNA ANALIZA I KOD PIN ---

        private async void BtnAnalyzeGlobal_Click(object sender, EventArgs e)
        {
            // Wyciągamy model i dobieramy odpowiedni klucz
            string model = cmbModels.Text;
            string keyToUse = "";

            if (model.StartsWith("Gemini")) keyToUse = apiKeyGemini;
            else if (model.StartsWith("OpenAI")) keyToUse = apiKeyOpenAI;

            if (string.IsNullOrEmpty(keyToUse))
            {
                MessageBox.Show($"Bitte konfigurieren Sie zuerst den API-Key dla {model.Split(':')[0]}!", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Zbieramy skrypty, które są zaznaczone ptaszkiem (☑) 
            // Jeśli żaden nie jest zaznaczony, zapytamy, czy skanować całą bibliotekę
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
                // Globalny skan wszystkiego
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

            LogToProtocol($"Starte Massenalyse dla {rowsToAnalyze.Count} Skryptów...");
            btnAnalyze.Enabled = false; // Blokujemy przycisk na czas analizy

            foreach (DataGridViewRow row in rowsToAnalyze)
            {
                string path = row.Cells["Path"].Value.ToString();
                string fileName = row.Cells["FileName"].Value.ToString();

                // Sprawdzamy, czy ten plik nie ma już statusu "OK" (żeby nie tracić kasy z API)
                string currentStatus = row.Cells["Status"].Value.ToString();
                if (isManualSelection)
                {
                    if (currentStatus == "🟢") continue; // Pozwalamy na analizę duplikatów, jeśli ręcznie zaznaczone
                }
                else
                {
                    if (currentStatus == "🟢" || currentStatus == "📑") continue; // Przy skanie całego folderu ignorujemy zarówno 🟢 jak i duplikaty 📑
                }

                LogToProtocol($"Analysiere: {fileName}...");
                row.Cells["Status"].Value = "⏳";

                try
                {
                    string scriptContent = File.ReadAllText(path);
                    string aiResponse = await AIEngine.AnalyzeScriptAsync(scriptContent, keyToUse, model);

                    DatabaseHelper.SaveScript(path, fileName, "🟢", aiResponse);
                    row.Cells["Status"].Value = "🟢";
                    row.Cells["Select"].Value = false; // Odznaczamy ptaszka po sukcesie
                    LogToProtocol($"Fertig: {fileName}");
                }
                catch (Exception ex)
                {
                    row.Cells["Status"].Value = "🔴";
                    LogToProtocol($"Fehler bei {fileName}: {ex.Message}");
                }

                // Małe opóźnienie, żeby nie zalać API żądaniami w ułamku sekundy
                await Task.Delay(500);
            }

            btnAnalyze.Enabled = true;
            LogToProtocol("Massenalyse beendet.");
            MessageBox.Show("Die Analyse została zakończona!", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // --- MENU POD PRAWYM PRZYCISKIEM MYSZY ---

        private void DgvScripts_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            // Reakcja tylko na prawy przycisk myszy i poprawne wiersze (ignoruj nagłówki)
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                // Zaznacz kliknięty wiersz
                dgvScripts.ClearSelection();
                dgvScripts.Rows[e.RowIndex].Selected = true;

                // Pokaż menu dokładnie w miejscu kursora
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

                // Wyciągamy model i dobieramy odpowiedni klucz
                string model = cmbModels.Text;
                string keyToUse = "";

                if (model.StartsWith("Gemini")) keyToUse = apiKeyGemini;
                else if (model.StartsWith("OpenAI")) keyToUse = apiKeyOpenAI;

                if (string.IsNullOrEmpty(keyToUse))
                {
                    MessageBox.Show($"Bitte konfigurieren Sie zuerst den API-Key dla {model.Split(':')[0]}!", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!File.Exists(path)) return;

                LogToProtocol($"Starte Analyse dla: {fileName} z {model}...");
                row.Cells["Status"].Value = "⏳"; // Ikonka ładowania

                try
                {
                    string scriptContent = File.ReadAllText(path); // Czytamy plik z dysku

                    // WYSYŁAMY DO AI (Czeka, nie blokując interfejsu)
                    string aiResponse = await AIEngine.AnalyzeScriptAsync(scriptContent, keyToUse, model);

                    // Zapisujemy wynik do naszej bazy SQLite
                    DatabaseHelper.SaveScript(path, fileName, "🟢", aiResponse);

                    // Aktualizujemy tabelę i opis w interfejsie
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

                // Dynamiczne okienko edycji
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
                    // Zapis do SQLite
                    DatabaseHelper.SaveScript(path, fileName, currentStatus, txtDesc.Text);

                    // Aktualizacja w tabeli UI
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
                    // Otwiera plik w edytorze PowerShell ISE
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
                    // Skopiowanie tekstu do schowka Windows
                    Clipboard.SetText(path);

                    // Wyświetlamy potwierdzenie na dolnym pasku zamiast wyskakującego okienka (żeby nie irytować)
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

        // --- WYSZUKIWARKA (FILTR NA ŻYWO) ---

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            string filterText = txtSearch.Text.ToLower();

            dgvScripts.SuspendLayout();
            dgvScripts.CurrentCell = null; // Unikamy błędu ukrywania aktualnie zaznaczonej komórki

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

        private void BtnApiKey_Click(object sender, EventArgs e)
        {
            using (Form f = new Form())
            {
                f.Text = "Einstellungen";
                f.Width = 500;
                f.Height = 300;
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false;

                Label lblDb = new Label() { Text = "Datenbank:", Left = 20, Top = 20, Width = 120 };
                TextBox txtDb = new TextBox() { Left = 150, Top = 20, Width = 180, Text = DatabaseHelper.DbFileName, ReadOnly = true };

                Button btnOpenDb = new Button() { Text = "📂", Left = 340, Top = 18, Width = 40 };
                Button btnSaveDb = new Button() { Text = "💾", Left = 390, Top = 18, Width = 40 };

                bool dbPathChanged = false;
                bool copyDatabase = false;

                btnOpenDb.Click += (s, args) =>
                {
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Filter = "SQLite Datenbank|*.sqlite|Alle Dateien|*.*";
                        ofd.Title = "Vorhandene Datenbank öffnen";
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            txtDb.Text = ofd.FileName;
                            dbPathChanged = true;
                            copyDatabase = false; // Just reconnect
                        }
                    }
                };

                btnSaveDb.Click += (s, args) =>
                {
                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.Filter = "SQLite Datenbank|*.sqlite|Alle Dateien|*.*";
                        sfd.FileName = "scripts_db.sqlite";
                        sfd.Title = "Datenbank speichern unter";
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            txtDb.Text = sfd.FileName;
                            dbPathChanged = true;
                            copyDatabase = true; // Needs copying
                        }
                    }
                };

                Label lblOpenAI = new Label() { Text = "OpenAI API-Key:", Left = 20, Top = 60, Width = 120 };
                TextBox txtOpenAI = new TextBox() { Left = 150, Top = 60, Width = 280, Text = apiKeyOpenAI, PasswordChar = '*' };

                Label lblGemini = new Label() { Text = "Gemini API-Key:", Left = 20, Top = 100, Width = 120 };
                TextBox txtGemini = new TextBox() { Left = 150, Top = 100, Width = 280, Text = apiKeyGemini, PasswordChar = '*' };

                Label lblClaude = new Label() { Text = "Claude API-Key:", Left = 20, Top = 140, Width = 120 };
                TextBox txtClaude = new TextBox() { Left = 150, Top = 140, Width = 280, Text = apiKeyClaude, PasswordChar = '*' };

                Button btnOk = new Button() { Text = "OK", Left = 250, Top = 190, Width = 80, DialogResult = DialogResult.OK };
                StylePrimaryButton(btnOk);
                Button btnCancel = new Button() { Text = "Abbrechen", Left = 340, Top = 190, Width = 90, DialogResult = DialogResult.Cancel };
                StyleSecondaryButton(btnCancel);

                f.Controls.Add(lblDb);
                f.Controls.Add(txtDb);
                f.Controls.Add(btnOpenDb);
                f.Controls.Add(btnSaveDb);
                f.Controls.Add(lblOpenAI);
                f.Controls.Add(txtOpenAI);
                f.Controls.Add(lblGemini);
                f.Controls.Add(txtGemini);
                f.Controls.Add(lblClaude);
                f.Controls.Add(txtClaude);
                f.Controls.Add(btnOk);
                f.Controls.Add(btnCancel);

                f.AcceptButton = btnOk;
                f.CancelButton = btnCancel;

                if (f.ShowDialog() == DialogResult.OK)
                {
                    string oldDbPath = DatabaseHelper.DbFileName;
                    if (dbPathChanged && oldDbPath != txtDb.Text)
                    {
                        if (copyDatabase)
                        {
                            try
                            {
                                File.Copy(oldDbPath, txtDb.Text, true);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Fehler beim Kopieren der Datenbank: " + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }

                        DatabaseHelper.ChangeDatabasePath(txtDb.Text);
                        lstSources.Items.Clear();
                        dgvScripts.Rows.Clear();
                        _simManager.Scripts.Clear();
                        lblSimilarityData.Text = "-";
                        LoadSavedSources();
                        LoadAlbums();
                        MessageBox.Show("Die Datenbank wurde erfolgreich gewechselt.", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    apiKeyOpenAI = txtOpenAI.Text;
                    apiKeyGemini = txtGemini.Text;
                    apiKeyClaude = txtClaude.Text;

                    DatabaseHelper.SaveSetting("OpenAI", apiKeyOpenAI);
                    DatabaseHelper.SaveSetting("Gemini", apiKeyGemini);
                    DatabaseHelper.SaveSetting("Claude", apiKeyClaude);

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
            }
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