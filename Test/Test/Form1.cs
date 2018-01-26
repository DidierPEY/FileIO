using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using llt.FileIO;

namespace llt.Test
{
    public partial class Form1 : Form
    {
        int nbl = 0; // Nombre de lignes lues
        int nbw = 0; // Nombre de lignes écrites
        string iobuffer; // Le contenu total de la lecture non formatée
        System.Collections.Generic.List<string> lignes; // Le contenu total de la lecture découpé en enregistrement
        BasicFileIO bfio1 = null; // Accès au fichier en mode Byte
        BasicFileIO bfio2 = null;
        TextFileIO tfio1 = null; // Accès au fichier en mode Text
        TextFileIO tfio2 = null;
        byte[] wbuffer; // Le buffer utilisé pour l'écriture en mode Byte
        System.Collections.Generic.List<TextFileIO._ENREG> wenrs; // Le tableau des enregistrements utilisés pour l'écriture en mode Text
        long woffset;
        bool ecrfile = false; // Si vrai, il faut écrire le contenu de la lecture dans un fichier

        // Les délegues utilisés pour renseignés contenu en fonction
        // du type de paramètre envoyé.
        delegate void dlgassContenu(string text);
        dlgassContenu mthassContenu;
        delegate void dlg2assContenu(string[] lignes);
        dlg2assContenu mth2assContenu;

        // Le délégué utilisé pour remplier le log
        delegate void dlgassLog(string text);
        dlgassLog mthassLog;

        public Form1()
        {
            InitializeComponent();
            mthassContenu = new dlgassContenu(assContenu);
            mth2assContenu = new dlg2assContenu(assContenu);
            mthassLog = new dlgassLog(assLog);
        }

