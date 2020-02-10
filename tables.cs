using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;

namespace llt.FileIO.ImportExport
{
    public partial class Convert
    {
        /// <summary>
        /// Classe gérant les tables
        /// </summary>
        private class Tables
        {
            // Les propriété privées
            private DataSet dsinterne;
            private LiensTables liens;
            
            /// <summary>
            /// Le DataSet utilisé par Export ou Import.
            /// </summary>
            public DataSet dsInterne
            {
                get { return dsinterne; }
                protected set { dsinterne = value; }
            }
            /// <summary>
            /// Le(s) lien(s) reliant les différents segments contenus dans le fichier.
            /// </summary>
            public LiensTables Liens
            {
                get { return liens; }
                protected set { liens = value; }
            }

            /// <summary>
            /// Création de la classe depuis le noeud xml de paramètrage
            /// </summary>
            /// <param name="xn">Le noeud <![CDATA[<tables>]]> du fichier XML</param>
            public Tables(System.Xml.XmlNode xn)
            {
                // Initialisation des valeurs par défaut
                dsInterne = new DataSet("Interne");
                Liens = new LiensTables();

                // Traitement des différents noeuds dépendant de fichier.
                foreach (System.Xml.XmlNode noeud in xn.ChildNodes)
                {
                    // Traitement des segments.
                    if (noeud.Name.ToLower().Equals("segment")) CreSEG(noeud);
                    // Traitement des liens.
                    if (noeud.Name.ToLower().Equals("liens")) CreLIENS(noeud);
                }

                // Il faut au minimum un segment
                if (dsInterne.Tables.Count == 0)
                    throw new FileIOError(this.GetType().FullName, "Il n'y a aucun segment dans <tables>");
                // Il faut qu'au minimum UN segment possède au moins UN champ sinon traitement inutile.
                bool ok = false;
                foreach (DataTable dt in dsInterne.Tables)
                {
                    if (ok = dt.Columns.Count > 0) break;
                }
                if (!ok)
                    throw new FileIOError(this.GetType().FullName, "Il n'y a aucun champ de définit dans <tables>");

                // Si plusieurs segments ...
                if (dsInterne.Tables.Count > 1)
                {
                    // ... il faut obligatoirement des liens.
                    if (Liens.Count == 0)
                        throw new FileIOError(this.GetType().FullName, "Il faut indiquer les liens entre les différents segments");

                    // ... il ne peut y avoir qu'un seul lien obligatoire au niveau 0.
                    int i = 0;
                    foreach (LienTables lt in Liens)
                    {
                        if (lt.Profondeur == 0 && !lt.Facultatif) i++;
                    }
                    if (i > 1)
                        throw new FileIOError(this.GetType().FullName, "Il est impossible d'avoir plusieurs segments de base obligatoire.");

                    // ... UniquePar est obligatoire pour les liens disposant de lien dépendant
                    string msgerr = "";
                    foreach (LienTables lt in Liens)
                    {
                        if (Liens.getLiens(lt).Length > 0 && lt.Segment.PrimaryKey.Length == 0)
                        {
                            if (msgerr.Equals("")) msgerr = msgerr + ", ";
                            msgerr = msgerr + "'" + lt.Segment.TableName + "'";
                        }
                    }
                    if (!msgerr.Equals(""))
                        throw new FileIOError(this.GetType().FullName, "Le(s) segment(s) " + msgerr + " ne comporte(nt) pas d'attribut 'uniquepar' permettant de les identifier");
                }
            }

