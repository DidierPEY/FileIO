﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;

namespace llt.FileIO.ImportExport
{
    /// <summary>
    /// La classe gérant la conversion fichier->tables et tables->fichier
    /// </summary>
    public partial class Convert
    {
        // Le document XML contenant les régles de conversion.
        private System.Xml.XmlDocument Regles;

        // La classe gérant les informations <fichier> du fichier XML.
        private Fichier fichier;

        // La classe gérant les informations <tables> du fichier XML
        private Tables tables;

        // La classe gérant l'exportation
        private Export export;
        // La classe gérant l'import
        private Import import;

        /// <summary>
        /// Liste des type de sources possible contenant les règles de conversion.
        /// </summary>
        public enum TypeSourceXMLEnum : short
        {
            /// <summary>
            /// Chaine contenant la description XML des régles de conversion
            /// </summary>
            ChaineXML,
            /// <summary>
            /// Fichier contentant la description XML des régles de conversion
            /// </summary>
            FichierXML
        }

        /// <summary>
        /// Création de l'objet gérant la convertion avec les régles à appliquer
        /// </summary>
        /// <param name="typesource">Précise le type de sourcexml</param>
        /// <param name="sourcexml">Les régles de conversion</param>
        public Convert(TypeSourceXMLEnum typesource, string sourcexml)
        {
            // Le document
            Regles = null;
            // Le reader XML
            System.Xml.XmlTextReader xr = null;
            // Liste de noeud
            System.Xml.XmlNodeList nl = null;

            try
            {
                // Récupération depuis un fichier
                if (typesource.Equals(TypeSourceXMLEnum.FichierXML))
                {
                    // Le fichier existe-il
                    if (!File.Exists(sourcexml))
                        throw new FileIOError(this.GetType().FullName, "Le fichier xml '" + sourcexml + "' est introuvable.");

                    // Ouverture du fichier XML
                    xr = new System.Xml.XmlTextReader(sourcexml);
                    xr.WhitespaceHandling = System.Xml.WhitespaceHandling.None;
                    if (!xr.Read()) throw new FileIOError(this.GetType().FullName, "Le fichier xml '" + sourcexml + "' est vide.");

                    // Chargement du document contenant les régles de conversion
                    Regles = new System.Xml.XmlDocument();
                    Regles.Load(xr);
                }
                else
                {
                    // Chargement du document contenant les régles de conversion
                    Regles = new System.Xml.XmlDocument();
                    Regles.LoadXml(sourcexml);
                }

                // Test si le noeud desciption existe.
                if (!Regles.DocumentElement.Name.ToLower().Equals("description"))
                    throw new FileIOError(this.GetType().FullName, "Le fichier xml doit commencer par l'élèment <description>");
                if (!Regles.DocumentElement.HasChildNodes)
                    throw new FileIOError(this.GetType().FullName, "L'élèment <description> est vide");

                // Recherche de l'élèment fichier.
                nl = Regles.DocumentElement.GetElementsByTagName("fichier");
                if (nl.Count==0)
                    throw new FileIOError(this.GetType().FullName, "L'élèment <fichier> n'existe pas.");
                if (nl.Count>1)
                    throw new FileIOError(this.GetType().FullName, "Un seul élément <fichier> doit être présent dans <description>.");
                
                // Création de l'objet fichier
                fichier = new Fichier(nl[0]);

                // Recherche de l'élément tables
                // Recherche un noued <tables>
                nl = Regles.GetElementsByTagName("tables");
                if (nl.Count == 0)
                    throw new FileIOError(this.GetType().FullName, "Aucun élément <tables> dans le fichier XML.");
                if (nl.Count > 1)
                    throw new FileIOError(this.GetType().FullName, "Un seul élément <tables> autorisé dans le fichier XML.");

                // L'objet tables à utiliser
                tables = new Tables(nl[0]);
            }
            catch (FileIOError)
            {
                throw;
            }
            catch (System.Exception eh)
            {
                throw new FileIOError(this.GetType().FullName, "Erreur inattendue lors de la création de l'objet.", eh);
            }
            finally
            {
                if (xr != null) xr.Close();
            }
        }

        /// <summary>
        /// Exporte les informations contenues dans le fichier vers les tables définies dans le fichier XML
        /// </summary>
        /// <remarks>Utilise le fichier spécifié dans la description XML</remarks>
        public DataSet ExportTables()
        {
            return ExportTables(fichier.Nom);
        }

        /// <summary>
        /// Exporte les informations contenues dans le fichier vers les tables définies dans le fichier XML
        /// <param name="FichierAExporter">{chemin\}NomDuFichier à utiliser pour l'exportation</param>
        /// </summary>
        public DataSet ExportTables(string FichierAExporter)
        {
            try
            {
                // Test le paramètre fichier
                if (FichierAExporter.Equals(""))
                    throw new FileIOError(this.GetType().FullName, "Le nom du fichier est obligatoire.");
                // Test complémentaire, uniquement si le fichier n'est pas celui prévu par la descrition
                if (!FichierAExporter.Equals(fichier.Nom))
                {
                    // Test si le répertoire existe dans le cas où il est précisé dans le nom.
                    string dir = Path.GetDirectoryName(FichierAExporter);
                    if (!dir.Equals("") && !Directory.Exists(dir))
                        throw new FileIOError(this.GetType().FullName, "Le répertoire '" + dir + "' n'existe pas");
                    // Le nom ne doit pas correspondre à un répertoire
                    if (Directory.Exists(FichierAExporter))
                        throw new FileIOError(this.GetType().FullName, "Le nom '" + FichierAExporter + "' ne doit pas être un répertoire");
                }
                // Recherche du noeud <export>
                if (export == null)
                {
                    System.Xml.XmlNodeList nl = Regles.DocumentElement.GetElementsByTagName("export");
                    if (nl.Count == 0)
                        throw new FileIOError(this.GetType().FullName, "L'élèment <export> n'existe pas.");
                    if (nl.Count > 1)
                        throw new FileIOError(this.GetType().FullName, "Un seul élément <export> doit être présent dans <description>.");
                    // Création de objet export
                    export = new Export(nl[0], fichier, tables);
                }

                // Suppression des enregistrements dans toutes les tables.
                tables.dsInterne.Clear();

                // Lance l'exportation
                export.Execute(FichierAExporter);

                // Renvoie une copie du DataSet
                return tables.dsInterne.Copy();
            }
            catch (FileIOError)
            {
                throw;
            }
            catch (System.Exception eh)
            {
                throw new FileIOError(this.GetType().FullName, "Erreur inattendue lors de l'exportation des données.", eh);
            }
            finally
            {
                // Suppression des enregistrements dans les tables.
                tables.dsInterne.Clear();
            }
        }

        /// <summary>
        /// Importe dans le fichier définies dans le fichier XML les informations contenues dans la(les) table(s)
        /// </summary>
        /// <param name="dsImport">Le DataSet contenant les tables</param>
        public void ImportFichier(DataSet dsImport)
        {
            ImportFichier(dsImport, fichier.Nom);
        }
        /// <summary>
        /// Importe dans le fichier passé en paramètre les informations contenues dans la(les) table(s)
        /// </summary>
        /// <param name="dsImport">Le DataSet contenant la(les) table(s)</param>
        /// <param name="FichierAImporter">{chemin\}NomDuFichier à utiliser pour l'immportation</param>
        public void ImportFichier(DataSet dsImport, string FichierAImporter)
        {
            try
            {
                // En mode import, le séparateur de fichier doit être connu
                if (fichier.SepEnr.Equals("auto", StringComparison.CurrentCultureIgnoreCase))
                    throw new FileIOError(this.GetType().FullName, "Impossible de déterminer automatiquement le séparateur d'enregistrement en cas d'import");
                // Test le paramètre fichier0
                if (FichierAImporter.Equals(""))
                    throw new FileIOError(this.GetType().FullName, "Le nom du fichier est obligatoire.");
                // Test complémentaire, uniquement si le fichier n'est pas celui prévu par la descrition
                if (!FichierAImporter.Equals(fichier.Nom))
                {
                    // Test si le répertoire existe dans le cas où il est précisé dans le nom.
                    string dir = Path.GetDirectoryName(FichierAImporter);
                    if (!dir.Equals("") && !Directory.Exists(dir))
                        throw new FileIOError(this.GetType().FullName, "Le répertoire '" + dir + "' n'existe pas");
                    // Le nom ne doit pas correspondre à un répertoire
                    if (Directory.Exists(FichierAImporter))
                        throw new FileIOError(this.GetType().FullName, "Le nom '" + FichierAImporter + "' ne doit pas être un répertoire");
                }
                // Le fichier ne doit pas exister
                if (File.Exists(FichierAImporter))
                    throw new FileIOError(this.GetType().FullName, "Le fichier '" + FichierAImporter + "' existe déja");

                // Suppression des enregistrements dans toutes les tables.
                tables.dsInterne.Clear();

                // Recherche du noeud <import>
                if (import == null)
                {
                    System.Xml.XmlNodeList nl = Regles.DocumentElement.GetElementsByTagName("import");
                    if (nl.Count == 0)
                        throw new FileIOError(this.GetType().FullName, "L'élèment <import> n'existe pas.");
                    if (nl.Count > 1)
                        throw new FileIOError(this.GetType().FullName, "Un seul élément <import> doit être présent dans <description>.");
                    // Création de objet export
                    import = new Import(nl[0], fichier, tables,dsImport);
                }

                // Lance l'importation
                import.Execute(FichierAImporter);
            }
            catch (FileIOError)
            {
                throw;
            }
            catch (System.Exception eh)
            {
                throw new FileIOError(this.GetType().FullName, "Erreur inattendue lors de l'exportation des données.", eh);
            }
            finally
            {
                // Suppression des enregistrements dans les tables.
                tables.dsInterne.Clear();
                // Suppression des tables virtuels
                tables.DelSEGVirtuel();
            }
        }

