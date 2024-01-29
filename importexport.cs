using System;
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
					xr = new System.Xml.XmlTextReader(sourcexml)
					{
						WhitespaceHandling = System.Xml.WhitespaceHandling.None
					};
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
				if (nl.Count == 0)
					throw new FileIOError(this.GetType().FullName, "L'élèment <fichier> n'existe pas.");
				if (nl.Count > 1)
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
		/// Renvoie une copie de la structure des tables 
		/// </summary>
		/// <returns>Null ou un DataSet contenant les tables</returns>
		public DataSet TablesDefinitions()
		{
			if (tables.dsInterne != null)
			{
				DataSet ds = tables.dsInterne.Copy();
				ds.Clear();
				return ds;
			}
			else
				return null;
		}

		/// <summary>
		/// Exporte les informations contenues dans le fichier vers les tables définies dans le fichier XML
		/// </summary>
		/// <param name="errFichier">Si vrai, consigne les erreurs de conversion d'enregistrement dans un fichier</param>
		/// <remarks>Utilise le fichier spécifié dans la description XML</remarks>
		public DataSet ExportTables(bool errFichier = false)
		{
			return ExportTables(fichier.Nom);
		}

		/// <summary>
		/// Exporte les informations contenues dans le fichier vers les tables définies dans le fichier XML
		/// <param name="FichierAExporter">{chemin\}NomDuFichier à utiliser pour l'exportation</param>
		/// <param name="errFichier">Si vrai, consigne les erreurs de conversion d'enregistrement dans un fichier</param>
		/// </summary>
		public DataSet ExportTables(string FichierAExporter, bool errFichier = false)
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
				export.Execute(FichierAExporter, errFichier);

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
			ImportFichier(dsImport, false);
		}
		/// <summary>
		/// Importe dans le fichier définies dans le fichier XML les informations contenues dans la(les) table(s)
		/// </summary>
		/// <param name="dsImport">Le DataSet contenant les tables</param>
		/// <param name="AjouteAuFichier">Si vrai, les enregistrements sont ajoutés à la fin du fichier</param>
		public void ImportFichier(DataSet dsImport, bool AjouteAuFichier)
		{
			ImportFichier(dsImport, AjouteAuFichier, fichier.Nom);
		}
		/// <summary>
		/// Importe dans le fichier passé en paramètre les informations contenues dans la(les) table(s)
		/// </summary>
		/// <param name="dsImport">Le DataSet contenant la(les) table(s)</param>
		/// <param name="AjouteAuFichier">Si vrai, les enregistrements sont ajoutés à la fin du fichier</param>
		/// <param name="FichierAImporter">{chemin\}NomDuFichier à utiliser pour l'immportation</param>
		public void ImportFichier(DataSet dsImport, bool AjouteAuFichier, string FichierAImporter)
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
				if (File.Exists(FichierAImporter) && !AjouteAuFichier)
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
					import = new Import(nl[0], fichier, tables, dsImport);
				}

				// Chargement des enregistrements dans le dataset interne en tenant compte des segments virtuels
				foreach (DataTable dt in tables.dsInterne.Tables)
				{
					// Création des enregistrements dans le datatable interne
					if (dsImport.Tables[dt.TableName].PrimaryKey.Length == 0)
					{
						foreach (DataRow dri in dsImport.Tables[dt.TableName].Rows)
						{
							DataRow dr = dt.NewRow();
							foreach (DataColumn dc in dt.Columns)
								dr[dc] = dri[dc.ColumnName];
							dt.Rows.Add(dr);
						}
					}
					else
					{
						string tri = "";
						foreach (DataColumn dc in dsImport.Tables[dt.TableName].PrimaryKey)
						{
							if (!tri.Equals("")) tri = tri + ",";
							tri = tri + dc.ColumnName;
						}
						DataRow[] drs = dsImport.Tables[dt.TableName].Select("", tri);
						foreach (DataRow dri in drs)
						{
							DataRow dr = dt.NewRow();
							foreach (DataColumn dc in dt.Columns)
								dr[dc] = dri[dc.ColumnName];
							dt.Rows.Add(dr);
						}
					}
					/* CODE ABANDONNE
                    // Les enregistrement à importer
                    DataRow[] dris = null;
                    string dtname = "";
                    bool dtselect = false;
   
                    // S'agit-il d'un segment virtuel
                    if (dt.ExtendedProperties.Contains("virtuelde"))
                    {
                        dris = dsImport.Tables[dt.ExtendedProperties["virtuelde"].ToString()].Select(dt.ExtendedProperties["si"].ToString());
                        dtname = dt.ExtendedProperties["virtuelde"].ToString();
                        dtselect = true;
                    }
                    else
                    {
                        // Un ou plusieurs segment tabme virtuel dépend de ce segment table
                        dtname = dt.TableName;
                        //DataTable[] segsVirtuel = tables.getSEGVirtuel(dt.TableName);
                        DataTable[] segsVirtuel = null;
                        if (segsVirtuel.Length > 0)
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
                            dris = dsImport.Tables[dt.TableName].Select(si);
                            dtselect = true;
                        }
                    }

                    // Création des enregistrements dans le datatable interne
                    foreach (DataRow dri in dsImport.Tables[dtname].Rows)
                    {
                        if (!dtselect || Array.IndexOf(dris, dri) >= 0)
                        {
                            DataRow dr = dt.NewRow();
                            foreach (DataColumn dc in dt.Columns)
                                dr[dc] = dri[dc.ColumnName];
                            dt.Rows.Add(dr);
                        }
                    }
                    */
				}

				// Lance l'importation
				import.Execute(FichierAImporter, AjouteAuFichier);
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
				/* CODE ABANDONNE
                // Suppression des tables virtuels
                tables.DelSEGVirtuel();
                */
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
			// Dans le cas d'un multi-segment fichier et mono-segment table
			// si sa valeur est >=0, il faut créer un nouvel enregistrement.
			// Dans ce cas, cette valeur correspont à l'enregistrement qu'il faut utiliser
			// pour initialiser le nouvel enregistrement.
			int forceNewRow;
			List<string> forceDataColumnName;
			// Liste des erreurs à consigner. 
			// Dans ce cas, une erreur dans un enregistrement n'est pas bloquante
			private List<TextFileIO._ENREG> listeErr;

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
				if (tables == null)
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
			/// <param name="FichierAExporter">Le fichier à traiter</param>
			/// <param name="errFichier">Si vrai, consigne les erreurs de conversion d'enregistrement dans un fichier</param>
			/// <remarks>
			/// Si <paramref name="errFichier"/> est vrai, l'erreur de conversion d'enregistrement n'est pas bloquante
			/// et le traitement continue.
			/// </remarks>
			public void Execute(string FichierAExporter, bool errFichier = false)
			{
				// Le fichier utilisé
				TextFileIO txtFileIO = null;
				// Le fichier des erreurs.
				TextFileIO txtErrFileIO = null;
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

					// Test si gestion des erreurs de conversion dans un fichier
					if (errFichier)
						listeErr = new List<TextFileIO._ENREG>();
					else
						listeErr = null;

					// Démarrage de l'exportation.
					txtFileIO.ReadFileSeq();

					// MAJ du éventuellent du dernier enregistrement contenant un champ mémo
					if (crsLien != null && crsLien.Segment.ChampMemo && crsLien.Segment.EnregChampMemo.Champs.Length > 0)
						MajEnregistrement(crsLien.Segment.EnregChampMemo);

					// MAJ des derniers enregistrements
					if (exportTables.Liens.Count > 0)
						exportTables.Liens.majEnregistrements();

					// Création du fichier des erreurs de conversion
					if (errFichier && listeErr.Count > 0)
					{
						try
						{
							string nomerr = FichierAExporter + ".err";
							if (File.Exists(nomerr)) File.Delete(nomerr);
							txtErrFileIO = new TextFileIO(nomerr, exportFichier.Codage, exesepenr, exportFichier.SepChamp, exportFichier.DelChamp, true);
							txtErrFileIO.WriteFile(listeErr);
							txtErrFileIO.WaitAllIO();
						}
						catch { }
						finally
						{
							// Fermeture du fichier 
							if (txtErrFileIO != null)
							{
								txtErrFileIO.Dispose();
								txtErrFileIO = null;
							}
							listeErr.Clear();
							listeErr = null;
						}
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
					// Chaine sans aucun caractère de contrôle
					string sc = "";
					foreach (char c in champ)
					{
						if (!Char.IsControl(c)) sc = sc + c.ToString();
					}
					sc = sc.Trim(); // Supprime les espaces inutiles
					if (sc.Equals("")) continue; // Si chaine vide, on passe à l'occurence suivante.
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
					for (; lfp != null;)
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
					// Il ne faut pas mettre à jour les champs fils s'ils sont présents dans le fichier
					LienTables lt = exportTables.Liens.getLien(exportTables.dsInterne.Tables[table]);
					string ignChampFils = "";
					foreach (string cf in lt.ChampsFils)
					{
						foreach (SegmentExport se in ses)
						{
							if (se.SegmentFichier.Champs.Contains(cf))
							{
								if (!ignChampFils.Equals("")) ignChampFils = ignChampFils + ",";
								ignChampFils = ignChampFils + cf;
								break;
							}
						}
					}
					// Si aucun enregistrement en cours, on crée sans test
					if (lt.crsEnregistrement == null)
						lt.addEnregistrement(ignChampFils.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
					// sinon, création d'un nouvel enregistrement si nécessaire.
					else if (CreEnregistrement(table))
						lt.addEnregistrement(ignChampFils.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
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
				for (; ld != null;)
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
				try
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
						//if (exportTables.dsInterne.Tables[0].Rows.Count == 0 || exportFichier.Liens.Count == 0)
						if (exportFichier.Liens.Count == 0 || crsLien.Profondeur == 0)
						{
							dr = exportTables.dsInterne.Tables[0].NewRow();
							if (exportFichier.Liens.Count > 0) forceNewRow = -1;
						}
						else
						{
							// Si création obligatoire d'un enregistrement
							if (forceNewRow > -1)
							{
								dr = exportTables.dsInterne.Tables[0].NewRow();
								foreach (string dcn in forceDataColumnName) dr[dcn] = exportTables.dsInterne.Tables[0].Rows[forceNewRow][dcn];
							}
							else
							{
								// Si le lien est unique, on reste sur le même enregistrement
								if (crsLien.Unique)
									dr = exportTables.dsInterne.Tables[0].Rows[exportTables.dsInterne.Tables[0].Rows.Count - 1];
								else
								{
									// On tombe sur un ligne non unique. S'agissant de la prmière fois, on utilise
									// le même enregistrement, lors des prochaines lecture, il faudra forcément ajouté
									// un nouvel enregistrement
									dr = exportTables.dsInterne.Tables[0].Rows[exportTables.dsInterne.Tables[0].Rows.Count - 1];
									forceNewRow = exportTables.dsInterne.Tables[0].Rows.Count - 1;
									if (forceDataColumnName == null) forceDataColumnName = new List<string>();
									else forceDataColumnName.Clear();
									// Mémorisation des champs non null à copier
									foreach (DataColumn dc in exportTables.dsInterne.Tables[0].Columns)
									{
										if (dr[dc] != DBNull.Value) forceDataColumnName.Add(dc.ColumnName);
									}
								}
							}
							/* CODE ABANDONNE
                            // Si multi-segment fichier, il faut vérifier que ce segment déclenche la création d'enregistrement
                            if (CreEnregistrement(exportTables.dsInterne.Tables[0].TableName))
                                dr = exportTables.dsInterne.Tables[0].NewRow();
                            else
                                dr = exportTables.dsInterne.Tables[0].Rows[exportTables.dsInterne.Tables[0].Rows.Count - 1];
                             */
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
				catch (System.Exception eh)
				{
					// Si l'erreur n'est pas consignée dans un fichier, on sort en erreur
					if (listeErr == null) throw;
					// Ajoute l'nregistrement aux erreurs.
					listeErr.Add(new TextFileIO._ENREG(new string[] { eh.Message }));
					listeErr.Add(enr);
					// On sort sans erreur
					return;
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
				if (tables == null)
					throw new FileIOError(this.GetType().FullName, "Aucune table n'est définie");
				if (ds == null)
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
			}

			/// <summary>
			/// Importation
			/// </summary>
			public void Execute(string ImporterFichier, bool AjouteAuFichier)
			{
				// Le fichier ut6ilisé
				TextFileIO txtFileIO = null;
				try
				{
					// Ouverture du fichier en lecture/ecriture.
					txtFileIO = new TextFileIO(ImporterFichier, importFichier.Codage, importFichier.SepEnr, importFichier.SepChamp, importFichier.DelChamp, true, AjouteAuFichier);

					// Création liste enregistrements
					if (Enregistrements == null) Enregistrements = new List<TextFileIO._ENREG>();
					else Enregistrements.Clear();

					// Si aucun lien (ni table ni fichier)
					if (importTables.Liens.Count == 0 && importFichier.Liens.Count == 0)
					{
						// Est-ce que cette table est utilisée.
						SegmentImport[] sit = Segments.getImports(importTables.dsInterne.Tables[0]);
						if (sit.Length == 0) return;
						SegmentImport[] sittrt = sit;

						// Est-ce que des segments virtuels dépendent de cette table
						System.Collections.Hashtable SegmentsVirtuel = importTables.SegmentsVirtuel(importTables.dsInterne.Tables[0].TableName);
						// Liste des enregistrements par segment virtuel
						Dictionary<string, DataRow[]> SegmentVirtuelEnrs;
						if (SegmentsVirtuel.Count == 0)
							SegmentVirtuelEnrs = new Dictionary<string, DataRow[]>();
						else
						{
							SegmentVirtuelEnrs = importTables.SegmentVirtuelEnrs(importTables.dsInterne.Tables[0].TableName, SegmentsVirtuel);
						}
						// Les segment import à utiliser en fonction du segment virtuel
						Dictionary<string, SegmentImport[]> sivt = new Dictionary<string, SegmentImport[]>();
						foreach (string key in SegmentVirtuelEnrs.Keys)
							sivt.Add(key, Segments.getImports(key));

						// Traitement de tous les enregistrements.
						foreach (DataRow dr in importTables.dsInterne.Tables[0].Rows)
						{
							// Création d'un nouvel enregistrement
							TextFileIO._ENREG enreg = importFichier.Segments[0].NewEnreg();

							// Quel segmentimport à utiliser
							if (SegmentVirtuelEnrs.Count > 0)
							{
								sittrt = sit;
								foreach (KeyValuePair<string, DataRow[]> kvp in SegmentVirtuelEnrs)
								{
									if (Array.IndexOf(kvp.Value, dr) > 0)
									{
										sittrt = sivt[kvp.Key];
										break;
									}
								}
							}

							// MAJ des champs
							foreach (SegmentImport si in sittrt)
							{
								if (!si.NomChampTable.StartsWith("="))
									si.ChampFichierMaj.SetValue(dr[si.NomChampTable], ref enreg);
								else
									si.ChampFichierMaj.SetValue(si.NomChampTable.Substring(1), ref enreg);
							}

							// Ajout de l'enregistrement
							Enregistrements.Add(enreg);
						}

						// Création des enregistrements dans le fichier
						txtFileIO.WriteFile(Enregistrements);
						Enregistrements.Clear();
						txtFileIO.WaitAllIO();
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
						// Est-ce que cette table est utilisée.
						SegmentImport[] sit = Segments.getImports(importTables.dsInterne.Tables[0]);
						if (sit.Length == 0) return;
						SegmentImport[] sittrt = sit;

						// La table doit forcément mettre à jour un lien fichier racine
						LienFichier racLF = getLienFichierRacine(importTables.dsInterne.Tables[0]);
						if (racLF == null)
							throw new FileIOError(this.GetType().FullName, "La table '" + importTables.dsInterne.Tables[0].TableName + "' ne met à jour aucun lien racine dans le fichier");

						// Si le lien racine est unique, la table ne doit pas contenir plusieurs enregistrements
						if (racLF.Unique && importTables.dsInterne.Tables[0].Rows.Count > 1)
							throw new FileIOError(this.GetType().FullName, "La table '" + importTables.dsInterne.Tables[0].TableName + "' contient plusieurs enregisrement alors que le lien racine dans le fichier est unique");

						// Est-ce que des segments virtuels dépendent de cette table
						System.Collections.Hashtable SegmentsVirtuel = importTables.SegmentsVirtuel(importTables.dsInterne.Tables[0].TableName);
						// Liste des enregistrements par segment virtuel
						Dictionary<string, DataRow[]> SegmentVirtuelEnrs;
						if (SegmentsVirtuel.Count == 0)
							SegmentVirtuelEnrs = new Dictionary<string, DataRow[]>();
						else
						{
							sit = Segments.getImports(importTables.dsInterne.Tables[0].TableName); // Plus besoin des segments virtuels.
							SegmentVirtuelEnrs = importTables.SegmentVirtuelEnrs(importTables.dsInterne.Tables[0].TableName, SegmentsVirtuel);
						}
						// Les segment import à utiliser en fonction du segment virtuel
						Dictionary<string, SegmentImport[]> sivt = new Dictionary<string, SegmentImport[]>();
						foreach (string key in SegmentVirtuelEnrs.Keys)
							sivt.Add(key, Segments.getImports(key));

						// Il faut vérifier que les segments obligatoires sur le fichier sont bien alimentés par la table
						testMajSegmentsFichierObligatoire(importTables.dsInterne.Tables[0].TableName, racLF);
						foreach (string key in SegmentVirtuelEnrs.Keys)
							testMajSegmentsFichierObligatoire(key, racLF);

						// Traitement de tous les enregistrements.
						//foreach (DataRow dr in importTables.dsInterne.Tables[i].Rows) majLiensFichier(dr, sit, racLF);
						for (int j = 0; j < importTables.dsInterne.Tables[0].Rows.Count; j++)
						{
							// Quel segmentimport à utiliser
							if (SegmentVirtuelEnrs.Count > 0)
							{
								sittrt = sit;
								foreach (KeyValuePair<string, DataRow[]> kvp in SegmentVirtuelEnrs)
								{
									if (Array.IndexOf(kvp.Value, importTables.dsInterne.Tables[0].Rows[j]) >= 0)
									{
										sittrt = sivt[kvp.Key];
										break;
									}
								}
							}
							// Maj du fichier
							bool testAddSeg = (j > 0);
							LecLiensFichier(j, sittrt, racLF, ref testAddSeg);
						}

						// Création des enregistrements dans le fichier
						txtFileIO.WriteFile(Enregistrements);
						Enregistrements.Clear();
						txtFileIO.WaitAllIO();
					}

					// Si plusieurs liens table et fichier
					else if (importTables.Liens.Count > 0 && importFichier.Liens.Count > 0)
					{
						// Recherche des liens tables racines
						LienTables[] ltr = importTables.Liens.getLiens((LienTables)null);
						if (ltr.Length == 0) return;

						// Pour lien racine
						foreach (LienTables lt in ltr)
						{
							// La table doit forcément mettre à jour un lien fichier racine
							LienFichier racLF = getLienFichierRacine(lt.Segment);
							if (racLF == null)
								throw new FileIOError(this.GetType().FullName, "La table '" + lt.Segment.TableName + "' ne met à jour aucun lien racine dans le fichier");

							// S'agissant d'une table racine, il ne peut y avoir de segment virtuel.
							// On cherche donc uniquement les segments exports attachés à cette table.
							SegmentImport[] sit = Segments.getImports(lt.Segment.TableName, racLF.Segment);

							// Traitement de tous les enregistrements
							for (int iRow = 0; iRow < lt.Segment.Rows.Count; iRow++)
							{
								// Indique l'enregistrement en cours
								importTables.setIndexImportEnregistrement(lt.Segment, iRow);
								// Les segments exports en cours d'utilisation
								SegmentImport[] sittrt = sit;
								// Le lien suivant
								LienFichier lftrt = racLF;

								// Création d'un nouvel enregistrement
								while (sittrt.Length > 0)
								{
									TextFileIO._ENREG enreg = lftrt.Segment.NewEnreg();
									foreach (SegmentImport si in sittrt)
									{
										if (!si.NomChampTable.StartsWith("="))
											si.ChampFichierMaj.SetValue(lt.Segment.Rows[iRow][si.NomChampTable], ref enreg);
										else
											si.ChampFichierMaj.SetValue(si.NomChampTable.Substring(1), ref enreg);
									}
									// Ajout de l'enregistrement
									Enregistrements.Add(enreg);

									// Recherche du lien suivant
									lftrt = importFichier.Liens.getLienSuivant(lftrt);
									// Est-ce que ce lien est utilisé par ce segment table
									if (lftrt != null)
										sittrt = Segments.getImports(lt.Segment.TableName, lftrt.Segment);
									else
										sittrt = new SegmentImport[] { };
								}

								// Le lien suivant ne s'appuie pas sur la même table.
								if (lftrt != null)
									LecLiensFichierTable(lt, lftrt);
							}
						}

						// Création des enregistrements dans le fichier
						txtFileIO.WriteFile(Enregistrements);
						Enregistrements.Clear();
						txtFileIO.WaitAllIO();

						/* ABANDON DE CE CODE
                        //  La structure du fichier dirige la lecture des tables.
                        LienFichier[] lfs = importFichier.Liens.getLiens((LienFichier)null);

                        // Pour chaque lien racine du fichier
                        foreach (LienFichier lf in lfs)
                        {
                            // Recherche du lien table de même profondeur
                            LienTables lts = getLienTables(lf);
                            // Si non trouvé c'est une erreur
                            if (lts == null)
                                throw new FileIOError(this.GetType().FullName, "La segment fichier racine '" + lf.Segment.Nom + "' n'est mis à jour par aucune table");

                            // Est-ce que cette table est utilisée.
                            SegmentImport[] sit = Segments.getImports(lts.Segment.TableName, lf.Segment);
                            if (sit.Length == 0) continue;
                            SegmentImport[] sittrt = sit;

                            // Est-ce que des segments virtuels dépendent de cette table
                            System.Collections.Hashtable SegmentsVirtuel = importTables.SegmentsVirtuel(lts.Segment.TableName);
                            // Liste des enregistrements par segment virtuel
                            Dictionary<string, DataRow[]> SegmentVirtuelEnrs;
                            if (SegmentsVirtuel.Count == 0)
                                SegmentVirtuelEnrs = new Dictionary<string, DataRow[]>();
                            else
                            {
                                SegmentVirtuelEnrs = importTables.SegmentVirtuelEnrs(lts.Segment.TableName, SegmentsVirtuel);
                            }
                            // Les segment import à utiliser en fonction du segment virtuel
                            Dictionary<string, SegmentImport[]> sivt = new Dictionary<string, SegmentImport[]>();
                            foreach (string key in SegmentVirtuelEnrs.Keys)
                                sivt.Add(key, Segments.getImports(key, lf.Segment));

                            // Traitement de tous les enregistrements de la table
                            foreach (DataRow dr in lts.Segment.Rows)
                            {
                                // Création d'un nouvel enregistrement
                                TextFileIO._ENREG enreg = lf.Segment.NewEnreg();

                                // Quel segmentimport à utiliser
                                if (SegmentVirtuelEnrs.Count > 0)
                                {
                                    sittrt = sit;
                                    foreach (KeyValuePair<string, DataRow[]> kvp in SegmentVirtuelEnrs)
                                    {
                                        if (Array.IndexOf(kvp.Value, dr) > 0)
                                        {
                                            sittrt = sivt[kvp.Key];
                                            break;
                                        }
                                    }
                                }

                                // MAJ des champs
                                foreach (SegmentImport si in sittrt)
                                {
                                    if (!si.NomChampTable.StartsWith("="))
                                        si.ChampFichierMaj.SetValue(dr[si.NomChampTable], ref enreg);
                                    else
                                        si.ChampFichierMaj.SetValue(si.NomChampTable.Substring(1), ref enreg);
                                }

                                // Ajout de l'enregistrement
                                Enregistrements.Add(enreg);
                            }
                            
                        }
                        */

						/* ABANDON DE CE CODE
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
                        */
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
					// Est-ce que cette table est utilisée.
					SegmentImport[] sit = Segments.getImports(depLT.Segment.TableName);
					if (sit.Length == 0) continue;
					SegmentImport[] sittrt = sit;

					// Recherche des enregistrement à traiter
					DataRow[] drs = depLT.getRows(dependde);
					if (drs.Length == 0) continue;

					// Est-ce que des segments virtuels dépendent de cette table
					System.Collections.Hashtable SegmentsVirtuel = importTables.SegmentsVirtuel(depLT.Segment.TableName);
					// Liste des enregistrements par segment virtuel
					Dictionary<string, DataRow[]> SegmentVirtuelEnrs;
					if (SegmentsVirtuel.Count == 0)
						SegmentVirtuelEnrs = new Dictionary<string, DataRow[]>();
					else
					{
						SegmentVirtuelEnrs = importTables.SegmentVirtuelEnrs(depLT.Segment.TableName, SegmentsVirtuel);
					}
					// Les segment import à utiliser en fonction du segment virtuel
					Dictionary<string, SegmentImport[]> sivt = new Dictionary<string, SegmentImport[]>();
					foreach (string key in SegmentVirtuelEnrs.Keys)
						sivt.Add(key, Segments.getImports(key));

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
						//bool newenreg = false;
						if (depLT.Profondeur == 0 || depLT.Profondeur > 0 && j > 0)
						{
							Enregistrements.Add(importFichier.Segments[0].NewEnreg());
							//newenreg = true;
						}

						// Etant donné qu'il n'y a qu'un seul segment fichier, tous les segments import
						// mettent à jour le même enregistrement.
						TextFileIO._ENREG crsenreg = Enregistrements[Enregistrements.Count - 1];

						/* INUTILE
                        // Si création d'un nouvel enregistrement, on charge les champs à valeur fixe
                        if (newenreg)
                        {
                            foreach (SegmentImport si in Segments)
                            {
                                if (si.NomChampTable.StartsWith("="))
                                    si.ChampFichierMaj.SetValue(si.NomChampTable.Substring(1), ref crsenreg);
                            }
                        }
                        */

						// Quel segmentimport à utiliser
						if (SegmentVirtuelEnrs.Count > 0)
						{
							sittrt = sit;
							foreach (KeyValuePair<string, DataRow[]> kvp in SegmentVirtuelEnrs)
							{
								if (Array.IndexOf(kvp.Value, dr) > 0)
								{
									sittrt = sivt[kvp.Key];
									break;
								}
							}
						}

						// MAJ des champs
						foreach (SegmentImport si in sit)
						{
							if (si.NomChampTable.StartsWith("="))
								si.ChampFichierMaj.SetValue(si.NomChampTable.Substring(1), ref crsenreg);
							else
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
								SegmentImport[] sitinf = Segments.getImports(depInf.Segment.TableName);
								foreach (SegmentImport si in sitinf)
								{
									if (si.NomChampTable.StartsWith("="))
										si.ChampFichierMaj.SetValue(si.NomChampTable.Substring(1), ref crsenreg);
									else
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

			// Traitement de l'importation dans le cas d'un seul segment table mais plusieurs liens fichier
			private void LecLiensFichier(int iRow, SegmentImport[] sit, LienFichier lf, ref bool testAddSeg)
			{
				// Si le test de créaton d'enregistrement est actif, il est définitivement désactivé pour
				// l'enregistrement en cours de traitement dès que l'on rencontre un segment fichier non unique
				// de profondeur supérieur à 0. 
				// REMARQUE: il est IMPORTANT que ce test se fasse même si le segment fichier n'est pas utilisé.
				if (testAddSeg && lf.Profondeur > 0 && !lf.Unique) testAddSeg = false;

				// Est-que le segment est mis à jour par la table.
				bool segtrt = false;
				foreach (SegmentImport si in sit)
				{
					if (lf.Segment.Champs.Contains(si.ChampFichierMaj))
					{
						segtrt = true;
						break;
					}
				}
				// IMPORTANT
				// Il peut exister plusieurs lien racines. Dans ce cas, on commute sur le bon lien si
				// ce n'est pas le cas (uniquement dans le cas d'une profondeur à 0).
				if (!segtrt)
				{
					if (lf.Profondeur > 0) return;
					LienFichier[] racsLF = importFichier.Liens.getLiens((LienFichier)null);
					if (racsLF.Length == 0) return;
					foreach (LienFichier racLF in racsLF)
					{
						if (racLF.Equals(lf)) continue;
						foreach (SegmentImport si in sit)
						{
							if (racLF.Segment.Champs.Contains(si.ChampFichierMaj))
							{
								segtrt = true;
								lf = racLF;
								break;
							}
						}
						if (segtrt) break;
					}
					if (!segtrt) return;
				}

				// Par défaut, création d'un nouveau segment
				bool addSeg = true;

				// On test si on doit créer un nouveau segment
				if (testAddSeg)
				{
					// Si on est sur un segment fichier de dernier niveau, 
					// la création d'enregistrement est systémtique sauf si le segment est déclaré unique
					if (importFichier.Liens.getLiens(lf).Length == 0)
						addSeg = !lf.Unique;
					else
					{
						foreach (SegmentImport si in sit)
						{
							if (lf.Segment.Champs.Contains(si.ChampFichierMaj)
								&& !si.NomChampTable.StartsWith("=")
								&& si.SegmentTable.Rows[iRow][si.NomChampTable].Equals(si.SegmentTable.Rows[iRow - 1][si.NomChampTable]))
							{
								addSeg = false;
								break;
							}
						}
					}
				}

				// Création d'un nouveau segment si nécessaire
				if (addSeg)
				{
					// Recherche des segment imports liès à ce segment fichier
					if (sit.Length > 0)
					{
						// Création d'un nouvel enregistrement
						TextFileIO._ENREG enreg = lf.Segment.NewEnreg();
						// MAJ des champs
						foreach (SegmentImport si in sit)
						{
							if (lf.Segment.Champs.Contains(si.ChampFichierMaj))
							{
								if (!si.NomChampTable.StartsWith("="))
									si.ChampFichierMaj.SetValue(importTables.dsInterne.Tables[0].Rows[iRow][si.NomChampTable], ref enreg);
								else
									si.ChampFichierMaj.SetValue(si.NomChampTable.Substring(1), ref enreg);
							}
						}
						Enregistrements.Add(enreg);
					}
				}

				// Traitement des dépendances
				foreach (LienFichier lfd in importFichier.Liens.getLiens(lf)) LecLiensFichier(iRow, sit, lfd, ref testAddSeg);
			}

			// Traitement de l'importation dans le cas de plusieurs liens fichier et table.
			private void LecLiensFichierTable(LienTables crsLT, LienFichier crsLF)
			{
				// Recherche des liens tables racines
				LienTables[] ltr = importTables.Liens.getLiens(crsLT);
				if (ltr.Length == 0)
					throw new FileIOError(this.GetType().FullName, "La table '" + crsLT.Segment.TableName + "' n'a pas de dépedance permettant de renseigne le segment fichier '" + crsLF.Segment.Nom + "'");

				// Pour lien table
				foreach (LienTables lt in ltr)
				{
					// Est-ce que cette table est utilisée ?
					SegmentImport[] sit = Segments.getImports(lt.Segment.TableName, crsLF.Segment);
					if (sit.Length == 0)
						throw new FileIOError(this.GetType().FullName, "La table '" + lt.Segment.TableName + "' ne renseigne pas le segment fichier '" + crsLF.Segment.Nom + "'");

					// Recherche des enregistrements
					DataRow[] drs = lt.getRows(crsLT.Segment.Rows[importTables.getIndexImportEnregistrement(crsLT.Segment)]);
					if (drs.Length == 0)
					{
						if (!crsLF.Facultatif)
							throw new FileIOError(this.GetType().FullName, "La table '" + lt.Segment.TableName + "' ne contient pas d'enregistrement alors que le segment fichier '" + crsLF.Segment.Nom + "' est obligatoire.");
						continue;
					}

					// Est-ce que des segments virtuels dépendent de cette table
					System.Collections.Hashtable SegmentsVirtuel = importTables.SegmentsVirtuel(lt.Segment.TableName);
					// Liste des enregistrements par segment virtuel
					Dictionary<string, DataRow[]> SegmentVirtuelEnrs;
					if (SegmentsVirtuel.Count == 0)
						SegmentVirtuelEnrs = new Dictionary<string, DataRow[]>();
					else
					{
						SegmentVirtuelEnrs = importTables.SegmentVirtuelEnrs(lt.Segment.TableName, SegmentsVirtuel, lt.getRowsWhere(crsLT.Segment.Rows[importTables.getIndexImportEnregistrement(crsLT.Segment)]));
					}

					// Traitement de tous les enregistrements
					for (int iRow = 0; iRow < drs.Length; iRow++)
					{
						// Le lien encours d'utilisation
						LienFichier trtLF = crsLF;
						// Les segments exports en cours d'utilisation
						SegmentImport[] sittrt = sit;
						// Si segment virtuel, pour ce premier passage on ne prend que les enregistrements n'appartenant
						// pas à un segment virtiel
						if (SegmentsVirtuel.Count > 0)
						{
							foreach (KeyValuePair<string, DataRow[]> kvp in SegmentVirtuelEnrs)
							{
								if (Array.IndexOf(kvp.Value, drs[iRow]) >= 0)
								{
									sittrt = null;
									break;
								}
							}
							if (sittrt == null) continue;
						}
						// Indique l'enregistrement en cours
						importTables.setIndexImportEnregistrement(lt.Segment, lt.Segment.Rows.IndexOf(drs[iRow]));

						// Création d'un nouvel enregistrement
						while (sittrt.Length > 0)
						{
							TextFileIO._ENREG enreg = trtLF.Segment.NewEnreg();
							foreach (SegmentImport si in sittrt)
							{
								if (!si.NomChampTable.StartsWith("="))
									si.ChampFichierMaj.SetValue(drs[iRow][si.NomChampTable], ref enreg);
								else
									si.ChampFichierMaj.SetValue(si.NomChampTable.Substring(1), ref enreg);
							}
							// Ajout de l'enregistrement
							Enregistrements.Add(enreg);

							// Recherche du lien suivant
							trtLF = importFichier.Liens.getLienSuivant(trtLF);
							// Est-ce que ce lien est utilisé par ce segment table
							if (trtLF != null)
								sittrt = Segments.getImports(lt.Segment.TableName, trtLF.Segment);
							else
								sittrt = new SegmentImport[] { };
						}

						// Si lien suivant existe
						// NOTA : en cas de segments virtuels, il ne peux pas exister de dépendances de table.
						if (SegmentsVirtuel.Count == 0 && trtLF != null && importTables.Liens.getLiens(lt.Segment).Length > 0)
							LecLiensFichierTable(lt, trtLF);
					}

					// On traite les segments virtuels
					if (SegmentsVirtuel.Count > 0)
					{
						for (int iSegV = 0; iSegV < SegmentsVirtuel.Count; iSegV++)
						{
							// Recherche du line suivant
							crsLF = importFichier.Liens.getLienSuivant(crsLF, true);
							if (crsLF == null)
								throw new FileIOError(this.GetType().FullName, "Le segment fichier '" + crsLF.Segment.Nom + "' ne dispose pas de segment(s) suivant(s) de même niveau pour le(s) " +
									"segment(s) virtuel(s) de '" + lt.Segment.TableName + "'");
							// Le lien table doit être alimenté par un des liens virtuels
							string SegV = "";
							foreach (object key in SegmentsVirtuel.Keys)
							{
								sit = Segments.getImports(key.ToString(), crsLF.Segment);
								if (sit.Length > 0)
								{
									SegV = key.ToString();
									break;
								}
							}
							// Si aucun line export, c'est une erreur
							if (sit.Length == 0)
								throw new FileIOError(this.GetType().FullName, "Le segment fichier '" + crsLF.Segment.Nom + "' n'est renseigné par aucun " +
									"segment virtuel de '" + lt.Segment.TableName + "'");
							// Est-ce que des enregistrements existent
							if (!SegmentVirtuelEnrs.ContainsKey(SegV))
							{
								if (!crsLF.Facultatif)
									throw new FileIOError(this.GetType().FullName, "Le segment fichier '" + crsLF.Segment.Nom + "' est obligatoire. " +
										"Le segment virtuel '" + SegV + "' ne retourne aucun enregistrement");
								continue;
							}
							// Traitement de tous les enregistrements
							for (int iRow = 0; iRow < drs.Length; iRow++)
							{
								// Le lien encours d'utilisation
								LienFichier trtLF = crsLF;
								// Les segments exports en cours d'utilisation
								SegmentImport[] sittrt = sit;
								// Si segment virtuel, pour ce premier passage on ne prend que les enregistrements n'appartenant
								// pas à un segment virtiel
								if (Array.IndexOf(SegmentVirtuelEnrs[SegV], drs[iRow]) < 0) continue;
								// Indique l'enregistrement en cours
								importTables.setIndexImportEnregistrement(lt.Segment, lt.Segment.Rows.IndexOf(drs[iRow]));

								// Création d'un nouvel enregistrement
								while (sittrt.Length > 0)
								{
									TextFileIO._ENREG enreg = trtLF.Segment.NewEnreg();
									foreach (SegmentImport si in sittrt)
									{
										if (!si.NomChampTable.StartsWith("="))
											si.ChampFichierMaj.SetValue(drs[iRow][si.NomChampTable], ref enreg);
										else
											si.ChampFichierMaj.SetValue(si.NomChampTable.Substring(1), ref enreg);
									}
									// Ajout de l'enregistrement
									Enregistrements.Add(enreg);

									// Recherche du lien suivant
									trtLF = importFichier.Liens.getLienSuivant(trtLF);
									// Est-ce que ce lien est utilisé par ce segment table
									if (trtLF != null)
										sittrt = Segments.getImports(lt.Segment.TableName, trtLF.Segment);
									else
										sittrt = new SegmentImport[] { };
								}
							}
						}
					}

					// Recherche du lien suivant.
					crsLF = importFichier.Liens.getLienSuivant(crsLF, true);
				}
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
					else if (xa.Name.ToLower().Equals("virtuel") && !xa.Value.Equals(""))
						segmentvirtuel = xa.Value.Trim();
					else if (xa.Name.ToLower().Equals("si") && !xa.Value.Equals(""))
						si = xa.Value.Trim();
				}
				// Si aucun nom de segment, on sort en erreur.
				if (nomsegment.Equals(""))
					throw new FileIOError(this.GetType().FullName, "Un <segment> dans <import> est sans attribut 'nom'.");
				// Si segment virtuel, si est obligatoire
				if (!segmentvirtuel.Equals("") && si.Equals(""))
					throw new FileIOError(this.GetType().FullName, "L'attribut 'si' est obligatoire dans le cas d'un segment virtuel ('" + nomsegment + "')");

				// Test si le segment existe
				if (!importTables.dsInterne.Tables.Contains(segmentvirtuel.Equals("") ? nomsegment : segmentvirtuel))
					throw new FileIOError(this.GetType().FullName, "Le segment '" + (segmentvirtuel.Equals("") ? nomsegment : segmentvirtuel) + "' n'existe pas dans <tables>.");

				// Un segment virtuel ne peut pas s'appuyer sur un segment réelle avec des dépendances
				if (!segmentvirtuel.Equals("") && importTables.Liens.Count > 0)
				{
					if (importTables.Liens.getLiens(importTables.dsInterne.Tables[segmentvirtuel]).Length > 0)
						throw new FileIOError(this.GetType().FullName, "Le segment '" + segmentvirtuel + "' n'accepte pas les segments virtuels car il a des dépendances.");
				}

				// Dans le cas d'un segment virtuel, il faut le créer.
				/* ABANDONNE : le segment reste viruel et n'est pas créé dans Tables.
                if (!segmentvirtuel.Equals("")) importTables.CreSEGVirtuel(nomsegment, segmentvirtuel, si);
                */
				if (!segmentvirtuel.Equals("")) importTables.AddNomSegmentVirtuel(nomsegment, segmentvirtuel, si);

				// On cré un tableau de chaine correspondant à chaque ligne de <segment>
				string[] champs;
				champs = xn.FirstChild.Value.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
				if (champs.Length == 1 && champs[0].Equals(xn.FirstChild.Value))
					champs = xn.FirstChild.Value.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
				if (champs.Length == 0) return;

				// Pour chaque ligne trouvée, on crée un segment import
				foreach (string champ in champs)
				{
					// Chaine sans aucun caractère de contrôle
					string sc = "";
					foreach (char c in champ)
					{
						if (!Char.IsControl(c)) sc = sc + c.ToString();
					}
					sc = sc.Trim(); // Supprime les espaces inutiles
					if (sc.Equals("")) continue; // Si chaine vide, on passe à l'occurence suivante.
												 // La chaine doit au moins commencer par un point
					if (!sc.StartsWith("."))
						throw new FileIOError(this.GetType().FullName, "Le ligne doit obligatoirement commencer par un '.'");
					// Ajoute le segment import
					Segments.Add(new SegmentImport(nomsegment, importTables.dsInterne.Tables[segmentvirtuel == "" ? nomsegment : segmentvirtuel], importFichier.Segments, sc));
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
				List<string> nomsegmentimport = new List<string>();
				foreach (LienFichier testLF in racLFS)
				{
					SegmentImport[] sit = Segments.getImports(dt, testLF.Segment);
					if (sit.Length == 0) continue;
					if (racLF == null)
						racLF = testLF;
					else
					{
						if (nomsegmentimport.Contains(sit[0].NomSegmentImport))
							throw new FileIOError(this.GetType().FullName, "Le segment export pour la table '" + dt.TableName + "' met à jour plusieurs segments racine du fichier");
					}
					nomsegmentimport.Add(sit[0].NomSegmentImport);
				}

				// Renvoie le lien fichier racine trouvé
				return racLF;
			}

			/// <summary>
			/// Renvoie le lien table de même niveau que le lien fichier
			/// </summary>
			/// <param name="lf">Le lien fichier</param>
			/// <returns>le lien table ou null si non trouvé</returns>
			private LienTables getLienTables(LienFichier lf)
			{
				// Recherche les segments imports utilisant le segment fichier
				SegmentImport[] sis = Segments.getImports(lf.Segment);
				if (sis == null) return null;
				// Recherche dans les segments imports, d'un lien table de même profondeur
				// que le lien fichier
				foreach (SegmentImport si in sis)
				{
					LienTables lts = importTables.Liens.getLien(si.SegmentTable);
					if (lts != null && lts.Profondeur == lf.Profondeur) return lts;
				}
				// Aucune table trouvée
				return null;
			}

			// Test uniquement dans le cas d'un seul segment table et plusieurs segments fichiers
			private void testMajSegmentsFichierObligatoire(string nomsegmentimport, LienFichier dependde)
			{
				// Recherche des dépendances
				LienFichier[] lfs = importFichier.Liens.getLiens(dependde);
				if (lfs.Length == 0) return;
				// Des champs du segment supérieurs parent sont-il mis à jour.
				bool segsup = (Segments.getImports(nomsegmentimport, dependde.Segment).Length > 0);

				// Pour chaque dépendance
				foreach (LienFichier lf in lfs)
				{
					// Des champs sont-il MAJ. 
					bool segcrs = (Segments.getImports(nomsegmentimport, lf.Segment).Length > 0);

					// Si segment obligatoire, il faut un champ MAJ
					if (!lf.Facultatif && !segcrs)
						throw new FileIOError(this.GetType().FullName, "Le segment fichier '" + lf.Segment.Nom + "' n'est pas mis à jour par le segment import '" + nomsegmentimport + "'");

					// Si des champs sont MAJ mais aucun champ du niveau supérieur, c'est une erreur
					if (segcrs && !segsup)
						throw new FileIOError(this.GetType().FullName, "Le segment table '" + lf.Segment.Nom + "' est mis à jour alors que son segment parent '" + dependde.Segment.Nom + "' ne l'est jamais");

					// Recherche sur les dépedances
					testMajSegmentsFichierObligatoire(nomsegmentimport, lf);
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
					if (se.NomSegmentImport.Equals(item.NomSegmentImport) &&
						se.SegmentTable.Equals(item.SegmentTable) &&
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
			/// Renvoie un tableau des segments imports utilisant le nom de segment import
			/// </summary>
			/// <param name="nomsegmentimport">Le nom segment import</param>
			/// <returns>La liste des segments imports</returns>
			public SegmentImport[] getImports(string nomsegmentimport)
			{
				// Le tableau des liens
				SegmentImport[] ses = new SegmentImport[] { };

				// Recherche des dépendances
				for (int i = 0; i < this.Count; i++)
				{
					if (this[i].NomSegmentImport.Equals(nomsegmentimport))
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
			/// Renvoie un tableau des segments imports utilisant le segment fichier
			/// </summary>
			/// <param name="segmentfichier">Le segment fichier</param>
			/// <returns>La liste des segments imports</returns>
			public SegmentImport[] getImports(SegmentFichier segmentfichier)
			{
				// Le tableau des liens
				SegmentImport[] ses = new SegmentImport[] { };

				// Recherche des dépendances
				for (int i = 0; i < this.Count; i++)
				{
					if (segmentfichier.Champs.Contains(this[i].ChampFichierMaj))
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
			/// <param name="nomsegmentimport">Le nom du gement import</param>
			/// <param name="segmentfichier">Le segment fichier</param>
			/// <returns>La liste des segments imports</returns>
			public SegmentImport[] getImports(string nomsegmentimport, SegmentFichier segmentfichier)
			{
				// Le tableau des liens
				SegmentImport[] ses = new SegmentImport[] { };

				// Recherche des dépendances
				for (int i = 0; i < this.Count; i++)
				{
					if (this[i].NomSegmentImport.Equals(nomsegmentimport) && segmentfichier.Champs.Contains(this[i].ChampFichierMaj))
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
			private string nomsegmentimport;
			private DataTable segmenttable;
			private string nomchamptable;
			private ChampFichier champfichiermaj;

			/// <summary>
			/// Nom du segment import dans la descrition
			/// </summary>
			public string NomSegmentImport
			{
				get { return nomsegmentimport; }
				private set { nomsegmentimport = value; }
			}

			/// <summary>
			/// Le segment table utilisé par le segment import.
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
			/// <param name="nomSegmentImport">Nom du segment import</param>
			/// <param name="dtExport">La table à utiliser</param>
			/// <param name="segmentsfichier">Les segments fichier à importer</param>
			/// <param name="sc">La chaine contenant la régle de mise à jour</param>
			internal SegmentImport(string nomSegmentImport, DataTable dtExport, SegmentsFichier segmentsfichier, string sc)
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
				// Le nom du segment import
				this.NomSegmentImport = nomSegmentImport;
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