            /// <summary>
            /// Ajoute le nom et la condition associée d'un segment virtuel.
            /// </summary>
            /// <param name="nomsegment">Nom du segment virtuel</param>
            /// <param name="segmentvirtuelde">Nom du segment DataTable</param>
            /// <param name="si">La condition de sélection des enregistrements</param>
            internal void AddNomSegmentVirtuel(string nomsegment, string segmentvirtuelde, string si)
            {
                // Le segment doit exister
                if (!dsinterne.Tables.Contains(segmentvirtuelde))
                    throw new FileIOError(this.GetType().FullName, "Le segment table ('" + segmentvirtuelde + "') existe déja");
                // On ajoute l'information sur le segment virtuel
                dsinterne.Tables[segmentvirtuelde].ExtendedProperties.Add("SEGV_" + nomsegment, si);
            }
            /// <summary>
            /// Renvoie la liste des segments virtuels avec la condition associée
            /// </summary>
            /// <param name="segmentvirtuelde">Le segment table</param>
            /// <returns>Renvoie un liste (vide si aucun segment)</returns>
            internal System.Collections.Hashtable SegmentsVirtuel(string segmentvirtuelde)
            {
                System.Collections.Hashtable segmentsvirtuel = new System.Collections.Hashtable();
                foreach (object key in dsinterne.Tables[segmentvirtuelde].ExtendedProperties.Keys)
                {
                    if (key.ToString().StartsWith("segv_",StringComparison.CurrentCultureIgnoreCase))
                    {
                        segmentsvirtuel.Add(key.ToString().Substring(5), dsinterne.Tables[segmentvirtuelde].ExtendedProperties[key]);
                    }
                }
                return segmentsvirtuel;
            }
            /// <summary>
            /// Renvoie la liste des enregistrements par segment virtuel.
            /// </summary>
            /// <param name="segmentvirtuelde">Le segment table</param>
            /// <param name="segmentsvirtuel">La liste des segments virtuels</param>
            /// <param name="where">Contient une chaine SQL permettant de limiter la sélection des enregistrement</param>
            /// <returns>Renvoie un liste (vide si aucun enregistrement)</returns>
            internal Dictionary<string, DataRow[]> SegmentVirtuelEnrs(string segmentvirtuelde, System.Collections.Hashtable segmentsvirtuel, string where = "")
            {
                Dictionary<string, DataRow[]> segmentsvirtuelenrs = new Dictionary<string, DataRow[]>();
                foreach (object key in segmentsvirtuel.Keys)
                {
                    DataRow[] drs = dsinterne.Tables[segmentvirtuelde].Select(where + (where == "" ? "" : " and ") + segmentsvirtuel[key].ToString());
                    if (drs.Length > 0) segmentsvirtuelenrs.Add(key.ToString(), drs);
                }
                return segmentsvirtuelenrs;
            }

            /* ABANDON DE CE MODE DE FONCTIONNEMENT DES SEGMENTS VIRTUELS
            /// <summary>
            /// Création d'un segment table virtuel dans le DataSet
            /// </summary>
            /// <param name="nomsegment">Le nom du segment table à créér</param>
            /// <param name="segmentvirtuelde">Le nom du segment table dont il est issu</param>
            /// <param name="si">La condition à appliquer pour transfert les enregistrement d'un segment à l'autre</param>
            internal void CreSEGVirtuel(string nomsegment, string segmentvirtuelde, string si)
            {
                // Si ne doit pas être vide
                si = si.Trim();
                if (si.Equals(""))
                    throw new FileIOError(this.GetType().FullName, "L'attribut 'si' est obligatoire dans le cas d'un segment virtuel ('" + nomsegment + "')");
                // Le segment ne doit pas exister
                if (dsinterne.Tables.Contains(nomsegment))
                    throw new FileIOError(this.GetType().FullName, "Le segment table ('" + nomsegment + "') existe déja");
                // Création du segment table virtuel
                DataTable dt = dsInterne.Tables[segmentvirtuelde].Clone();
                dt.TableName = nomsegment;
                dt.ExtendedProperties["virtuelde"] = segmentvirtuelde;
                dt.ExtendedProperties["si"] = si;
                // Ajout de la table à l collection de table
                dsinterne.Tables.Add(dt);
            }
            
            /// <summary>
            /// Suppression des segments virtuels
            /// </summary>
            internal void DelSEGVirtuel()
            {
                for (int i = 0; i < dsinterne.Tables.Count; )
                {
                    if (dsinterne.Tables[i].ExtendedProperties.Contains("virtuelde"))
                        dsinterne.Tables.Remove(dsinterne.Tables[i]);
                    else
                        i++;
                }
            }

            /// <summary>
            /// Renvoie les segments table virtuel qui dépendent de <paramref name="segmentvirtuelde"/>
            /// </summary>
            /// <param name="segmentvirtuelde">Le nom du segment table dont doit dépendre le segment table virtuel</param>
            /// <returns>Tableau contenant les segments virtuels</returns>
            internal DataTable[] getSEGVirtuel(string segmentvirtuelde)
            {
                DataTable[] lsv = new DataTable[] { };
                foreach (DataTable dt in dsinterne.Tables)
                {
                    if (dt.ExtendedProperties.Contains("virtuelde") &&
                        dt.ExtendedProperties["virtuelde"].ToString().Equals(segmentvirtuelde, StringComparison.CurrentCultureIgnoreCase))
                    {
                        DataTable[] cplsv = new DataTable[lsv.Length + 1];
                        if (lsv.Length > 0) lsv.CopyTo(cplsv, 0);
                        cplsv[lsv.Length] = dt;
                        lsv = cplsv;
                    }
                }

                // Renvoie la liste
                return lsv;
            }
            /// <summary>
            /// Renvoie le segment <paramref name="segmentvirtuelde"/> plus les segments table virtuel qui en dépendent
            /// </summary>
            /// <param name="segmentvirtuelde">Le nom du segment table dont doit dépendre le segment table virtuel</param>
            /// <returns>Tableau contenant le segment <paramref name="segmentvirtuelde"/> plus les segments virtuels</returns>
            internal DataTable[] getSEGplusSEGVirtuel(DataTable segmentvirtuelde)
            {
                DataTable[] lsv = getSEGVirtuel(segmentvirtuelde.TableName);
                if (lsv.Length == 0) return new DataTable[] { segmentvirtuelde };

                // Création d'une liste contenant tous les segments.
                DataTable[] lssv = new DataTable[lsv.Length + 1];
                lssv[0] = segmentvirtuelde;
                lsv.CopyTo(lssv, 1);
                return lssv;
            }
            */
            