        /// <summary>
        /// Classe gérant l'export
        /// </summary>
        private class Export
        {
            // L'objet fichier
            private Fichier exportFichier;
            // L'objet tables
            private Tables exportTables;
            // La liste des segments export
            private SegmentsExport Segments;
            // Le lien en cours
            private LienFichier crsLien;

            /// <summary>
            /// Mise en place des régles de correspodances à l'export
            /// </summary>
            /// <param name="xn">Le noeud <![CDATA[<export>]]> du fichier XML</param>
            /// <param name="fichier">L'objet gérant le fichier</param>
            /// <param name="tables">L'objet gérant les tables</param>
            public Export(System.Xml.XmlNode xn, Fichier fichier, Tables tables)
            {
                // Test des paramètres
                if (fichier == null)
                    throw new FileIOError(this.GetType().FullName, "Aucun fichier n'est défini");
                if (tables==null)
                    throw new FileIOError(this.GetType().FullName, "Aucune table n'est définie");
                exportFichier = fichier;
                exportTables = tables;

                // Création de la liste des segments exports
                Segments = new SegmentsExport();
                
                // Traitement des différents noeuds dépendant de fichier.
                foreach (System.Xml.XmlNode noeud in xn.ChildNodes)
                {
                    // Traitement des segments.
                    if (noeud.Name.ToLower().Equals("segment")) CreSEG(noeud);
                }

                // Si aucun lien table, pas d'autre vérification
                if (exportTables.Liens.Count == 0) return;

                // On vérifie la cohérence de l'ordre de MAJ des tables
                if (exportTables.Liens.Count > 1)
                {
                    if (exportFichier.Liens.Count > 0)
                        CreSEGVerif((LienFichier)null, (LienTables)null, 0);
                    else
                    {
                        // Détermine le ou les liens table maj
                        List<LienTables> lts = new List<LienTables>();
                        foreach (SegmentExport se in Segments)
                        {
                            if (!lts.Contains(exportTables.Liens.getLien(se.ChampTable.Table))) lts.Add(exportTables.Liens.getLien(se.ChampTable.Table));
                        }

                        // Il faut oblogatoirement qu'un lien racine tables soit maj
                        LienTables racLien = null;
                        foreach (LienTables lt in lts)
                        {
                            if (lt.DependDe == null)
                            {
                                if (racLien == null) racLien = lt;
                                else if (racLien != lt)
                                    throw new FileIOError(this.GetType().FullName, "L'export met à jour plusieurs segments tables racine");
                            }
                        }
                        if (racLien == null)
                            throw new FileIOError(this.GetType().FullName, "L'export ne met à jour aucun segment table racine");
                        // Si plusieurs liens tables trouvés ...
                        if (lts.Count > 1)
                        {
                            // ... ils doivent tous dépendre du lien racine
                            int maxProfondeur = 0;
                            foreach (LienTables lt in lts)
                            {
                                if (lt.DependDe == null) continue;
                                if (lt.getLienRacine() != racLien)
                                    throw new FileIOError(this.GetType().FullName, "L'export met à jour un segment table '" + lt.Segment.TableName + "' qui ne dépend pas du segment table '" + racLien.Segment.TableName + "'");
                                if (lt.Profondeur > maxProfondeur) maxProfondeur = lt.Profondeur;
                            }
                            // ... la profondeur doit etre continue
                            bool[] ps = new bool[maxProfondeur + 1];
                            for (int i = 0; i < ps.Length; i++) ps[i] = false;
                            // ... indique si la profondeur est utilisée
                            foreach (LienTables lt in lts) ps[lt.Profondeur] = true;
                            // ... si une des profondeurs n'est pas mise à jour, c'est une erreur
                            for (int i = 0; i < ps.Length; i++)
                            {
                                if (!ps[i])
                                    throw new FileIOError(this.GetType().FullName, "L'export omet de mettre à jour une table dans la hiérachie des liens");
                            }
                            // ... un enregistrement fichier mettant à jour plusieurs table, il faut vérifier que l'enregistrement
                            // n'existe pas avant de le créer dans la table (sauf pour le lien le plus profond)
                            foreach (LienTables lt in lts)
                            {
                                if (lt.Profondeur < maxProfondeur) lt.verifCreEnr = true;
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Exportation
            /// </summary>
            public void Execute(string FichierAExporter)
            {
                // Le fichier ut6ilisé
                TextFileIO txtFileIO = null;
                // Pas de lien en cours
                crsLien = null;

                try
                {
                    // Détermine le séparateur d'enregistrement
                    string exesepenr = exportFichier.SepEnr;
                    if (exesepenr.Equals("auto", StringComparison.CurrentCultureIgnoreCase))
                    {
                        exesepenr = TextFileIO.GetEnrSepText(FichierAExporter, exportFichier.Codage);
                        if (exesepenr.Length == 0)
                            throw new FileIOError(this.GetType().FullName, "Impossible de déterminer le séparateur d'enregistrement");
                    }
                    // Ouverture du fichier en lecture.
                    txtFileIO = new TextFileIO(FichierAExporter, exportFichier.Codage, exesepenr, exportFichier.SepChamp, exportFichier.DelChamp, false);
                    // Attache l'évènement
                    txtFileIO.TextFileIOEvent += new TextFileIOEventHandler(trtLectureFaite);

                    // Démarrage de l'exportation.
                    txtFileIO.ReadFileSeq();

                    // MAJ du éventuellent du dernier enregistrement contenant un champ mémo
                    if (crsLien != null && crsLien.Segment.ChampMemo && crsLien.Segment.EnregChampMemo.Champs.Length > 0)
                        MajEnregistrement(crsLien.Segment.EnregChampMemo);

                    // MAJ des derniers enregistrements
                    if (exportTables.Liens.Count > 0)
                        exportTables.Liens.majEnregistrements();
                }
                catch (FileIOError)
                {
                    throw;
                }
                catch (System.Exception eh)
                {
                    throw new FileIOError(this.GetType().FullName, "Erreur inattendue lors de l'exportation des données.", eh);
                }
                finally
                {
                    // Fermeture du fichier 
                    if (txtFileIO != null)
                    {
                        txtFileIO.Dispose();
                        txtFileIO = null;
                    }
                }
            }

            /// <summary>
            /// Création des segments export
            /// </summary>
            /// <param name="xn"></param>
            private void CreSEG(System.Xml.XmlNode xn)
            {
                //Si aucun attribut, c'est une erreur.
                if (xn.Attributes.Count == 0)
                    throw new FileIOError(this.GetType().FullName, "Un <segment> dans <export> est sans attribut.");

                // Nom du segment
                string nomsegment = "";
                // Recherche de l'attribut nom obligatoire
                foreach (System.Xml.XmlAttribute xa in xn.Attributes)
                {
                    if (xa.Name.ToLower().Equals("nom") && !xa.Value.Equals(""))
                    {
                        nomsegment = xa.Value.Trim();
                        break;
                    }
                }
                // Si aucun nom de segment, on sort en erreur.
                if (nomsegment.Equals(""))
                    throw new FileIOError(this.GetType().FullName, "Un <segment> dans <export> est sans attribut 'nom'.");
                // Test si le segment existe
                if (!exportFichier.Segments.Contains(nomsegment))
                    throw new FileIOError(this.GetType().FullName, "le segment '" + nomsegment + "' n'existe pas dans <fichier>.");

                // On cré un tableau de chaine correspondant à chaque ligne de <segment>
                string[] champs;
                champs = xn.FirstChild.Value.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (champs.Length == 1 && champs[0].Equals(xn.FirstChild.Value))
                    champs = xn.FirstChild.Value.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (champs.Length == 0) return;

                // Pour chaque ligne trouvée, on crée un segment export
                foreach (string champ in champs)
                {
                    // Chaine sans aucun caractère de contrôle avant le premier '.'
                    string sc = "";
                    int scp = -1;
                    foreach (char c in champ)
                    {
                        scp++;
                        if (!Char.IsControl(c)) break;
                    }
                    // Si chaine vide, on passe à l'occurence suivante.
                    if (scp < 0) continue;
                    if (scp.Equals(champ.Length - 1)) continue;
                    sc = champ.Substring(scp);
                    // La chaine doit au moins commencer par un point
                    if (!sc.StartsWith("."))
                        throw new FileIOError(this.GetType().FullName, "Le ligne doit obligatoirement commencer par un '.'");
                    // Ajoute le segment export
                    Segments.Add(new SegmentExport(exportFichier.Segments[nomsegment], exportTables.dsInterne, sc));
                }
            }

            /// <summary>
            /// Vérification de la cohérence des segments exports créés
            /// </summary>
            /// <param name="DependDe">Vérification du les lien fichiers dépendand de celui passé en paramètre</param>
            /// <param name="racLien">Le lien racine table</param>
            /// <param name="pmaxProfondeur">La profondeur maxi (liens tables) maj sur le niveau précédent</param>
            private void CreSEGVerif(LienFichier DependDe, LienTables racLien, int pmaxProfondeur)
            {
                LienFichier[] lfs = exportFichier.Liens.getLiens(DependDe);
                foreach (LienFichier lf in lfs)
                {
                    // Test si ce segment fichier met à jour des segments export
                    SegmentExport[] ses = Segments.getExports(lf.Segment);
                    if (ses.Length == 0) continue;

                    // Détermine le ou les liens table maj
                    List<LienTables> lts = new List<LienTables>();
                    foreach (SegmentExport se in ses)
                    {
                        if (!lts.Contains(exportTables.Liens.getLien(se.ChampTable.Table))) lts.Add(exportTables.Liens.getLien(se.ChampTable.Table));
                    }

                    // Un segment fichier racine doit mettre à jour obligatoirement un et un seul segment table racine
                    if (lf.DependDe == null)
                    {
                        racLien = null;
                        pmaxProfondeur = 0;
                        foreach (LienTables lt in lts)
                        {
                            if (lt.DependDe == null)
                            {
                                if (racLien == null) racLien = lt;
                                else if (racLien != lt)
                                    throw new FileIOError(this.GetType().FullName, "Le segment fichier '" + lf.Segment.Nom + "met à jour plusieurs segments tables racine");
                            }
                        }
                    }
                    if (racLien == null)
                        throw new FileIOError(this.GetType().FullName, "Le segment fichier '" + lf.Segment.Nom + "ne met à jour aucun segment table racine");

                    // La profondeux maxi maj au niveau des liens tables à jour par le lien fichier
                    int maxProfondeur = pmaxProfondeur;

                    // Si plusieurs liens tables trouvés ...
                    if (lts.Count > 1)
                    {
                        // ... ils doivent tous dépendre du lien racine
                        foreach (LienTables lt in lts)
                        {
                            if (lt.DependDe == null) continue;
                            if (lt.getLienRacine() != racLien)
                                throw new FileIOError(this.GetType().FullName, "Le segment fichier '" + lf.Segment.Nom + "met à jour un segment table '" + lt.Segment.TableName + "' qui ne dépend pas du segment table '" + racLien.Segment.TableName + "'");
                            if (lt.Profondeur > maxProfondeur) maxProfondeur = lt.Profondeur;
                        }
                        // ... la profondeur doit etre continue
                        if (maxProfondeur > pmaxProfondeur)
                        {
                            bool[] ps = new bool[maxProfondeur - pmaxProfondeur + 1];
                            for (int i = 0; i < ps.Length; i++) ps[i] = false;
                            // ... indique si la profondeur est utilisée
                            foreach (LienTables lt in lts)
                            {
                                if (lt.Profondeur >= pmaxProfondeur) ps[lt.Profondeur - pmaxProfondeur] = true;
                            }
                            // ... si une des profondeurs n'est pas mise à jour, c'est une erreur
                            for (int i = 0; i < ps.Length; i++)
                            {
                                if (!ps[i])
                                    throw new FileIOError(this.GetType().FullName, "Le segment fichier '" + lf.Segment.Nom + " omet de mettre à jour une table dans la hiérachie des liens");
                            }
                            // ... un enregistrement fichier mettant à jour plusieurs table, il faut vérifier que l'enregistrement
                            // n'existe pas avant de le créer dans la table (sauf pour le lien le plus profond)
                            foreach (LienTables lt in lts)
                            {
                                if (lt.Profondeur < maxProfondeur) lt.verifCreEnr = true;
                            }
                        }
                    }
                    else if (lts[0].Profondeur > maxProfondeur)
                        maxProfondeur = lts[0].Profondeur;

                    // On descend la hiérachie des liens tables
                    CreSEGVerif(lf, racLien, maxProfondeur);
                }
            }

            /// <summary>
            /// Traitement de l'évènement LectureFaite (et uniquement lui).
            /// </summary>
            /// <param name="sender">L'emetteur</param>
            /// <param name="e">Les paramètres de l'évènement</param>
            private void trtLectureFaite(object sender, TextFileIOEventArgs e)
            {
                // Seul l'évènement indiquant que la lecture est faite est traité.
                if (!e.TypeIOEvent.Equals(TypeIOEventEnum.lecturefaite)) return;

                // Traitement de chaque enregistrement
                foreach (TextFileIO._ENREG enr in e.Enregs)
                {
                    // Est-ce que l'enregistrement contient des informations.
                    if (enr.Champs.Length == 0) continue;
                    // Renvoie le nom du segment correspondant à l'enregistrement.
                    // NOTA : ce test est valable pour des champs fixes ou délimités.
                    SegmentFichier sf = exportFichier.Segments.FindSegment(enr.Champs[0]);
                    if (sf == null)
                    {
                        // Un segment non trouvé peut être du à la présence d'un champ mémo
                        // ... Si aucun lien en cours
                        if (crsLien == null) continue;
                        // ... Existe-t-il un champ mémo dans le segment du lien en cours.
                        if (!crsLien.Segment.ChampMemo) continue;
                        // Si le nombre de champ mémorisé est inférieur au nombre de champ total
                        // dans le segment, on ajoute les champs à l'enregistrement existant.
                        if (crsLien.Segment.Champs.Count > crsLien.Segment.EnregChampMemo.Champs.Length)
                        {
                            string[] newenr = new string[crsLien.Segment.Champs.Count + crsLien.Segment.EnregChampMemo.Champs.Length];
                            crsLien.Segment.EnregChampMemo.Champs.CopyTo(newenr, 0);
                            enr.Champs.CopyTo(newenr, crsLien.Segment.Champs.Count);
                            crsLien.Segment.EnregChampMemo = new TextFileIO._ENREG(newenr);
                        }
                        else
                        {
                            // Le champ Mémo est le dernier champ de l'enregistrement. On cumul les données
                            crsLien.Segment.EnregChampMemo.Champs[crsLien.Segment.EnregChampMemo.Champs.Length - 1] =
                                crsLien.Segment.EnregChampMemo.Champs[crsLien.Segment.EnregChampMemo.Champs.Length - 1] +
                                exportFichier.SepEnr + enr.Champs[0];
                        }
                        // On passe à l'enregistrement suivant
                        continue;
                    }
                    // Que le segment trouvé soit utilisé ou non par l'export, qu'il soit conforme ou non à la description,
                    // dans le cas d'un segment contenant un champ mémo, il faut traiter la mise à jour de ce lien
                    if (crsLien != null && crsLien.Segment.ChampMemo && crsLien.Segment.EnregChampMemo.Champs.Length > 0)
                    {
                        // MAJ de l'enregistrement
                        MajEnregistrement(crsLien.Segment.EnregChampMemo);
                        // Initialisation de 
                        crsLien.Segment.EnregChampMemo = new TextFileIO._ENREG(new string[] { });
                    }
                    // Est-ce que ce segment est utilisé par l'export
                    if (!Segments.Contains(sf)) continue;
                    // Est-ce que le segment trouvé est conforme à celui attendu.
                    if (!Conforme(sf))
                        throw new FileIOError(this.GetType().FullName, "Contenu du fichier non conforme. Arrêt du traitement");
                    // Maj de l'enregistrement si le segment ne contient pas de champ mémo.
                    // Sinon, on mémorise l'enregistrement
                    if (!sf.ChampMemo) MajEnregistrement(enr);
                    else sf.EnregChampMemo = enr;
                }
            }
            
            /// <summary>
            /// Indique si le segment trouvé dans le fichier est conforme par rapport aux liens.
            /// </summary>
            /// <param name="segment"></param>
            /// <returns></returns>
            private bool Conforme(SegmentFichier segment)
            {
                // Si aucun lien, forcément conforme
                if (exportFichier.Liens.Count == 0) return true;
                // Recherche du lien
                LienFichier lf = exportFichier.Liens.getLien(segment, crsLien == null ? (LienFichier)null : crsLien.getLienRacine());
                if (lf == null) return false;
                // Si aucun lien en cours, il faut que le lien trouvé soit un lien racine
                if (crsLien == null)
                {
                    if (lf.Profondeur != 0) return false;
                    else
                    {
                        crsLien = lf;
                        return true;
                    }
                }
                // S'agit-il du même lien ?
                if (crsLien.Equals(lf))
                {
                    // Si ce segment dispose de sous-segment(s) obligaoire(s), cette égalité est une anomalie
                    LienFichier[] lfs = exportFichier.Liens.getLiens(segment);
                    foreach (LienFichier lfu in lfs)
                    {
                        if (!lfu.Facultatif) return false;
                    }
                    // Si le lien doit-être unique, c'est une erreur sinon, le segment est conforme.
                    return (!lf.Unique);
                }
                // Le lien a changé.
                // Si ce n'est pas un lien racine ...
                if (lf.DependDe != null)
                {
                    // ... S'il s'agit d'un lien dépendant du lien en cours, c'est normal
                    if (lf.DependDe.Equals(crsLien))
                    {
                        crsLien = lf;
                        return true;
                    }
                    // ... S'il partage le même parent, le changement de lien est correcte
                    LienFichier lfp = crsLien;
                    for (; lfp != null; )
                    {
                        if (lfp.DependDe.Equals(lf.DependDe))
                        {
                            crsLien = lf;
                            return true;
                        }
                        // On descend l'arborescence
                        lfp = lfp.DependDe;
                    }
                }
                // Si c'est un lien racine ...
                else
                {
                    // Si le lien doit-être unique, c'est une erreur sinon, le segment est conforme.
                    if (lf.Unique) return false;
                    else
                    {
                        crsLien = lf;
                        return true;
                    }
                }

                // A ce stade, le segment est forcément non conforme
                return false;
            }

            /// <summary>
            /// Vérifie s'il faut créer un nouvel enregisrement dans la(les) table(s) concernée(s) par les segments exports
            /// </summary>
            /// <param name="ses">Les segments exports à traiter</param>
            private void CreEnregistrement(SegmentExport[] ses)
            {
                // Liste des différentes tables utilisés par ces segments export
                System.Collections.Specialized.StringCollection tables = new System.Collections.Specialized.StringCollection();
                foreach (SegmentExport se in ses)
                {
                    if (!tables.Contains(se.ChampTable.Table.TableName))
                        tables.Add(se.ChampTable.Table.TableName);
                }
                // Pour chaque table, on vérifie s'il faut créer un nouvel enregistrement
                foreach (string table in tables)
                {
                    // Si aucun enregistrement en cours, on crée sans test
                    if (exportTables.Liens.getLien(exportTables.dsInterne.Tables[table]).crsEnregistrement == null)
                        exportTables.Liens.getLien(exportTables.dsInterne.Tables[table]).addEnregistrement();
                    // sinon, création d'un nouvel enregistrement si nécessaire.
                    else if (CreEnregistrement(table))
                        exportTables.Liens.getLien(exportTables.dsInterne.Tables[table]).addEnregistrement();
                }
            }

            /// <summary>
            /// Vérifie s'il faut créer un nouvel enregisrement dans la(les) table(s) concernée(s) par les segments exports
            /// </summary>
            private bool CreEnregistrement(string table)
            {
                // On vérifie si la table n'et pas utilisé dans un segment inférieur
                LienFichier ld = crsLien.DependDe;
                // Par défaut, il faut créer un nouvel enregistrement
                bool addenr = true;
                // Si la table est utilisé dans un lien parent, il ne faut pas créer un nouvel enregistrement mais
                // utilisé l'existant
                for (; ld != null; )
                {
                    SegmentExport[] sesd = Segments.getExports(ld.Segment);
                    foreach (SegmentExport sed in sesd)
                    {
                        // Si la table est déjà utilisé, il ne faut pas créer un nouvel enregistrement
                        if (sed.ChampTable.Table.TableName.Equals(table, StringComparison.CurrentCultureIgnoreCase))
                        {
                            addenr = false;
                            break;
                        }
                    }
                    // Si l'enregistrement n'est pas à créé, on sort de la boucle de recherche
                    if (!addenr) break;
                    // On passe sur le lien parent
                    ld = ld.DependDe;
                }
                return addenr;
            }

            /// <summary>
            /// MAJ enregistrement segment table
            /// </summary>
            /// <param name="enr">L'enregistrement du segment table</param>
            private void MajEnregistrement(TextFileIO._ENREG enr)
            {
                // Traitement en multi-segments (fichier et tables)
                if (exportFichier.Liens.Count > 0 && exportTables.Liens.Count > 0)
                {
                    // Si le segment en cours est un segment racine, il faut déclencher l'ajout des enregistrements
                    // du précédent traitement
                    if (crsLien.Profondeur == 0) exportTables.Liens.majEnregistrements();
                }

                // Récupération de tous les segments export
                SegmentExport[] ses;
                if (exportFichier.Liens.Count > 0)
                    ses = Segments.getExports(crsLien.Segment);
                else
                    ses = Segments.getExports(exportFichier.Segments[0]);

                // Traitement en multi-segments tables
                if (exportTables.Liens.Count > 0)
                {
                    // Avant de traiter les segments, il faut vérifier si on doit créer des enregistrements
                    CreEnregistrement(ses);

                    // Traitement de tous les segments
                    foreach (SegmentExport se in ses)
                    {
                        LienTables lt = exportTables.Liens.getLien(se.ChampTable.Table);
                        MajChamp(lt.crsEnregistrement, enr, se);
                    }

                    // En mono-segment fichier, on déclenche la mise à jour
                    if (exportFichier.Liens.Count == 0) exportTables.Liens.majEnregistrements();
                }
                // Traitement en mono-segment tables
                else
                {
                    // L'enregistrement à utiliser
                    DataRow dr;
                    // Création d'un nouvel enregistrement
                    if (exportTables.dsInterne.Tables[0].Rows.Count == 0 || exportFichier.Liens.Count == 0)
                        dr = exportTables.dsInterne.Tables[0].NewRow();
                    else 
                    {
                        // Si multi-segment fichier, il faut vérifier que ce segment déclenche la création d'enregistrement
                        if (CreEnregistrement(exportTables.dsInterne.Tables[0].TableName))
                            dr = exportTables.dsInterne.Tables[0].NewRow();
                        else
                            dr = exportTables.dsInterne.Tables[0].Rows[exportTables.dsInterne.Tables[0].Rows.Count - 1];
                    }

                    // MAJ des champs
                    foreach (SegmentExport se in ses)
                    {
                        MajChamp(dr, enr, se);
                    }

                    // Ajout de l'enregistrement
                    if (dr.RowState.Equals(DataRowState.Detached))
                        exportTables.dsInterne.Tables[0].Rows.Add(dr);
                }
            }

            /// <summary>
            /// Mis à jour du segment export
            /// </summary>
            /// <param name="dr">L'enregistrement à utiliser issu du segment table</param>
            /// <param name="enr">L'enregistrement à utiliser issu du segment fichier</param>
            /// <param name="se">Le segment export</param>
            private void MajChamp(DataRow dr, TextFileIO._ENREG enr, SegmentExport se)
            {
                // Si champ non renseigné
                if (dr.IsNull(se.ChampTable))
                {
                    if (!se.NomChampFichier.StartsWith("="))
                    {
                        object value = se.SegmentFichier.Champs[se.NomChampFichier].GetValue(enr);
                        dr[se.ChampTable] = value != null ? value : DBNull.Value;
                    }
                    else
                        dr[se.ChampTable] = se.NomChampFichier.Substring(1);
                }
                // Le champ de la table reçoit plusieurs valeurs à concaténer (ou à ajouter)
                else
                {
                    // Récupération de la valeur à ajouter
                    object value;
                    if (!se.NomChampFichier.StartsWith("="))
                        value = se.SegmentFichier.Champs[se.NomChampFichier].GetValue(enr);
                    else
                        value = se.NomChampFichier.Substring(1);

                    // Si la valeur est nulle, on ne concatène rien
                    if (value == null) return;

                    // Effectue la concaténation
                    if (se.ChampTable.DataType.Equals(System.Type.GetType("System.String")))
                        dr[se.ChampTable] = System.Convert.ToString(dr[se.ChampTable]) + (string)value;
                    else if (se.ChampTable.DataType.Equals(System.Type.GetType("System.Int32")) ||
                        se.ChampTable.DataType.Equals(System.Type.GetType("System.Int16")))
                        dr[se.ChampTable] = System.Convert.ToInt32(dr[se.ChampTable]) + (int)value;
                    else if (se.ChampTable.DataType.Equals(System.Type.GetType("System.Single")) ||
                        se.ChampTable.DataType.Equals(System.Type.GetType("System.Double")) ||
                        se.ChampTable.DataType.Equals(System.Type.GetType("System.Decimal")))
                        dr[se.ChampTable] = System.Convert.ToDecimal(dr[se.ChampTable]) + (decimal)value;
                    else if (se.ChampTable.DataType.Equals(System.Type.GetType("System.DateTime")))
                        dr[se.ChampTable] = System.Convert.ToDateTime(dr[se.ChampTable]).AddTicks(((DateTime)value).Ticks);
                    else
                        throw new FileIOError(this.GetType().FullName, "Type '" + se.ChampTable.DataType.FullName + "' non supporté en concaténation de champ");
                }
            }
        }

        /// <summary>
        /// Classe gérant l'import
        /// </summary>
        private class Import
        {
            // L'objet fichier
            private Fichier importFichier;
            // L'objet tables
            private Tables importTables;
            // La liste des segments export
            private SegmentsImport Segments;
            // La liste des enregistrements
            private List<TextFileIO._ENREG> Enregistrements;

            /// <summary>
            /// Mise en place des régles de correspodances à l'import
            /// </summary>
            /// <param name="xn">Le noeud <![CDATA[<export>]]> du fichier XML</param>
            /// <param name="fichier">L'objet gérant le fichier</param>
            /// <param name="tables">L'objet gérant les tables</param>
            /// <param name="ds">Le dataset à utiliser pour effectuer l'importation</param>
            public Import(System.Xml.XmlNode xn, Fichier fichier, Tables tables, DataSet ds)
            {
                // Test des paramètres
                if (fichier == null)
                    throw new FileIOError(this.GetType().FullName, "Aucun fichier n'est défini");
                if (tables==null)
                    throw new FileIOError(this.GetType().FullName, "Aucune table n'est définie");
                if (ds==null)
                    throw new FileIOError(this.GetType().FullName, "Aucun dataset n'est définie");
                importFichier = fichier;
                importTables = tables;

                // On vérifie que toutes les tables et tous les champs définit dans tables sont présents
                foreach (DataTable dt in importTables.dsInterne.Tables)
                {
                    // Vérification de la table
                    if (!ds.Tables.Contains(dt.TableName))
                        throw new FileIOError(this.GetType().FullName, "Le dataset ne contient aucune table nommée '" + dt.TableName + "'");
                    // Vérification des champs de la table
                    DataTable dti = ds.Tables[dt.TableName];
                    foreach (DataColumn dc in dt.Columns)
                    {
                        if (!dti.Columns.Contains(dc.ColumnName))
                            throw new FileIOError(this.GetType().FullName, "La table '" + dt.TableName + "' ne contient pas le champ '" + dc.ColumnName + "'");
                    }
                }
                
                // Création de la liste des segments imports
                Segments = new SegmentsImport();
                
                // Traitement des différents noeuds dépendant de fichier.
                foreach (System.Xml.XmlNode noeud in xn.ChildNodes)
                {
                    // Traitement des segments.
                    if (noeud.Name.ToLower().Equals("segment")) CreSEG(noeud);
                }

                // Chargement des enregistrements dans le dataset interne en tenant compte des segments virtuels
                foreach (DataTable dt in importTables.dsInterne.Tables)
                {
                    // Les enregistrement à importer
                    DataRow[] dris = null;

                    // S'agit-il d'un segment virtuel
                    if (dt.ExtendedProperties.Contains("virtuelde"))
                        dris = ds.Tables[dt.ExtendedProperties["virtuelde"].ToString()].Select(dt.ExtendedProperties["si"].ToString());
                    else
                    {
                        // Un ou plusieurs segment tabme virtuel dépend de ce segment table
                        DataTable[] segsVirtuel = importTables.getSEGVirtuel(dt.TableName);
                        if (segsVirtuel.Length == 0)
                            dris = ds.Tables[dt.TableName].Select();
                        else
                        {
                            // On exclu tous les enregistrements du ou des segments virtuels
                            string si = "";
                            foreach (DataTable dtv in segsVirtuel)
                            {
                                if (!si.Equals("")) si = si + " or ";
                                si = si + "(" + dtv.ExtendedProperties["si"].ToString() + ")";
                            }
                            si = "not (" + si + ")";
                            // Chargement des enregistrements à importer
                            dris = ds.Tables[dt.TableName].Select(si);
                        }
                    }
                    
                    // Création des enregistrements dans le datatable interne
                    foreach (DataRow dri in dris)
                    {
                        DataRow dr = dt.NewRow();
                        foreach (DataColumn dc in dt.Columns)
                            dr[dc] = dri[dc.ColumnName];
                        dt.Rows.Add(dr);
                    }
                }
            }
            
            /// <summary>
            /// Importation
            /// </summary>
            public void Execute(string ImporterFichier)
            {
                // Le fichier ut6ilisé
                TextFileIO txtFileIO = null;
                try
                {
                    // Ouverture du fichier en lecture/ecriture.
                    txtFileIO = new TextFileIO(ImporterFichier, importFichier.Codage, importFichier.SepEnr, importFichier.SepChamp, importFichier.DelChamp, true);

                    // Création liste enregistrements
                    if (Enregistrements == null) Enregistrements = new List<TextFileIO._ENREG>();
                    else Enregistrements.Clear();

                    // Si aucun lien (ni table ni fichier)
                    if (importTables.Liens.Count == 0 && importFichier.Liens.Count == 0)
                    {
                        // Traitement de toutes les tables (il peut exister un segment ou plusisurs virtuel)
                        for (int i = 0; i < importTables.dsInterne.Tables.Count; i++)
                        {
                            // Est-ce que cette table est utilisée.
                            SegmentImport[] sit = Segments.getImports(importTables.dsInterne.Tables[i]);
                            if (sit.Length == 0) continue;

                            // Traitement de tous les enregistrements.
                            foreach (DataRow dr in importTables.dsInterne.Tables[i].Rows)
                            {
                                // Création d'un nouvel enregistrement
                                TextFileIO._ENREG enreg = importFichier.Segments[0].NewEnreg();

                                // MAJ des champs
                                foreach (SegmentImport si in sit)
                                {
                                    si.ChampFichierMaj.SetValue(dr[si.NomChampTable], ref enreg);
                                }

                                // Ajout de l'enregistrement
                                Enregistrements.Add(enreg);
                            }

                            // Création des enregistrements dans le fichier
                            txtFileIO.WriteFile(Enregistrements);
                            Enregistrements.Clear();
                            txtFileIO.WaitAllIO();
                        }
                    }

                    // Si aucun lien fichier mais plusieurs tables
                    else if (importTables.Liens.Count > 0 && importFichier.Liens.Count == 0)
                    {
                        // Traitement en fonction des lien tables
                        LecLiensTables(null);

                        // Création des enregistrements dans le fichier
                        txtFileIO.WriteFile(Enregistrements);
                        Enregistrements.Clear();
                        txtFileIO.WaitAllIO();
                    }

                    // Si aucun lien table mais plusieurs liens fichier
                    else if (importTables.Liens.Count == 0 && importFichier.Liens.Count > 0)
                    {
                        // Traitement de toutes les tables (il peut exister un segment ou plusisurs virtuel)
                        for (int i = 0; i < importTables.dsInterne.Tables.Count; i++)
                        {
                            // Est-ce que cette table est utilisée.
                            SegmentImport[] sit = Segments.getImports(importTables.dsInterne.Tables[i]);
                            if (sit.Length == 0) continue;

                            // La table doit forcément mettre à jour un lien fichier racine
                            LienFichier racLF = getLienFichierRacine(importTables.dsInterne.Tables[i]);
                            if (racLF == null)
                                throw new FileIOError(this.GetType().FullName, "La table '" + importTables.dsInterne.Tables[i].TableName + "' ne met à jour aucun lien racine dans le fichier");

                            // Si le lien racine est unique, la table ne doit pas contenir plusieurs enregistrements
                            if (racLF.Unique && importTables.dsInterne.Tables[i].Rows.Count > 1)
                                throw new FileIOError(this.GetType().FullName, "La table '" + importTables.dsInterne.Tables[i].TableName + "' contient plusieurs enregisrement alors que le lien racine dans le fichier est unique");

                            // Il faut vérifier que les segments obligatoires sur le fichier sont bien alimentés par la table
                            testMajSegmentsFichierObligatoire(importTables.dsInterne.Tables[i], racLF);

                            // Traitement de tous les enregistrements.
                            foreach (DataRow dr in importTables.dsInterne.Tables[i].Rows) majLiensFichier(dr, sit, racLF);
                        }

                        // Création des enregistrements dans le fichier
                        txtFileIO.WriteFile(Enregistrements);
                        Enregistrements.Clear();
                        txtFileIO.WaitAllIO();
                    }

                    // Si plusieurs liens table et fichier
                    else if (importTables.Liens.Count > 0 && importFichier.Liens.Count > 0)
                    {
                        // Recherche des tables racines
                        LienTables[] lfr = importTables.Liens.getLiens((LienTables)null);
 
                        // Traitement de toutes les tables racines 
                        for (int i = 0; i < lfr.Length; i++)
                        {
                            // Il peut exister un segment ou plusisurs virtuel
                            DataTable[] dtrs = importTables.getSEGplusSEGVirtuel(lfr[i].Segment);

                            // Traitement de toutes les tables racines.
                            foreach (DataTable dtr in dtrs)
                            {
                                // Est-ce que cette table est utilisée.
                                SegmentImport[] sit = Segments.getImports(dtr);
                                if (sit.Length == 0) continue;

                                // La table doit forcément mettre à jour un lien fichier racine
                                LienFichier racLF = getLienFichierRacine(dtr);
                                if (racLF == null)
                                    throw new FileIOError(this.GetType().FullName, "La table '" + lfr[i].Segment.TableName + "' ne met à jour aucun lien racine dans le fichier");

                                // Si le lien racine est unique, la table ne doit pas contenir plusieurs enregistrements
                                if (racLF.Unique && lfr[i].Segment.Rows.Count > 1)
                                    throw new FileIOError(this.GetType().FullName, "La table '" + importTables.dsInterne.Tables[i].TableName + "' contient plusieurs enregisrement alors que le lien racine dans le fichier est unique");

                                // Traitement de tous les enregistrements
                                for (int j = 0; j < dtr.Rows.Count; j++)
                                {
                                    // Indique l'enregistrement en cours
                                    importTables.setIndexImportEnregistrement(dtr, j);

                                    // Création des enregistrement fichier
                                    majLiensFichier(dtr.Rows[j], sit, racLF);

                                }
                            }
                        }
                        
                        // Non supporté pour l'instant
                        throw new FileIOError(this.GetType().FullName, "Format d'export non supporté");
                    }
                }
                catch (FileIOError)
                {
                    throw;
                }
                catch (System.Exception eh)
                {
                    throw new FileIOError(this.GetType().FullName, "Erreur inattendue lors de l'exportation des données.", eh);
                }
                finally
                {
                    // Fermeture du fichier 
                    if (txtFileIO != null)
                    {
                        txtFileIO.Dispose();
                        txtFileIO = null;
                    }
                }
            }

            // Traitement de l'importation dans le cas d'un seul segment fichier mais plusieurs liens table
            private void LecLiensTables(DataRow dependde)
            {
                // Recherche liens dépendant
                LienTables[] depLTS;
                if (dependde == null) depLTS = importTables.Liens.getLiens((LienTables)null);
                else depLTS = importTables.Liens.getLiens(importTables.Liens.getLien(dependde.Table));

                // Pour chaque lien trouvé
                foreach (LienTables depLT in depLTS)
                {
                    // On détermine les tables à traiter (ne pas oublier les segments virtuels !)
                    DataTable[] segtrt = importTables.getSEGplusSEGVirtuel(depLT.Segment);

                    // Traitement de tous les segments tables
                    for (int i = 0; i < segtrt.Length; i++)
                    {
                        // Est-ce que cette table est utilisée.
                        SegmentImport[] sit = Segments.getImports(segtrt[i]);
                        if (sit.Length == 0) continue;

                        // Recherche des enregistrement à traiter
                        DataRow[] drs = depLT.getRows(dependde);
                        if (drs.Length == 0) continue;

                        // Traitement de tous les enregistrements.
                        for (int j = 0; j < drs.Length; j++)
                        {
                            // Affection de l'enregistrement en cours.
                            importTables.setIndexImportEnregistrement(depLT.Segment, j);
                            DataRow dr = drs[j];

                            // Création d'un nouvel enregistrement :
                            // - a chaque changement d'enregistrement pour le segment de profondeur 0
                            // - a chaque changement d'enregistrement pour un segment de profondeur supérieure 
                            // sauf pour le premier enregistrement traité puisque la création aura été faite
                            // par un segment de profondeux inférieur
                            if (depLT.Profondeur == 0 || depLT.Profondeur > 0 && j > 0)
                                Enregistrements.Add(importFichier.Segments[0].NewEnreg());

                            // Etant donné qu'il n'y a qu'un seul segment fichier, tous les segments import
                            // mettent à jour le même enregistrement.
                            TextFileIO._ENREG crsenreg = Enregistrements[Enregistrements.Count - 1];

                            // MAJ des champs
                            foreach (SegmentImport si in sit)
                            {
                                si.ChampFichierMaj.SetValue(dr[si.NomChampTable], ref crsenreg);
                            }

                            // Chargement des champs pour les liens parents
                            if (depLT.Profondeur > 0 && j > 0)
                            {
                                LienTables depInf = depLT.DependDe;
                                while (depInf != null)
                                {
                                    // L'enregistrement en cours pour ce lien
                                    DataRow drinf = depInf.Segment.Rows[importTables.getIndexImportEnregistrement(depInf.Segment)];
                                    // MAJ des champs
                                    SegmentImport[] sitinf = Segments.getImports(depInf.Segment);
                                    foreach (SegmentImport si in sitinf)
                                    {
                                        si.ChampFichierMaj.SetValue(drinf[si.NomChampTable], ref crsenreg);
                                    }
                                    // On passe à la dépendance supérieure
                                    depInf = depInf.DependDe;
                                }
                            }

                            // On stock la nouvelle valeur de l'enregistrement
                            Enregistrements[Enregistrements.Count - 1] = crsenreg;

                            // On descend les niveau dans les liens
                            LecLiensTables(dr);
                        }
                    }
                }
            }

            // MAJ des segments fichiers dans le cas d'un seul segment table
            private void majLiensFichier(DataRow dr, SegmentImport[] sit, LienFichier lf)
            {
                // Création d'un nouvel enregistrement (pour le lien racine)
                TextFileIO._ENREG enreg = lf.Segment.NewEnreg();

                // MAJ des champs
                foreach (SegmentImport si in sit)
                {
                    if (lf.Segment.Champs.Contains(si.ChampFichierMaj))
                        si.ChampFichierMaj.SetValue(dr[si.NomChampTable], ref enreg);
                }

                // Traitement des dépendances
                foreach (LienFichier lfd in importFichier.Liens.getLiens(lf)) majLiensFichier(dr, sit, lfd);
            }

            /// <summary>
            /// Création des segments import
            /// </summary>
            /// <param name="xn"></param>
            private void CreSEG(System.Xml.XmlNode xn)
            {
                //Si aucun attribut, c'est une erreur.
                if (xn.Attributes.Count == 0)
                    throw new FileIOError(this.GetType().FullName, "Un <segment> dans <import> est sans attribut.");

                // Nom du segment
                string nomsegment = "";
                string segmentvirtuel = "";
                string si = "";
                // Recherche de l'attribut nom obligatoire
                foreach (System.Xml.XmlAttribute xa in xn.Attributes)
                {
                    if (xa.Name.ToLower().Equals("nom") && !xa.Value.Equals(""))
                            nomsegment = xa.Value.Trim();
                    else if (xa.Name.ToLower().Equals("virtuel")&& !xa.Value.Equals(""))
                        segmentvirtuel = xa.Value.Trim();
                    else if (xa.Name.ToLower().Equals("si") && !xa.Value.Equals(""))
                        si = xa.Value.Trim();
                }
                // Si aucun nom de segment, on sort en erreur.
                if (nomsegment.Equals(""))
                    throw new FileIOError(this.GetType().FullName, "Un <segment> dans <import> est sans attribut 'nom'.");
                // Si segment virtuel, si est obligatoire
                if (!segmentvirtuel.Equals("") && si.Equals(""))
                    throw new FileIOError(this.GetType().FullName, "l'attribut 'si' est obligatoire dans le cas d'un segment virtuel ('" + nomsegment + "')");

                // Test si le segment existe
                if (!importTables.dsInterne.Tables.Contains(segmentvirtuel.Equals("") ? nomsegment : segmentvirtuel))
                    throw new FileIOError(this.GetType().FullName, "le segment '" + (segmentvirtuel.Equals("") ? nomsegment : segmentvirtuel) + "' n'existe pas dans <tables>.");

                // Dans le cas d'un segment virtuel, il faut le créer.
                if (!segmentvirtuel.Equals("")) importTables.CreSEGVirtuel(nomsegment, segmentvirtuel, si);

                // On cré un tableau de chaine correspondant à chaque ligne de <segment>
                string[] champs;
                champs = xn.FirstChild.Value.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (champs.Length == 1 && champs[0].Equals(xn.FirstChild.Value))
                    champs = xn.FirstChild.Value.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (champs.Length == 0) return;

                // Pour chaque ligne trouvée, on crée un segment import
                foreach (string champ in champs)
                {
                    // Chaine sans aucun caractère de contrôle avant le premier '.'
                    string sc = "";
                    int scp = -1;
                    foreach (char c in champ)
                    {
                        scp++;
                        if (!Char.IsControl(c)) break;
                    }
                    // Si chaine vide, on passe à l'occurence suivante.
                    if (scp < 0) continue;
                    if (scp.Equals(champ.Length - 1)) continue;
                    sc = champ.Substring(scp);
                    // La chaine doit au moins commencer par un point
                    if (!sc.StartsWith("."))
                        throw new FileIOError(this.GetType().FullName, "Le ligne doit obligatoirement commencer par un '.'");
                    // Ajoute le segment import
                    Segments.Add(new SegmentImport(importTables.dsInterne.Tables[nomsegment], importFichier.Segments, sc));
                }
            }

            /// <summary>
            /// Renvoi pour un segment table le lien fichier racine qui est lié
            /// </summary>
            /// <param name="dt">Le segment table</param>
            /// <returns>Le lien fichier racine</returns>
            /// <remarks>La recherche est basée sur les segments imports</remarks>
            private LienFichier getLienFichierRacine(DataTable dt)
            {
                // La table doit être précisée
                if (dt == null) return null;
                if (importFichier.Liens.Count == 0) return null;

                // Si plusieurs tables, il faut que cette table soit une table de base
                if (importTables.Liens.Count > 0)
                {
                    LienTables lt = importTables.Liens.getLien(dt);
                    if (lt == null)
                        throw new FileIOError(this.GetType().FullName, "Imporsible de retrouver le lien pour la table '" + dt.TableName + "'");
                    if (lt.Profondeur > 0)
                        throw new FileIOError(this.GetType().FullName, "La table '" + dt.TableName + "' ne peut pas mettre à jour de lien fichier racine");
                }

                // Par défaut, aucun Lien fichier racine trouvé
                LienFichier racLF = null;
                // Recherche des liens racines pour le fichier
                LienFichier[] racLFS = importFichier.Liens.getLiens((LienFichier)null);
                if (racLFS.Length == 0) return null;

                // On cherche le segment racine fichier MAJ par le segment table
                foreach (LienFichier testLF in racLFS)
                {
                    SegmentImport[] sit = Segments.getImports(dt, testLF.Segment);
                    if (sit.Length == 0) continue;
                    if (racLF == null) racLF = testLF;
                    else
                        throw new FileIOError(this.GetType().FullName, "Le segment export pour la table '" + dt.TableName + "' met à jour plusieurs segments racine du fichier");
                }

                // Renvoie le lien fichier racine trouvé
                return racLF;
            }

            // Test uniquement dans le cas d'un seul segment table et plusieurs segments fichiers
            private void testMajSegmentsFichierObligatoire(DataTable dt, LienFichier dependde)
            {
                // Recherche des dépendances
                LienFichier[] lfs = importFichier.Liens.getLiens(dependde);
                if (lfs.Length == 0) return;
                // Des champs du segment supérieurs parent sont-il mis à jour.
                bool segsup = (Segments.getImports(dt, dependde.Segment).Length == 0); 

                // Pour chaque dépendance
                foreach (LienFichier lf in lfs)
                {
                    // Des champs sont-il MAJ. 
                    bool segcrs = (Segments.getImports(dt, lf.Segment).Length == 0);

                    // Si segment obligatoire, il faut un champ MAJ
                    if (!lf.Facultatif && !segcrs)
                        throw new FileIOError(this.GetType().FullName, "Le segment table '" + lf.Segment.Nom + "' n'est pas mis à jour par le segment table '" + dt.TableName + "'");

                    // Si des champs sont MAJ mais aucun champ du niveau supérieur, c'est une erreur
                    if (segcrs && !segsup)
                        throw new FileIOError(this.GetType().FullName, "Le segment table '" + lf.Segment.Nom + "' est mis à jour alors que son segment parent '" + dependde.Segment.Nom + "' ne l'est jamais");

                    // Recherche sur les dépedances
                    testMajSegmentsFichierObligatoire(dt, lf);
                }
            }
        }

        /// <summary>
        /// Collection des segments export
        /// </summary>
        private class SegmentsExport : List<SegmentExport>
        {
            /// <summary>
            /// Ajoute le segment export en vérifiant qu'il n'existe pas déja
            /// </summary>
            /// <param name="item">Le segmeent export</param>
            public new void Add(SegmentExport item)
            {
                // Vérification que ce segment export n'existe pas
                foreach (SegmentExport se in this)
                {
                    if (se.SegmentFichier.Equals(item.SegmentFichier) &&
                        se.NomChampFichier.Equals(item.NomChampFichier) &&
                        se.ChampTable.Equals(item.ChampTable))
                        throw new FileIOError(this.GetType().FullName, "Segment export en double ('" + item.SegmentFichier.Nom + "." + item.NomChampFichier + "." + item.ChampTable.Table.TableName + "." + item.ChampTable.ColumnName + ")");
                }
                // Ajout du segment export à la liste
                base.Add(item);
            }
            /// <summary>
            /// Indique si le segment fichier est utilisé lors de l'export
            /// </summary>
            /// <param name="segment">Le segment fichier</param>
            /// <returns>Vrai si utilisé</returns>
            public bool Contains(SegmentFichier segment)
            {
                // Vérification que ce segment export n'existe pas
                foreach (SegmentExport se in this)
                {
                    if (se.SegmentFichier.Equals(segment)) return true;
                }
                // Segment non trouvé
                return false;
            }

            /// <summary>
            /// Renvoie un tableau des segments exports utilisant le segment fichier
            /// </summary>
            /// <param name="segmentfichier">Le segment fichier</param>
            public SegmentExport[] getExports(SegmentFichier segmentfichier)
            {
                // Le tableau des liens
                SegmentExport[] ses = new SegmentExport[] { };

                // Recherche des dépendances
                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i].SegmentFichier.Equals(segmentfichier))
                    {
                        SegmentExport[] cpses = new SegmentExport[ses.Length + 1];
                        if (ses.Length > 0) ses.CopyTo(cpses, 0);
                        cpses[ses.Length] = this[i];
                        ses = cpses;
                    }
                }
                // Revoie les segments export dépendant.
                return ses;
            }
        }

        /// <summary>
        /// Segment décrivant quels champs fichier mettent à jour quels champs tables
        /// </summary>
        private class SegmentExport
        {
            // Valeurs privées des propriétés
            private SegmentFichier segmentfichier;
            private string nomchampfichier;
            private DataColumn champtable;

            /// <summary>
            /// Le segment fichier
            /// </summary>
            public SegmentFichier SegmentFichier
            {
                get { return segmentfichier; }
                private set { segmentfichier = value; }
            }
            /// <summary>
            /// Nom du champ
            /// </summary>
            /// <remarks>ATTENTION : si commence par '=', il s'agit d'une constante</remarks>
            public string NomChampFichier
            {
                get { return nomchampfichier; }
                private set { nomchampfichier = value; }
            }
            /// <summary>
            /// La colonne à mettre à jour
            /// </summary>
            public DataColumn ChampTable
            {
                get { return champtable; }
                private set { champtable = value; }
            }

            /// <summary>
            /// Créatioh du segment export
            /// </summary>
            /// <param name="segmentfichier">Le segment fichier à utiliser</param>
            /// <param name="dsExport">Le dataset export à utiliser</param>
            /// <param name="sc">La chaine contenant la régle de mise à jour</param>
            internal SegmentExport(SegmentFichier segmentfichier, DataSet dsExport, string sc)
            {
                // Analyse de la syntaxe de la ligne
                string[] corr = sc.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                // Une constante peut-être noté .=valeur ou .=[valeur]
                if (corr[0].StartsWith("=["))
                {
                    // Par défaut, pas de crochet fermant trouvé
                    int icf = -1;
                    // Recherche du cochet fermant
                    for (int i = 0; i < corr.Length; i++)
                    {
                        if (corr[i].EndsWith("]"))
                        {
                            icf = i;
                            break;
                        }
                    }
                    // Si aucun crochet fermant, la syntaxe est incorrecte
                    if (icf == -1)
                        throw new FileIOError(this.GetType().FullName, "'" + sc + "' a une syntaxe incorrecte (contante avec une '[' mais sans ']'");
                    // On enlève le crochet ouvrant '['
                    corr[0] = corr[0].Remove(1, 1);
                    // On enlève le crochet fermant ']'
                    corr[icf] = corr[icf].Substring(0, corr[icf].Length - 1);
                    // Si des '.' on provoqué des splits entre les deux crochets, on concatène les chaines en rajoutant les '.' nécessaire
                    if (icf > 0)
                    {
                        // On copie le tableau dans un tableau tempoire
                        string[] tmpc = new string[corr.Length];
                        corr.CopyTo(tmpc, 0);
                        // Le nouveau tableau à une longueur diminuée du nombre d'occurente à concaténer
                        corr = new string[tmpc.Length - icf];
                        for (int ic = 0; ic < tmpc.Length; ic++)
                        {
                            if (ic <= icf)
                            {
                                if (ic > 0) corr[0] = corr[0] + ".";
                                corr[0] = corr[0] + tmpc[ic];
                            }
                            else
                                corr[ic - icf] = tmpc[ic];
                        }                       
                    }
                }
                // Test si le nombre d'intération est correcte
                if (corr.Length < 2 || corr.Length > 3)
                    throw new FileIOError(this.GetType().FullName, "'" + sc + "' a une syntaxe incorrecte");
                // Le segment
                this.SegmentFichier = segmentfichier;
                // Si le champ n'est pas une constante, on vérifie l'existance dans la collection des champs
                if (!corr[0].StartsWith("=") && !this.SegmentFichier.Champs.Contains(corr[0]))
                    throw new FileIOError(this.GetType().FullName, "Le champ '" + corr[0] + "' n'existe pas dans le segment fichier '" + segmentfichier.Nom + "'");
                NomChampFichier = corr[0];
                // Le segment table existe-t-il
                if (!dsExport.Tables.Contains(corr[1]))
                    throw new FileIOError(this.GetType().FullName, "'" + corr[1] + "' n'existe pas dans <tables>");
                // La colonne existe-t-elle ?
                if (!dsExport.Tables[corr[1]].Columns.Contains(corr.Length == 3 ? corr[2] : corr[0]))
                    throw new FileIOError(this.GetType().FullName, "'" + (corr.Length == 3 ? corr[2] : corr[0]) + "' n'existe pas dans la tables '" + corr[1] + "'");
                ChampTable = dsExport.Tables[corr[1]].Columns[corr.Length == 3 ? corr[2] : corr[0]];
            }
        }

        /// <summary>
        /// Collection des segments import
        /// </summary>
        private class SegmentsImport : List<SegmentImport>
        {
            /// <summary>
            /// Ajoute le segment import en vérifiant qu'il n'existe pas déja
            /// </summary>
            /// <param name="item">Le segmeent import</param>
            public new void Add(SegmentImport item)
            {
                // Vérification que ce segment export n'existe pas
                foreach (SegmentImport se in this)
                {
                    if (se.SegmentTable.Equals(item.SegmentTable) &&
                        se.NomChampTable.Equals(item.NomChampTable) &&
                        se.ChampFichierMaj.Equals(item.ChampFichierMaj))
                        throw new FileIOError(this.GetType().FullName, "Segment import en double ('" + item.SegmentTable.TableName + "." + item.NomChampTable + "." + item.ChampFichierMaj.Nom + "')");
                }
                // Ajout du segment export à la liste
                base.Add(item);
            }

            /// <summary>
            /// Indique si le segment table est utilisé lors de l'import
            /// </summary>
            /// <param name="segment">Le segment table</param>
            /// <returns>Vrai si utilisé</returns>
            public bool Contains(DataTable segment)
            {
                // Vérification que ce segment export n'existe pas
                foreach (SegmentImport se in this)
                {
                    if (se.SegmentTable.Equals(segment)) return true;
                }
                // Segment non trouvé
                return false;
            }

            /// <summary>
            /// Renvoie un tableau des segments imports utilisant le segment table
            /// </summary>
            /// <param name="segmenttable">Le segment table</param>
            /// <returns>La liste des segments imports</returns>
            public SegmentImport[] getImports(DataTable segmenttable)
            {
                // Le tableau des liens
                SegmentImport[] ses = new SegmentImport[] { };

                // Recherche des dépendances
                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i].SegmentTable.Equals(segmenttable))
                    {
                        SegmentImport[] cpses = new SegmentImport[ses.Length + 1];
                        if (ses.Length > 0) ses.CopyTo(cpses, 0);
                        cpses[ses.Length] = this[i];
                        ses = cpses;
                    }
                }
                // Revoie les segments export dépendant.
                return ses;
            }

