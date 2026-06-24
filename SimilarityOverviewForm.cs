using System.Text;

namespace PowerShellAnalyzer
{
    public class SimilarityOverviewForm : Form
    {
        public bool HasDeletedScripts { get; private set; } = false;

        private DataGridView dgv;
        private List<ScriptSimilarityInfo> _scripts;
        private TabControl tabControl;
        private TabPage tabOverview;
        private TabPage tabAIResult;
        private DataGridView dgvAIResult;

        // Zmienne do API
        private string _apiKey;
        private string _selectedModel;

        public SimilarityOverviewForm(List<ScriptSimilarityInfo> scripts, string apiKey = "", string selectedModel = "")
        {
            _scripts = scripts;
            _apiKey = apiKey;
            _selectedModel = selectedModel;

            Text = "Übersicht ähnlicher Skripte";
            Width = 900;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;

            tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.75F) };
            Controls.Add(tabControl);

            tabOverview = new TabPage("Übersicht");
            tabAIResult = new TabPage("KI Analyse Ergebnis");

            tabControl.TabPages.Add(tabOverview);
            tabControl.TabPages.Add(tabAIResult);

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White };
            tabOverview.Controls.Add(pnlTop);

            var btnExport = new Button { Text = "Als CSV exportieren", Width = 150, Height = 35, Left = 10, Top = 8, BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += BtnExport_Click;
            pnlTop.Controls.Add(btnExport);

            var btnCompare = new Button { Text = "Ausgewählte vergleichen", Width = 200, Height = 35, Left = 170, Top = 8, BackColor = Color.FromArgb(240, 244, 248), FlatStyle = FlatStyle.Flat };
            btnCompare.Click += BtnCompare_Click;
            pnlTop.Controls.Add(btnCompare);

            var btnAI = new Button { Text = "KI-Analyse Duplikate", Width = 200, Height = 35, Left = 380, Top = 8, BackColor = Color.DarkOrange, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAI.FlatAppearance.BorderSize = 0;
            btnAI.Click += BtnAI_Click;
            pnlTop.Controls.Add(btnAI);

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgv.Columns.Add("GroupId", "Gruppe");
            dgv.Columns.Add("Id", "ID");
            dgv.Columns.Add("FileName", "Dateiname");
            dgv.Columns.Add("Score", "Ähnlichkeit");
            dgv.Columns.Add("BestMatch", "Am ähnlichsten zu ID");
            dgv.Columns.Add("Path", "Pfad");

            tabOverview.Controls.Add(dgv);
            dgv.BringToFront();

            dgvAIResult = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.WhiteSmoke,
                RowHeadersVisible = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 40,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };
            dgvAIResult.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 244, 248);
            dgvAIResult.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            dgvAIResult.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvAIResult.DefaultCellStyle.Padding = new Padding(5);

            dgvAIResult.Columns.Add("Status", "Aktion");
            dgvAIResult.Columns["Status"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgvAIResult.Columns.Add("Script", "Skript");
            dgvAIResult.Columns["Script"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgvAIResult.Columns.Add("Reason", "Begründung");
            dgvAIResult.Columns.Add("Path", "Pfad");
            dgvAIResult.Columns["Path"].Visible = false; // Hide path to keep UI clean, but keep data

            var pnlAITop = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White };
            var btnDeleteGlobal = new Button { Text = "Alle markierten löschen", Width = 200, Height = 35, Left = 10, Top = 8, BackColor = Color.IndianRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnDeleteGlobal.FlatAppearance.BorderSize = 0;
            btnDeleteGlobal.Click += BtnDeleteGlobal_Click;
            pnlAITop.Controls.Add(btnDeleteGlobal);
            tabAIResult.Controls.Add(pnlAITop);

            ContextMenuStrip aiContextMenu = new ContextMenuStrip();
            aiContextMenu.Items.Add("Ausgewähltes Skript physisch löschen", null, ContextMenu_DeleteSingleAI);
            dgvAIResult.ContextMenuStrip = aiContextMenu;

            tabAIResult.Controls.Add(dgvAIResult);
            dgvAIResult.BringToFront();

            LoadData();
        }

        private void LoadData()
        {
            var grouped = _scripts.Where(s => !string.IsNullOrEmpty(s.SimilarityGroupId))
                                  .OrderBy(s => s.SimilarityGroupId)
                                  .ToList();

            string currentGroup = "";
            bool isAlternateColor = false;

            foreach (var s in grouped)
            {
                if (currentGroup != s.SimilarityGroupId)
                {
                    currentGroup = s.SimilarityGroupId;
                    isAlternateColor = !isAlternateColor;
                }

                int rowIndex = dgv.Rows.Add(s.SimilarityGroupId, s.Id, s.FileName, s.BestSimilarityScore.ToString("P1"), s.BestMatchScriptId, s.Path);

                // Zastosuj naprzemienne kolory dla różnych grup skryptów
                if (isAlternateColor)
                {
                    dgv.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(245, 248, 252);
                }
                else
                {
                    dgv.Rows[rowIndex].DefaultCellStyle.BackColor = Color.White;
                }
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog { Filter = "CSV-Datei (*.csv)|*.csv", FileName = "Aehnliche_Skripte.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Gruppe;ID;Dateiname;Ähnlichkeit;ÄhnlichZuID;Pfad");
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        sb.AppendLine($"{row.Cells[0].Value};{row.Cells[1].Value};{row.Cells[2].Value};{row.Cells[3].Value};{row.Cells[4].Value};{row.Cells[5].Value}");
                    }
                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Export erfolgreich!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void BtnCompare_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 2)
            {
                var id1 = dgv.SelectedRows[0].Cells["Id"].Value.ToString();
                var id2 = dgv.SelectedRows[1].Cells["Id"].Value.ToString();

                var script1 = _scripts.FirstOrDefault(s => s.Id == id1);
                var script2 = _scripts.FirstOrDefault(s => s.Id == id2);

                if (script1 != null && script2 != null)
                {
                    double score = 0;
                    if (script1.BestMatchScriptId == id2) score = script1.BestSimilarityScore;
                    else if (script2.BestMatchScriptId == id1) score = script2.BestSimilarityScore;

                    var diff = new DiffForm(script1.Id, script1.FileName, script1.Content, script2.Id, script2.FileName, script2.Content, score);
                    diff.ShowDialog();
                }
            }
            else
            {
                MessageBox.Show("Bitte genau 2 Skripte zum Vergleichen auswählen.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void BtnAI_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count < 2)
            {
                MessageBox.Show("Bitte wählen Sie mindestens 2 Skripte aus der Liste zur Analyse aus.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_selectedModel))
            {
                MessageBox.Show("API-Schlüssel oder Modell ist nicht konfiguriert. Bitte konfigurieren Sie diese zuerst im Hauptfenster.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            List<ScriptSimilarityInfo> selectedDupes = new List<ScriptSimilarityInfo>();
            foreach (DataGridViewRow row in dgv.SelectedRows)
            {
                string id = row.Cells["Id"].Value.ToString();
                var s = _scripts.FirstOrDefault(x => x.Id == id);
                if (s != null) selectedDupes.Add(s);
            }

            dgvAIResult.Rows.Clear();
            dgvAIResult.Rows.Add("⏳", "", "Analysiere Duplikate. Bitte warten, dies kann einige Sekunden dauern...");
            tabControl.SelectedTab = tabAIResult;

            try
            {
                string result = await AIEngine.AnalyzeDuplicatesAsync(selectedDupes, _apiKey, _selectedModel);

                dgvAIResult.Rows.Clear();

                try
                {
                    // Wytnij blok kodu JSON jeśli AI dodał markdown ```json
                    if (result.Contains("```json"))
                    {
                        int start = result.IndexOf("```json") + 7;
                        int end = result.LastIndexOf("```");
                        if (end > start)
                            result = result.Substring(start, end - start).Trim();
                    }
                    else if (result.Contains("```"))
                    {
                        int start = result.IndexOf("```") + 3;
                        int end = result.LastIndexOf("```");
                        if (end > start)
                            result = result.Substring(start, end - start).Trim();
                    }

                    var jsonArray = Newtonsoft.Json.Linq.JArray.Parse(result);
                    foreach (var item in jsonArray)
                    {
                        string status = item["Status"]?.ToString() ?? "";
                        string icon = status.ToLower() == "behalten" ? "🟢 Behalten" : "🔴 Löschen";
                        string scriptName = item["Skript"]?.ToString() ?? "";
                        string reason = item["Grund"]?.ToString() ?? "";

                        var targetScript = selectedDupes.FirstOrDefault(s => s.FileName == scriptName || s.Id == scriptName || s.FileName.Contains(scriptName));
                        string scriptPath = targetScript?.Path ?? "";

                        int r = dgvAIResult.Rows.Add(icon, scriptName, reason, scriptPath);
                        if (status.ToLower() == "löschen")
                            dgvAIResult.Rows[r].DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 240);
                        else
                            dgvAIResult.Rows[r].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                    }
                }
                catch
                {
                    // Fallback jeśli AI nie zwróci poprawnego JSONa
                    dgvAIResult.Rows.Add("⚠️ Fehler", "JSON Parse Fehler", result, "");
                }
            }
            catch (Exception ex)
            {
                dgvAIResult.Rows.Clear();
                dgvAIResult.Rows.Add("🔴 Fehler", "", $"Ein API-Fehler ist aufgetreten: {ex.Message}", "");
            }
        }

        private void BtnDeleteGlobal_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Möchten Sie wirklich alle von der KI zum Löschen markierten Skripte unwiederbringlich von der Festplatte löschen?",
                                "Massenlöschung bestätigen", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                int deletedCount = 0;
                var rowsToRemove = new List<DataGridViewRow>();

                foreach (DataGridViewRow row in dgvAIResult.Rows)
                {
                    if (row.Cells[0].Value?.ToString() == "🔴 Löschen")
                    {
                        string path = row.Cells["Path"].Value?.ToString();
                        if (!string.IsNullOrEmpty(path))
                        {
                            try
                            {
                                if (File.Exists(path)) File.Delete(path);
                                DatabaseHelper.DeleteScriptData(path);
                                deletedCount++;
                                HasDeletedScripts = true;
                                rowsToRemove.Add(row);
                            }
                            catch { }
                        }
                    }
                }

                foreach (var r in rowsToRemove) dgvAIResult.Rows.Remove(r);

                if (deletedCount > 0)
                {
                    MessageBox.Show($"{deletedCount} Skripte wurden erfolgreich gelöscht. Bitte aktualisieren Sie die Hauptansicht, um die Änderungen zu sehen.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ContextMenu_DeleteSingleAI(object sender, EventArgs e)
        {
            if (dgvAIResult.SelectedRows.Count > 0)
            {
                var row = dgvAIResult.SelectedRows[0];
                string path = row.Cells["Path"].Value?.ToString();

                if (!string.IsNullOrEmpty(path) && MessageBox.Show($"Möchten Sie dieses Skript wirklich von der Festplatte löschen?\n\n{path}", "Löschen bestätigen", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    try
                    {
                        if (File.Exists(path)) File.Delete(path);
                        DatabaseHelper.DeleteScriptData(path);
                        dgvAIResult.Rows.Remove(row);
                        HasDeletedScripts = true;
                        MessageBox.Show("Skript wurde erfolgreich gelöscht.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Löschen des Skripts:\n{ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (string.IsNullOrEmpty(path))
                {
                    MessageBox.Show("Zu diesem Eintrag konnte kein gültiger Dateipfad gefunden werden.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}