            /// <summary>
            /// Renvoie l'index de l'enregistrement en cours
            /// </summary>
            /// <param name="segment">Le segment table</param>
            /// <returns>Renvoie la position (-1 si aucun enregistrement en cours)</returns>
            internal int getIndexImportEnregistrement(DataTable segment)
            {
                if (!segment.ExtendedProperties.Contains("IndexImportEnregistrement"))
                    setIndexImportEnregistrement(segment, (int)-1);
                return (int)segment.ExtendedProperties["IndexImportEnregistrement"];
            }
            /// <summary>
            /// Définit l'enregistrement en cours
            /// </summary>
            /// <param name="segment">Le segment table</param>
            /// <param name="index">L'index de l'eenregistrement en cours</param>
            internal void setIndexImportEnregistrement(DataTable segment, int index)
            {
                if (!segment.ExtendedProperties.Contains("IndexImportEnregistrement"))
                    segment.ExtendedProperties.Add("IndexImportEnregistrement", index);
                else
                    segment.ExtendedProperties["IndexImportEnregistrement"] = index;
            }

            /// <summary>
            /// Création du segment dans le DataSet
            /// </summary>
            /// <param name="xn">Le noued XML <![CDATA[<segment>]]> à traiter.</param>
            private void CreSEG(System.Xml.XmlNode xn)
            {
                //Si aucun attribut, c'est une erreur.
                if (xn.Attributes.Count == 0)
                    throw new FileIOError(this.GetType().FullName, "Un <segment> dans <fichier> est sans attribut.");
                // Nom du segment et uniquepar
                string nomsegment = "";
                string uniquepar = "";
                // Recherche de l'attribut nom obligatoire
                foreach (System.Xml.XmlAttribute xa in xn.Attributes)
                {
                    if (xa.Name.ToLower().Equals("nom") && !xa.Value.Equals("") && nomsegment.Equals("")) nomsegment = xa.Value.Trim();
                    if (xa.Name.ToLower().Equals("uniquepar") && !xa.Value.Equals("") && uniquepar.Equals("")) uniquepar = xa.Value.Trim();
                }
                // Si aucun nom de segment, on sort en erreur.
                if (nomsegment.Equals(""))
                    throw new FileIOError(this.GetType().FullName, "Un <segment> dans <tables> est sans attribut 'nom'.");

                // Test si segment n'existe pas déja dans le DataSet
                if (dsInterne.Tables.Contains(nomsegment))
                    throw new FileIOError(this.GetType().FullName, "Le segment '" + nomsegment + "' dans <tables> doit être unique.");

                // Création de la table
                DataTable dt = dsInterne.Tables.Add(nomsegment);

                // On ajoute les champs.
                foreach (System.Xml.XmlNode champ in xn.ChildNodes)
                {
                    if (champ.Name.ToLower().Equals("champ"))
                    {
                        //Si aucun attribut, c'est une erreur.
                        if (champ.Attributes.Count == 0)
                            throw new FileIOError(this.GetType().FullName, "Le segment '" + nomsegment + "' contient un élément <champ> sans attribut.");
                        // Nom du champ et format
                        string nomchamp = "";
                        string formatchamp = "";

                        // Recherche de l'attribut nom obligatoire
                        foreach (System.Xml.XmlAttribute xa in champ.Attributes)
                        {
                            if (xa.Name.ToLower().Equals("nom") && !xa.Value.Equals("") && nomchamp.Equals("")) nomchamp = xa.Value.Trim();
                            if (xa.Name.ToLower().Equals("format") && !xa.Value.Equals("") && formatchamp.Equals("")) formatchamp = xa.Value.Trim();
                        }
                        if (nomchamp.Equals("") || formatchamp.Equals(""))
                            throw new FileIOError(this.GetType().FullName, "Le segment '" + nomsegment + "' contient un élément <champ> sans l'attribut 'nom' et/ou 'format'.");

                        // Ajoute le champ à la collection des champs pour le segmet
                        System.Type type = System.Type.GetType(formatchamp, false, true);
                        if (type == null)
                            throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' n\'est pas un type de données reconnu par .NET.");
                        dt.Columns.Add(nomchamp, type);
                    }
                }

                // Une fois les champs créés, on traite la clé primaire.
                if (!uniquepar.Equals(""))
                {
                    // Récupération du(des) champ(s)
                    string[] cles = uniquepar.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (cles.Length > 0)
                    {
                        // Test si le(s) champ(s) existent
                        foreach (string cle in cles)
                        {
                            if (!dt.Columns.Contains(cle))
                                throw new FileIOError(this.GetType().FullName, "La cle '" + cle + "' n'existe pas dans le segment table '" + nomsegment + "'");
                        }
                        // Création de la clé primaire
                        DataColumn[] pk = new DataColumn[cles.Length];
                        for (int i = 0; i < cles.Length; i++)
                        {
                            dt.Columns[cles[i]].AllowDBNull = false;
                            pk[i] = dt.Columns[cles[i]];
                        }
                        // Assignation de lé clé primaire
                        dt.PrimaryKey = pk;
                    }
                }
            }