            /// <summary>
            /// Renvoie un tableau des segments imports utilisant le segment table et le segment fichier
            /// </summary>
            /// <param name="segmenttable">Le segment table</param>
            /// <param name="segmentfichier">Le segment fichier</param>
            /// <returns>La liste des segments imports</returns>
            public SegmentImport[] getImports(DataTable segmenttable, SegmentFichier segmentfichier)
            {
                // Le tableau des liens
                SegmentImport[] ses = new SegmentImport[] { };

                // Recherche des dépendances
                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i].SegmentTable.Equals(segmenttable) && segmentfichier.Champs.Contains(this[i].ChampFichierMaj))
                    {
                        SegmentImport[] cpses = new SegmentImport[ses.Length + 1];
                        if (ses.Length > 0) ses.CopyTo(cpses, 0);
                        cpses[ses.Length] = this[i];
                        ses = cpses;
                    }
                }
                // Revoie les segments export dépendant.
                return ses;
            }
        }

        /// <summary>
        /// Segment décrivant quels champs tables mettent à jour quels champs fichier 
        /// </summary>
        private class SegmentImport
        {
            // Valeurs privées des propriétés
            private DataTable segmenttable;
            private string nomchamptable;
            private ChampFichier champfichiermaj;

            /// <summary>
            /// Le segment table
            /// </summary>
            public DataTable SegmentTable
            {
                get { return segmenttable; }
                private set { segmenttable = value; }
            }
            /// <summary>
            /// Nom du champ dans la table
            /// </summary>
            /// <remarks>ATTENTION : si commence par '=', il s'agit d'une constante</remarks>
            public string NomChampTable
            {
                get { return nomchamptable; }
                private set { nomchamptable = value; }
            }

            /// <summary>
            /// La champ fichier à mettre à jour
            /// </summary>
            public ChampFichier ChampFichierMaj
            {
                get { return champfichiermaj; }
                private set { champfichiermaj = value; }
            }

            /// <summary>
            /// Créatioh du segment import
            /// </summary>
            /// <param name="dtExport">La table à utiliser</param>
            /// <param name="segmentsfichier">Les segments fichier à importer</param>
            /// <param name="sc">La chaine contenant la régle de mise à jour</param>
            internal SegmentImport(DataTable dtExport, SegmentsFichier segmentsfichier, string sc)
            {
                // Analyse de la syntaxe de la ligne
                string[] corr = sc.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                // Une constante peut-être noté .=valeur ou .=[valeur]
                if (corr[0].StartsWith("=["))
                {
                    // Par défaut, pas de crochet fermant trouvé
                    int icf = -1;
                    // Recherche du cochet fermant
                    for (int i = 0; i < corr.Length; i++)
                    {
                        if (corr[i].EndsWith("]"))
                        {
                            icf = i;
                            break;
                        }
                    }
                    // Si aucun crochet fermant, la syntaxe est incorrecte
                    if (icf == -1)
                        throw new FileIOError(this.GetType().FullName, "'" + sc + "' a une syntaxe incorrecte (contante avec une '[' mais sans ']'");
                    // On enlève le crochet ouvrant '['
                    corr[0] = corr[0].Remove(1, 1);
                    // On enlève le crochet fermant ']'
                    corr[icf] = corr[icf].Substring(0, corr[icf].Length - 1);
                    // Si des '.' on provoqué des splits entre les deux crochets, on concatène les chaines en rajoutant les '.' nécessaire
                    if (icf > 0)
                    {
                        // On copie le tableau dans un tableau tempoire
                        string[] tmpc = new string[corr.Length];
                        corr.CopyTo(tmpc, 0);
                        // Le nouveau tableau à une longueur diminuée du nombre d'occurente à concaténer
                        corr = new string[tmpc.Length - icf];
                        for (int ic = 0; ic < tmpc.Length; ic++)
                        {
                            if (ic <= icf)
                            {
                                if (ic > 0) corr[0] = corr[0] + ".";
                                corr[0] = corr[0] + tmpc[ic];
                            }
                            else
                                corr[ic - icf] = tmpc[ic];
                        }                       
                    }
                }
                // Test si le nombre d'intération est correcte
                if (corr.Length < 2 || corr.Length > 3)
                    throw new FileIOError(this.GetType().FullName, "'" + sc + "' a une syntaxe incorrecte");
                // Le segment table
                this.SegmentTable = dtExport;
                // Si le champ n'est pas une constante, on vérifie l'existance dans la collection des champs
                if (!corr[0].StartsWith("=") && !this.SegmentTable.Columns.Contains(corr[0]))
                    throw new FileIOError(this.GetType().FullName, "Le champ '" + corr[0] + "' n'existe pas dans le segment table '" + SegmentTable.TableName + "'");
                NomChampTable = corr[0];
                // Le segment fichier existe-t-il
                if (!segmentsfichier.Contains(corr[1]))
                    throw new FileIOError(this.GetType().FullName, "'" + corr[1] + "' n'existe pas dans <fichiers>");
                // Le champ existe-t-il ?
                if (!segmentsfichier[corr[1]].Champs.Contains(corr.Length == 3 ? corr[2] : corr[0]))
                    throw new FileIOError(this.GetType().FullName, "'" + (corr.Length == 3 ? corr[2] : corr[0]) + "' n'existe pas dans le segment fichier '" + corr[1] + "'");
                ChampFichierMaj = segmentsfichier[corr[1]].Champs[corr.Length == 3 ? corr[2] : corr[0]];
            }
        }
    }
}