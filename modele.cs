using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace llt.FileIO
{
    /// <summary>
    /// Création de l'objet gérant les modèles de nom de fichier
    /// </summary>
    public class ModeleNomFichier
    {
        // Lettres de l'alphabet
        private char[] lettres = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };

        // Modèle pour le nom (sans l'extension)
        private string ModeleNom;
        // Modèle pour l'extension
        private string ModeleExtension;

        /// <summary>
        /// Test si le modele de nom de fichier est correct
        /// </summary>
        /// <param name="modelenomfichier">Le modèle à vérifier</param>
        /// <param name="modelessupportés">Modéles suggérés ou null si aucune erreur</param>
        /// <returns>Vrai si modèle correcte</returns>
        public static bool TestModele(string modelenomfichier, out string modelessupportés)
        {
            // Par défaut, pas d'envoie d'information.
            modelessupportés = null;
            // On teste l'extension.
            string[] modeles;
            int e = modelenomfichier.IndexOf("].[");
            if (e < 0)
                modeles = new string[] { modelenomfichier };
            else
                modeles = new string[] { modelenomfichier.Substring(0, e + 1), modelenomfichier.Substring(e + 2) };
            // Test de la validité du modèle
            foreach (string modele in modeles)
            {
                if (modele.Equals("[0-9]") ||
                    modele.Equals("[0-9-*]") ||
                    modele.Equals("[a-z]", StringComparison.CurrentCultureIgnoreCase) ||
                    modele.Equals("[a-z-*]", StringComparison.CurrentCultureIgnoreCase))
                    continue;
                else if (modele.Equals("[*]"))
                    continue;
                else if (modele.StartsWith("['"))
                {
                    // Commence par ...
                    int i = modele.IndexOf("'-*]");
                    bool commencepar = (i >= 0);
                    // Egal à ...
                    if (i < 0) i = modele.IndexOf("']");
                    // Anomalie si aucun des deux formats.
                    if (i < 0)
                    {
                        modelessupportés = "Le modèle '" + modele + "' comporte une erreur. La syntaxe est ['<caractere(s)>'] ou ['<caractere(s)>'-*]";
                        return false;
                    }
                    else
                        continue;
                }
                else
                {
                    modelessupportés = "Le modèle '" + modele + "' est non connu. Les modèles suivant sont supportés : \r\n" +
                        "[0-9] ou [0-9-*] \r\n" +
                        "[a-z] ou [a-z-*] \r\n" +
                        "[*] \r\n" +
                        "['<caractere(s)>'] ou ['<caractere(s)>'-*]";
                    return false;
                }
            }
            // Modèle conforme.
            return true;
        }

        /// <summary>
        /// Création du modèle de nom de fichier
        /// </summary>
        /// <param name="modelenomfichier">La chaine de caractère contenant le modèle à créer</param>
        /// <remarks>
        /// Le modèle doit être de la forme suivante 'modelenom'.'modeleextension' supportant
        /// les mots clés suivants
        /// <para>
        /// [0-9] : ne doit contenir que des chiffres 
        /// [0-9-*] : doit commencer par au moins un chiffre
        /// [a-z] : ne doit contenir que des caractères alphabétiques
        /// [a-z-*] : doit commencer par au moins un caractère alphabétique
        /// ['caractère(s)'-*] : doit commercer par les caractères entre ''
        /// ['caractère(s)'] : doit être égal au caractères entre ''
        /// [*] : aucun controle
        /// </para>
        /// </remarks>
        public ModeleNomFichier(string modelenomfichier)
        {
            // On teste l'extension.
            string[] modeles;
            int e = modelenomfichier.IndexOf("].[");
            if (e < 0)
                modeles = new string[] { modelenomfichier };
            else
                modeles = new string[] { modelenomfichier.Substring(0, e + 1), modelenomfichier.Substring(e + 2) };
            // Détermine le modèle du nom (sans l'extension)
            ModeleNom = TestModele(modeles[0]);
            if (ModeleNom == null)
                throw new llt.FileIO.FileIOError(this.GetType().FullName, "Le modèle '" + modeles[0] + "' n\'est pas reconnu (nom du fichier)");
            // Détermine le modèle de l'extension
            if (modeles.Length == 1)
                ModeleExtension = "";
            else
            {
                ModeleExtension = TestModele(modeles[1]);
                if (ModeleExtension == null)
                    throw new llt.FileIO.FileIOError(this.GetType().FullName, "Le modèle '" + modeles[0] + "' n\'est pas reconnu (extension du fichier)");
            }
        }

        /// <summary>
        /// Liste des fichiers contenu dans le répertoire local correspondant au modèle de nom de fichier
        /// </summary>
        /// <param name="LocalPath">Le répertoire à tester</param>
        /// <returns>Liste des fichiers trouvés</returns>
        public string[] ListeFichiers(string LocalPath)
        {
            try
            {
                // Récupéère la liste des fichiers
                System.Collections.Generic.List<string> fichiers = new System.Collections.Generic.List<string>();
                fichiers.AddRange(System.IO.Directory.GetFiles(RepertoireAbsolu(LocalPath)));
                // Test les noms de fichiers
                TestListeFichiers(fichiers);
                // Renvoie la liste obtenue
                return fichiers.ToArray();
            }
            catch
            {
                return new string[] { };
            }
        }
        /// <summary>
        /// Renvoie un nom de fichier conforme au modèle de nom de fichier et qui n'existe pas dans le répertoire local
        /// </summary>
        /// <param name="LocalPath">Le répertoire à tester</param>
        /// <returns>Un nom de fichier</returns>
        public string NomFichier(string LocalPath)
        {
            // On travaille en répertoire absolu
            string absLocalPath = RepertoireAbsolu(LocalPath);
            // Test si le répertoire existe
            if (!System.IO.Directory.Exists(absLocalPath))
                throw new llt.FileIO.FileIOError(this.GetType().FullName, "Le répertoire '" + absLocalPath + "' n\'existe pas.");
            // Création du nom
            string nom = "";
            bool nomvariable = false;
            int tentative = 0;
            while (tentative < 5)
            {
                if (ModeleNom.Equals("*") || ModeleNom.Equals("[a-z]", StringComparison.CurrentCultureIgnoreCase) ||
                    ModeleNom.Equals("[a-z-*]", StringComparison.CurrentCultureIgnoreCase))
                {
                    nomvariable = true;
                    System.Random r = new Random();
                    for (int i = 0; i < 8; i++)
                    {
                        nom = nom + lettres[r.Next(0, 26)].ToString();
                    }
                }
                else if (ModeleNom.Equals("[0-9]") || ModeleNom.Equals("[0-9-*]"))
                {
                    nomvariable = true;
                    System.Random r = new Random();
                    for (int i = 0; i < 8; i++)
                    {
                        nom = nom + r.Next(0, 10).ToString();
                    }
                }
                else if (!ModeleNom.Equals(""))
                {
                    nom = ModeleNom;
                    if (nom.EndsWith("*"))
                    {
                        nomvariable = true;
                        nom = nom.Substring(0, nom.Length - 1);
                        int maxi = 8 - nom.Length;
                        if (maxi < 0) maxi = 1;
                        System.Random r = new Random();
                        for (int i = 0; i < maxi; i++)
                        {
                            nom = nom + r.Next(0, 10).ToString();
                        }
                    }
                }

                // Création de l'extension
                string ext = "";
                if (ModeleExtension.Equals("*") || ModeleExtension.Equals("[a-z]", StringComparison.CurrentCultureIgnoreCase) ||
                    ModeleExtension.Equals("[a-z-*]", StringComparison.CurrentCultureIgnoreCase))
                {
                    ext = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames[(int)System.DateTime.Today.DayOfWeek];
                }
                else if (ModeleExtension.Equals("[0-9]") || ModeleExtension.Equals("[0-9-*]"))
                    ext = System.DateTime.Today.DayOfYear.ToString("000");
                else if (!ModeleExtension.Equals(""))
                {
                    ext = ModeleExtension;
                    if (ext.EndsWith("*")) ext = ext.Substring(0, ext.Length - 1);
                }

                // Est-ce que le fichier existe dans le répertoire
                string nomext = nom;
                if (!ext.Equals("")) nomext = nom + "." + ext;
                if (!System.IO.File.Exists(absLocalPath + "\\" + nom + "." + ext)) return nom + "." + ext;
                if (!nomvariable)
                    throw new llt.FileIO.FileIOError(this.GetType().FullName, "Le fichier '" + nom + "." + ext + "' existe déja dans le répertoire '" + absLocalPath + "'");

                // Incrémente le nombre de tentative
                tentative++;
            }

            // Anomalie après 5 tentative
            throw new llt.FileIO.FileIOError(this.GetType().FullName, "Impossible de proposer un nom de fichier inexistant pour le répertoire '" + LocalPath + "'");
        }

        /// <summary>
        /// Supprime de la liste de nom de fichier tous les fichiers non conforme au modèle
        /// </summary>
        /// <param name="fichiers">Liste de nom de fichier à tester</param>
        public void TestListeFichiers(System.Collections.Generic.List<string> fichiers)
        {
            // On parcours l'ensemble des fichiers trouvés pour voir s'ils correspondent
            for (int ifichiers = 0; ifichiers < fichiers.Count; )
            {
                fichiers[ifichiers] = System.IO.Path.GetFileName(fichiers[ifichiers]);
                // Test du filtre sur le nom
                if (!TestChaine(System.IO.Path.GetFileNameWithoutExtension(fichiers[ifichiers]), this.ModeleNom))
                    fichiers.RemoveAt(ifichiers);
                else
                {
                    // Recherche de l'extension
                    string ext = System.IO.Path.GetExtension(fichiers[ifichiers]);
                    // Suppression du point si nécessaire
                    // NOTA : le fichier peut ne pas avoir d'extension
                    if (ext.StartsWith(".")) ext = ext.Substring(1);
                    if (!TestChaine(ext, this.ModeleExtension))
                        fichiers.RemoveAt(ifichiers);
                    else
                        ifichiers++;
                }
            }
        }

        /// <summary>
        /// Test le modèle de recherche
        /// </summary>
        /// <param name="modele">Le modèle à tester</param>
        /// <returns>Null si modèle incorrect sinon le modèle accepté</returns>
        private string TestModele(string modele)
        {
            // Test si on comprend le modele
            if (modele.Equals("[0-9]") ||
                modele.Equals("[0-9-*]") ||
                modele.Equals("[a-z]", StringComparison.CurrentCultureIgnoreCase) ||
                modele.Equals("[a-z-*]", StringComparison.CurrentCultureIgnoreCase))
                return modele;
            else if (modele.Equals("[*]"))
                return "*";
            else if (modele.StartsWith("['"))
            {
                // Commence par ...
                int i = modele.IndexOf("'-*]");
                bool commencepar = (i >= 0);
                // Egal à ...
                if (i < 0) i = modele.IndexOf("']");
                // Anomalie si aucun des deux formats.
                if (i < 0)
                    return null;
                else
                {
                    modele = modele.Substring(2, i - 2);
                    if (commencepar) modele = modele + "*";
                    return modele;
                }
            }
            else
                return null; // Modele non reconnu
        }

        /// <summary>
        /// Test si une chaine de caractère correspond au modele demandé
        /// </summary>
        /// <param name="testchaine">La chaine à tester</param>
        /// <param name="modele">Le modèle utilisé</param>
        /// <returns></returns>
        private bool TestChaine(string testchaine, string modele)
        {
            // Si aucun filtre, la chaine est forcément correcte.
            if (modele.Equals("*")) return true;
            // Traitement en fonction du filtre
            if (modele.Equals("[0-9]"))
            {
                char[] cs = testchaine.ToCharArray();
                foreach (char c in cs)
                {
                    if (!System.Char.IsNumber(c)) return false;
                }
                return true;
            }
            else if (modele.Equals("[a-z]", StringComparison.CurrentCultureIgnoreCase))
            {
                char[] cs = testchaine.ToCharArray();
                foreach (char c in cs)
                {
                    if (!System.Char.IsLetter(c)) return false;
                }
                return true;
            }
            else if (modele.Equals("[0-9-*]", StringComparison.CurrentCultureIgnoreCase))
            {
                return Char.IsNumber(testchaine, 0);
            }
            else if (modele.Equals("[a-z-*]"))
            {
                return Char.IsLetter(testchaine, 0);
            }
            else
            {
                // A ce stade il s'agit forcément du modèle ['caractère(s)'-*] ou ['caractère(s)']
                // ATTENTION : seule caractères(s) est conservé au niveau du modele
                if (modele.EndsWith("*"))
                    return testchaine.StartsWith(modele.Substring(0, modele.Length - 1), StringComparison.CurrentCultureIgnoreCase);
                else
                    return testchaine.Equals(modele, StringComparison.CurrentCultureIgnoreCase);
            }
        }
        
        /// <summary>
        /// Retourne un répertoire absolu
        /// </summary>
        /// <param name="repertoire">Le répertoire à traiter</param>
        /// <returns>Le répertoire absolue de <paramref name="repertoire"/></returns>
        private string RepertoireAbsolu(string repertoire)
        {
            // Le répertoire doit exister
            string dir = System.AppDomain.CurrentDomain.BaseDirectory;
            if (!repertoire.Equals(""))
            {
                if (repertoire.StartsWith("\\\\"))
                    throw new llt.FileIO.FileIOError(this.GetType().FullName, "Impossible de faire référence à un chemin réseau ('" + repertoire + ")'");
                // Chemin absolu
                else if (System.IO.Path.IsPathRooted(repertoire))
                {
                    // Test si le volume est présent.
                    if (repertoire.IndexOf(System.IO.Path.VolumeSeparatorChar) >= 0)
                        dir = repertoire;
                    else
                    {
                        dir = dir.Substring(0, dir.IndexOf(System.IO.Path.VolumeSeparatorChar) + 1) + repertoire;
                    }
                }
                // On ajoute le répertoire d'éxécution au répertoire local
                else
                    dir = dir + "\\" + repertoire;
            }
            if (!System.IO.Directory.Exists(dir))
                throw new llt.FileIO.FileIOError(this.GetType().FullName, "Le répertoire '" + dir + "' n'existe pas");

            // Répertoire absolu.
            return dir;
        }
    }
}