            /// <summary>
            /// Création des liens (relation) entre les tables.
            /// </summary>
            /// <param name="xn">Le noued XML <![CDATA[<liens>]]> à traiter.</param>
            private void CreLIENS(System.Xml.XmlNode xn)
            {
                // Le premier et le dernier noeud enfant doivent être identique.
                if (!xn.FirstChild.Equals(xn.LastChild))
                    throw new FileIOError(this.GetType().FullName, "La balise <liens> n'a pas une syntaxe correct");

                // On cré un tableau de chaine correspondant à chaque ligne de <liens>
                string[] liens;
                liens = xn.FirstChild.Value.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (liens.Length == 1 && liens[0].Equals(xn.FirstChild.Value))
                    liens = xn.FirstChild.Value.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (liens.Length == 0) return;

                // Traitement de chaine
                foreach (string s in liens)
                {
                    // Chaine sans aucun caractère de contrôle
                    string sc = "";
                    foreach (char c in s)
                    {
                        if (!Char.IsControl(c)) sc = sc + c.ToString();
                    }
                    sc = sc.Trim(); // Supprime les espaces inutiles
                    if (sc.Equals("")) continue; // Si chaine vide, on passe à l'occurence suivante.
                    // La chaine doit au moins commencer par un point
                    if (!sc.StartsWith("."))
                        throw new FileIOError(this.GetType().FullName, "Un lien doit obligatoirement commencer par un '.'");
                    // On calcul la profondeur en fonction du nombre de point (attention, base 0)
                    int profondeur = -1;
                    foreach (char c in sc)
                    {
                        if (c.Equals('.')) profondeur++; else break;
                    }
                    // Test si des caractères après les points.
                    if (sc.Length.Equals(profondeur + 1))
                        throw new FileIOError(this.GetType().FullName, "'" + sc + "' : pas de nom de segment.");

                    // Si profondeur supérieure à 0, cela veut dire qu'il s'agit d'un lien enfant.
                    LienTables lienparent = null;
                    if (profondeur > 0)
                    {
                        // Recherche du lien parent
                        for (int il = Liens.Count - 1; il >= 0; il--)
                        {
                            if (Liens[il].Profondeur.Equals(profondeur - 1))
                            {
                                lienparent = Liens[il];
                                break;
                            }
                        }
                        // Si aucun lien parent, il s'agit d'une anomalie.
                        if (lienparent == null)
                            throw new FileIOError(this.GetType().FullName, "'" + sc + "' : le nombre de '.' est incorrect.");
                    }
                    // Détermine le nom du segment
                    string nomsegment = "";
                    string champspere = "";
                    string champsfils = "";
                    bool facultatif = false;
                    bool unique = false;
                    int ins = sc.IndexOf('.', profondeur + 1);
                    if (ins == -1)
                    {
                        // Pas de champs fils ni d'option. Si segment racine, c'est normal
                        if (profondeur == 0)
                            nomsegment = sc.Substring(profondeur + 1).Trim();
                        // ... sinon, c'est une anomalie
                        else
                            throw new FileIOError(this.GetType().FullName, "'" + sc + "' a une syntaxe incorrecte.");
                    }
                    else
                    {
                        // Nom du segment
                        nomsegment = sc.Substring(profondeur + 1, (ins - profondeur - 1)).Trim();
                        // Nom des champs parents si une dépendance exite
                        if (profondeur > 0)
                        {
                            int ins1 = sc.IndexOf('.', ins + 1);
                            if (ins1 < 0)
                                throw new FileIOError(this.GetType().FullName, "'" + sc + "' a une syntaxe incorrecte.");
                            champspere = sc.Substring(ins + 1, ins1 - ins - 1);
                            // Nom des champs fils
                            ins = sc.IndexOf('.', ins1 + 1);
                            if (ins < 0)
                                champsfils = sc.Substring(ins1 + 1).Trim(); // Pas d'option après le .
                            else
                            {
                                champsfils = sc.Substring(ins1 + 1, ins - ins1 - 1);
                                // Détermine les options
                                string opt = sc.Substring(ins + 1).ToUpper().Trim();
                                if (opt.Equals("F"))
                                    facultatif = true;
                                else if (opt.Equals("U"))
                                    unique = true;
                                else if (opt.Equals("FU"))
                                {
                                    facultatif = true;
                                    unique = true;
                                }
                                else
                                    throw new FileIOError(this.GetType().FullName, "'" + sc + "' : '" + opt + "' a une syntaxe incorrecte.");
                            }
                        }
                        // Pas de dépendance, les options suivent immediatement le nom du segment
                        else
                        {
                            // Détermine les options
                            string opt = sc.Substring(ins + 1).ToUpper().Trim();
                            if (opt.Equals("F"))
                                facultatif = true;
                            else if (opt.Equals("U"))
                                unique = true;
                            else if (opt.Equals("FU"))
                            {
                                facultatif = true;
                                unique = true;
                            }
                            else
                                throw new FileIOError(this.GetType().FullName, "'" + sc + "' : '" + opt + "' a une syntaxe incorrecte.");
                        }
                    }
                    // Est-ce que le segment existe.
                    if (!dsInterne.Tables.Contains(nomsegment))
                        throw new FileIOError(this.GetType().FullName, "Le segment '" + nomsegment + "' n'existe pas.");
                    // Création du lien
                    if (profondeur == 0)
                    {
                        // Il n'est pas possible d'avoir plusieurs fois le même segment au niveau racine.
                        foreach (LienTables lf in Liens)
                        {
                            if (lf.Profondeur == 0 && lf.Segment.TableName.Equals(nomsegment))
                                throw new FileIOError(this.GetType().FullName, "Il n'est pas possible d'avoir plusieurs fois le même segment ('" + nomsegment + "') au niveau racine.");
                        }
                        Liens.Add(new LienTables(dsInterne.Tables[nomsegment], facultatif, unique));
                    }
                    else
                    {
                        // Il n'est pas possible d'avoir plusieurs fois le même segment dépendant d'un même lien.
                        foreach (LienTables lf in Liens)
                        {
                            if (lf.Profondeur > 0 && lf.DependDe.Equals(lienparent) && lf.Segment.TableName.Equals(nomsegment))
                                throw new FileIOError(this.GetType().FullName, "Il n'est pas possible d'avoir plusieurs fois le même sous-segment ('" + nomsegment + "') au niveau du segment '" + lienparent.Segment.TableName + "'.");
                        }
                        Liens.Add(new LienTables(dsInterne.Tables[nomsegment], facultatif, unique, lienparent, champspere.Split(','), champsfils.Split(',')));
                    }
                }
            }
        }
        
