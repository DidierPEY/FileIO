namespace llt.Test
{
    partial class Form1
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.log = new System.Windows.Forms.RichTextBox();
            this.btnSepEnr = new System.Windows.Forms.Button();
            this.btnLecEcrTxt = new System.Windows.Forms.Button();
            this.btnLecTxt = new System.Windows.Forms.Button();
            this.btnLecEcr = new System.Windows.Forms.Button();
            this.txtFileW = new System.Windows.Forms.TextBox();
            this.contenu = new System.Windows.Forms.RichTextBox();
            this.chkAsync = new System.Windows.Forms.CheckBox();
            this.txtFile = new System.Windows.Forms.TextBox();
            this.btnLec = new System.Windows.Forms.Button();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.txtFTPDownload = new System.Windows.Forms.TextBox();
            this.txtFTPUpload = new System.Windows.Forms.TextBox();
            this.txtFTPLocal = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txtFTPDistant = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.chkFTPKeepAlive = new System.Windows.Forms.CheckBox();
            this.txtFTPMotDePasse = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtFTPUtilisateur = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtFTPServeur = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.btnTestFTP2 = new System.Windows.Forms.Button();
            this.btnTestFTP = new System.Windows.Forms.Button();
            this.tabPage0 = new System.Windows.Forms.TabPage();
            this.txtDelChamp = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtSepChamp = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.btnChgRows = new System.Windows.Forms.Button();
            this.txtFile2 = new System.Windows.Forms.TextBox();
            this.chkNomChamp = new System.Windows.Forms.CheckBox();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.txtXML2 = new System.Windows.Forms.TextBox();
            this.txtFicData2 = new System.Windows.Forms.TextBox();
            this.btnImport = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.tabControl2 = new System.Windows.Forms.TabControl();
            this.tabXML = new System.Windows.Forms.TabPage();
            this.rXML = new System.Windows.Forms.RichTextBox();
            this.tabExport = new System.Windows.Forms.TabPage();
            this.label5 = new System.Windows.Forms.Label();
            this.lstExport = new System.Windows.Forms.ListBox();
            this.dgvExport = new System.Windows.Forms.DataGridView();
            this.label3 = new System.Windows.Forms.Label();
            this.txtXML = new System.Windows.Forms.TextBox();
            this.txtFicData = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage0.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.tabPage3.SuspendLayout();
            this.tabControl2.SuspendLayout();
            this.tabXML.SuspendLayout();
            this.tabExport.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvExport)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage0);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(6, 15);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(756, 361);
            this.tabControl1.TabIndex = 13;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.log);
            this.tabPage2.Controls.Add(this.btnSepEnr);
            this.tabPage2.Controls.Add(this.btnLecEcrTxt);
            this.tabPage2.Controls.Add(this.btnLecTxt);
            this.tabPage2.Controls.Add(this.btnLecEcr);
            this.tabPage2.Controls.Add(this.txtFileW);
            this.tabPage2.Controls.Add(this.contenu);
            this.tabPage2.Controls.Add(this.chkAsync);
            this.tabPage2.Controls.Add(this.txtFile);
            this.tabPage2.Controls.Add(this.btnLec);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(748, 335);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Test lecture fichier";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // log
            // 
            this.log.Location = new System.Drawing.Point(451, 53);
            this.log.Margin = new System.Windows.Forms.Padding(2);
            this.log.Name = "log";
            this.log.Size = new System.Drawing.Size(288, 277);
            this.log.TabIndex = 22;
            this.log.Text = "";
            // 
            // btnSepEnr
            // 
            this.btnSepEnr.Location = new System.Drawing.Point(309, 24);
            this.btnSepEnr.Margin = new System.Windows.Forms.Padding(2);
            this.btnSepEnr.Name = "btnSepEnr";
            this.btnSepEnr.Size = new System.Drawing.Size(134, 24);
            this.btnSepEnr.TabIndex = 21;
            this.btnSepEnr.Text = "Test Sép. enregistrement";
            this.btnSepEnr.UseVisualStyleBackColor = true;
            this.btnSepEnr.Click += new System.EventHandler(this.btn_click);
            // 
            // btnLecEcrTxt
            // 
            this.btnLecEcrTxt.Location = new System.Drawing.Point(558, 24);
            this.btnLecEcrTxt.Margin = new System.Windows.Forms.Padding(2);
            this.btnLecEcrTxt.Name = "btnLecEcrTxt";
            this.btnLecEcrTxt.Size = new System.Drawing.Size(96, 24);
            this.btnLecEcrTxt.TabIndex = 20;
            this.btnLecEcrTxt.Text = "Lit et ecrit (texte)";
            this.btnLecEcrTxt.UseVisualStyleBackColor = true;
            this.btnLecEcrTxt.Click += new System.EventHandler(this.btn_click);
            // 
            // btnLecTxt
            // 
            this.btnLecTxt.Location = new System.Drawing.Point(116, 24);
            this.btnLecTxt.Margin = new System.Windows.Forms.Padding(2);
            this.btnLecTxt.Name = "btnLecTxt";
            this.btnLecTxt.Size = new System.Drawing.Size(96, 24);
            this.btnLecTxt.TabIndex = 19;
            this.btnLecTxt.Text = "Lit (texte)";
            this.btnLecTxt.UseVisualStyleBackColor = true;
            this.btnLecTxt.Click += new System.EventHandler(this.btn_click);
            // 
            // btnLecEcr
            // 
            this.btnLecEcr.Location = new System.Drawing.Point(451, 24);
            this.btnLecEcr.Margin = new System.Windows.Forms.Padding(2);
            this.btnLecEcr.Name = "btnLecEcr";
            this.btnLecEcr.Size = new System.Drawing.Size(96, 24);
            this.btnLecEcr.TabIndex = 18;
            this.btnLecEcr.Text = "Lit et ecrit (byte)";
            this.btnLecEcr.UseVisualStyleBackColor = true;
            this.btnLecEcr.Click += new System.EventHandler(this.btn_click);
            // 
            // txtFileW
            // 
            this.txtFileW.Location = new System.Drawing.Point(451, 1);
            this.txtFileW.Margin = new System.Windows.Forms.Padding(2);
            this.txtFileW.Name = "txtFileW";
            this.txtFileW.Size = new System.Drawing.Size(203, 20);
            this.txtFileW.TabIndex = 17;
            this.txtFileW.Text = ".\\ecriture.txt";
            // 
            // contenu
            // 
            this.contenu.Location = new System.Drawing.Point(9, 53);
            this.contenu.Margin = new System.Windows.Forms.Padding(2);
            this.contenu.Name = "contenu";
            this.contenu.Size = new System.Drawing.Size(434, 277);
            this.contenu.TabIndex = 16;
            this.contenu.Text = "";
            // 
            // chkAsync
            // 
            this.chkAsync.AutoSize = true;
            this.chkAsync.Location = new System.Drawing.Point(309, 5);
            this.chkAsync.Margin = new System.Windows.Forms.Padding(2);
            this.chkAsync.Name = "chkAsync";
            this.chkAsync.Size = new System.Drawing.Size(134, 17);
            this.chkAsync.TabIndex = 15;
            this.chkAsync.Text = "Traitement asynchrone";
            this.chkAsync.UseVisualStyleBackColor = true;
            // 
            // txtFile
            // 
            this.txtFile.Location = new System.Drawing.Point(9, 1);
            this.txtFile.Margin = new System.Windows.Forms.Padding(2);
            this.txtFile.Name = "txtFile";
            this.txtFile.Size = new System.Drawing.Size(203, 20);
            this.txtFile.TabIndex = 14;
            this.txtFile.Text = ".\\exemple.txt";
            // 
            // btnLec
            // 
            this.btnLec.Location = new System.Drawing.Point(9, 24);
            this.btnLec.Margin = new System.Windows.Forms.Padding(2);
            this.btnLec.Name = "btnLec";
            this.btnLec.Size = new System.Drawing.Size(96, 24);
            this.btnLec.TabIndex = 13;
            this.btnLec.Text = "Lit (byte)";
            this.btnLec.UseVisualStyleBackColor = true;
            this.btnLec.Click += new System.EventHandler(this.btn_click);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.txtFTPDownload);
            this.tabPage1.Controls.Add(this.txtFTPUpload);
            this.tabPage1.Controls.Add(this.txtFTPLocal);
            this.tabPage1.Controls.Add(this.label10);
            this.tabPage1.Controls.Add(this.txtFTPDistant);
            this.tabPage1.Controls.Add(this.label9);
            this.tabPage1.Controls.Add(this.chkFTPKeepAlive);
            this.tabPage1.Controls.Add(this.txtFTPMotDePasse);
            this.tabPage1.Controls.Add(this.label8);
            this.tabPage1.Controls.Add(this.txtFTPUtilisateur);
            this.tabPage1.Controls.Add(this.label7);
            this.tabPage1.Controls.Add(this.txtFTPServeur);
            this.tabPage1.Controls.Add(this.label6);
            this.tabPage1.Controls.Add(this.btnTestFTP2);
            this.tabPage1.Controls.Add(this.btnTestFTP);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(748, 335);
            this.tabPage1.TabIndex = 2;
            this.tabPage1.Text = "Test FTP";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // txtFTPDownload
            // 
            this.txtFTPDownload.Location = new System.Drawing.Point(306, 136);
            this.txtFTPDownload.Margin = new System.Windows.Forms.Padding(2);
            this.txtFTPDownload.Name = "txtFTPDownload";
            this.txtFTPDownload.Size = new System.Drawing.Size(203, 20);
            this.txtFTPDownload.TabIndex = 39;
            // 
            // txtFTPUpload
            // 
            this.txtFTPUpload.Location = new System.Drawing.Point(9, 136);
            this.txtFTPUpload.Margin = new System.Windows.Forms.Padding(2);
            this.txtFTPUpload.Name = "txtFTPUpload";
            this.txtFTPUpload.Size = new System.Drawing.Size(203, 20);
            this.txtFTPUpload.TabIndex = 38;
            this.txtFTPUpload.Text = "exemple.txt";
            // 
            // txtFTPLocal
            // 
            this.txtFTPLocal.Location = new System.Drawing.Point(99, 98);
            this.txtFTPLocal.Name = "txtFTPLocal";
            this.txtFTPLocal.Size = new System.Drawing.Size(375, 20);
            this.txtFTPLocal.TabIndex = 37;
            this.txtFTPLocal.Text = ".\\";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 101);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(78, 13);
            this.label10.TabIndex = 36;
            this.label10.Text = "Répetoire local";
            // 
            // txtFTPDistant
            // 
            this.txtFTPDistant.Location = new System.Drawing.Point(99, 69);
            this.txtFTPDistant.Name = "txtFTPDistant";
            this.txtFTPDistant.Size = new System.Drawing.Size(375, 20);
            this.txtFTPDistant.TabIndex = 35;
            this.txtFTPDistant.Text = "/";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 72);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(87, 13);
            this.label9.TabIndex = 34;
            this.label9.Text = "Répetoire distant";
            // 
            // chkFTPKeepAlive
            // 
            this.chkFTPKeepAlive.AutoSize = true;
            this.chkFTPKeepAlive.Location = new System.Drawing.Point(328, 19);
            this.chkFTPKeepAlive.Name = "chkFTPKeepAlive";
            this.chkFTPKeepAlive.Size = new System.Drawing.Size(118, 17);
            this.chkFTPKeepAlive.TabIndex = 33;
            this.chkFTPKeepAlive.Text = "supporte KeepAlive";
            this.chkFTPKeepAlive.UseVisualStyleBackColor = true;
            // 
            // txtFTPMotDePasse
            // 
            this.txtFTPMotDePasse.Location = new System.Drawing.Point(328, 42);
            this.txtFTPMotDePasse.Name = "txtFTPMotDePasse";
            this.txtFTPMotDePasse.Size = new System.Drawing.Size(146, 20);
            this.txtFTPMotDePasse.TabIndex = 32;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(251, 45);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(71, 13);
            this.label8.TabIndex = 31;
            this.label8.Text = "Mot de passe";
            // 
            // txtFTPUtilisateur
            // 
            this.txtFTPUtilisateur.Location = new System.Drawing.Point(99, 42);
            this.txtFTPUtilisateur.Name = "txtFTPUtilisateur";
            this.txtFTPUtilisateur.Size = new System.Drawing.Size(146, 20);
            this.txtFTPUtilisateur.TabIndex = 30;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 46);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(53, 13);
            this.label7.TabIndex = 29;
            this.label7.Text = "Utilisateur";
            // 
            // txtFTPServeur
            // 
            this.txtFTPServeur.Location = new System.Drawing.Point(99, 16);
            this.txtFTPServeur.Name = "txtFTPServeur";
            this.txtFTPServeur.Size = new System.Drawing.Size(221, 20);
            this.txtFTPServeur.TabIndex = 28;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 19);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(67, 13);
            this.label6.TabIndex = 27;
            this.label6.Text = "Serveur FTP";
            // 
            // btnTestFTP2
            // 
            this.btnTestFTP2.Location = new System.Drawing.Point(216, 123);
            this.btnTestFTP2.Margin = new System.Windows.Forms.Padding(2);
            this.btnTestFTP2.Name = "btnTestFTP2";
            this.btnTestFTP2.Size = new System.Drawing.Size(77, 44);
            this.btnTestFTP2.TabIndex = 26;
            this.btnTestFTP2.Text = "Test Upload FTP";
            this.btnTestFTP2.UseVisualStyleBackColor = true;
            this.btnTestFTP2.Click += new System.EventHandler(this.btn_click);
            // 
            // btnTestFTP
            // 
            this.btnTestFTP.Location = new System.Drawing.Point(513, 123);
            this.btnTestFTP.Margin = new System.Windows.Forms.Padding(2);
            this.btnTestFTP.Name = "btnTestFTP";
            this.btnTestFTP.Size = new System.Drawing.Size(77, 44);
            this.btnTestFTP.TabIndex = 25;
            this.btnTestFTP.Text = "Test Dowload FTP";
            this.btnTestFTP.UseVisualStyleBackColor = true;
            this.btnTestFTP.Click += new System.EventHandler(this.btn_click);
            // 
            // tabPage0
            // 
            this.tabPage0.Controls.Add(this.txtDelChamp);
            this.tabPage0.Controls.Add(this.label2);
            this.tabPage0.Controls.Add(this.txtSepChamp);
            this.tabPage0.Controls.Add(this.label1);
            this.tabPage0.Controls.Add(this.dgv);
            this.tabPage0.Controls.Add(this.btnChgRows);
            this.tabPage0.Controls.Add(this.txtFile2);
            this.tabPage0.Controls.Add(this.chkNomChamp);
            this.tabPage0.Location = new System.Drawing.Point(4, 22);
            this.tabPage0.Name = "tabPage0";
            this.tabPage0.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage0.Size = new System.Drawing.Size(748, 335);
            this.tabPage0.TabIndex = 3;
            this.tabPage0.Text = "Test ChgRows";
            this.tabPage0.UseVisualStyleBackColor = true;
            // 
            // txtDelChamp
            // 
            this.txtDelChamp.Location = new System.Drawing.Point(319, 27);
            this.txtDelChamp.Name = "txtDelChamp";
            this.txtDelChamp.Size = new System.Drawing.Size(23, 20);
            this.txtDelChamp.TabIndex = 21;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(210, 30);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(103, 13);
            this.label2.TabIndex = 20;
            this.label2.Text = "Délimiteur de champ";
            // 
            // txtSepChamp
            // 
            this.txtSepChamp.Location = new System.Drawing.Point(121, 27);
            this.txtSepChamp.Name = "txtSepChamp";
            this.txtSepChamp.Size = new System.Drawing.Size(23, 20);
            this.txtSepChamp.TabIndex = 19;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(109, 13);
            this.label1.TabIndex = 18;
            this.label1.Text = "Séparateur de champ";
            // 
            // dgv
            // 
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Location = new System.Drawing.Point(6, 53);
            this.dgv.Name = "dgv";
            this.dgv.RowHeadersWidth = 51;
            this.dgv.Size = new System.Drawing.Size(736, 276);
            this.dgv.TabIndex = 17;
            // 
            // btnChgRows
            // 
            this.btnChgRows.Location = new System.Drawing.Point(630, 5);
            this.btnChgRows.Margin = new System.Windows.Forms.Padding(2);
            this.btnChgRows.Name = "btnChgRows";
            this.btnChgRows.Size = new System.Drawing.Size(113, 24);
            this.btnChgRows.TabIndex = 16;
            this.btnChgRows.Text = "Chargement table";
            this.btnChgRows.UseVisualStyleBackColor = true;
            this.btnChgRows.Click += new System.EventHandler(this.btn_click);
            // 
            // txtFile2
            // 
            this.txtFile2.Location = new System.Drawing.Point(5, 5);
            this.txtFile2.Margin = new System.Windows.Forms.Padding(2);
            this.txtFile2.Name = "txtFile2";
            this.txtFile2.Size = new System.Drawing.Size(203, 20);
            this.txtFile2.TabIndex = 15;
            this.txtFile2.Text = ".\\exemple.csv";
            // 
            // chkNomChamp
            // 
            this.chkNomChamp.AutoSize = true;
            this.chkNomChamp.Location = new System.Drawing.Point(213, 7);
            this.chkNomChamp.Name = "chkNomChamp";
            this.chkNomChamp.Size = new System.Drawing.Size(260, 17);
            this.chkNomChamp.TabIndex = 0;
            this.chkNomChamp.Text = "Nom des colonnes dans le premier enregistrement";
            this.chkNomChamp.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.txtXML2);
            this.tabPage3.Controls.Add(this.txtFicData2);
            this.tabPage3.Controls.Add(this.btnImport);
            this.tabPage3.Controls.Add(this.btnExport);
            this.tabPage3.Controls.Add(this.tabControl2);
            this.tabPage3.Controls.Add(this.label3);
            this.tabPage3.Controls.Add(this.txtXML);
            this.tabPage3.Controls.Add(this.txtFicData);
            this.tabPage3.Controls.Add(this.label4);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(748, 335);
            this.tabPage3.TabIndex = 4;
            this.tabPage3.Text = "Test Import/Export";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // txtXML2
            // 
            this.txtXML2.Location = new System.Drawing.Point(520, 34);
            this.txtXML2.Margin = new System.Windows.Forms.Padding(2);
            this.txtXML2.Name = "txtXML2";
            this.txtXML2.Size = new System.Drawing.Size(203, 20);
            this.txtXML2.TabIndex = 25;
            // 
            // txtFicData2
            // 
            this.txtFicData2.Location = new System.Drawing.Point(520, 6);
            this.txtFicData2.Margin = new System.Windows.Forms.Padding(2);
            this.txtFicData2.Name = "txtFicData2";
            this.txtFicData2.Size = new System.Drawing.Size(203, 20);
            this.txtFicData2.TabIndex = 29;
            // 
            // btnImport
            // 
            this.btnImport.Location = new System.Drawing.Point(336, 30);
            this.btnImport.Margin = new System.Windows.Forms.Padding(2);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(162, 24);
            this.btnImport.TabIndex = 27;
            this.btnImport.Text = "Fichier->DataSet -> Fichier";
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btn_click);
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(336, 2);
            this.btnExport.Margin = new System.Windows.Forms.Padding(2);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(162, 24);
            this.btnExport.TabIndex = 20;
            this.btnExport.Text = "Fichier->DataSet";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btn_click);
            // 
            // tabControl2
            // 
            this.tabControl2.Controls.Add(this.tabXML);
            this.tabControl2.Controls.Add(this.tabExport);
            this.tabControl2.Location = new System.Drawing.Point(11, 79);
            this.tabControl2.Name = "tabControl2";
            this.tabControl2.SelectedIndex = 0;
            this.tabControl2.Size = new System.Drawing.Size(731, 250);
            this.tabControl2.TabIndex = 26;
            // 
            // tabXML
            // 
            this.tabXML.Controls.Add(this.rXML);
            this.tabXML.Location = new System.Drawing.Point(4, 22);
            this.tabXML.Name = "tabXML";
            this.tabXML.Padding = new System.Windows.Forms.Padding(3);
            this.tabXML.Size = new System.Drawing.Size(723, 224);
            this.tabXML.TabIndex = 0;
            this.tabXML.Text = "XML";
            this.tabXML.UseVisualStyleBackColor = true;
            // 
            // rXML
            // 
            this.rXML.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rXML.Location = new System.Drawing.Point(4, 6);
            this.rXML.Name = "rXML";
            this.rXML.Size = new System.Drawing.Size(713, 212);
            this.rXML.TabIndex = 24;
            this.rXML.Text = "<! ou regle de conversion  -->";
            // 
            // tabExport
            // 
            this.tabExport.Controls.Add(this.label5);
            this.tabExport.Controls.Add(this.lstExport);
            this.tabExport.Controls.Add(this.dgvExport);
            this.tabExport.Location = new System.Drawing.Point(4, 22);
            this.tabExport.Name = "tabExport";
            this.tabExport.Padding = new System.Windows.Forms.Padding(3);
            this.tabExport.Size = new System.Drawing.Size(723, 224);
            this.tabExport.TabIndex = 1;
            this.tabExport.Text = "Fichier->DataSet";
            this.tabExport.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(7, 7);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(132, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "Choix de la table à afficher";
            // 
            // lstExport
            // 
            this.lstExport.FormattingEnabled = true;
            this.lstExport.Location = new System.Drawing.Point(145, 6);
            this.lstExport.Name = "lstExport";
            this.lstExport.Size = new System.Drawing.Size(154, 30);
            this.lstExport.TabIndex = 1;
            this.lstExport.SelectedIndexChanged += new System.EventHandler(this.btn_click);
            // 
            // dgvExport
            // 
            this.dgvExport.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvExport.Location = new System.Drawing.Point(6, 39);
            this.dgvExport.Name = "dgvExport";
            this.dgvExport.RowHeadersWidth = 51;
            this.dgvExport.Size = new System.Drawing.Size(711, 179);
            this.dgvExport.TabIndex = 0;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 36);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(63, 13);
            this.label3.TabIndex = 25;
            this.label3.Text = "Fichier XML";
            // 
            // txtXML
            // 
            this.txtXML.Location = new System.Drawing.Point(111, 33);
            this.txtXML.Margin = new System.Windows.Forms.Padding(2);
            this.txtXML.Name = "txtXML";
            this.txtXML.Size = new System.Drawing.Size(203, 20);
            this.txtXML.TabIndex = 24;
            this.txtXML.Text = ".\\Test1.xml";
            // 
            // txtFicData
            // 
            this.txtFicData.Location = new System.Drawing.Point(111, 6);
            this.txtFicData.Margin = new System.Windows.Forms.Padding(2);
            this.txtFicData.Name = "txtFicData";
            this.txtFicData.Size = new System.Drawing.Size(203, 20);
            this.txtFicData.TabIndex = 22;
            this.txtFicData.Text = ".\\Test1.csv";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 9);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(97, 13);
            this.label4.TabIndex = 21;
            this.label4.Text = "Fichier de données";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(846, 380);
            this.Controls.Add(this.tabControl1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "Test llt.FileIO";
            this.tabControl1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage0.ResumeLayout(false);
            this.tabPage0.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.tabControl2.ResumeLayout(false);
            this.tabXML.ResumeLayout(false);
            this.tabExport.ResumeLayout(false);
            this.tabExport.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvExport)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.RichTextBox log;
        private System.Windows.Forms.Button btnSepEnr;
        private System.Windows.Forms.Button btnLecEcrTxt;
        private System.Windows.Forms.Button btnLecTxt;
        private System.Windows.Forms.Button btnLecEcr;
        private System.Windows.Forms.TextBox txtFileW;
        private System.Windows.Forms.RichTextBox contenu;
        private System.Windows.Forms.CheckBox chkAsync;
        private System.Windows.Forms.TextBox txtFile;
        private System.Windows.Forms.Button btnLec;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Button btnTestFTP2;
        private System.Windows.Forms.Button btnTestFTP;
        private System.Windows.Forms.TabPage tabPage0;
        private System.Windows.Forms.TextBox txtFile2;
        private System.Windows.Forms.CheckBox chkNomChamp;
        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.Button btnChgRows;
        private System.Windows.Forms.TextBox txtDelChamp;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtSepChamp;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtFicData;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtXML;
        private System.Windows.Forms.TabControl tabControl2;
        private System.Windows.Forms.TabPage tabXML;
        private System.Windows.Forms.RichTextBox rXML;
        private System.Windows.Forms.TabPage tabExport;
        private System.Windows.Forms.ListBox lstExport;
        private System.Windows.Forms.DataGridView dgvExport;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.TextBox txtFicData2;
        private System.Windows.Forms.TextBox txtXML2;
        private System.Windows.Forms.TextBox txtFTPMotDePasse;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtFTPUtilisateur;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtFTPServeur;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox chkFTPKeepAlive;
        private System.Windows.Forms.TextBox txtFTPDistant;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtFTPDownload;
        private System.Windows.Forms.TextBox txtFTPUpload;
        private System.Windows.Forms.TextBox txtFTPLocal;
        private System.Windows.Forms.Label label10;
    }
}

