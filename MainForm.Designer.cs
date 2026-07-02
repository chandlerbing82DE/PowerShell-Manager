namespace PowerShellAnalyzer
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblAppTitle;
        private System.Windows.Forms.Label lblAppSubtitle;
        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabAnalyse;
        private System.Windows.Forms.TabPage tabBibliothek;
        private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
        private System.Windows.Forms.Panel pnlLeft;
        private System.Windows.Forms.Panel pnlLeftTop;
        private System.Windows.Forms.Label lblGridTitle;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.DataGridView dgvScripts;
        private System.Windows.Forms.Panel pnlRight;
        private System.Windows.Forms.Label lblSources;
        private System.Windows.Forms.ListBox lstSources;
        private System.Windows.Forms.Button btnAddSource;
        private System.Windows.Forms.Button btnAnalyze;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblModel;
        private System.Windows.Forms.ComboBox cmbModels;
        private System.Windows.Forms.Button btnApiKey;
        private System.Windows.Forms.Label lblApiKeyStatus;

        // NOWE ELEMENTY DO OPISÓW:
        private System.Windows.Forms.Label lblDescriptionTitle;
        private System.Windows.Forms.TextBox txtDescriptionRight;
        private System.Windows.Forms.Button btnEditDesc;
        private System.Windows.Forms.Button btnSaveDesc;

        private System.Windows.Forms.Panel pnlProtocol;
        private System.Windows.Forms.Label lblProtocol;
        private System.Windows.Forms.TextBox txtProtocol;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatusInfo;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            pnlHeader = new Panel();
            lblAppTitle = new Label();
            lblAppSubtitle = new Label();
            tabControlMain = new TabControl();
            tabAnalyse = new TabPage();
            tableLayoutMain = new TableLayoutPanel();
            pnlLeft = new Panel();
            dgvScripts = new DataGridView();
            dataGridViewTextBoxColumn1 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new DataGridViewTextBoxColumn();
            pnlLeftTop = new Panel();
            lblGridTitle = new Label();
            txtSearch = new TextBox();
            pnlRight = new Panel();
            lblSources = new Label();
            lstSources = new ListBox();
            btnAddSource = new Button();
            btnAnalyze = new Button();
            btnCancel = new Button();
            lblModel = new Label();
            cmbModels = new ComboBox();
            btnApiKey = new Button();
            lblApiKeyStatus = new Label();
            lblDescriptionTitle = new Label();
            txtDescriptionRight = new TextBox();
            btnEditDesc = new Button();
            btnSaveDesc = new Button();
            pnlProtocol = new Panel();
            txtProtocol = new TextBox();
            lblProtocol = new Label();
            tabBibliothek = new TabPage();
            statusStrip = new StatusStrip();
            lblStatusInfo = new ToolStripStatusLabel();
            pnlHeader.SuspendLayout();
            tabControlMain.SuspendLayout();
            tabAnalyse.SuspendLayout();
            tableLayoutMain.SuspendLayout();
            pnlLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvScripts).BeginInit();
            pnlLeftTop.SuspendLayout();
            pnlRight.SuspendLayout();
            pnlProtocol.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.White;
            pnlHeader.Controls.Add(lblAppTitle);
            pnlHeader.Controls.Add(lblAppSubtitle);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Margin = new Padding(4, 3, 4, 3);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(1400, 92);
            pnlHeader.TabIndex = 1;
            // 
            // lblAppTitle
            // 
            lblAppTitle.AutoSize = true;
            lblAppTitle.Font = new Font("Segoe UI", 18F);
            lblAppTitle.Location = new Point(23, 17);
            lblAppTitle.Margin = new Padding(4, 0, 4, 0);
            lblAppTitle.Name = "lblAppTitle";
            lblAppTitle.Size = new Size(232, 32);
            lblAppTitle.TabIndex = 0;
            lblAppTitle.Text = "PowerShell Manager";
            // 
            // lblAppSubtitle
            // 
            lblAppSubtitle.AutoSize = true;
            lblAppSubtitle.Font = new Font("Segoe UI", 9.75F);
            lblAppSubtitle.ForeColor = Color.DimGray;
            lblAppSubtitle.Location = new Point(27, 54);
            lblAppSubtitle.Margin = new Padding(4, 0, 4, 0);
            lblAppSubtitle.Name = "lblAppSubtitle";
            lblAppSubtitle.Size = new Size(415, 17);
            lblAppSubtitle.TabIndex = 1;
            lblAppSubtitle.Text = "Analyse, Bewertung und strukturierte Bibliothek für PowerShell-Skripte.";
            // 
            // tabControlMain
            // 
            tabControlMain.Controls.Add(tabAnalyse);
            tabControlMain.Controls.Add(tabBibliothek);
            tabControlMain.Dock = DockStyle.Fill;
            tabControlMain.Font = new Font("Segoe UI", 9.75F);
            tabControlMain.Location = new Point(0, 92);
            tabControlMain.Margin = new Padding(4, 3, 4, 3);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(1400, 834);
            tabControlMain.TabIndex = 0;
            // 
            // tabAnalyse
            // 
            tabAnalyse.BackColor = Color.White;
            tabAnalyse.Controls.Add(tableLayoutMain);
            tabAnalyse.Location = new Point(4, 26);
            tabAnalyse.Margin = new Padding(4, 3, 4, 3);
            tabAnalyse.Name = "tabAnalyse";
            tabAnalyse.Padding = new Padding(12);
            tabAnalyse.Size = new Size(1392, 804);
            tabAnalyse.TabIndex = 0;
            tabAnalyse.Text = "Analyse";
            // 
            // tableLayoutMain
            // 
            tableLayoutMain.ColumnCount = 2;
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 490F));
            tableLayoutMain.Controls.Add(pnlLeft, 0, 0);
            tableLayoutMain.Controls.Add(pnlRight, 1, 0);
            tableLayoutMain.Controls.Add(pnlProtocol, 0, 1);
            tableLayoutMain.Dock = DockStyle.Fill;
            tableLayoutMain.Location = new Point(12, 12);
            tableLayoutMain.Margin = new Padding(4, 3, 4, 3);
            tableLayoutMain.Name = "tableLayoutMain";
            tableLayoutMain.RowCount = 2;
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 185F));
            tableLayoutMain.Size = new Size(1368, 780);
            tableLayoutMain.TabIndex = 0;
            // 
            // pnlLeft
            // 
            pnlLeft.Controls.Add(dgvScripts);
            pnlLeft.Controls.Add(pnlLeftTop);
            pnlLeft.Dock = DockStyle.Fill;
            pnlLeft.Location = new Point(4, 3);
            pnlLeft.Margin = new Padding(4, 3, 4, 3);
            pnlLeft.Name = "pnlLeft";
            pnlLeft.Padding = new Padding(0, 0, 12, 0);
            pnlLeft.Size = new Size(870, 589);
            pnlLeft.TabIndex = 0;
            // 
            // dgvScripts
            // 
            dgvScripts.AllowUserToAddRows = false;
            dgvScripts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvScripts.BackgroundColor = Color.White;
            dgvScripts.Columns.AddRange(new DataGridViewColumn[] { dataGridViewTextBoxColumn1, dataGridViewTextBoxColumn2, dataGridViewTextBoxColumn3, dataGridViewTextBoxColumn4 });
            dgvScripts.Dock = DockStyle.Fill;
            dgvScripts.Location = new Point(0, 69);
            dgvScripts.Margin = new Padding(4, 3, 4, 3);
            dgvScripts.Name = "dgvScripts";
            dgvScripts.ReadOnly = true;
            dgvScripts.RowHeadersVisible = false;
            dgvScripts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvScripts.Size = new Size(858, 520);
            dgvScripts.TabIndex = 0;
            dgvScripts.CellContentClick += dgvScripts_CellContentClick;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.HeaderText = "Status";
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.HeaderText = "ID";
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.HeaderText = "Datei";
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.HeaderText = "Pfad";
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            // 
            // pnlLeftTop
            // 
            pnlLeftTop.BackColor = Color.White;
            pnlLeftTop.Controls.Add(lblGridTitle);
            pnlLeftTop.Controls.Add(txtSearch);
            pnlLeftTop.Dock = DockStyle.Top;
            pnlLeftTop.Location = new Point(0, 0);
            pnlLeftTop.Margin = new Padding(4, 3, 4, 3);
            pnlLeftTop.Name = "pnlLeftTop";
            pnlLeftTop.Size = new Size(858, 69);
            pnlLeftTop.TabIndex = 1;
            // 
            // lblGridTitle
            // 
            lblGridTitle.AutoSize = true;
            lblGridTitle.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            lblGridTitle.Location = new Point(0, 6);
            lblGridTitle.Margin = new Padding(4, 0, 4, 0);
            lblGridTitle.Name = "lblGridTitle";
            lblGridTitle.Size = new Size(122, 17);
            lblGridTitle.TabIndex = 0;
            lblGridTitle.Text = "Gefundene Skripte";
            // 
            // txtSearch
            // 
            txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSearch.Location = new Point(0, 35);
            txtSearch.Margin = new Padding(4, 3, 4, 3);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new Size(857, 25);
            txtSearch.TabIndex = 1;
            // 
            // pnlRight
            // 
            pnlRight.Controls.Add(lblSources);
            pnlRight.Controls.Add(lstSources);
            pnlRight.Controls.Add(btnAddSource);
            pnlRight.Controls.Add(btnAnalyze);
            pnlRight.Controls.Add(btnCancel);
            pnlRight.Controls.Add(lblModel);
            pnlRight.Controls.Add(cmbModels);
            pnlRight.Controls.Add(btnApiKey);
            pnlRight.Controls.Add(lblApiKeyStatus);
            pnlRight.Controls.Add(lblDescriptionTitle);
            pnlRight.Controls.Add(txtDescriptionRight);
            pnlRight.Controls.Add(btnEditDesc);
            pnlRight.Controls.Add(btnSaveDesc);
            pnlRight.Dock = DockStyle.Fill;
            pnlRight.Location = new Point(882, 3);
            pnlRight.Margin = new Padding(4, 3, 4, 3);
            pnlRight.Name = "pnlRight";
            pnlRight.Size = new Size(482, 589);
            pnlRight.TabIndex = 1;
            // 
            // lblSources
            // 
            lblSources.AutoSize = true;
            lblSources.Location = new Point(12, 6);
            lblSources.Margin = new Padding(4, 0, 4, 0);
            lblSources.Name = "lblSources";
            lblSources.Size = new Size(145, 17);
            lblSources.TabIndex = 0;
            lblSources.Text = "Skriptquellen verwalten:";
            // 
            // lstSources
            // 
            lstSources.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lstSources.Location = new Point(15, 31);
            lstSources.Margin = new Padding(4, 3, 4, 3);
            lstSources.Name = "lstSources";
            lstSources.Size = new Size(453, 89);
            lstSources.TabIndex = 1;
            // 
            // btnAddSource
            // 
            btnAddSource.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAddSource.Location = new Point(329, 144);
            btnAddSource.Margin = new Padding(4, 3, 4, 3);
            btnAddSource.Name = "btnAddSource";
            btnAddSource.Size = new Size(140, 35);
            btnAddSource.TabIndex = 2;
            btnAddSource.Text = "+ Dodaj źródło";
            // 
            // btnAnalyze
            // 
            btnAnalyze.BackColor = Color.SteelBlue;
            btnAnalyze.ForeColor = Color.White;
            btnAnalyze.Location = new Point(15, 196);
            btnAnalyze.Margin = new Padding(4, 3, 4, 3);
            btnAnalyze.Name = "btnAnalyze";
            btnAnalyze.Size = new Size(222, 46);
            btnAnalyze.TabIndex = 3;
            btnAnalyze.Text = "Analyse starten";
            btnAnalyze.UseVisualStyleBackColor = false;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancel.BackColor = Color.LightGray;
            btnCancel.Location = new Point(247, 196);
            btnCancel.Margin = new Padding(4, 3, 4, 3);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(222, 46);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Abbrechen";
            btnCancel.UseVisualStyleBackColor = false;
            // 
            // lblModel
            // 
            lblModel.AutoSize = true;
            lblModel.Location = new Point(12, 260);
            lblModel.Margin = new Padding(4, 0, 4, 0);
            lblModel.Name = "lblModel";
            lblModel.Size = new Size(112, 17);
            lblModel.TabIndex = 5;
            lblModel.Text = "AI-Modell wählen:";
            // 
            // cmbModels
            // 
            cmbModels.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbModels.Location = new Point(15, 283);
            cmbModels.Margin = new Padding(4, 3, 4, 3);
            cmbModels.Name = "cmbModels";
            cmbModels.Size = new Size(221, 25);
            cmbModels.TabIndex = 6;
            // 
            // btnApiKey
            // 
            btnApiKey.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnApiKey.Location = new Point(247, 282);
            btnApiKey.Margin = new Padding(4, 3, 4, 3);
            btnApiKey.Name = "btnApiKey";
            btnApiKey.Size = new Size(222, 31);
            btnApiKey.TabIndex = 7;
            btnApiKey.Text = "⚙ API-Schlüssel konfigurieren";
            // 
            // lblApiKeyStatus
            // 
            lblApiKeyStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblApiKeyStatus.AutoSize = true;
            lblApiKeyStatus.ForeColor = Color.Green;
            lblApiKeyStatus.Location = new Point(244, 317);
            lblApiKeyStatus.Margin = new Padding(4, 0, 4, 0);
            lblApiKeyStatus.Name = "lblApiKeyStatus";
            lblApiKeyStatus.Size = new Size(174, 17);
            lblApiKeyStatus.TabIndex = 8;
            lblApiKeyStatus.Text = "Status: Schlüssel gespeichert";
            // 
            // lblDescriptionTitle
            // 
            lblDescriptionTitle.AutoSize = true;
            lblDescriptionTitle.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            lblDescriptionTitle.Location = new Point(12, 358);
            lblDescriptionTitle.Margin = new Padding(4, 0, 4, 0);
            lblDescriptionTitle.Name = "lblDescriptionTitle";
            lblDescriptionTitle.Size = new Size(95, 17);
            lblDescriptionTitle.TabIndex = 9;
            lblDescriptionTitle.Text = "Beschreibung:";
            // 
            // txtDescriptionRight
            // 
            txtDescriptionRight.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtDescriptionRight.BackColor = Color.WhiteSmoke;
            txtDescriptionRight.Location = new Point(15, 387);
            txtDescriptionRight.Margin = new Padding(4, 3, 4, 3);
            txtDescriptionRight.Multiline = true;
            txtDescriptionRight.Name = "txtDescriptionRight";
            txtDescriptionRight.ReadOnly = true;
            txtDescriptionRight.ScrollBars = ScrollBars.Vertical;
            txtDescriptionRight.Size = new Size(453, 139);
            txtDescriptionRight.TabIndex = 10;
            // 
            // btnEditDesc
            // 
            btnEditDesc.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnEditDesc.Location = new Point(15, 538);
            btnEditDesc.Margin = new Padding(4, 3, 4, 3);
            btnEditDesc.Name = "btnEditDesc";
            btnEditDesc.Size = new Size(93, 35);
            btnEditDesc.TabIndex = 11;
            btnEditDesc.Text = "Edit";
            // 
            // btnSaveDesc
            // 
            btnSaveDesc.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSaveDesc.BackColor = Color.SteelBlue;
            btnSaveDesc.ForeColor = Color.White;
            btnSaveDesc.Location = new Point(120, 538);
            btnSaveDesc.Margin = new Padding(4, 3, 4, 3);
            btnSaveDesc.Name = "btnSaveDesc";
            btnSaveDesc.Size = new Size(117, 35);
            btnSaveDesc.TabIndex = 12;
            btnSaveDesc.Text = "Speichern";
            btnSaveDesc.UseVisualStyleBackColor = false;
            // 
            // pnlProtocol
            // 
            tableLayoutMain.SetColumnSpan(pnlProtocol, 2);
            pnlProtocol.Controls.Add(txtProtocol);
            pnlProtocol.Controls.Add(lblProtocol);
            pnlProtocol.Dock = DockStyle.Fill;
            pnlProtocol.Location = new Point(4, 598);
            pnlProtocol.Margin = new Padding(4, 3, 4, 3);
            pnlProtocol.Name = "pnlProtocol";
            pnlProtocol.Size = new Size(1360, 179);
            pnlProtocol.TabIndex = 2;
            // 
            // txtProtocol
            // 
            txtProtocol.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtProtocol.BackColor = Color.WhiteSmoke;
            txtProtocol.Location = new Point(0, 35);
            txtProtocol.Margin = new Padding(4, 3, 4, 3);
            txtProtocol.Multiline = true;
            txtProtocol.Name = "txtProtocol";
            txtProtocol.ReadOnly = true;
            txtProtocol.ScrollBars = ScrollBars.Vertical;
            txtProtocol.Size = new Size(1360, 139);
            txtProtocol.TabIndex = 0;
            // 
            // lblProtocol
            // 
            lblProtocol.AutoSize = true;
            lblProtocol.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            lblProtocol.Location = new Point(0, 6);
            lblProtocol.Margin = new Padding(4, 0, 4, 0);
            lblProtocol.Name = "lblProtocol";
            lblProtocol.Size = new Size(65, 17);
            lblProtocol.TabIndex = 1;
            lblProtocol.Text = "Protokoll";
            // 
            // tabBibliothek
            // 
            tabBibliothek.BackColor = Color.White;
            tabBibliothek.Location = new Point(4, 26);
            tabBibliothek.Margin = new Padding(4, 3, 4, 3);
            tabBibliothek.Name = "tabBibliothek";
            tabBibliothek.Size = new Size(1392, 804);
            tabBibliothek.TabIndex = 1;
            tabBibliothek.Text = "Bibliothek";
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatusInfo });
            statusStrip.Location = new Point(0, 926);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(1, 0, 16, 0);
            statusStrip.Size = new Size(1400, 22);
            statusStrip.TabIndex = 2;
            // 
            // lblStatusInfo
            // 
            lblStatusInfo.Name = "lblStatusInfo";
            lblStatusInfo.Size = new Size(145, 17);
            lblStatusInfo.Text = "Bereit. 0 Skripte gefunden.";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1400, 948);
            Controls.Add(tabControlMain);
            Controls.Add(pnlHeader);
            Controls.Add(statusStrip);
            Margin = new Padding(4, 3, 4, 3);
            MinimumSize = new Size(1164, 802);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "PowerShell Manager";
            Load += MainForm_Load;
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            tabControlMain.ResumeLayout(false);
            tabAnalyse.ResumeLayout(false);
            tableLayoutMain.ResumeLayout(false);
            pnlLeft.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvScripts).EndInit();
            pnlLeftTop.ResumeLayout(false);
            pnlLeftTop.PerformLayout();
            pnlRight.ResumeLayout(false);
            pnlRight.PerformLayout();
            pnlProtocol.ResumeLayout(false);
            pnlProtocol.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
    }
}