        /// <summary>
        /// Collection des liens pour les tables
        /// </summary>
        private class LiensTables : List<LienTables>
        {
            /// <summary>
            /// Retourne le lien correspondant à un segment
            /// </summary>
            /// <param name="segment">Le segment à chercher</param>
            /// <returns>le lien ou null si segment non trouvé</returns>
            public LienTables getLien(DataTable segment)
            {
                // Si le segmnet est un segment virtuel, on utilise le segment réel pour faire la recherche
                DataTable segtrt = segment.ExtendedProperties.Contains("virtuelde") ? (DataTable)(segment.ExtendedProperties["virtuelde"]) : segment;
                // Recherche du lien correspondant au segment
                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i].Segment.Equals(segtrt)) return this[i];
                }
                return null;
            }
            /// <summary>
            /// Retourne les liens pour lesquels un segment est dépendant du segment passé en paramètre
            /// </summary>
            /// <param name="dependde">Le segment parent</param>
            /// <returns>Tableau contenant les liens</returns>
            public LienTables[] getLiens(DataTable dependde)
            {
                // Le tableau des liens
                LienTables[] lfs = new LienTables[] { };

                // Si le segmnet est un segment virtuel, on utilise le segment réel pour faire la recherche
                DataTable segtrt = dependde.ExtendedProperties.Contains("virtuelde") ? (DataTable)(dependde.ExtendedProperties["virtuelde"]) : dependde;

                // Recherche des dépendances
                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i].DependDe != null && this[i].DependDe.Segment.Equals(segtrt))
                    {
                        LienTables[] cplfs = new LienTables[lfs.Length + 1];
                        if (lfs.Length > 0) lfs.CopyTo(cplfs, 0);
                        cplfs[lfs.Length] = this[i];
                        lfs = cplfs;
                    }
                }
                // Revoie les liens dépendants.
                return lfs;
            }
            /// <summary>
            /// Retourne les liens dépendant de celui passé en paramètre
            /// </summary>
            /// <param name="dependde">Le segment parent</param>
            /// <returns>Tableau contenant les liens</returns>
            /// <remarks>Si <paramref name="dependde"/> est null, le tableau contient les liens n'ayant pas de dépendances/></remarks>
            public LienTables[] getLiens(LienTables dependde)
            {
                // Le tableau des liens
                LienTables[] lfs = new LienTables[] { };

                // Recherche des dépendances
                for (int i = 0; i < this.Count; i++)
                {
                    if (dependde != null && this[i].DependDe != null && this[i].DependDe.Equals(dependde) ||
                        dependde == null && this[i].DependDe == null)
                    {
                        LienTables[] cplfs = new LienTables[lfs.Length + 1];
                        if (lfs.Length > 0) lfs.CopyTo(cplfs, 0);
                        cplfs[lfs.Length] = this[i];
                        lfs = cplfs;
                    }
                }
                // Revoie les liens dépendants.
                return lfs;
            }

            /// <summary>
            /// Pour tous les liens, créé dans la table concernée tous les enregistrement présent dans la liste les enregistrements à ajouter
            /// </summary>
            internal void majEnregistrements()
            {
                majEnregistrements(null);
            }
            /// <summary>
            /// MAJ récursive en fonction de l'ordre des liens.
            /// </summary>
            /// <param name="dependde"></param>
            private void majEnregistrements(LienTables dependde)
            {
                // Recherche des liens dépendant
                LienTables[] lt=getLiens(dependde);
                // Pour chaque lien trouvé
                for (int i = 0; i < lt.Length; i++)
                {
                    // MAJ des enregistrements dans la table
                    lt[i].majEnregistrements();
                    // Traitement des lien fils
                    majEnregistrements(lt[i]);
                }
            }
        }

        /// <summary>
        /// Lien entre les différents segments tables.
        /// </summary>
        private class LienTables
        {
            // Valeurs privées des propriétés
            private int profondeur;
            private DataTable segment;
            private LienTables dependde;
            private string[] champspere;
            private string[] champsfils;
            private bool facultatif;
            private bool unique;

            // Liste des enregistrement à ajouter aux enregistrements de la table (opération export)
            private List<DataRow> Enregistrements;
            // Vérifie si l'enregistrement n'existe pas avant de le créer dans la table
            internal bool verifCreEnr;

            /// <summary>
            /// Profondeur dans l'arborescence (0 = niveau racine)
            /// </summary>
            public int Profondeur
            {
                get { return profondeur; }
                protected set { profondeur = value; }
            }
            /// <summary>
            /// Segment concerné par le lien.
            /// </summary>
            public DataTable Segment
            {
                get { return segment; }
                protected set
                {
                    if (value == null)
                        throw new FileIOError(this.GetType().FullName, "Le segment est obligatoire");
                    segment = value;
                }
            }
            /// <summary>
            /// Lien parent
            /// </summary>
            public LienTables DependDe
            {
                get { return dependde; }
                protected set { dependde = value; }
            }
            /// <summary>
            /// Les champs du segment père utilisé dans la relation
            /// </summary>
            public string[] ChampsPere
            {
                get { return champspere; }
                protected set { champspere = value; }
            }
            /// <summary>
            /// Les champs du segment fils utilisé dans la relation
            /// </summary>
            public string[] ChampsFils
            {
                get { return champsfils; }
                protected set { champsfils = value; }
            }
            /// <summary>
            /// Segment facultatif
            /// </summary>
            public bool Facultatif
            {
                get { return facultatif; }
                protected set { facultatif = value; }
            }
            /// <summary>
            /// Segment unique
            /// </summary>
            public bool Unique
            {
                get { return unique; }
                set { unique = value; }
            }
            /// <summary>
            /// Création d'un lien parent
            /// </summary>
            /// <param name="segment">Le segment</param>
            /// <param name="facultatif">Segment facultatif</param>
            /// <param name="unique">Segment unique</param>
            public LienTables(DataTable segment, bool facultatif, bool unique)
                : this(segment, facultatif, unique, null, new string[] { }, new string[] { })
            {
            }
            /// <summary>
            /// Création d'un lien parent ou enfant si <paramref name="dependde"/> est renseigné.
            /// </summary>
            /// <param name="segment">Le segment</param>
            /// <param name="facultatif">Segment facultatif</param>
            /// <param name="champspere">Le(s) champ(s) du segment <paramref name="dependde"/> utilisés dans le lien</param>
            /// <param name="champsfils">Le(s) champ(s) du segment <paramref name="segment"/> utilisés dans le lien</param>
            /// <param name="unique">Segment unique</param>
            /// <param name="dependde">Segment parent</param>
            public LienTables(DataTable segment, bool facultatif, bool unique, LienTables dependde, string[] champspere, string[] champsfils)
            {
                Segment = segment;
                Facultatif = facultatif;
                Unique = unique;
                DependDe = dependde;
                if (dependde == null)
                {
                    Profondeur = 0;
                    ChampsPere = new string[] { };
                    ChampsFils = new string[] { };
                }
                else
                {
                    // Test si redondance des segments
                    TestRedondance(dependde, segment);
                    // Test cohérence des champs utilisés dans la relation
                    if (champspere.Length == 0) throw new FileIOError(this.GetType().FullName, "Il faut au moins un champs pour créer une relation entre '" + dependde.Segment.TableName + "' et '" + segment.TableName + "'");
                    if (champspere.Length != champsfils.Length)
                        throw new FileIOError(this.GetType().FullName, "Le nombre de champ doit être identique entre '" + dependde.Segment.TableName + "' et '" + segment.TableName + "')");
                    // Les champs doivent exister dans leur segment respectif
                    foreach (string champ in champspere)
                    {
                        if (!dependde.Segment.Columns.Contains(champ))
                            throw new FileIOError(this.GetType().FullName, "Le champ '" + champ + "' n'existe pas dans le segment '" + dependde.Segment.TableName + "'");
                    }
                    foreach (string champ in champsfils)
                    {
                        if (!Segment.Columns.Contains(champ))
                            throw new FileIOError(this.GetType().FullName, "Le champ '" + champ + "' n'existe pas dans le segment '" + Segment.TableName + "'");
                    }
                    // Création du lien
                    Profondeur = dependde.Profondeur + 1;
                    ChampsPere = champspere;
                    ChampsFils = champsfils;
                }
                // La création de l'objet est effectuée qu'en cas d'export
                Enregistrements = null;
                // Avant de créer l'enregistrement dans la table, vérification que l'enregistrement n'existe pas (en cas d'export)
                verifCreEnr = false;
            }
            /// <summary>
            /// Donne le lien racine pour un ce lien.
            /// </summary>
            /// <returns>Lui même ou son lien racine</returns>
            public LienTables getLienRacine()
            {
                // On remonte l'arborescence
                LienTables raclien = this;
                while (raclien.DependDe != null) raclien = raclien.DependDe;
                return raclien;
            }

            /// <summary>
            /// Ajoute un nouvel enregistrement à la liste des enregistrements
            /// </summary>
            /// <remarks>
            /// IMPORTANT : l'enregistrement n'est pas créé dans table de ce lien mais uniquement
            /// dans la liste des enregistrement à ajouter.
            /// </remarks>
            internal void addEnregistrement(string[] ignChampsFils)
            {
                // Création de la liste si nécessaire
                if (Enregistrements == null) Enregistrements = new List<DataRow>();
                // Ajoute l'enregistrement
                Enregistrements.Add(Segment.NewRow());
                // Alimente les champs entrants la dépendance.
                if (DependDe != null)
                {
                    for (int i = 0; i < ChampsPere.Length; i++)
                    {
                        bool ignChampFils = false;
                        foreach (string cf in ignChampsFils)
                        {
                            if (ChampsFils[i].Equals(cf,StringComparison.CurrentCultureIgnoreCase))
                            {
                               ignChampFils=true;
                               break;
                            }
                        }
                        if (!ignChampFils)
                            crsEnregistrement[ChampsFils[i]] = DependDe.crsEnregistrement[ChampsPere[i]];
                    }
                }
            }

            /// <summary>
            /// Enregistrement en cours
            /// </summary>
            /// <remarks>Null si aucun enregistrements ou le dernier enregistrement créé</remarks>
            internal DataRow crsEnregistrement
            {
                get
                {
                    if (Enregistrements == null || Enregistrements.Count == 0) return null;
                    return Enregistrements[Enregistrements.Count - 1];
                }
            }

            /// <summary>
            /// Créé dans la table tous les enregistrement présent dans la liste les enregistrements à ajouter
            /// </summary>
            /// <remarks>
            /// La liste des enregistrement à ajouter est réinitialisée.
            /// </remarks>
            internal void majEnregistrements()
            {
                // Test si mise à jour à faire.
                if (Enregistrements == null || Enregistrements.Count == 0) return;
                // Ajoute les enregistrements
                for (int i = 0; i < Enregistrements.Count; i++)
                {
                    // Test si la création est à vérfiée
                    if (verifCreEnr)
                    {
                        object[] pk = new object[Segment.PrimaryKey.Length];
                        for (int ipk = 0; ipk < pk.Length; ipk++) pk[ipk] = Enregistrements[i][Segment.PrimaryKey[ipk]];
                        if (Segment.Rows.Find(pk) != null) continue;
                    }
                    Segment.Rows.Add(Enregistrements[i]);
                }
                // Efface la liste des enregistrement à ajouter.
                Enregistrements.Clear();
            }

            /// <summary>
            /// Renvoie les enregistrements en lien avec <paramref name="dependde"/>
            /// </summary>
            /// <param name="dependde">L'enregistrement père</param>
            /// <returns>La liste des enregistrements</returns>
            /// <remarks>Si <paramref name="dependde"/> est null, tous les enregistrement de la table sont renvoyés</remarks>
            internal DataRow[] getRows(DataRow dependde)
            {
                // Si clé primaire, on tri par la clé primaire
                string tri = "";
                foreach (DataColumn dc in Segment.PrimaryKey)
                {
                    if (!tri.Equals("")) tri = tri + ",";
                    tri = tri + dc.ColumnName;
                }
                // Renvoie les enregistrements correspondant à ce critére
                return (tri.Equals("") ? Segment.Select(getRowsWhere(dependde)) : Segment.Select(getRowsWhere(dependde), tri));
            }

            /// <summary>
            /// Renvoie une chaine SQL pour sélectionner les enregistrement en lien avec <paramref name="dependde"/>
            /// </summary>
            /// <param name="dependde">L'enregistrement père</param>
            /// <returns>La chaine SQL à utiliser dans une clause WHERE</returns>
            /// <remarks>Si <paramref name="dependde"/> est null, la chaine renvoyée est vide</remarks>
            internal string getRowsWhere(DataRow dependde)
            {
                string where = "";
                if (dependde != null)
                {
                    for (int i = 0; i < ChampsPere.Length; i++)
                    {
                        if (!where.Equals("")) where = where + " and ";
                        where = where + "[" + ChampsFils[i] + "]=";
                        switch (Segment.Columns[ChampsFils[i]].DataType.FullName)
                        {
                            case "System.String":
                            case "System.Char":
                            case "DateTime":
                                where = where + "'";
                                where = where + dependde[ChampsPere[i]].ToString();
                                where = where + "'";
                                break;
                            default:
                                where = where + dependde[ChampsPere[i]].ToString();
                                break;
                        }
                    }
                }
                return where;
            }
            /// <summary>
            /// Test si le lien parent ne contient pas le segment fils.
            /// </summary>
            /// <param name="lienparent">Lien supérieur à tester</param>
            /// <param name="segment">Le segment sur lequel pour le test de redondance.</param>
            private void TestRedondance(LienTables lienparent, DataTable segment)
            {
                // Redondance dans les liens.
                if (lienparent.segment.Equals(segment))
                    throw new FileIOError(this.GetType().FullName, "Un segment ne peut pas dépendre de lui-même ('" + segment.TableName + "')");

                // Remonte l'arborescence
                if (lienparent.DependDe != null)
                    TestRedondance(lienparent.DependDe, segment);
            }
        }
    }
}
