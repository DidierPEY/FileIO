using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace llt.FileIO
{
    /// <summary>
    /// Cette classe permet de traiter un fichier texte de façon très simple.
    /// Elle permet seulement la lecture séquentielle d'un fichier ou l'écriture
    /// séquentielle d'un fichier.
    /// Il faut créer un objet par fichier.
    /// </summary>
    /// <remarks>ATTENTION : cette classe est multi-thread</remarks>
    public class TextFileIO : IDisposable
    {
        /// <summary>
        /// L'objet gérant l'accès physique au fichier.
        /// </summary>
        private llt.FileIO.BasicFileIO bfio;
        /// <summary>
        /// Le nombre d'octet pour chaque opération d'entrèe/sortie limité
        /// au minimum à 512ko
        /// </summary>
        private int nbbyteio;
        /// <summary>
        /// Contient le fragment d'un enregistrement provenant de la précédente lecture.
        /// </summary>
        private byte[] FragmentEnr;
        /// <summary>
        /// Définition du codage choisit pour transformer le buffer de byte en chaine de caractère
        /// </summary>
        private System.Text.Encoding TextCodage;

        /// <summary>
        /// Le(s) caractère(s)de fin d'enregistrement.
        /// </summary>
        private byte[] EnrSep;
        /// <summary>
        /// Le séparateur de champ. Si non renseigné, l'enregistrement est constitué
        /// d'un seul champ.
        /// </summary>
        private byte[] ChampSep;
        /// <summary>
        /// Le délimiteur de champ. Cette valeur n'est prise ne compte que dans le
        /// cas ou ChampSep est renseigné.
        /// </summary>
        private byte[] ChampDel;

        /// <summary>
        /// L'objet table utilisé pour charger les enregistrement
        /// </summary>
        private System.Data.DataTable ChgRowsDT;
        /// <summary>
        /// Indique si la première ligne du fichier doit contenir le nom des champs.
        /// </summary>
        private bool NomChamp;
        /// <summary>
        /// Indique si les champs ont été chargés d'après la première ligne de fichier.
        /// </summary>
        private bool NomChampChg;
        /// <summary>
        /// Correspondance en l'indice de la colonne dans le fichier et le nom de champ dans la table;
        /// </summary>
        private System.Collections.Generic.Dictionary<int, string> iColNomChamp;

        /// <summary>
        /// La structure pour un enregistrement
        /// </summary>
        public struct _ENREG
        {
            /// <summary>
            /// Création d'un enregistrement depuis un tableau de string
            /// </summary>
            /// <param name="champs">Le tableau des champs</param>
            public _ENREG(string[] champs)
            {
                Champs = champs;
            }
            /// <summary>
            /// La liste des champs.
            /// </summary>
            public string[] Champs;

            /// <summary>
            /// Le nombre de caractères dans l'enregistrement
            /// </summary>
            public int Length
            {
                get
                { 
                    int length=0;
                    foreach (string s in Champs) length += s.Length;
                    return length;
                }
            }
        }

        /// <summary>
        /// L'évènement indiquant l'état de l'opération d'entrée/sortie
        /// </summary>
        /// <remarks>
        /// IMPORTANT : cet évènement peut être déclenché dans un thread séparé.
        /// </remarks>
        public event TextFileIOEventHandler TextFileIOEvent;

        /// <summary>
        /// Création de l'objet pour l'accès au fichier en lecture seule. Les caratères
        /// de fin d'enregistrement utilisés sont CR/LF
        /// </summary>
        /// <param name="nomfichier">Le nom du fichier respectant les régles de nommage du système d'explotation</param>
        /// <param name="codage">Le codage à utiliser pour transcrire de byte(s) en caractère et inversement</param>
        public TextFileIO(string nomfichier, System.Text.Encoding codage)
            : this(nomfichier, codage, "\r\n", ' ', ' ')
        {
        }
        /// <summary>
        /// Création de l'objet pour l'accès au fichier en lecture seule.
        /// </summary>
        /// <param name="nomfichier">Le nom du fichier respectant les régles de nommage du système d'explotation</param>
        /// <param name="codage">Le codage à utiliser pour transcrire de byte(s) en caractère et inversement</param>
        /// <param name="finenr">Le(s) caractère(s) de fin d'enregistrement</param>
        /// <param name="champsep">Le séparateur de champ</param>
        /// <param name="champdel">Le délimitateur de champ</param>
        public TextFileIO(string nomfichier, System.Text.Encoding codage, string finenr, char champsep, char champdel)
            : this(nomfichier, codage, finenr, champsep, champdel, false)
        {
        }
        /// <summary>
        /// Création de l'objet pour l'accès au fichier. 
        /// </summary>
        /// <param name="nomfichier">Le nom du fichier respectant les régles de nommage du système d'explotation</param>
        /// <param name="codage">Le codage à utiliser pour transcrire de byte(s) en caractère et inversement</param>
        /// <param name="finenr">Le(s) caractère(s) de fin d'enregistrement</param>
        /// <param name="champsep">Le séparateur de champ</param>
        /// <param name="champdel">Le délimitateur de champ</param>
        /// <param name="lectureecriture">Si vrai, l'accès du fichier se fait en lecture/écriture</param>
        public TextFileIO(string nomfichier, System.Text.Encoding codage, string finenr, char champsep, char champdel, bool lectureecriture)
            : this(nomfichier, codage, finenr, champsep, champdel, lectureecriture, false)
        {
        }
        /// <summary>
        /// Création de l'objet pour l'accès au fichier. 
        /// </summary>
        /// <param name="nomfichier">Le nom du fichier respectant les régles de nommage du système d'explotation</param>
        /// <param name="codage">Le codage à utiliser pour transcrire de byte(s) en caractère et inversement</param>
        /// <param name="finenr">Le(s) caractère(s) de fin d'enregistrement</param>
        /// <param name="champsep">Le séparateur de champ</param>
        /// <param name="champdel">Le délimitateur de champ</param>
        /// <param name="lectureecriture">Si vrai, l'accès du fichier se fait en lecture/écriture</param>
        /// <param name="appendmode">Si vrai, les enregistrement sont ajoutés à la fin du fichier</param>
        public TextFileIO(string nomfichier, System.Text.Encoding codage, string finenr, char champsep, char champdel, bool lectureecriture, bool appendmode)
        {
            // Le caractère de fin d'enregistrement est obligatoire.
            if (finenr.Equals(""))
                throw new FileIOError("new_TextFileIO", "Il faut préciser au moins un caractère de fin d'enregistrement");
            // Si délimitateur de champs, il faut forcément un séparateur de champ
            if (!char.IsWhiteSpace(champdel) && char.IsWhiteSpace(champsep))
                throw new FileIOError("new_TextFileIO", "Si un délimitateur de champ est spécifié, il faut aussi un séparateur de champ");
            // Création de l'objet permettant traitement l'accès physique au fichier
            bfio = new BasicFileIO(nomfichier, true, lectureecriture, appendmode);
            bfio.BasicFileIOEvent += new BasicFileIOEventHandler(trtBasicFileIOEvent);
            // Initialisation du fragment si nécessaire.
            FragmentEnr = new byte[] { };
            // Mémorise le codage
            TextCodage = codage;
            // Initialisation caractère(s) fin d'enregistrement, séparateur de champ et délimiteur de champ
            EnrSep = codage.GetBytes(finenr);
            if (champsep.Equals(Char.MinValue) || Char.IsWhiteSpace(champsep))
            {
                ChampSep = new byte[] { };
                ChampDel = new byte[] { };
            }
            else
            {
                ChampSep = codage.GetBytes(new char[] { champsep });
                if (!champdel.Equals(Char.MinValue) && !Char.IsWhiteSpace(champdel))
                    ChampDel = codage.GetBytes(new char[] { champdel });
                else
                    ChampDel = new byte[] { };
            }

            // Calcul dynamique de la taille du buffer limité à 5% de la mémoire
            // allouée avec un minimum de 512ko et un maximum de 2048ko.
            long ma = System.Environment.WorkingSet;
            ma = ma / 20 / 1024;
            if (ma < 512)
                nbbyteio = 512 * 1024;
            else
            {
                if (ma > 2048)
                    nbbyteio = 2048 * 1024;
                else
                    nbbyteio = (int)ma * 1024;
            }
        }

        /// <summary>
        /// Détermine si la fin d'enregistrement est 'CR/LF' ou 'CR' ou 'LF'
        /// </summary>
        /// <param name="nomfichier">Le nom du fichier respectant les régles de nommage du système d'explotation</param>
        /// <param name="codage">Le codage à utiliser pour transcrire byte en caractère et inversement</param>
        /// <returns>Chaine vide si aucun des trois séparateurs de trouvé</returns>
        public static string GetEnrSepText(string nomfichier, System.Text.Encoding codage)
        {
            // Pour lire le fichier.
            TextFileIO tfio = null;

            // Test avec CR/LF
            try
            {
                tfio = new TextFileIO(nomfichier, codage, "\r\n", ' ', ' ');
                tfio.TextFileIOEvent += new TextFileIOEventHandler(tfio_TextFileIOEvent);
                tfio.ReadFile();
                tfio.WaitAllIO();
                return "\r\n";
            }
            catch
            {
            }
            finally
            {
                if (tfio != null) tfio.Dispose();
            }

            // Test avec CR seul
            try
            {
                tfio = new TextFileIO(nomfichier, codage, "\r", ' ', ' ');
                tfio.TextFileIOEvent += new TextFileIOEventHandler(tfio_TextFileIOEvent);
                tfio.ReadFile();
                tfio.WaitAllIO();
                return "\r";
            }
            catch
            {
            }
            finally
            {
                if (tfio != null) tfio.Dispose();
            }

            // Test avec LF seul
            try
            {
                tfio = new TextFileIO(nomfichier, codage, "\n", ' ', ' ');
                tfio.TextFileIOEvent += new TextFileIOEventHandler(tfio_TextFileIOEvent);
                tfio.ReadFile();
                tfio.WaitAllIO();
                return "\n";
            }
            catch
            {
            }
            finally
            {
                if (tfio != null) tfio.Dispose();
            }

            // Aucun séparateur trouvé.
            return "";
        }

        private static void tfio_TextFileIOEvent(object sender, TextFileIOEventArgs e)
        {
            if (e.TypeIOEvent.Equals(FileIO.TypeIOEventEnum.lecturefaite))
            {
                // Si un seul enregistrement, on sort en erreur car on suppose
                // que le séparateur n'est pas le bon
                if (e.Enregs.Count == 0) throw new FileIO.FileIOError("tfio_TextFileIO", "Séparateur d'enregistrement non trouvé");
            }
        }

        /// <summary>
        /// Chargement dans une table du contenu du fichier
        /// </summary>
        /// <param name="nomfichier">Le nom du fichier respectant les régles de nommage du système d'explotation</param>
        /// <param name="codage">Le codage à utiliser pour transcrire byte en caractère et inversement</param>
        /// <param name="finenr">Le(s) caractère(s) de fin d'enregistrement</param>
        /// <param name="champsep">Le séparateur de champ</param>
        /// <param name="champdel">Le délimitateur de champ</param>
        /// <param name="dt">L'objet DataTable sur lequel sont créés les enregistrements</param>
        /// <remarks>
        /// Les champs sont crées avec le nom 'Champx' ou 'x' est le numéro séquentiel du champ.
        /// Le premier champ est 'Champ1'.
        /// <para>
        /// Les champs peuvent être créés en amont par l'appelant. Cette solution est utile dans le cas de fichier
        /// possédant un séparateur de champ. Il seront renseignés en fonction de leur ordre chronologique dans la 
        /// collection DataTable.Columns. La collection est automatiquement complétée si le nombre de champs déclarés
        /// est inférieur au nombre de champs dans le fichier.
        /// </para>
        /// </remarks>
        public static void ChgRows(string nomfichier, System.Text.Encoding codage,
            string finenr, char champsep, char champdel, System.Data.DataTable dt)
        {
            ChgRows(nomfichier, codage, finenr, champsep, champdel, dt, false);
        }
        /// <summary>
        /// Chargement dans une table du contenu du fichier
        /// </summary>
        /// <param name="nomfichier">Le nom du fichier respectant les régles de nommage du système d'explotation</param>
        /// <param name="codage">Le codage à utiliser pour transcrire byte en caractère et inversement</param>
        /// <param name="finenr">Le(s) caractère(s) de fin d'enregistrement</param>
        /// <param name="champsep">Le séparateur de champ</param>
        /// <param name="champdel">Le délimitateur de champ</param>
        /// <param name="dt">L'objet DataTable sur lequel sont créés les enregistrements</param>
        /// <param name="nomchamp">Indique si la première ligne du fichier contient les noms de champ à créer dans DataTableColums</param>
        /// <remarks>
        /// Si <paramref name="nomchamp"/> est faux, se référer aux remarques de la méthode initiale.
        /// <para>
        /// Si <paramref name="nomchamp"/> est vrai, les noms de champs à créer dans <paramref name="dt"/> doivent être présent
        /// dans la première ligne du fichier. Dans le cas où aucun nom de champ n'est indiqué pour une colonne, l'information ne sera jamais traitée
        /// pour cette colonne.
        /// </para>
        /// </remarks>
        public static void ChgRows(string nomfichier, System.Text.Encoding codage,
            string finenr, char champsep, char champdel, System.Data.DataTable dt, bool nomchamp)
        {
            // La table doit être présente
            if (dt == null)
                throw new FileIOError("ChgRows", "Il faut un objet DataTable valide.");
            // Pas de colonne sir nomchamp est à vrai
            if (nomchamp && dt.Columns.Count > 0)
                throw new FileIOError("ChgRows", "La table ne doit pas contenir de colonnes dans le cas où celles-ci sont créées d'après la première ligne du fichier.");
            // Pour lire le fichier.
            TextFileIO tfio = new TextFileIO(nomfichier, codage, finenr, champsep, champdel);
            // Gestion de l'évènement spécifique
            tfio.TextFileIOEvent += new TextFileIOEventHandler(ChgRows_TextFileIOEvent);
            // Indique l'objet table à utiliser
            tfio.ChgRowsDT = dt;
            // Est-ce que le nom des colonnes est contenu dans le première ligne du fichier.
            tfio.NomChamp = nomchamp;
            tfio.NomChampChg = false;
            // Lecture de tous les enregistrements
            tfio.ReadFileSeq();
            // Fermeture du fichier
            tfio.Dispose();
        }
        /// <summary>
        /// Ecrit dans le fichier le contenu d'une table.
        /// </summary>
        /// <param name="nomfichier">Le nom du fichier respectant les régles de nommage du système d'explotation</param>
        /// <param name="finenr">Le(s) caractère(s) de fin d'enregistrement</param>
        /// <param name="champsep">Le séparateur de champ</param>
        /// <param name="champdel">Le délimitateur de champ</param>
        /// <param name="dt">L'objet DataTable utiliser pour créer le fichier</param>
        /// <param name="nomchamp">Indique si le premier enregistrement doti contenir les noms de champ</param>
        /// <remarks>Tous les champs sont convertit en chaine de caractère en utilisant la culture en cours et son TextInfo.ANSICodePage associé</remarks>
        public static void WriteRows(string nomfichier, string finenr, char champsep, char champdel, System.Data.DataTable dt, bool nomchamp)
        {
            WriteRows(nomfichier, finenr, champsep, champdel, dt, nomchamp, System.Globalization.CultureInfo.CurrentCulture);
        }
        /// <summary>
        /// Ecrit dans le fichier le contenu d'une table.
        /// </summary>
        /// <param name="nomfichier">Le nom du fichier respectant les régles de nommage du système d'explotation</param>
        /// <param name="finenr">Le(s) caractère(s) de fin d'enregistrement</param>
        /// <param name="champsep">Le séparateur de champ</param>
        /// <param name="champdel">Le délimitateur de champ</param>
        /// <param name="dt">L'objet DataTable utiliser pour créer le fichier</param>
        /// <param name="nomchamp">Indique si le premier enregistrement doti contenir les noms de champ</param>
        /// <param name="culture">Culture utilisée et son TextInfo.ANSICodePage associé pour effectuer la conversion des champs en chaine de caractères</param>
        public static void WriteRows(string nomfichier, string finenr, char champsep, char champdel, System.Data.DataTable dt, bool nomchamp, System.Globalization.CultureInfo culture)
        {
            WriteRows(nomfichier, finenr, champsep, champdel, dt, nomchamp, culture, false);
        }
        /// <summary>
        /// Ecrit dans le fichier le contenu d'une table.
        /// </summary>
        /// <param name="nomfichier">Le nom du fichier respectant les régles de nommage du système d'explotation</param>
        /// <param name="finenr">Le(s) caractère(s) de fin d'enregistrement</param>
        /// <param name="champsep">Le séparateur de champ</param>
        /// <param name="champdel">Le délimitateur de champ</param>
        /// <param name="dt">L'objet DataTable utiliser pour créer le fichier</param>
        /// <param name="nomchamp">Indique si le premier enregistrement doti contenir les noms de champ</param>
        /// <param name="culture">Culture utilisée et son TextInfo.ANSICodePage associé pour effectué la conversion des champs en chaine de caractères</param>
        /// <param name="appendmode">Ajoute les enregistrements à la fin du fichier</param>
        public static void WriteRows(string nomfichier, string finenr, char champsep, char champdel, System.Data.DataTable dt, bool nomchamp, System.Globalization.CultureInfo culture, bool appendmode)
        {
            WriteRows(nomfichier, finenr, champsep, champdel, dt, nomchamp, culture, appendmode, System.Text.Encoding.GetEncoding(culture.TextInfo.ANSICodePage));
        }
        /// <summary>
        /// Ecrit dans le fichier le contenu d'une table.
        /// </summary>
        /// <param name="nomfichier">Le nom du fichier respectant les régles de nommage du système d'explotation</param>
        /// <param name="finenr">Le(s) caractère(s) de fin d'enregistrement</param>
        /// <param name="champsep">Le séparateur de champ</param>
        /// <param name="champdel">Le délimitateur de champ</param>
        /// <param name="dt">L'objet DataTable utiliser pour créer le fichier</param>
        /// <param name="nomchamp">Indique si le premier enregistrement doti contenir les noms de champ</param>
        /// <param name="culture">Culture utilisée pour effectué la conversion des champs en chaine de caractères</param>
        /// <param name="appendmode">Ajoute les enregistrements à la fin du fichier</param>
        /// <param name="codage">Le codage spécifique à utiliser pour le format de chaine de caractères</param>
        public static void WriteRows(string nomfichier, string finenr, char champsep, char champdel, System.Data.DataTable dt, bool nomchamp, System.Globalization.CultureInfo culture, bool appendmode, System.Text.Encoding codage)
        {
            // La table doit être présente
            if (dt == null)
                throw new FileIOError("WriteRows", "Il faut un objet DataTable valide.");
            // Pour écrite dans le fichier.
            TextFileIO tfio = new TextFileIO(nomfichier, codage, finenr, champsep, champdel, true, appendmode);
            // Lancement de l'écriture
            tfio.WriteRows(dt, nomchamp, culture);
            // Fermeture du fichier
            tfio.Dispose();
        }
        /// <summary>
        /// Chargement de la table lors de la lecture du fichier
        /// </summary>
        /// <param name="sender">L'objet TExtFileIO émetteur</param>
        /// <param name="e">Les paramètres de l'évènement</param>
        private static void ChgRows_TextFileIOEvent(object sender, TextFileIOEventArgs e)
        {
            if (e.TypeIOEvent.Equals(FileIO.TypeIOEventEnum.lecturefaite))
            {
                // On convertit le sender en tant que TextFileIO
                TextFileIO tfio = sender as TextFileIO;
                // Si conversion échoue, on sort.
                if (tfio == null) return;

                try
                {
                    foreach (_ENREG enr in e.Enregs)
                    {
                        // Traitement dans le cas classique (pas de nom de champs dans le fichier en première ligne)
                        if (!tfio.NomChamp)
                        {
                            // Test s'il faut rajouter des champs
                            if (enr.Champs.Length > tfio.ChgRowsDT.Columns.Count) ChgRows_CreatCol(tfio.ChgRowsDT, enr);

                            // Création du nouvel enregistrement
                            System.Data.DataRow dr = tfio.ChgRowsDT.NewRow();
                            // Chargement des champs
                            for (int i = 0; i < tfio.ChgRowsDT.Columns.Count; i++)
                            {
                                if (i < enr.Champs.Length)
                                    dr[i] = enr.Champs[i];
                                else
                                    dr[i] = "";
                            }
                            // On ajoute l'enregistrement.
                            tfio.ChgRowsDT.Rows.Add(dr);
                        }
                        // Traitement dans le cas ou les noms de champs sont présent dans la première ligne du fichier
                        else
                        {
                            // Chargement des nom de colonnes.
                            if (!tfio.NomChampChg)
                            {
                                // Création du tableau de correspondance.
                                tfio.iColNomChamp = new Dictionary<int, string>();
                                // Chargement des noms de champs.
                                for (int icol = 0; icol < enr.Champs.Length; icol++)
                                {
                                    if (!enr.Champs[icol].ToString().Trim().Equals(""))
                                    {
                                        tfio.iColNomChamp.Add(icol, enr.Champs[icol].ToString().Trim());
                                        System.Data.DataColumn dc = new System.Data.DataColumn();
                                        dc.ColumnName = enr.Champs[icol].ToString().Trim();
                                        dc.DataType = System.Type.GetType("System.String");
                                        dc.AllowDBNull = true;
                                        tfio.ChgRowsDT.Columns.Add(dc);
                                    }
                                }
                                // Si aucun nom de colonne, arrêt du traitement.
                                if (tfio.iColNomChamp.Count == 0)
                                    throw new FileIOError("ChgRows_TextFileIOEvent", "Aucun nom de champ dans le premier enregistrement du fichier");
                                // Chargement des noms de champs effectué.
                                tfio.NomChampChg = true;
                            }
                            else
                            {
                                // Création du nouvel enregistrement
                                System.Data.DataRow dr = tfio.ChgRowsDT.NewRow();
                                // Chargement des valeurs.
                                for (int icol = 0; icol < enr.Champs.Length; icol++)
                                {
                                    if (tfio.iColNomChamp.ContainsKey(icol))
                                        dr[tfio.iColNomChamp[icol]] = enr.Champs[icol];
                                }
                                // On ajoute l'enregistrement.
                                tfio.ChgRowsDT.Rows.Add(dr);
                            }
                        }
                    }
                }
                catch (FileIOError)
                {
                    // On arrête tous les traitements.
                    tfio.bfio.StopAllIO();
                    // On toute l'erreur telle quelle.
                    throw;
                }
                catch (System.Exception eh)
                {
                    // On arrête tous les traitements.
                    tfio.bfio.StopAllIO();
                    // On renvoie le message d'erreur                      
                    throw new FileIOError("ChgRows_TextFileIOEvent", "Erreur lors de la création des enregistrements", eh);
                }
            }
        }

        /// <summary>
        /// Création des colonnes dans la table lors du chargement des enregistrements
        /// </summary>
        /// <param name="dt">L'objet table à utiliser</param>
        /// <param name="enr">L'enregistrement à utiliser</param>
        private static void ChgRows_CreatCol(System.Data.DataTable dt, _ENREG enr)
        {
            // Si nombre de colonnes supérieur ou égal.
            if (dt.Columns.Count >= enr.Champs.Length) return;
            // Création des colonnes.
            for (int i = dt.Columns.Count; i < enr.Champs.Length; i++)
            {
                System.Data.DataColumn dc = new System.Data.DataColumn();
                dc.ColumnName = "Champ" + (i + 1).ToString();
                dc.DataType = System.Type.GetType("System.String");
                dc.AllowDBNull = true;
                dt.Columns.Add(dc);
            }
        }

        /// <summary>
        /// Lecture du fichier depuis la position en cours dans le fichier.
        /// </summary>
        /// <returns>Faux si la lecture n'a pu être effectuée ou si fin de fichier</returns>
        public bool ReadFile()
        {
            return bfio.ReadFile(nbbyteio);
        }

        /// <summary>
        /// Lecture séquentiel du fichier.
        /// </summary>
        /// <remarks>
        /// La lecture démarre FORCEMENT à partir du début du fichier.
        /// Cette méthode attend automatiquement que tous les lectures soient terminées.
        /// </remarks>
        public void ReadFileSeq()
        {
            bfio.ReadFileSeq(nbbyteio);
        }

        /// <summary>
        /// Ecrit dans le fichier les enregistrements envoyés en paramètre.
        /// </summary>
        /// <param name="enrs">Liste des enregsitrements</param>
        /// <returns>Vrai si l'écriture est correctement effectuée</returns>
        public bool WriteFile(System.Collections.Generic.List<_ENREG> enrs)
        {
            // Si aucun enregistrement, on sort.
            if (enrs == null) return false;
            if (enrs.Count == 0) return true;

            // Le buffer de travail.
            System.Collections.Generic.List<byte> b = new List<byte>();
            // Le buffer envoyé d'entrée/sortie
            byte[] buffer = new byte[nbbyteio];

            // Traitement de chaque enregsitrement
            foreach (_ENREG enr in enrs)
            {
                // Si aucun séparateur de champ, le premier champ contient l'ensemble
                // de l'enregistrement
                if (ChampSep.Length == 0)
                {
                    b.AddRange(TextCodage.GetBytes(enr.Champs[0]));
                }
                else
                {
                    // Ajout du separateur de champ nécessaire.
                    bool addchampsep = false;
                    // Pour chaque champs
                    foreach (string s in enr.Champs)
                    {
                        // Si pas de délimitateur de champ, la chaine de caractère ne doit pas contenir de sepérateur de champ
                        if (ChampDel.Length == 0)
                        {
                            if (s.IndexOf(TextCodage.GetString(ChampSep)) >= 0)
                                throw new FileIOError("WriteFile", "'" + s + "' contient un séparateur de champ '" + TextCodage.GetString(ChampSep) + "'");
                        }
                        else
                        {
                            // La chaine de caratère ne doit pas contenir le couple ChampDel+ChampSep
                            if (s.IndexOf((TextCodage.GetString(ChampDel) + TextCodage.GetString(ChampSep))) >= 0)
                                throw new FileIOError("WriteFile", "'" + s + "' contient le délimitateur de champ '" + TextCodage.GetString(ChampDel) + "' suivi du séparateur de champ '" + TextCodage.GetString(ChampSep) + "'");
                        }
                        // Ajoute le séparateur de champ (sauf sur premier champ)
                        if (addchampsep) b.AddRange(ChampSep); else addchampsep = true;
                        // Ajoute le délimitateur de champs
                        if (ChampDel.Length > 0) b.AddRange(ChampDel);
                        // Ajoute le contenu du champ en lui-même
                        if (s.Length > 0) b.AddRange(TextCodage.GetBytes(s));
                        // Ajoute le délimitateur de champs
                        if (ChampDel.Length > 0) b.AddRange(ChampDel);
                    }
                }
                // Ajout séparateur d'enregistrement
                b.AddRange(EnrSep);
                // Si la taille du buffer de travail est supérieur ou égal à la
                // taille maxi autorisée, on lance une écriture.
                if (b.Count >= nbbyteio)
                {
                    b.CopyTo(0, buffer, 0, nbbyteio);
                    if (!bfio.WriteFile(nbbyteio, buffer)) return false;
                    b.RemoveRange(0, nbbyteio);
                }
            }
            // En final, s'il reste des enregistrements à mettre à jour, 
            // on lance la dernière écriture.
            if (b.Count > 0)
            {
                if (!bfio.WriteFile(b.Count, b.ToArray())) return false;
            }

            // Ecriture effectuée.
            return true;
        }

        /// <summary>
        /// Ecrit dans le fichier le contenu d'une table.
        /// </summary>
        /// <param name="dt">La table contenant les enregistrements</param>
        /// <param name="NomChamp">Indique si le premier enregistrement doti contenir les noms de champ</param>
        /// <remarks>Tous les champs sont convertit en chaine de caractère en utilisant la culture en cours</remarks>
        public void WriteRows(System.Data.DataTable dt, bool NomChamp)
        {
            WriteRows(dt, NomChamp, System.Globalization.CultureInfo.CurrentCulture);
        }
        /// <summary>
        /// Ecrit dans le fichier le contenu d'une table.
        /// </summary>
        /// <param name="dt">La table contenant les enregistrements</param>
        /// <param name="NomChamp">Indique si le premier enregistrement doti contenir les noms de champ</param>
        /// <param name="culture">Culture utilisée pour effectué la conversion des champs e chaine de caractères</param>
        public void WriteRows(System.Data.DataTable dt, bool NomChamp, System.Globalization.CultureInfo culture)
        {
            // Si aucun enregistrement à traiter, on sort.
            if (dt == null) return;
            if (dt.Rows.Count == 0) return;
            // Il faut une culture pour la conversion en chaine de caractère.
            if (culture == null) culture = System.Globalization.CultureInfo.CurrentCulture;

            // Création du buffer.
            try
            {
                System.Collections.Generic.List<_ENREG> enrs;
                if (NomChamp)
                    enrs = new List<_ENREG>(dt.Rows.Count + 1);
                else
                    enrs = new List<_ENREG>(dt.Rows.Count);
                int enrsLength = 0;

                // Le premier enregistrement, le nom des champs
                string[] nc = new string[dt.Columns.Count];
                if (NomChamp)
                {
                    for (int ic = 0; ic < dt.Columns.Count; ic++)
                        nc[ic] = dt.Columns[ic].ColumnName;
                    enrs.Add(new _ENREG((string[])nc.Clone()));
                    enrsLength += enrs[0].Length +
                        (ChampSep.Length > 0 && enrs[0].Champs.Length > 2 ? enrs[0].Champs.Length - 1 : 0) +
                        (ChampDel.Length > 0 ? enrs[0].Champs.Length * 2 : 0);
                }

                // Traitement de l'ensemble des enregistrements.
                for (int ir = 0; ir < dt.Rows.Count; ir++)
                {
                    for (int ic = 0; ic < nc.Length; ic++)
                    {
                        if (dt.Rows[ir][ic] is System.DBNull || dt.Rows[ir][ic] == null)
                            nc[ic] = "";
                        else
                            nc[ic] = System.Convert.ToString(dt.Rows[ir][ic], culture);
                    }
                    enrs.Add(new _ENREG((string[])nc.Clone()));
                    enrsLength += enrs[enrs.Count - 1].Length +
                        (ChampSep.Length > 0 && enrs[enrs.Count - 1].Champs.Length > 2 ? enrs[enrs.Count - 1].Champs.Length - 1 : 0) +
                        (ChampDel.Length > 0 ? enrs[enrs.Count - 1].Champs.Length * 2 : 0);

                    // Si on approche la taille du buffer, on lance une écriture
                    // NOTA : la valeur de 90% est empirique
                    if ((enrsLength * TextCodage.GetByteCount(" ") + EnrSep.Length * enrs.Count) > (nbbyteio * .9))
                    {
                        WriteFile(enrs);
                        enrs.Clear();
                        enrsLength = 0;
                    }
                }

                // Ecriture dans le fichier.
                WriteFile(enrs);
            }
            finally
            {
                WaitAllIO();
            }
        }
        /// <summary>
        /// Attend que tous les entrèes/sorties soient effectuées
        /// </summary>
        /// <remarks>
        /// Il est INDISPENSABLE d'appeller cette méthode pour un traitement stable dans
        /// le cas de l'utilisation de ReadFile et WriteFile. Cet appel doit se faire
        /// sur la DERNIERE opération de lecture ou d'écriture.
        /// Il est INUTILE d'appeler cette méthode à la suite de ReadFileSeq().
        /// </remarks>
        public void WaitAllIO()
        {
            if (bfio != null) bfio.WaitAllIO();
        }

        /// <summary>
        /// Déclenchement de l'évènement TextFileIOEvent
        /// </summary>
        /// <param name="tfioargs">L'argument utilisé pour déclencher l'évènement</param>
        /// <remarks>
        /// </remarks>
        protected void OnTextFileIOEvent(TextFileIOEventArgs tfioargs)
        {
            if (TextFileIOEvent != null) TextFileIOEvent(this, tfioargs);
        }

        /// <summary>
        /// Traitement des différents évènements générés par BasicFileIO
        /// </summary>
        /// <param name="sender">L'objet à l'origine de l'évènement</param>
        /// <param name="e">Les paramètres de l'évènement</param>
        private void trtBasicFileIOEvent(object sender, llt.FileIO.BasicFileIOEventArgs e)
        {
            if (e.TypeIOEvent.Equals(TypeIOEventEnum.ecritureencours))
            {
                OnTextFileIOEvent(new TextFileIOEventArgs(TypeIOEventEnum.ecritureencours));
            }
            else if (e.TypeIOEvent.Equals(TypeIOEventEnum.ecriturefaite))
            {
                OnTextFileIOEvent(new TextFileIOEventArgs(TypeIOEventEnum.ecriturefaite));
            }
            else if (e.TypeIOEvent.Equals(TypeIOEventEnum.lectureencours))
            {
                OnTextFileIOEvent(new TextFileIOEventArgs(TypeIOEventEnum.lectureencours));
            }
            else if (e.TypeIOEvent.Equals(TypeIOEventEnum.lecturefaite))
            {
                trtLectureFaite(sender, e);
            }
        }

        /// <summary>
        /// Traitement dans le cas la lecture est faite.
        /// </summary>
        /// <param name="sender">L'objet à l'origine de l'évènement</param>
        /// <param name="e">Les paramètres de l'évènement</param>
        private void trtLectureFaite(object sender, llt.FileIO.BasicFileIOEventArgs e)
        {
            // La liste des enregistrements
            System.Collections.Generic.List<_ENREG> enrs = new List<_ENREG>();

            // Si aucune lecture, on envoie le dernier fragment d'enregistrement.
            if (e.NbIO == 0)
            {
                if (FragmentEnr.Length > 0)
                {
                    // Ajoute l'enregistrement.
                    enrs.Add(new _ENREG(MiseEnFormeEnreg(TextCodage.GetString(FragmentEnr, 0, FragmentEnr.Length))));
                    OnTextFileIOEvent(new TextFileIOEventArgs(TypeIOEventEnum.lecturefaite, enrs));
                    FragmentEnr = new byte[] { };
                }
            }
            else
            {
                // Le buffer de travail
                byte[] buffer;
                // Si un fragment d'enregistrement, il faut le mettre en debut de buffer
                // de travail.
                if (FragmentEnr.Length > 0)
                {
                    buffer = new byte[FragmentEnr.Length + e.NbIO];
                    FragmentEnr.CopyTo(buffer, 0);
                    System.Array.Copy(e.IOBuffer, 0, buffer, FragmentEnr.Length, e.NbIO);
                }
                else
                {
                    buffer = new byte[e.NbIO];
                    System.Array.Copy(e.IOBuffer, 0, buffer, 0, e.NbIO);
                }
                // Position dans le buffer
                int poscrs = 0;
                // Position dans le buffer du début de l'enregistrement.
                int posdebenr = 0;
                // Indique si entre deux délimitateur de champ
                bool entrechampdel = false;
                // Traitement de l'ensemble du buffer
                for (; poscrs < buffer.Length; )
                {
                    // Si séparateur et délimitateur de champs
                    if (ChampSep.Length > 0 && ChampDel.Length > 0)
                    {
                        // Si on n'est pas entre deux délimitateurs de champs, on vérifie si on rentre dans un champ délimité
                        if (!entrechampdel)
                        {
                            if (poscrs == 0)
                                entrechampdel = FileIO.BasicFileIO.Contient(ref buffer, poscrs, ref ChampDel);
                            else if (poscrs >= ChampSep.Length)
                                entrechampdel = FileIO.BasicFileIO.Contient(ref buffer, poscrs, ref ChampDel)
                                    && FileIO.BasicFileIO.Contient(ref buffer, poscrs - ChampSep.Length, ref ChampSep);
                        }
                        else if (FileIO.BasicFileIO.Contient(ref buffer, poscrs, ref ChampDel))
                        {
                            // On sort d'un champ délimité si le délimitateur est suivi d'un sépaprateur de champ ...
                            if (poscrs + ChampSep.Length < buffer.Length)
                                entrechampdel = !FileIO.BasicFileIO.Contient(ref buffer, poscrs + ChampDel.Length, ref ChampSep);
                            if (entrechampdel && poscrs + EnrSep.Length < buffer.Length)
                                entrechampdel = !FileIO.BasicFileIO.Contient(ref buffer, poscrs + ChampDel.Length, ref EnrSep);
                        }
                    }
                    // Si on est pas entre un délimitabeur de champs, test si fin d'enregistrement
                    if (!entrechampdel)
                    {
                        if (FileIO.BasicFileIO.Contient(ref buffer, poscrs, ref EnrSep))
                        {
                            // Ajoute l'enregistrement.
                            enrs.Add(new _ENREG(MiseEnFormeEnreg(TextCodage.GetString(buffer, posdebenr, poscrs - posdebenr))));
                            // on ne retourne pas le(s) caractère(s) de fin d'enregistrement
                            poscrs = poscrs + EnrSep.Length;
                            posdebenr = poscrs;
                        }
                        else
                            poscrs++;
                    }
                    else
                        poscrs++; // on passe au caratère suivant
                }
                // Il existe un fragment d'enregistrement non traité le pointeur
                // de début d'enregistrement n'est pas identique au pointeur du buffer
                if (poscrs != posdebenr)
                {
                    FragmentEnr = new byte[buffer.Length - posdebenr];
                    System.Array.Copy(buffer, posdebenr, FragmentEnr, 0, FragmentEnr.Length);
                }
                else
                    FragmentEnr = new byte[] { };

                // On envoie les enregistrements ainsi créés
                OnTextFileIOEvent(new TextFileIOEventArgs(TypeIOEventEnum.lecturefaite, enrs));
            }
        }

        /// <summary>
        /// Effectue la mise en forme de l'enregistrement en fonction des différents
        /// paramètres
        /// </summary>
        /// <param name="enregbrut">L'enregistrement brute sous forme de chaine de caractère</param>
        /// <returns>Un tableau de chaine contenant les champs</returns>
        private string[] MiseEnFormeEnreg(string enregbrut)
        {
            // Si aucun séparateur de champ, on traite tel quel
            if (ChampSep.Length == 0)
            {
                return new string[] { enregbrut };
            }
            else
            {
                // Si aucun délimitateur de champ, on split tel quel
                if (ChampDel.Length == 0)
                    return enregbrut.Split(TextCodage.GetChars(ChampSep));
                else
                {
                    // On va créer une sous chaine pour chaque ChampDel+ChampSep
                    string cdcs = TextCodage.GetString(ChampDel) + TextCodage.GetString(ChampSep);
                    // Exécution du split
                    string[] scdcs = enregbrut.Split(new string[] { cdcs }, StringSplitOptions.None);
                    // Le délimitateur de champ n'est pas obligatoire
                    // Si la chaine ne commence pas par ChampDel, c'est qu'on est sur une série de champ sans ChampDel
                    // Sauf éventuellement me dernier champ
                    List<string> verif = new List<string>();
                    for (int iscdcs = 0; iscdcs < scdcs.Length; iscdcs++)
                    {
                        // Test si ChampDel est bien présent en début de chaine.
                        if (scdcs[iscdcs].StartsWith(TextCodage.GetString(ChampDel)))
                            verif.Add(scdcs[iscdcs]);
                        else
                        {
                            // Test si ChampSep+ChampDel présent ce qui signifie la présence d'un champ
                            // avec délimitateur de champ
                            if (scdcs[iscdcs].Contains(TextCodage.GetString(ChampSep) + TextCodage.GetString(ChampDel)))
                            {
                                // On split la chaine pour séparer le dernier champ
                                string cdcs2 = TextCodage.GetString(ChampSep) + TextCodage.GetString(ChampDel);
                                string[] scdcs2 = scdcs[iscdcs].Split(new string[] { cdcs2 }, StringSplitOptions.None);
                                // La première occurence contient les champs sans ChampDel
                                verif.AddRange(scdcs2[0].Split(TextCodage.GetChars(ChampSep)));
                                // La deuxième occurence est telle quelle
                                verif.Add(scdcs2[1]);
                            }
                            else
                                // On split seulement avec ChampSep
                                verif.AddRange(scdcs[iscdcs].Split(TextCodage.GetChars(ChampSep)));
                        }
                    }
                    // On restitue la chaine de caractère
                    scdcs = verif.ToArray();
                    // A chaque début de chaine, on enlève le ChampDel
                    for (int iscdcs = 0; iscdcs < scdcs.Length; iscdcs++)
                    {
                        // Test si ChampDel est bien présent en début de chaine.
                        if (scdcs[iscdcs].StartsWith(TextCodage.GetString(ChampDel)))
                        {
                            scdcs[iscdcs] = scdcs[iscdcs].Substring(TextCodage.GetString(ChampDel).Length);
                        }
                        // Sur la dernière occurence, on enlève le ChampDel de fin
                        if (iscdcs == (scdcs.Length - 1) && scdcs[iscdcs].EndsWith(TextCodage.GetString(ChampDel)))
                        {
                            scdcs[iscdcs] = scdcs[iscdcs].Substring(0, scdcs[iscdcs].Length - TextCodage.GetString(ChampDel).Length);
                        }
                    }
                    // Renvoie le tableau de chaine.
                    return scdcs;
                }
            }
        }

        /// <summary>
        /// Le destructeur appelé automatiquement
        /// </summary>
        ~TextFileIO()
        {
            Disposing(false);
        }
        /// <summary>
        /// Demande de nettoyage des ressources utilisées par l'objet.
        /// </summary>
        public virtual void Dispose()
        {
            Disposing(true);
        }
        /// <summary>
        /// Exécution du nettoyage des ressources utilisées par l'objet.
        /// </summary>
        /// <param name="dispose">Si vrai, le nettoyage a été demandé par programmation et non par le Garbage Collector</param>
        protected void Disposing(bool dispose)
        {
            try
            {
                if (bfio != null)
                {
                    if (dispose) bfio.Dispose();
                }
            }
            catch
            {
            }
            finally
            {
                bfio = null;
                TextFileIOEvent = null;
            }
        }
    }

    /// <summary>
    /// L'agument utilisé par l'évenement TextFileIOEvent
    /// </summary>
    /// <remarks>Attention aux remarques lièes au thread d'exécution</remarks>
    public class TextFileIOEventArgs : EventArgs
    {
        private TypeIOEventEnum typeioevent;
        /// <summary>
        /// Le type d'évènement traité
        /// </summary>
        public TypeIOEventEnum TypeIOEvent
        {
            get { return typeioevent; }
        }

        private System.Collections.Generic.List<llt.FileIO.TextFileIO._ENREG> enregs;
        /// <summary>
        /// Les enregistrements.
        /// </summary>
        public System.Collections.Generic.List<llt.FileIO.TextFileIO._ENREG> Enregs
        {
            get { return enregs; }
        }

        /// <summary>
        /// Création de la liste d'arguments dans le cas d'une opération en cours.
        /// </summary>
        /// <param name="typeioevent">Le type d'évènement</param>
        public TextFileIOEventArgs(TypeIOEventEnum typeioevent)
            : this(typeioevent, null)
        {
        }
        /// <summary>
        /// Création de la liste d'arguments dans le cas d'une opération en cours.
        /// </summary>
        /// <param name="typeioevent">Le type d'évènement</param>
        /// <param name="enregs">Les enregistrements associés</param>
        public TextFileIOEventArgs(TypeIOEventEnum typeioevent, System.Collections.Generic.List<llt.FileIO.TextFileIO._ENREG> enregs)
        {
            this.typeioevent = typeioevent;
            this.enregs = enregs;
        }
    }
}