        private void btn_click(object sender, EventArgs e)
        {
            try
            {
                if (sender.Equals(btnLec))
                {
                    log.Lines = new string[] { "" };
                    contenu.Text = "";
                    System.Threading.ThreadPool.QueueUserWorkItem(exeBtnLec);
                }
                else if (sender.Equals(btnLecEcr))
                {
                    log.Lines = new string[] { "" };
                    iobuffer = "";
                    System.Threading.ThreadPool.QueueUserWorkItem(exeBtnLecEcr);
                }
                else if (sender.Equals(btnLecTxt) || sender.Equals(btnLecEcrTxt))
                {
                    log.Lines = new string[] { "" };
                    contenu.Text = "";
                    if (sender.Equals(btnLecTxt))
                        ecrfile = false;
                    else
                        ecrfile = true;
                    System.Threading.ThreadPool.QueueUserWorkItem(exeBtnLecText);
                }
                else if (sender.Equals(btnSepEnr))
                {
                    string enrsep = FileIO.TextFileIO.GetEnrSepText(txtFile.Text, Encoding.Default);
                    switch (enrsep)
                    {
                        case "\r\n":
                            MessageBox.Show("Séparateur enregistrement 'cr/lf'");
                            break;
                        case "\r":
                            MessageBox.Show("Séparateur enregistrement 'cr'");
                            break;
                        case "\n":
                            MessageBox.Show("Séparateur enregistrement 'lf'");
                            break;
                        default:
                            MessageBox.Show("Séparateur inconnu");
                            break;
                    }
                }
                else if (sender.Equals(btnTestFTP))
                {
                    //llt.FileIO.BasicFTP bftp = new BasicFTP("213.251.149.243", "llt34", "net-llt2648",true, "/update", "c:\\tmp\\test");
                    llt.FileIO.BasicFTP bftp = new BasicFTP("ftp.dilicom.net", "55916912", "TECH0402", false, "/pub/O", "c:\\tmp\\test");
                    bftp.OWRFichierCopie = true;
                    bftp.DELFichierSource = BasicFTP._DELFICHIERSOURCE.sicopie;
                    bftp.CopyPathFiles(false, "[0-9]");
                    //bftp.DELFichierSource = BasicFTP._DELFICHIERSOURCE.non;
                    //bftp.CopyFile(false, "02938087");
                    //bftp.DelFile(false, "02938087");
                }
                else if (sender.Equals(btnTestFTP2))
                {
                    //llt.FileIO.BasicFTP bftp = new BasicFTP("ftp.dilicom.net", "55916912", "TECH0402",false, "/pub/O", "c:\\tmp\\test");
                    //llt.FileIO.BasicFTP bftp = new BasicFTP("213.251.149.243", "llt34", "net-llt2648",true, "/update", "c:\\tmp\\test");
                    //bftp.OWRFichierCopie = true;
                    //bftp.DELFichierSource = BasicFTP._DELFICHIERSOURCE.non;
                    //bftp.CopyPathFiles(true, "[0-9]");
                    llt.FileIO.BasicFTP bftp = new BasicFTP("213.251.149.243", "interdacta", "zaib8IeS", true);
                    bftp.ServeurPath = "/CATALOGUE 2016/TELECHARGEMENTS EFFECTUES";
                    string[] fichiers = bftp.PathFiles(false, "['k'-*].['']");
                    //string[] fichiers = bftp.PathFiles(false, "['k'-*].['jpg']");
                    foreach (string photo in fichiers)
                    {
                        bftp.LocalPath = "C:\\Developpement\\Site Internet\\NetLorPhotos";
                        if (!bftp.ExistFile(true, photo))
                        {
                            bftp.LocalPath = "C:\\Developpement\\Site Internet\\NetLorPhotosNew";
                            bftp.CopyFile(false, photo);
                        }
                    }
                }
                else if (sender.Equals(btnChgRows))
                {
                    DataTable dt = new DataTable();
                    try
                    {
                        string finenr = TextFileIO.GetEnrSepText(txtFile2.Text, Encoding.Default);
                        TextFileIO.ChgRows(txtFile2.Text, Encoding.Default, finenr,
                            txtSepChamp.Text.Equals("") ? ' ' : txtSepChamp.Text[0],
                            txtDelChamp.Text.Equals("") ? ' ' : txtDelChamp.Text[0],
                            dt, chkNomChamp.Checked);
                        dgv.DataSource = dt;
                        /*
                        // Test WriteRows
                        TextFileIO.WriteRows(txtFile2.Text + ".dbg", finenr,
                            txtSepChamp.Text.Equals("") ? ' ' : txtSepChamp.Text[0],
                            txtDelChamp.Text.Equals("") ? ' ' : txtDelChamp.Text[0],
                            dt, chkNomChamp.Checked);
                         */
                    }
                    catch (System.Exception eh)
                    {
                        string msg = eh.Message;
                        System.Exception innererr = eh.InnerException;
                        while (innererr != null)
                        {
                            msg = msg + "\r\n (inner)Source  : " + innererr.Source +
                                "\r\n (inner)Message : " + innererr.Message;
                            innererr = innererr.InnerException;
                        }
                        MessageBox.Show(msg, eh.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                    }
                }
                else if (sender.Equals(btnExport))
                {
                    // Lancement de l'exportation
                    FileIO.ImportExport.Convert cv;
                    if (!txtXML.Text.Equals(""))
                        cv = new llt.FileIO.ImportExport.Convert(FileIO.ImportExport.Convert.TypeSourceXMLEnum.FichierXML, txtXML.Text);
                    else
                        cv = new llt.FileIO.ImportExport.Convert(FileIO.ImportExport.Convert.TypeSourceXMLEnum.ChaineXML, rXML.Text);
                    // Le résultat
                    DataSet h = null;
                    if (txtFicData.Text.Equals(""))
                        h = cv.ExportTables();
                    else
                        h = cv.ExportTables(txtFicData.Text);
                    // On consigne le résultat
                    if (h == null || h.Tables.Count == 0)
                    {
                        lstExport.Items.Clear();
                        dgvExport.DataSource = null;
                    }
                    else
                    {
                        foreach (DataTable dt in h.Tables)
                        {
                            lstExport.Items.Add(dt.TableName);
                        }
                        dgvExport.DataSource = h;
                        lstExport.SelectedIndex = 0;
                        dgvExport.DataMember = lstExport.Items[0].ToString();
                    }
                    tabControl2.SelectTab(tabExport.Name);
                }
                else if (sender.Equals(btnImport))
                {
                    // On lance d'abord l'exportation
                    btn_click(btnExport, null);
                    // Si traitement non effectué on sort.
                    DataSet h = dgvExport.DataSource as DataSet;
                    if (h == null) return;
                    // Test valeur null
                    h.Tables[0].Rows[0][2] = System.DBNull.Value;
                    // Lancement de l'exportation
                    FileIO.ImportExport.Convert cv;            
                    if (!txtXML2.Text.Equals(""))
                        cv = new llt.FileIO.ImportExport.Convert(FileIO.ImportExport.Convert.TypeSourceXMLEnum.FichierXML, txtXML2.Text);
                    else
                        cv = new llt.FileIO.ImportExport.Convert(FileIO.ImportExport.Convert.TypeSourceXMLEnum.ChaineXML, rXML.Text);
                    if (txtFicData2.Text.Equals(""))
                        cv.ImportFichier(h);
                    else
                        cv.ImportFichier(h, false,txtFicData2.Text);
                }
                else if (sender.Equals(lstExport))
                {
                    dgvExport.DataMember = lstExport.Items[lstExport.SelectedIndex].ToString();
                }
            }
            catch (System.Exception eh)
            {
                System.Windows.Forms.MessageBox.Show(eh.Message);
            }
        }

        // Exécution en mode asynchrone de BtnLec.
        // REMARQUE : stateinfo n'est pas utilisé.
        void exeBtnLec(object stateinfo)
        {
            BasicFileIO bfio = null;
            iobuffer = "";
            try
            {
                bfio = new BasicFileIO(txtFile.Text, chkAsync.Checked);
                bfio.BasicFileIOEvent += new BasicFileIOEventHandler(bfio_BasicFileIOEvent);
                // Lancement de la lecture de tout le fichier.
                nbl = 0;
                ecrfile = false;
                bfio.ReadFileSeq(10240);
            }
            catch (FileIOError eh)
            {
                string msg = eh.Message;
                System.Exception innererr = eh.InnerException;
                while (innererr != null)
                {
                    msg = msg + "\r\n (inner)Source  : " + innererr.Source +
                        "\r\n (inner)Message : " + innererr.Message;
                    innererr = innererr.InnerException;
                }
                MessageBox.Show(msg, eh.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (System.Exception eh)
            {
                string msg = eh.Message;
                System.Exception innererr = eh.InnerException;
                while (innererr != null)
                {
                    msg = msg + "\r\n (inner)Source  : " + innererr.Source +
                        "\r\n (inner)Message : " + innererr.Message;
                    innererr = innererr.InnerException;
                }
                MessageBox.Show(msg, eh.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (bfio != null) bfio.Dispose();
                assContenu(iobuffer);
            }
        }

        // Exécution en mode asynchrone de BtnLec.
        // REMARQUE : stateinfo n'est pas utilisé.
        void exeBtnLecEcr(object stateinfo)
        {
            try
            {
                bfio1 = new BasicFileIO(txtFile.Text, chkAsync.Checked);
                bfio1.BasicFileIOEvent += new BasicFileIOEventHandler(bfio_BasicFileIOEvent);
                bfio2 = new BasicFileIO(txtFileW.Text, chkAsync.Checked, true);
                bfio2.BasicFileIOEvent += new BasicFileIOEventHandler(bfio_BasicFileIOEvent);
                wbuffer = new byte[] { };
                // Lancement de la lecture de tout le fichier.
                nbl = 0;
                nbw = 0;
                woffset = 0;
                ecrfile = true;
                bfio1.ReadFileSeq(102400);
                // Si écriture et traitement synchrone, on écrit sur le fichier.
                if (!chkAsync.Checked && wbuffer != null)
                {
                    bfio2.WriteFile(wbuffer.Length, 0, wbuffer);
                }
                else
                {
                    lock (wbuffer)
                    {
                        if (wbuffer.Length > 0)
                        {
                            bfio2.WriteFile(wbuffer.Length, woffset, wbuffer);
                            wbuffer = new byte[] { };
                        }
                    }
                }
                // On attend que le dernier traitement soit effectué.
                bfio2.WaitAllIO();
            }
            catch (System.Exception eh)
            {
                string msg = eh.Message;
                System.Exception innererr = eh.InnerException;
                while (innererr != null)
                {
                    msg = msg + "\r\n (inner)Source  : " + innererr.Source +
                        "\r\n (inner)Message : " + innererr.Message;
                    innererr = innererr.InnerException;
                }
                MessageBox.Show(msg, eh.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (bfio1 != null) bfio1.Dispose();
                if (bfio2 != null) bfio2.Dispose();
                assContenu(iobuffer);
            }
        }

        // Exécution en mode asynchrone de BtnLecText et BtnLecEcrText.
        // REMARQUE : stateinfo n'est pas utilisé.
        void exeBtnLecText(object stateinfo)
        {
            if (lignes != null)
                lignes.Clear();
            else
                lignes = new List<string>();
            nbl = 0;
            nbw = 0;
            if (wenrs != null)
                wenrs.Clear();
            else
                wenrs = new List<TextFileIO._ENREG>();
            try
            {
                string finenr = TextFileIO.GetEnrSepText(txtFile.Text, Encoding.Default);
                tfio1 = new TextFileIO(txtFile.Text, Encoding.Default,finenr,' ',' ');
                tfio1.TextFileIOEvent += new TextFileIOEventHandler(tfio_TextFileIOEvent);
                if (ecrfile)
                {
                    tfio2 = new TextFileIO(txtFileW.Text, Encoding.UTF8,"\r\n",' ',' ',true);
                    tfio2.TextFileIOEvent += new TextFileIOEventHandler(tfio_TextFileIOEvent);
                }
                // Lecture de tout le fichier
                tfio1.ReadFileSeq();
                // On attend que tous les IO soient traitées.
                if (ecrfile)
                {
                    if (wenrs.Count > 0)
                    {
                        tfio2.WriteFile(wenrs);
                        wenrs.Clear();
                    }
                    tfio2.WaitAllIO();
                }
            }
            catch (System.Exception eh)
            {
                string msg = eh.Message;
                System.Exception innererr = eh.InnerException;
                while (innererr != null)
                {
                    msg = msg + "\r\n (inner)Source  : " + innererr.Source +
                        "\r\n (inner)Message : " + innererr.Message;
                    innererr = innererr.InnerException;
                }
                MessageBox.Show(msg, eh.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (tfio1 != null) tfio1.Dispose();
                if (tfio2 != null) tfio2.Dispose();
                assContenu(lignes.ToArray());
            }
        }

        // Assignation de la propriété .Text
        void assContenu(string text)
        {
            if (contenu.InvokeRequired)
            {
                contenu.BeginInvoke(mthassContenu, new Object[] { text });
            }
            else
            {
                contenu.Text = text;
            }
        }
        // Assignation de la propriété .Lines
        void assContenu(string[] lignes)
        {
            if (contenu.InvokeRequired)
            {
                contenu.BeginInvoke(mth2assContenu, new object[] { lignes });
            }
            else
            {
                contenu.Lines = lignes;
            }
        }
        // Ajoute une ligne de log
        void assLog(string ligne)
        {
            if (log.InvokeRequired)
            {
                log.BeginInvoke(mthassLog, new object[] { ligne });
            }
            else
            {
                string[] tmplog = new string[log.Lines.Length + 1];
                if (log.Lines.Length>0) log.Lines.CopyTo(tmplog, 1);
                tmplog[0] = ligne;
                log.Lines = tmplog;
            }
        }

        void tfio_TextFileIOEvent(object sender, TextFileIOEventArgs e)
        {
            if (e.TypeIOEvent.Equals(FileIO.TypeIOEventEnum.lecturefaite))
            {
                // Traitement des enregistrements envoyés
                if (e.Enregs.Count > 0)
                {
                    // Avancement
                    assLog(System.DateTime.Now.ToString() + ":" + " - lecturefaite " + (e.Enregs[0].Champs[0].Length <= 10 ? e.Enregs[0].Champs[0] : e.Enregs[0].Champs[0].Substring(0, 10)) + " ...");

                    // Bascule les enregistrement dans l'affichage.
                    foreach (llt.FileIO.TextFileIO._ENREG enr in e.Enregs)
                    {
                        lignes.Add(enr.Champs[0]);
                    }

                    // Si écriture dans le fichier
                    if (ecrfile)
                    {
                        lock (wenrs)
                        {
                            wenrs.AddRange(e.Enregs);
                        }
                    }
                }
            }
            else if (e.TypeIOEvent.Equals(FileIO.TypeIOEventEnum.lectureencours))
            {
                // Avancement
                nbl++;
                assLog(System.DateTime.Now.ToString() + ":" + nbl.ToString() + " - lectureencours");
                // Si écriture sur fichier, on en profite pour lancer le traitement.
                if (ecrfile)
                {
                    lock (wenrs)
                    {
                        if (wenrs.Count > 0)
                        {
                            tfio2.WriteFile(wenrs);
                            wenrs.Clear();
                        }
                    }
                }
            }
            else if (e.TypeIOEvent.Equals(FileIO.TypeIOEventEnum.ecritureencours))
            {
                nbw++;
                assLog(System.DateTime.Now.ToString() + ":" + nbw.ToString() + " - ecritureencours");
            }
            else if (e.TypeIOEvent.Equals(FileIO.TypeIOEventEnum.ecriturefaite))
            {
                assLog(System.DateTime.Now.ToString() + ":" + " - ecriturefaite ");
            }
        }
        void bfio_BasicFileIOEvent(object sender, BasicFileIOEventArgs e)
        {
            try
            {
                if (e.TypeIOEvent.Equals(TypeIOEventEnum.lecturefaite))
                {
                    // Avancement
                    if (!chkAsync.Checked)
                    {
                        nbl++;
                        assLog(System.DateTime.Now.ToString() + ":" + nbl.ToString() + " - lecturefaite");
                    }
                    else
                        assLog(System.DateTime.Now.ToString() + ":" + " - lecturefaite OFFSET " + e.Offset.ToString());

                    // On transforme le buffer en chaine de caractére.
                    if (e.NbIO > 0)
                    {
                        if (e.NbIO == e.IOBuffer.Length)
                            iobuffer = iobuffer + System.Text.Encoding.Default.GetString(e.IOBuffer);
                        else
                        {
                            byte[] r = new byte[e.NbIO];
                            System.Array.Copy(e.IOBuffer, r, e.NbIO);
                            iobuffer = iobuffer + System.Text.Encoding.Default.GetString(r);
                        }
                        // Si écriture.
                        if (ecrfile)
                        {
                            lock (wbuffer)
                            {
                                if (wbuffer.Length == 0)
                                {
                                    woffset = e.Offset;
                                    wbuffer = new byte[e.NbIO];
                                    System.Array.Copy(e.IOBuffer, wbuffer, e.NbIO);
                                }
                                else
                                {
                                    byte[] s = (byte[])wbuffer.Clone();
                                    wbuffer = new byte[s.Length + e.NbIO];
                                    System.Array.Copy(s, wbuffer, s.Length);
                                    System.Array.Copy(e.IOBuffer, 0, wbuffer, s.Length, e.NbIO);
                                }
                            }
                        }
                        // Pour éclencher une erreir
                        //int i = 15 / (e.NbIO-e.NbIO);
                    }
                }
                else if (e.TypeIOEvent.Equals(TypeIOEventEnum.lectureencours))
                {
                    nbl++;
                    assLog(System.DateTime.Now.ToString() + ":" + nbl.ToString() + " - lectureencours OFFSET "+e.Offset.ToString());
                    // Si écriture sur fichier, on en profite pour lancer le traitement.
                    if (ecrfile)
                    {
                        lock (wbuffer)
                        {
                            if (wbuffer.Length>0)
                            {
                                bfio2.WriteFile(wbuffer.Length, woffset, wbuffer);
                                wbuffer = new byte[] {};
                            }
                        }
                    }
                }
                else if (e.TypeIOEvent.Equals(TypeIOEventEnum.ecritureencours))
                {
                    nbw++;
                    assLog(System.DateTime.Now.ToString() + ":" + nbw.ToString() + " - ecritureencours OFFSET " + e.Offset.ToString());
                }
                else if (e.TypeIOEvent.Equals(TypeIOEventEnum.ecriturefaite))
                {
                    if (!chkAsync.Checked)
                    {
                        nbw++;
                        assLog(System.DateTime.Now.ToString() + ":" + nbw.ToString() + " - ecriturefaite");
                    }
                    else
                        assLog(System.DateTime.Now.ToString() + ":" + " - ecriturefaite OFFSET " + e.Offset.ToString());
                }
            }
            catch (System.Exception eh)
            {
                throw new FileIOError("bfio_BasicFileIOEvent","Erreur lors du traitement de l'évènement", eh);
            }
        }
    }
}
