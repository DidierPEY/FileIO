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
        /// La classe de base pour définir les types de données.
        /// </summary>
        private abstract class TypeDonnee
        {
            // Valeurs privées des propriétés
            private int debut;
            private int longueur;

            // Position du premier caractère.
            public int Debut
            {
                get { return debut; }
                protected set { debut = value; }
            }
            // Longueur totale du champ.
            public int Longueur
            {
                get { return longueur; }
                protected set { longueur = value; }
            }

            /// <summary>
            /// Contructeur de base pour les champs à longueur variable
            /// </summary>
            protected TypeDonnee()
            {
                Debut = -1;
                Longueur = -1;
            }
            /// <summary>
            /// Contructeur de base pour les champs à longueur fixe
            /// </summary>
            protected TypeDonnee(int debut)
            {
                // Le positionnement dans l'enregistrement doit être supérieur ou égale à zéro
                if (debut < 0)
                    throw new FileIOError(this.GetType().FullName, "La position de départ '" + debut + "' est incorrecte");
                Debut = debut;
                Longueur = -1;
            }

            /// <summary>
            /// Test si l'information de longueur est correcte.
            /// </summary>
            /// <param name="longueur">La chaine à tester</param>
            /// <returns></returns>
            protected static int TestLongueur(string longueur)
            {
                try
                {
                    // Supprime les éventuels '[' et ']'.
                    if (longueur.StartsWith("[")) longueur = longueur.Substring(1);
                    if (longueur.EndsWith("]")) longueur = longueur.Substring(0, longueur.Length - 1);
                    if (longueur.Length == 0) return System.Int32.MinValue;
                    // Test si la longueur est correcte.
                    if (System.Convert.ToInt32(longueur) < 1) return System.Int32.MinValue;
                    // Aucune erreur.
                    return System.Convert.ToInt32(longueur);
                }
                catch
                {
                    return System.Int32.MinValue;
                }
            }
            /// <summary>
            /// Donne la valeur du champ en fonction du type de donnée.
            /// </summary>
            /// <param name="fromvalue">La chaine de caractère contenant la valeur</param>
            /// <returns>Renvoie une valeur typée</returns>
            internal abstract object GetValue(string fromvalue);
            /// <summary>
            /// Convetit la valeur du champ en chaine de caractère en tenant compte type de donnée.
            /// </summary>
            /// <param name="value">Valeur à convertir</param>
            /// <returns>Chaine de caractère obtenu</returns>
            internal abstract string SetValue(object value);
        }

        /// <summary>
        /// Le type de donnée pour les chaines de caractères
        /// </summary>
        private class Alphanumerique : TypeDonnee
        {
            /// <summary>
            /// Création dans le cas de format="a" (champ à longueur variable)
            /// </summary>
            /// <param name="formatchamp">Format du champ</param>
            public Alphanumerique(string formatchamp)
                : base()
            {
                // Type alphanumérique
                if (!formatchamp.ToLower().StartsWith("a"))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' est incorrecte pour ce type de données");
            }
            /// <summary>
            /// Création dans le cas de format="a[l] (l=longueur de la chaine)" (champ à longueur fixe)
            /// </summary>
            /// <param name="formatchamp">Format du champ</param>
            /// <param name="debut">Position du premier caractère dans l'enregistrement</param>
            public Alphanumerique(string formatchamp, int debut)
                : base(debut)
            {
                // Type alphanumérique
                if (!formatchamp.ToLower().StartsWith("a"))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' est incorrecte pour ce type de données");

                // Recherche de la taille.
                if (!formatchamp.ToLower().StartsWith("a["))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de taille correcte");
                if (!formatchamp.ToLower().EndsWith("]"))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de taille correcte");
                Longueur = TypeDonnee.TestLongueur(formatchamp.Substring(1, formatchamp.Length - 1));
                if (Longueur.Equals(Int32.MinValue))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de taille correcte");
            }

            /// <summary>
            /// Donne la valeur du champ
            /// </summary>
            /// <param name="fromvalue">La chaine de caractère contenant la valeur</param>
            /// <returns>Renvoie une valeur de type System.String</returns>
            /// <remarks>
            /// Le champ alphanumérique est le seul à tolérer que la longueur des données soit
            /// inférieur à la longueur annoncée.
            /// </remarks>
            internal override object GetValue(string fromvalue)
            {
                if (Debut == -1)
                    return fromvalue;
                else
                {
                    // Test si la chaine est suffisamment grande
                    if (Debut >= fromvalue.Length) return "";
                    // Renvoie la valeur en ajustant éventuellement la taille
                    if (fromvalue.Length > Debut + Longueur)
                        return fromvalue.Substring(Debut, Longueur);
                    else
                        return fromvalue.Substring(Debut);
                }
            }
            /// <summary>
            /// Convetit la valeur du champ en chaine de caractère en tenant compte type de donnée.
            /// </summary>
            /// <param name="value">Valeur à convertir</param>
            /// <returns>Chaine de caractère obtenu</returns>
            internal override string SetValue(object value)
            {
                if (value != null && value != System.DBNull.Value)
                {
                    if (Debut == -1)
                        return value.ToString();
                    else
                    {
                        string tmpvalue = (value == null ? "" : value.ToString());
                        if (tmpvalue.Length > Longueur)
                            throw new FileIOError(this.GetType().FullName, "La taille de la chaine '" + tmpvalue + "' est trop grande (longueur maxi " + Longueur.ToString() + ")");
                        else
                            tmpvalue = tmpvalue.PadRight(Longueur - tmpvalue.Length, ' ');
                        return tmpvalue;
                    }
                }
                else
                {
                    if (Debut == -1) return "";
                    else return (new StringBuilder(" ", Longueur)).ToString();
                }
            }
        }

        /// <summary>
        /// Le type de donnée pour les entiers.
        /// </summary>
        private class Entier : TypeDonnee
        {
            /// <summary>
            /// Création dans le cas de format="9" (champ à longueur variable)
            /// </summary>
            /// <param name="formatchamp">Format du champ</param>
            public Entier(string formatchamp)
                : base()
            {
                // Type numérique
                if (!formatchamp.StartsWith("9"))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' est incorrecte pour ce type de données");
                // S'agit-il d'un nombre entier uniquement
                if (formatchamp.Contains("0"))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' est incorrecte pour ce type de données");
            }
            /// <summary>
            /// Création dans le cas de format="9[l] (l=nombre de chiffres)" (champ à longueur fixe)
            /// </summary>
            /// <param name="formatchamp">Format du champ</param>
            /// <param name="debut">Position du premier caractère dans l'enregistrement</param>
            public Entier(string formatchamp, int debut)
                : base(debut)
            {
                // Type numérique
                if (!formatchamp.StartsWith("9"))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' est incorrecte pour ce type de données");
                // S'agit-il d'un nombre entier uniquement
                if (formatchamp.Contains("0"))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' est incorrecte pour ce type de données");

                // Recherche de la taille.
                if (!formatchamp.ToLower().StartsWith("9["))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de taille correcte");
                if (!formatchamp.ToLower().EndsWith("]"))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de taille correcte");
                Longueur = TypeDonnee.TestLongueur(formatchamp.Substring(1, formatchamp.Length - 1));
                if (Longueur.Equals(Int32.MinValue))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de taille correcte");
            }
            /// <summary>
            /// Donne la valeur du champ
            /// </summary>
            /// <param name="fromvalue">La chaine de caractère contenant la valeur</param>
            /// <returns>Renvoie une valeur de type System.Int32</returns>
            internal override object GetValue(string fromvalue)
            {
                // Test si la chaine est suffisamment grande
                if (Debut > -1 && Debut >= fromvalue.Length) return null;

                // Récupère la valeur
                string tmpvalue = Debut == -1 ? fromvalue : fromvalue.Substring(Debut, Longueur);
                if (tmpvalue.Trim().Equals("")) return null;
                return System.Convert.ToInt32(tmpvalue);
            }
            /// <summary>
            /// Convetit la valeur du champ en chaine de caractère en tenant compte type de donnée.
            /// </summary>
            /// <param name="value">Valeur à convertir</param>
            /// <returns>Chaine de caractère obtenu</returns>
            internal override string SetValue(object value)
            {
                if (value != null && value != System.DBNull.Value)
                {
                    if (Debut == -1)
                        return System.Convert.ToInt32(value).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    else
                    {
                        string tmpvalue = System.Convert.ToInt32(value).ToString(System.Globalization.CultureInfo.InvariantCulture);
                        if (tmpvalue.Length > Longueur)
                            throw new FileIOError(this.GetType().FullName, "La taille du nombre entier '" + tmpvalue + "' est trop grande (nombre de chiffre(s) maximum " + Longueur.ToString() + ")");
                        else if (tmpvalue.Length < Longueur)
                            tmpvalue = tmpvalue.PadLeft(Longueur - tmpvalue.Length, '0');
                        return tmpvalue;
                    }
                }
                else
                {
                    if (Debut == -1) return "";
                    else return (new StringBuilder(" ", Longueur)).ToString();
                }
            }
        }

        /// <summary>
        /// Le type de donnée pour les nombres décimaux.
        /// </summary>
        private class NombreDecimal : TypeDonnee
        {
            // Longueur de la partie entière
            private int LongueurEntier;
            // Longueur de la partie décimale
            private int LongueurDecimale;
            // Le séparateur de décimale
            private char SepDecimale;
            /// <summary>
            /// Création dans le cas de format="9<![CDATA[<separateur de décimale>]]>0" ou "9[l]0" ou "90[l] (l=nombre de chiffres)" (champ à longueur variable)
            /// </summary>
            /// <param name="formatchamp">Format du champ</param>
            public NombreDecimal(string formatchamp)
                : base()
            {
                // Initialisation des longueurs secondaires.
                LongueurEntier = -1;
                LongueurDecimale = -1;
                SepDecimale = Char.MinValue;

                // Type numérique
                if (!formatchamp.StartsWith("9"))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' est incorrecte pour ce type de données");
                // S'agit-il d'un nombre entier uniquement
                if (!formatchamp.Contains("0"))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' est incorrecte pour ce type de données");

                // Test si format de type 9<séparateur décimal>0.
                if (formatchamp.Substring(1, 1).IndexOfAny(new char[] { '0', '9', '[', ']' }) < 0)
                {
                    // Mémorise le séparateur de décimale.
                    SepDecimale = formatchamp[1]; ;
                }
                // Taille précisée sur le partie entière
                else if (formatchamp.StartsWith("9["))
                {
                    int ifc = formatchamp.IndexOf("]");
                    if (ifc < 0) // Aucun caratère de fin de taille "]" présent.
                        throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de taille correcte");
                    // Test si la longueur est correcte
                    LongueurEntier = TestLongueur(formatchamp.Substring(1, ifc));
                    if (LongueurEntier.Equals(Int32.MinValue))
                        throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de taille correcte");
                }
                // Taille précisée sur la partie décimale
                else
                {
                    if (!formatchamp.EndsWith("]"))
                        throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de taille correcte");
                    int ifc = formatchamp.LastIndexOf("0"); // Recherche l'emplacement du "0" dans la chaine.
                    if (ifc < 0)
                        throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de taille correcte");
                    // Test si la longueur est correcte
                    LongueurDecimale = TestLongueur(formatchamp.Substring(ifc + 1));
                    if (LongueurDecimale.Equals(Int32.MinValue))
                        throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de taille correcte");
                }
            }

            /// <summary>
            /// Création dans la cas de format="9[e]0[d]" ou "9[e]<![CDATA[<separateur de décimale>]]>0[d] (e/d : nombres de chiffres" (champ à longueur fixe) 
            /// </summary>
            /// <param name="formatchamp">Format du champ</param>
            /// <param name="debut">Position du premire caractère dans l'enregistrement</param>
            public NombreDecimal(string formatchamp, int debut)
                : base(debut)
            {
                // Initialisation des longueurs secondaires.
                LongueurEntier = -1;
                LongueurDecimale = -1;
                SepDecimale = Char.MinValue;

                // Type numérique
                if (!formatchamp.StartsWith("9"))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' est incorrecte pour ce type de données");
                // S'agit-il d'un nombre entier uniquement
                if (!formatchamp.Contains("0"))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' est incorrecte pour ce type de données");

                // Il faut obligatoirement une information de taille sur la partie entière
                if (!formatchamp.StartsWith("9["))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne possède aucune information de longueur pour la partie entière");
                int ifc = formatchamp.IndexOf("]");
                if (ifc < 0) // Aucun caratère de fin de taille "]" présent.
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de taille correcte");
                // Test si la longueur est correcte
                LongueurEntier = TestLongueur(formatchamp.Substring(1, ifc));
                if (LongueurEntier.Equals(Int32.MinValue))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de taille correcte pour la partie entière");

                // Un séparateur de décimal est-il précisé
                ifc++;
                if (formatchamp.Substring(ifc, 1).IndexOfAny(new char[] { '0', '9', '[', ']' }) < 0)
                {
                    SepDecimale = formatchamp[ifc];
                    ifc++;
                }

                // La suite de la chaine doit forcément commencer par "0["
                if (!formatchamp.Substring(ifc).StartsWith("0["))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de taille correcte pour la partie décimale");
                if (!formatchamp.EndsWith("]"))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de taille correcte pour la partie décimale");
                LongueurDecimale = TestLongueur(formatchamp.Substring(ifc + 1));
                if (LongueurDecimale.Equals(Int32.MinValue))
                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de taille correcte pour la partie décimale");

                // Longueur totale
                Longueur = LongueurDecimale + LongueurEntier;
                if (!SepDecimale.Equals(Char.MinValue)) Longueur++;
            }

            /// <summary>
            /// Donne la valeur du champ
            /// </summary>
            /// <param name="fromvalue">La chaine de caractère contenant la valeur</param>
            /// <returns>Renvoie une valeur de type System.Decimal</returns>
            internal override object GetValue(string fromvalue)
            {
                if (Debut == -1)
                {
                    if (fromvalue.Trim().Equals("")) return null;
                    if (!SepDecimale.Equals(Char.MinValue))
                    {
                        int isd = fromvalue.IndexOf(SepDecimale);
                        if (isd < 0)
                            return System.Convert.ToDecimal(fromvalue);
                        else if (isd == 0)
                            return System.Convert.ToDecimal(fromvalue) / (decimal)System.Math.Pow(10, fromvalue.Length - 1);
                        else
                            return System.Convert.ToDecimal(fromvalue.Substring(0, isd)) +
                                System.Convert.ToDecimal(fromvalue.Substring(isd + 1)) / (decimal)System.Math.Pow(10, fromvalue.Length - isd - 1);
                    }
                    else if (LongueurEntier != -1)
                    {
                        if (fromvalue.Length <= LongueurEntier)
                            return System.Convert.ToDecimal(fromvalue);
                        else
                            return System.Convert.ToDecimal(fromvalue.Substring(0, LongueurEntier)) +
                                System.Convert.ToDecimal(fromvalue.Substring(LongueurEntier)) / (decimal)System.Math.Pow(10, fromvalue.Length - LongueurEntier);
                    }
                    else
                    {
                        if (fromvalue.Length <= LongueurDecimale)
                            return System.Convert.ToDecimal(fromvalue) / (decimal)System.Math.Pow(10, fromvalue.Length);
                        else
                            return System.Convert.ToDecimal(fromvalue.Substring(0, fromvalue.Length - LongueurDecimale)) +
                                System.Convert.ToDecimal(fromvalue.Substring(fromvalue.Length - LongueurDecimale)) / (decimal)System.Math.Pow(10, LongueurDecimale);
                    }
                }
                else
                {
                    // Test si la chaine est suffisamment grande
                    if (Debut >= fromvalue.Length) return null;

                    // Récupère la valeur
                    string tmpvalue = fromvalue.Substring(Debut, Longueur);
                    if (tmpvalue.Trim().Equals("")) return null;
                    return System.Convert.ToDecimal(tmpvalue.Substring(0, LongueurEntier)) +
                        System.Convert.ToDecimal(tmpvalue.Substring(Longueur - LongueurDecimale)) / (decimal)System.Math.Pow(10, LongueurDecimale);
                }
            }
            /// <summary>
            /// Convetit la valeur du champ en chaine de caractère en tenant compte type de donnée.
            /// </summary>
            /// <param name="value">Valeur à convertir</param>
            /// <returns>Chaine de caractère obtenu</returns>
            internal override string SetValue(object value)
            {
                if (value != null && value != System.DBNull.Value)
                {
                    // Création de la partie entière et de la partie décimale
                    string tmpentiere = System.Decimal.Truncate(System.Convert.ToDecimal(value)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string tmpdecimale = (System.Convert.ToDecimal(value) - System.Decimal.Truncate(System.Convert.ToDecimal(value))).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if (!tmpdecimale.Equals("0")) tmpdecimale = tmpdecimale.Substring(2);
                    else tmpdecimale = "";

                    // Conversion
                    if (Debut == -1)
                    {
                        if (!SepDecimale.Equals(Char.MinValue))
                            return tmpentiere + (tmpdecimale.Equals("") ? "" : SepDecimale + tmpdecimale);
                        else
                        {
                            if (LongueurEntier != -1)
                            {
                                if (tmpentiere.Length > LongueurEntier)
                                    throw new FileIOError(this.GetType().FullName, "La partie entiere '" + tmpentiere + "' est trop grande (nombre de chiffre(s) maximum " + LongueurEntier.ToString() + ")");
                                else if (tmpentiere.Length < LongueurEntier)
                                    tmpentiere = tmpentiere.PadLeft(LongueurEntier - tmpentiere.Length, '0');
                            }
                            else
                            {
                                if (tmpdecimale.Length > LongueurDecimale)
                                    tmpdecimale = tmpdecimale.Substring(LongueurDecimale); // Pas d'erreur, simple perte de précision
                                else if (tmpdecimale.Length < LongueurDecimale)
                                    tmpdecimale = tmpdecimale.PadRight(LongueurDecimale - tmpdecimale.Length, '0');
                            }
                            return tmpentiere + tmpdecimale;
                        }
                    }
                    else
                    {
                        if (tmpentiere.Length > LongueurEntier)
                            throw new FileIOError(this.GetType().FullName, "La partie entiere '" + tmpentiere + "' est trop grande (nombre de chiffre(s) maximum " + LongueurEntier.ToString() + ")");
                        else if (tmpentiere.Length < LongueurEntier)
                            tmpentiere = tmpentiere.PadLeft(LongueurEntier - tmpentiere.Length, '0');
                        if (tmpdecimale.Length > LongueurDecimale)
                            tmpdecimale = tmpdecimale.Substring(LongueurDecimale); // Pas d'erreur, simple perte de précision
                        else if (tmpdecimale.Length < LongueurDecimale)
                            tmpdecimale = tmpdecimale.PadRight(LongueurDecimale - tmpdecimale.Length, '0');
                        return tmpentiere + (SepDecimale.Equals(Char.MinValue) ? "" : SepDecimale.ToString()) + tmpdecimale;
                    }
                }
                else
                {
                    if (Debut == -1) return "";
                    else return (new StringBuilder(" ", Longueur).ToString());
                }
            }
        }

        /// <summary>
        /// Le type de donnée pour les dates (Date seule, Heure seule, Date + Heure)
        /// </summary>
        private class DateHeure : TypeDonnee
        {
            // Mots clés supportés dans le champ date/heure
            private string[] MOTCLES = new string[] { "dj", "dm", "da", "hh", "hm", "hs" };
            // Position des différents mots clé dans le format
            private System.Collections.Generic.Dictionary<string, int> debutmc;
            // Position des différents mots clés triés par position
            // REMARQUE : 
            // Seul les mots clés utilisés sont présents dans cette liste.
            // Cette liste n'est créé qu'en cas de l'utilisation de SetValue.
            private System.Collections.Generic.SortedDictionary<int, string> debutmct;
            // Longueur de l'année (2 ou 4)
            private int longueurda;
            // Séparateur utilisé pour la date
            char SepDate;
            // Séparateur utilisé pour l'heure
            char SepHeure;
            // Séparateur entre la date et l'heure
            char SepDateHeure;

            /// <summary>
            /// Création dans le cas de format date/heure (champ à longueur variable)
            /// </summary>
            /// <param name="formatchamp">Format du champ</param>
            public DateHeure(string formatchamp)
                : base()
            {
                // Test si format correct
                TestFormatDate(formatchamp);
            }
            /// <summary>
            /// Création dans le cas de format date/heure (champ à longueur variable)
            /// </summary>
            /// <param name="formatchamp">Format du champ</param>
            /// <param name="debut">Position du premire caractère dans l'enregistrement</param>
            public DateHeure(string formatchamp, int debut)
                : base(debut)
            {
                // Test si format correct
                TestFormatDate(formatchamp);
                // Mémorise la longueur
                Longueur = formatchamp.Length;
            }

            /// <summary>
            /// Donne la valeur du champ
            /// </summary>
            /// <param name="fromvalue">La chaine de caractère contenant la valeur</param>
            /// <returns>
            /// Renvoie une valeur de type System.DateTime ou System.TimeSpan si seule l'heure est précisée dans le format.
            /// </returns>
            internal override object GetValue(string fromvalue)
            {
                // Test si la chaine est suffisamment grande
                if (Debut > -1 && Debut >= fromvalue.Length) return null;

                // La chaine de caractère à convertir en date
                string fdate;
                if (Debut > -1) fdate = fromvalue.Substring(Debut, Longueur);
                else fdate = fromvalue;

                // Si la chaine est vide, on renvoie null
                if (fdate.Trim().Equals("")) return null;

                // Si seulement une heure de précisée
                if (debutmc["dj"].Equals(Int32.MinValue))
                {
                    return new System.TimeSpan(System.Convert.ToInt32(fdate.Substring(debutmc["hh"], 2)),
                        debutmc["hm"].Equals(Int32.MinValue) ? 0 : System.Convert.ToInt32(fdate.Substring(debutmc["hh"], 2)),
                        debutmc["hs"].Equals(Int32.MinValue) ? 0 : System.Convert.ToInt32(fdate.Substring(debutmc["hs"], 2)));
                }
                // Si seulement une date de précisée
                else if (debutmc["hh"].Equals(Int32.MinValue))
                {
                    return new System.DateTime(
                        longueurda == 2 ? System.Globalization.CultureInfo.CurrentCulture.Calendar.ToFourDigitYear(System.Convert.ToInt32(fdate.Substring(debutmc["da"], longueurda)))
                                      : System.Convert.ToInt32(fdate.Substring(debutmc["da"], longueurda)),
                        System.Convert.ToInt32(fdate.Substring(debutmc["dm"], 2)),
                        System.Convert.ToInt32(fdate.Substring(debutmc["dj"], 2)));
                }
                // Date et heure
                else
                    return new System.DateTime(
                        longueurda == 2 ? System.Globalization.CultureInfo.CurrentCulture.Calendar.ToFourDigitYear(System.Convert.ToInt32(fdate.Substring(debutmc["da"], longueurda)))
                                      : System.Convert.ToInt32(fdate.Substring(debutmc["da"], longueurda)),
                        System.Convert.ToInt32(fdate.Substring(debutmc["dm"], 2)),
                        System.Convert.ToInt32(fdate.Substring(debutmc["dj"], 2)),
                        System.Convert.ToInt32(fdate.Substring(debutmc["hh"], 2)),
                        debutmc["hm"].Equals(Int32.MinValue) ? 0 : System.Convert.ToInt32(fdate.Substring(debutmc["hh"], 2)),
                        debutmc["hs"].Equals(Int32.MinValue) ? 0 : System.Convert.ToInt32(fdate.Substring(debutmc["hs"], 2)));
            }
            /// <summary>2
            /// Convetit la valeur du champ en chaine de caractère en tenant compte type de donnée.
            /// </summary>
            /// <param name="value">Valeur à convertir</param>
            /// <returns>Chaine de caractère obtenu</returns>
            internal override string SetValue(object value)
            {
                if (value != null && value != System.DBNull.Value)
                {
                    // La valeur retourné
                    string tmpvalue = "";
                    DateTime tmpdate = System.Convert.ToDateTime(value);
                    if (debutmct == null)
                    {
                        // Création de la liste des mots clés triés par position
                        debutmct = new SortedDictionary<int, string>();
                        // On enregistre que les mots clés utilisés
                        foreach (KeyValuePair<string, int> kvp in debutmc)
                        {
                            if (!kvp.Value.Equals(Int32.MinValue)) debutmct.Add(kvp.Value, kvp.Key);
                        }
                    }
                    // Mémorisation de la clé précédente
                    KeyValuePair<int, string> kvpp = new KeyValuePair<int, string>(-1, "");
                    // Traitement de la date dans l'ordre
                    foreach (KeyValuePair<int, string> kvp in debutmct)
                    {
                        // Détermine si on soit ajouter un séparateur
                        if (kvpp.Value.Equals(""))
                            kvpp = kvp;
                        else
                        {
                            if (kvpp.Value.Substring(0, 1).Equals(kvp.Value.Substring(0, 1), StringComparison.CurrentCultureIgnoreCase))
                            {
                                // Séparateur de date ou d'heure
                                if (kvpp.Value.Substring(0, 1).Equals("d", StringComparison.CurrentCultureIgnoreCase))
                                    tmpvalue = tmpvalue + (SepDate.Equals(Char.MinValue) ? "" : SepDate.ToString());
                                else
                                    tmpvalue = tmpvalue + (SepHeure.Equals(Char.MinValue) ? "" : SepHeure.ToString());
                            }
                            else
                                // Séparateur date et heure
                                tmpvalue = tmpvalue + (SepDateHeure.Equals(Char.MinValue) ? "" : SepDateHeure.ToString());
                        }
                        // Traitement du mot clé
                        switch (kvp.Value)
                        {
                            case "dj":
                                tmpvalue = tmpvalue + (tmpdate.Day.ToString().Length == 2 ? "" : "0") + tmpdate.Day.ToString();
                                break;
                            case "dm":
                                tmpvalue = tmpvalue + (tmpdate.Month.ToString().Length == 2 ? "" : "0") + tmpdate.Month.ToString();
                                break;
                            case "da":
                                if (longueurda == 4) tmpvalue = tmpvalue + tmpdate.Year.ToString();
                                else tmpvalue = tmpvalue + tmpdate.Year.ToString().Substring(2);
                                break;
                            case "hh":
                                tmpvalue = tmpvalue + tmpdate.Hour.ToString();
                                break;
                            case "hm":
                                tmpvalue = tmpvalue + tmpdate.Minute.ToString();
                                break;
                            case "hs":
                                tmpvalue = tmpvalue + tmpdate.Second.ToString();
                                break;
                        }
                    }
                    // Renvoie date et/ou heure mise en forme
                    return tmpvalue;
                }
                else
                {
                    if (Debut == -1) return "";
                    else return (new StringBuilder(" ", Longueur).ToString());
                }
            }

            /// <summary>
            /// Test la validité du format
            /// </summary>
            /// <param name="formatchamp"></param>
            private void TestFormatDate(string formatchamp)
            {
                // Correctif position si utilisation de da[2] ou da[4]
                int corr = 0;
                // Recherche des différents mots-clés.
                debutmc = new Dictionary<string, int>();
                foreach (string mc in MOTCLES)
                {
                    // Recherche du mot clé.
                    int i = formatchamp.IndexOf(mc);
                    if (i < 0)
                        debutmc.Add(mc, Int32.MinValue); // Mot clé non trouvé.
                    else
                    {
                        // Mot clé trouvé.
                        debutmc.Add(mc, i);
                        // Test si mot clé en doublon
                        if (formatchamp.Length > (i + 2) && formatchamp.Substring(i + 2).IndexOf(mc) >= 0)
                            throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' comporte plusieurs '" + mc + "'");
                        // Est-ce qu'une information de taille est présente
                        if (formatchamp.Length > (i + 2) && formatchamp[i + 2] == '[')
                        {
                            if (!mc.Equals("da"))
                                throw new FileIOError(this.GetType().FullName, "Aucune information de taille n'est autorisée pour '" + mc + "'");
                            else
                            {
                                longueurda = TypeDonnee.TestLongueur(formatchamp.Substring(i + 2, 3));
                                if (longueurda != 2 && longueurda != 4)
                                {
                                    throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' indique une taille incorrecte pour l'année");
                                }
                                // La taille de da[2] ou da[4] est de 5 caractères. Cela fausse la position
                                // réelle des caractères situés après l'année.
                                corr = 5 - longueurda;
                            }

                        }
                        else if (mc.Equals("da"))
                            longueurda = 2;
                    }
                }

                // Si au moins UNE information de date dans le format ...
                if (!(debutmc["dj"].Equals(Int32.MinValue) && debutmc["dm"].Equals(Int32.MinValue) && debutmc["da"].Equals(Int32.MinValue)))
                {
                    // ... il FAUT que "dj","dm" et "da" soient tous les trois présents.
                    if (debutmc["dj"].Equals(Int32.MinValue) || debutmc["dm"].Equals(Int32.MinValue) || debutmc["da"].Equals(Int32.MinValue))
                        throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' ne comporte pas une information de date valide");
                }

                // Le format heure est plus souple, "hh" étant le seul élément obligatoire.
                if (!(debutmc["hh"].Equals(Int32.MinValue) && debutmc["hm"].Equals(Int32.MinValue) && debutmc["hs"].Equals(Int32.MinValue)))
                {
                    if (debutmc["hh"].Equals(Int32.MinValue))
                        throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' doit comporter une valeur 'hh'");
                    if (debutmc["hm"].Equals(Int32.MinValue) && !debutmc["hs"].Equals(Int32.MinValue))
                        throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' doit comporter une valeur 'hm' pour supporter 'hs'");
                }

                // Application du correctif de position pour tous les caractères situés après l'année.
                if (corr != 0)
                {
                    int posda = debutmc["da"];
                    foreach (string mc in MOTCLES)
                    {
                        if (debutmc[mc] > posda) debutmc[mc] -= corr;
                    }
                }
                // Recheche de la position du premier et dernier caractère composant la date ou de l'heure pour vérifier qu'il n'y a pas
                // une information mélangée
                int debutd = Int32.MaxValue;
                int find = Int32.MinValue;
                int debuth = Int32.MaxValue;
                int finh = Int32.MinValue;
                foreach (string mc in MOTCLES)
                {
                    if (mc.Equals("dj") || mc.Equals("dm") || mc.Equals("da"))
                    {
                        if (debutd > debutmc[mc]) debutd = debutmc[mc]; // début de la date
                        if (find < debutmc[mc]) find = debutmc[mc] + 1; // fin de la date
                    }
                    if (mc.Equals("hh") || mc.Equals("hm") || mc.Equals("hs"))
                    {
                        if (debuth > debutmc[mc]) debuth = debutmc[mc]; // début de la date
                        if (finh < debutmc[mc]) finh = debutmc[mc] + 1; // fin de la date
                    }
                }
                foreach (KeyValuePair<string, int> kvp in debutmc)
                {
                    if (!kvp.Key.Equals("dj") && !kvp.Key.Equals("dm") && !kvp.Key.Equals("da"))
                    {
                        if (!kvp.Value.Equals(Int32.MinValue) && kvp.Value > debutd && kvp.Value < find)
                        {
                            throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' mélange ls information de date et heure");
                        }
                    }
                    else
                    {
                        if (!kvp.Value.Equals(Int32.MinValue) && kvp.Value > debuth && kvp.Value < finh)
                        {
                            throw new FileIOError(this.GetType().FullName, "Le format '" + formatchamp + "' mélange ls information de date et heure");
                        }
                    }
                }

                // Pour finir, on détermine les différents séparateurs
                if (debutmc["dj"].Equals(Int32.MinValue))
                {
                    // Pas de date, uniquement une heure
                    SepDate = Char.MinValue;
                    SepDateHeure = char.MinValue;
                    SepHeure = '?'; // A déterminer
                }
                else if (debutmc["hh"].Equals(Int32.MinValue))
                {
                    // Pas d'heure
                    SepHeure = Char.MinValue;
                    SepDateHeure = char.MinValue;
                    SepDate = '?'; // A déterminer                    
                }
                else if (debutmc["hm"].Equals(Int32.MinValue))
                {
                    // Seulement un heure
                    SepHeure = Char.MinValue;
                    SepDateHeure = '?'; // A déterminer
                    SepDate = '?'; // A déterminer                    
                }

                // Détermination du séparateur de date
                if (SepDate.Equals('?'))
                {
                    if (formatchamp.Substring(debutd).StartsWith("da["))
                    {
                        // Si la date commence par da[x], le séparateur de date se trouve forcément 5 caractères plus loin. 
                        if (formatchamp[debutd + 5].Equals('d'))
                            SepDate = Char.MinValue;
                        else
                            SepDate = formatchamp[debutd + 5];
                    }
                    else
                    {
                        // Si la date ne commence pas par da[x], le séparateur de date se trouve forcément 2 caractères plus loin
                        if (formatchamp[debutd + 2].Equals('d'))
                            SepDate = Char.MinValue;
                        else
                            SepDate = formatchamp[debutd + 2];
                    }
                }
                // Détermination du sépareur de l'heure
                if (SepHeure.Equals('?'))
                {
                    if (formatchamp[debuth + 2].Equals('h'))
                        SepDate = Char.MinValue;
                    else
                        SepDate = formatchamp[debutd + 2];
                }
                // Détermination du séparateur entre la date et l'heure
                if (SepDateHeure.Equals('?'))
                {
                    if (find < debuth)
                    {
                        if ((find + 1).Equals(debuth))
                            SepDateHeure = Char.MinValue;
                        else
                            SepDateHeure = formatchamp[find + 1];
                    }
                    else
                    {
                        if ((finh + 1).Equals(debutd))
                            SepDateHeure = Char.MinValue;
                        else
                            SepDateHeure = formatchamp[finh + 1];
                    }
                }
            }
        }

        /// <summary>
        /// Le type de donnée le champ mémo
        /// </summary>
        private class Memo : TypeDonnee
        {
            /// <summary>
            /// Création dans le cas de format="m" (champ à longueur variable)
            /// </summary>
            /// <param name="formatchamp">Format du champ</param>
            public Memo(string formatchamp)
                : base()
            {
            }
            /// <summary>
            /// Donne la valeur du champ
            /// </summary>
            /// <param name="fromvalue">La chaine de caractère contenant la valeur</param>
            /// <returns>
            /// Renvoie une chaine de caractère multiligne contenant éventuellements de caractères de contrôle.
            /// </returns>
            internal override object GetValue(string fromvalue)
            {
                return fromvalue;
            }
            /// <summary>
            /// Convetit la valeur du champ en chaine de caractère en tenant compte type de donnée.
            /// </summary>
            /// <param name="value">Valeur à convertir</param>
            /// <returns>Chaine de caractère obtenu</returns>
            internal override string SetValue(object value)
            {
                if (value != null && value != System.DBNull.Value) return value.ToString();
                else return "";
            }
        }

        /// <summary>
        /// Champ utilisé dans un segment de type fichier.
        /// </summary>
        private class ChampFichier
        {
            // Valeurs privées des propriétés
            private string formatchamp;
            private string nom;
            private TypeDonnee td;

            // La position dans la description si champ délimité (et non à longueut fixe)
            internal int Position;

            // Le format.
            public string FormatChamp
            {
                get { return formatchamp; }
                protected set { formatchamp = value; }
            }
            // Le Nom
            public string Nom
            {
                get { return nom; }
                protected set { nom = value; }
            }
            // Le type de données
            public TypeDonnee TD
            {
                get { return td; }
                protected set { td = value; }
            }

            /// <summary>
            /// Création d'un champ à longueur variable (cas d'un fichier avec séparteur de champ)
            /// </summary>
            /// <param name="formatchamp">Le format du champ</param>
            /// <param name="nom">Le nom du champ</param>
            internal ChampFichier(string formatchamp, string nom)
            {
                // Test le format
                if (formatchamp.Equals(""))
                    throw new FileIOError(this.GetType().FullName, "Le format du chammp est obligatoire");
                if (nom.Equals(""))
                    throw new FileIOError(this.GetType().FullName, "Le nom du chammp est obligatoire");
                // Mémorisation des propriétés.
                Nom = nom;
                FormatChamp = formatchamp;
                // Création du type de données.
                if (formatchamp.StartsWith("a")) TD = new Alphanumerique(formatchamp);
                else if (formatchamp.StartsWith("9"))
                {
                    if (!formatchamp.Contains("0")) TD = new Entier(formatchamp);
                    else TD = new NombreDecimal(formatchamp);
                }
                else if (formatchamp.Contains("dj") || formatchamp.Contains("hh"))
                    TD = new DateHeure(formatchamp);
                else if (formatchamp.StartsWith("m"))
                    TD = new Memo(formatchamp);
                else
                    throw new FileIOError(this.GetType().FullName, "Le format'" + formatchamp + "' est inconnu");
                // La position dans la description doit être renseignée
                Position = -1;
            }
            /// <summary>
            /// Création d'un champ à longueur fixe (cas d'un fichier sans séparateur de champ)
            /// </summary>
            /// <param name="formatchamp">Le format du champ</param>
            /// <param name="nom">Le nom du champ</param>
            /// <param name="debut">Position du premier caractère dans l'enregistrement</param>
            internal ChampFichier(string formatchamp, string nom, int debut)
            {
                if (nom.Equals(""))
                    throw new FileIOError(this.GetType().FullName, "Le Nom du chammp est obligatoire");
                // Mémorisation des propriétés.
                Nom = nom;
                FormatChamp = formatchamp;
                // Création du type de données.
                if (formatchamp.StartsWith("a")) TD = new Alphanumerique(formatchamp, debut);
                else if (formatchamp.StartsWith("9"))
                {
                    if (!formatchamp.Contains("0")) TD = new Entier(formatchamp, debut);
                    else TD = new NombreDecimal(formatchamp, debut);
                }
                else if (formatchamp.Contains("dj") || formatchamp.Contains("hh"))
                    TD = new DateHeure(formatchamp, debut);
                else if (formatchamp.StartsWith("m"))
                    throw new FileIOError(this.GetType().FullName, "Le format'" + formatchamp + "' est forcément de taille variable");
                else
                    throw new FileIOError(this.GetType().FullName, "Le format'" + formatchamp + "' est inconnu");
                // La position dans la description n'est pas utilisée dans le cas des champs fixe.
                Position = -1;
            }
            /// <summary>
            /// Revoie la valeur d'un champ en appliquant la mise en forme propre au type de données du champ.
            /// </summary>
            /// <param name="enreg">L'enregistrement en cours</param>
            /// <returns>La valeur mise en forme</returns>
            public object GetValue(TextFileIO._ENREG enreg)
            {
                // Si champ à longueur variable
                if (Position != -1)
                {
                    // Les champs ne sont pas forcément tous présents sur l'enregistrement
                    if (Position < enreg.Champs.Length)
                    {
                        try
                        {
                            return td.GetValue(enreg.Champs[Position]);
                        }
                        catch (System.Exception eh)
                        {
                            string info = "";
                            for (int i = 0; i < enreg.Champs.Length; i++) info = info + "Champ[" + i.ToString() + "]: " + enreg.Champs[i] + "\r\n";
                            throw new FileIOError(this.GetType().FullName, "La valeur '" + enreg.Champs[Position] + "' pour le champ '" + Position.ToString() + "' est incorrecte. L'enregistrement en cours est :\r\n" + info, eh);
                        }
                    }
                    else
                        return null;
                }
                else
                {
                    try
                    {
                        return td.GetValue(enreg.Champs[0]);
                    }
                    catch (System.Exception eh)
                    {
                        throw new FileIOError(this.GetType().FullName, "La valeur '" + enreg.Champs[0] + "' est incorrecte.", eh);
                    }
                }
            }
            /// <summary>
            /// Met à jour un champ à partir de <paramref name="value"/> en appliquant la mise en forme propre au type de données du champ.
            /// </summary>
            /// <param name="value">La valeur avant mise en forme</param>
            /// <param name="enreg">L'enregistrement en cours</param>
            public void SetValue(object value, ref TextFileIO._ENREG enreg)
            {
                // Si champ à longueur variable
                if (Position != -1)
                    enreg.Champs[Position] = TD.SetValue(value);
                else
                {
                    if (enreg.Champs[0].Length < TD.Debut)
                    {
                        enreg.Champs[0] = enreg.Champs[0].PadRight(TD.Debut - enreg.Champs[0].Length, ' ');
                        enreg.Champs[0] = enreg.Champs[0] + TD.SetValue(value);
                    }
                    else
                    {
                        string tmpvalue = TD.Debut == 0 ? "" : enreg.Champs[0].Substring(0, TD.Debut);
                        tmpvalue = tmpvalue + TD.SetValue(value);
                        if (enreg.Champs[0].Length > (TD.Debut + TD.Longueur))
                            tmpvalue = tmpvalue + enreg.Champs[0].Substring(TD.Debut + TD.Longueur);
                    }
                }
            }
        }

        /// <summary>
        /// Collection de champs à utiliser dans le cas des segments de type fichier.
        /// </summary>
        private class ChampsFichier : System.Collections.Specialized.NameObjectCollectionBase, System.Collections.IEnumerator
        {
            // La position pour l'énumération
            int position;

            /// <summary>
            /// Création de la classe (casse insensible)
            /// </summary>
            internal ChampsFichier()
                : base(System.StringComparer.CurrentCultureIgnoreCase)
            {
            }

            /// <summary>
            /// Renvoie un champ par rapport à l'index
            /// </summary>
            public ChampFichier this[int index]
            {
                get
                {
                    if (index >= this.Count)
                        throw new FileIOError(this.GetType().FullName, "Index au-delà du nombre d'occurence");
                    if (index < 0)
                        throw new FileIOError(this.GetType().FullName, "L'index doit être supérieur à zéro");
                    return (ChampFichier)this.BaseGet(index);
                }
            }
            /// <summary>
            /// Renvoie un champ par rapport à son nom
            /// </summary>
            public ChampFichier this[string nom]
            {
                get
                {
                    foreach (string n in BaseGetAllKeys())
                    {
                        if (n.Equals(nom, StringComparison.CurrentCultureIgnoreCase))
                            return (ChampFichier)this.BaseGet(nom);
                    }
                    throw new FileIOError(this.GetType().FullName, "Le champ '" + nom + "' n'existe pas");
                }
            }

            /// <summary>
            /// Indique si un nom de champ existe dans la collection
            /// </summary>
            /// <param name="nom">Le nom de champ à rechercher</param>
            /// <returns>Vrai si trouvé, sinon faux</returns>
            public bool Contains(string nom)
            {
                foreach (string n in BaseGetAllKeys())
                {
                    if (n.Equals(nom, StringComparison.CurrentCultureIgnoreCase)) return true;
                }
                return false;
            }
            /// <summary>
            /// Indique si le champ appatient à cette collection de champs
            /// </summary>
            /// <param name="champ">Le champ à rechercher</param>
            /// <returns>Vrai si trouvé, sinon faux</returns>
            public bool Contains(ChampFichier champ)
            {
                foreach (ChampFichier cf in this)
                {
                    if (cf.Equals(champ)) return true;
                }
                return false;
            }
            /// <summary>
            /// Création d'un nouveau champ dans la collection.
            /// </summary>
            /// <param name="champfichier">Le champ à ajouter</param>
            public void Add(ChampFichier champfichier)
            {
                foreach (string n in BaseGetAllKeys())
                {
                    if (n.Equals(champfichier.Nom, StringComparison.CurrentCultureIgnoreCase))
                        throw new FileIOError(this.GetType().FullName, "Le champ '" + champfichier.Nom + "' existe dèja");
                }
                BaseAdd(champfichier.Nom, champfichier);
            }
            /// <summary>
            /// Renvoie l'énumérateur typé SegmentFichier
            /// </summary>
            /// <returns></returns>
            public override System.Collections.IEnumerator GetEnumerator()
            {
                Reset();
                return this;
            }

            /// <summary>
            /// Le segment en cours.
            /// </summary>
            public object Current
            {
                get
                {
                    return this[position];
                }
            }

            /// <summary>
            /// Déplacement dans la collection
            /// </summary>
            /// <returns>Vrai si déplacement possible</returns>
            public bool MoveNext()
            {
                position++;
                return (position < this.Count);
            }

            /// <summary>
            /// Initialise la position
            /// </summary>
            public void Reset()
            {
                position = -1;
            }
        }

        /// <summary>
        /// Collection des liens dans un fichier
        /// </summary>
        private class LiensFichier : List<LienFichier>
        {
            /// <summary>
            /// Retourne le lien correspondant à un segment
            /// </summary>
            /// <param name="segment">Le segment à chercher</param>
            /// <param name="racLien">Le lien racine en cours</param>
            /// <returns>le lien ou null si segment non trouvé</returns>
            public LienFichier getLien(SegmentFichier segment, LienFichier racLien)
            {
                // Recherche du lien correspondant au segment
                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i].Segment.Equals(segment))
                    {
                        // Si aucun lien racine en cours, on renvoie le premier lien trouvé
                        if (racLien == null) return this[i];
                        // Si lien racine, c'est forcément le bon
                        if (this[i].DependDe == null) return this[i];
                        // Il faut que lien trouvé dépende de la racine en cours
                        if (this[i].getLienRacine().Equals(racLien)) return this[i];
                    }
                }
                return null;            
            }
            /// <summary>
            /// Retourne les liens pour lesquels un segment est dépendant du segment passé en paramètre
            /// </summary>
            /// <param name="dependde">Le segment parent</param>
            /// <returns>Tableau contenant les liens</returns>
            public LienFichier[] getLiens(SegmentFichier dependde)
            {
                // Le tableau des liens
                LienFichier[] lfs = new LienFichier[] { };

                // Recherche des dépendances
                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i].DependDe != null && this[i].DependDe.Segment.Equals(dependde))
                    {
                        LienFichier[] cplfs = new LienFichier[lfs.Length + 1];
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
            /// <returns>Tableau contenant les liens</returns>
            /// <remarks>Si <paramref name="dependde"/> est null, le tableau contient les liens n'ayant pas de dépendances/></remarks>
            public LienFichier[] getLiens(LienFichier dependde)
            {
                // Le tableau des liens
                LienFichier[] lfs = new LienFichier[] { };
                // Recherche des dépendances
                for (int i = 0; i < this.Count; i++)
                {
                    if (dependde != null && this[i].DependDe != null && this[i].DependDe.Equals(dependde) ||
                        dependde == null && this[i].DependDe == null)
                    {
                        LienFichier[] cplfs = new LienFichier[lfs.Length + 1];
                        if (lfs.Length > 0) lfs.CopyTo(cplfs, 0);
                        cplfs[lfs.Length] = this[i];
                        lfs = cplfs;
                    }
                }
                // Revoie les liens dépendants.
                return lfs;
            }
        }
        /// <summary>
        /// Lien entre les différents segments au sein du fichier.
        /// </summary>
        private class LienFichier
        {
            // Valeurs privées des propriétés
            private int profondeur;
            private SegmentFichier segment;
            private LienFichier dependde;
            private bool facultatif;
            private bool unique;

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
            public SegmentFichier Segment
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
            public LienFichier DependDe
            {
                get { return dependde; }
                protected set { dependde = value; }
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
                protected set { unique = value; }
            }


            /// <summary>
            /// Création d'un lien parent
            /// </summary>
            /// <param name="segment">Le segment</param>
            /// <param name="facultatif">Segment facultatif</param>
            /// <param name="unique">Segment unique</param>
            public LienFichier(SegmentFichier segment, bool facultatif, bool unique)
                : this(segment, facultatif, unique, null)
            {
            }
            /// <summary>
            /// Création d'un lien parent ou enfant si <paramref name="dependde"/> est renseigné.
            /// </summary>
            /// <param name="segment">Le segment</param>
            /// <param name="facultatif">Segment facultatif</param>
            /// <param name="unique">Segment unique</param>
            /// <param name="dependde">Segment parent</param>
            public LienFichier(SegmentFichier segment, bool facultatif, bool unique, LienFichier dependde)
            {
                Segment = segment;
                Facultatif = facultatif;
                Unique = unique;
                DependDe = dependde;
                if (dependde == null)
                    Profondeur = 0;
                else
                {
                    TestRedondance(dependde, segment);
                    Profondeur = dependde.Profondeur + 1;
                }
            }

            /// <summary>
            /// Donne le lien racine pour un ce lien.
            /// </summary>
            /// <returns>Lui même ou son lien racine</returns>
            public LienFichier getLienRacine()
            {
                // On remonte l'arborescence
                LienFichier raclien = this;
                while (raclien.DependDe != null) raclien = raclien.DependDe;
                return raclien;
            }

            /// <summary>
            /// Test si le lien parent ne contient pas le segment fils.
            /// </summary>
            /// <param name="lienparent">Lien supérieur à tester</param>
            /// <param name="segment">Le segment sur lequel pour le test de redondance.</param>
            private void TestRedondance(LienFichier lienparent, SegmentFichier segment)
            {
                // Redondance dans les liens.
                if (lienparent.segment.Equals(segment))
                    throw new FileIOError(this.GetType().FullName, "Un segment ne peut pas dépendre de lui-même ('" + segment.Nom + "')");

                // Remonte l'arborescence
                if (lienparent.DependDe != null)
                    TestRedondance(lienparent.DependDe, segment);
            }
        }

        /// <summary>
        /// La classe contenant les segments
        /// </summary>
        private class SegmentsFichier : System.Collections.Specialized.NameObjectCollectionBase, System.Collections.IEnumerator
        {
            // La position pour l'énumération
            int position;

            /// <summary>
            /// Le constructeur avec par défaut, la comparaison se faisant sans tenir compte de la casse. 
            /// </summary>
            public SegmentsFichier()
                : base(System.StringComparer.CurrentCultureIgnoreCase)
            {
            }
            /// <summary>0
            /// Renvoie un segment par rapport à l'index
            /// </summary>
            public SegmentFichier this[int index]
            {
                get
                {
                    if (index >= this.Count)
                        throw new FileIOError(this.GetType().FullName, "Index au-delà du nombre d'occurence");
                    if (index < 0)
                        throw new FileIOError(this.GetType().FullName, "L'index doit être supérieur à zéro");
                    return (SegmentFichier)this.BaseGet(index);
                }
            }
            /// <summary>
            /// Renvoie un segment par rapport à son nom
            /// </summary>
            /// <param name="nom">Le nom du segmnt</param>
            public SegmentFichier this[string nom]
            {
                get
                {
                    foreach (string n in BaseGetAllKeys())
                    {
                        if (n.Equals(nom, StringComparison.CurrentCultureIgnoreCase))
                            return (SegmentFichier)this.BaseGet(nom);
                    }
                    throw new FileIOError(this.GetType().FullName, "Le segment '" + nom + "' n'existe pas");
                }
            }

            /// <summary>
            /// Indique si un nom de segment existe dans la collection
            /// </summary>
            /// <param name="nom">le nom à rechercher</param>
            /// <returns>Vrai si trouvé, sinon faux</returns>
            public bool Contains(string nom)
            {
                foreach (string n in BaseGetAllKeys())
                {
                    if (n.Equals(nom, StringComparison.CurrentCultureIgnoreCase)) return true;
                }
                return false;
            }

            /// <summary>
            /// Création d'un nouveau segment dans la collection.
            /// </summary>
            /// <param name="segment">Le segment à ajouter</param>
            public void Add(SegmentFichier segment)
            {
                foreach (string n in BaseGetAllKeys())
                {
                    if (n.Equals(segment.Nom, StringComparison.CurrentCultureIgnoreCase))
                        throw new FileIOError(this.GetType().FullName, "Le segment '" + segment.Nom + "' existe dèja");
                }
                BaseAdd(segment.Nom, segment);
            }

            /// <summary>
            /// Recherche du segment commencant par uniquepar
            /// </summary>
            /// <param name="uniquepar">La chaine de caractère à recherche</param>
            /// <returns>Le segment ou null si non trouvé</returns>
            public SegmentFichier FindSegment(string uniquepar)
            {
                if (this.Count > 1)
                {
                    // Si plusieurs segment, lancement de la recherche
                    foreach (SegmentFichier sf in this)
                    {
                        if (sf.ChampLongueurFixe)
                        {
                            if (uniquepar.StartsWith(sf.UniquePar, StringComparison.CurrentCultureIgnoreCase)) return sf;
                        }
                        else
                        {
                            if (uniquepar.Equals(sf.UniquePar, StringComparison.CurrentCultureIgnoreCase)) return sf;
                        }
                    }
                }
                else
                {
                    // Si aucun uniquepar, on renvoie forcément le segment
                    if (this[0].UniquePar.Equals("")) return this[0];
                    if (uniquepar.StartsWith(this[0].UniquePar, StringComparison.CurrentCultureIgnoreCase)) return this[0];
                }

                // Segment non trouvé
                return null;
            }

            /// <summary>
            /// Renvoie l'énumérateur typé SegmentFichier
            /// </summary>
            /// <returns></returns>
            public override System.Collections.IEnumerator GetEnumerator()
            {
                Reset();
                return this;
            }

            /// <summary>
            /// Le segment en cours.
            /// </summary>
            public object Current
            {
                get
                {
                    return this[position];
                }
            }

            /// <summary>
            /// Déplacement dans la collection
            /// </summary>
            /// <returns>Vrai si déplacement possible</returns>
            public bool MoveNext()
            {
                position++;
                return (position < this.Count);
            }

            /// <summary>
            /// Initialise la position
            /// </summary>
            public void Reset()
            {
                position = -1;
            }
        }

        /// <summary>
        /// Segment décrivant le contenu des données pour un fichier.
        /// </summary>
        private class SegmentFichier
        {
            // Valeurs privées des propriétés
            private string nom;
            private string uniquepar;
            private bool champlongueurfixe;
            private ChampsFichier champs;
            private bool champmemo;
            private TextFileIO._ENREG enregchampmemo;

            /// <summary>
            /// Nom du segment
            /// </summary>
            public string Nom
            {
                get { return nom; }
                protected set
                {
                    if (value.Equals(""))
                        throw new FileIOError(this.GetType().FullName, "Le Nom du segment est obligatoire");
                    nom = value;
                }
            }
            /// <summary>
            /// Le ou les premiers caractères identifiant de façon unique un segment
            /// </summary>
            public string UniquePar
            {
                get { return uniquepar; }
                protected set { uniquepar = value; }
            }
            /// <summary>
            /// Indique si le segment est composé de champ à longueur fixe.
            /// </summary>
            public bool ChampLongueurFixe
            {
                get { return champlongueurfixe; }
                protected set { champlongueurfixe = value; }
            }
            /// <summary>
            /// Liste des champs contenus dans le segment.
            /// </summary>
            public ChampsFichier Champs
            {
                get { return champs; }
                protected set { champs = value; }
            }

            /// <summary>
            /// Indique si le segment supporte un ou plusieurs champs mémo
            /// </summary>
            public bool ChampMemo
            {
                get {return champmemo;}
            }

            /// <summary>
            /// Stock l'enregistrement avant de le traiter dans le cas ou un champ mémo est présent dans la description
            /// </summary>
            public TextFileIO._ENREG EnregChampMemo
            {
                get {return enregchampmemo;}
                set { enregchampmemo = value; }
            }

            /// <summary>
            /// Création d'un segment
            /// </summary>
            /// <param name="nom">Nom du segment</param>
            /// <param name="champlongueurfixe">Précise si le segment contient des champs à longueur fixe</param>
            public SegmentFichier(string nom, bool champlongueurfixe)
            {
                Nom = nom;
                ChampLongueurFixe = champlongueurfixe;
                UniquePar = "";
                champs = new ChampsFichier();
                champmemo = false;
                enregchampmemo = new TextFileIO._ENREG(new string[] { });
            }
            /// <summary>
            /// Création d'un segment avec identification unique
            /// </summary>
            /// <param name="nom">Nom du champ</param>
            /// <param name="champlongueurfixe">Précise si le segment contient des champs à longueur fixe</param>
            /// <param name="uniquepar">Premier(s) caractère(s) identifiant de façon unique un segment</param>
            public SegmentFichier(string nom, bool champlongueurfixe, string uniquepar)
                : this(nom, champlongueurfixe)
            {
                if (uniquepar.Equals(""))
                    throw new FileIOError(this.GetType().FullName, "La valeur identifiant de façon unique un segment doit être renseignée");
                UniquePar = uniquepar;
            }

            /// <summary>
            /// Ajoute un champ au segment
            /// </summary>
            /// <param name="formatchamp">Format du champ</param>
            /// <param name="nom">Nom du champ</param>
            public void AddChamp(string formatchamp, string nom)
            {
                if (!ChampLongueurFixe)
                {
                    ChampFichier cf = new ChampFichier(formatchamp, nom);
                    cf.Position = Champs.Count;
                    champs.Add(cf);
                    if (cf.TD is Convert.Memo) this.champmemo = true; 
                }
                else
                {
                    int debut = 0;
                    if (champs.Count > 0) debut = champs[champs.Count - 1].TD.Debut + champs[champs.Count - 1].TD.Longueur;
                    champs.Add(new ChampFichier(formatchamp, nom, debut));
                }
            }

            /// <summary>
            /// Création d'un enregistrement en fonction du segment fichier
            /// </summary>
            /// <returns>Le nouvel enregistrement</returns>
            public TextFileIO._ENREG NewEnreg()
            {
                if (ChampLongueurFixe)
                    return new TextFileIO._ENREG(new string[1]);
                else
                    return new TextFileIO._ENREG(new string[Champs.Count]);
            }
        }

        /// <summary>
        /// Classe contenant toutes les informations concernant le fichier
        /// </summary>
        private class Fichier
        {
            // Valeurs privées des propriétés
            private string nom;
            private System.Text.Encoding codage;
            private string sepenr;
            private char sepchamp;
            private char delchamp;
            private SegmentsFichier segments;
            private LiensFichier liens;

            /// <summary>
            /// Nom du fichier (chemin éventuellement inlus)
            /// </summary>
            public string Nom
            {
                get { return nom; }
                protected set { nom = value; }
            }
            /// <summary>
            /// Codage des caractères dans le fichier
            /// </summary>
            public System.Text.Encoding Codage
            {
                get { return codage; }
                protected set { codage = value; }
            }
            /// <summary>
            /// Le séparateur d'enregistrement
            /// </summary>
            public string SepEnr
            {
                get { return sepenr; }
                protected set { sepenr = value; }
            }
            /// <summary>
            /// Le séparateur de champ
            /// </summary>
            public char SepChamp
            {
                get { return sepchamp; }
                protected set { sepchamp = value; }
            }
            /// <summary>
            /// Le délimiteur de champ.
            /// </summary>
            public char DelChamp
            {
                get { return delchamp; }
                protected set { delchamp = value; }
            }
            /// <summary>
            /// Le(s) segment(s) contenu(s) dans le fichier.
            /// </summary>
            public SegmentsFichier Segments
            {
                get { return segments; }
                protected set { segments = value; }
            }
            /// <summary>
            /// Le(s) lien(s) reliant les différents segments contenus dans le fichier.
            /// </summary>
            public LiensFichier Liens
            {
                get { return liens; }
                protected set { liens = value; }
            }

            /// <summary>
            /// Classe gérant le fichier
            /// </summary>
            /// <param name="xn">Le noeud <![CDATA[<fichier>]]> du fichier XML</param>
            public Fichier(System.Xml.XmlNode xn)
            {
                // Initialisation des valeurs par défaut
                Nom = "";
                Codage = System.Text.Encoding.Default;
                SepEnr = "";
                SepChamp = Char.MinValue;
                DelChamp = Char.MinValue;
                Segments = new SegmentsFichier();
                Liens = new LiensFichier();

                // Chargement des attributs liés à la balise <fichier>
                CreIFI(xn);
                // Mise en place du séparateur d'enregistrement
                CreSepEnr();
                // Traitement des différents noeuds dépendant de fichier.
                foreach (System.Xml.XmlNode noeud in xn.ChildNodes)
                {
                    // Traitement des segments.
                    if (noeud.Name.ToLower().Equals("segment")) CreSEG(noeud);
                    // Traitement des liens.
                    if (noeud.Name.ToLower().Equals("liens")) CreLIENS(noeud);
                }

                // Il faut au minimum un segment
                if (Segments.Count == 0)
                    throw new FileIOError(this.GetType().FullName, "Il n'y a aucun segment dans <fichier>");
                // Il faut qu'au minimum UN segment possède au moins UN champ sinon traitement inutile.
                bool ok = false;
                foreach (SegmentFichier seg in Segments)
                {
                    if (ok = (seg.Champs.Count > 0)) break;
                }
                if (!ok)
                    throw new FileIOError(this.GetType().FullName, "Il n'y a aucun champ de définit dans <fichier>");

                // Si plusieurs segments ...
                if (Segments.Count > 1)
                {
                    // ... UniquePar est obligatoire 
                    string msgerr = "";
                    foreach (SegmentFichier seg in Segments)
                    {
                        if (seg.UniquePar.Equals(""))
                        {
                            if (msgerr.Equals("")) msgerr = msgerr + ", ";
                            msgerr = msgerr + "'" + seg.Nom + "'";
                        }
                    }
                    if (!msgerr.Equals(""))
                        throw new FileIOError(this.GetType().FullName, "Le(s) segment(s) " + msgerr + " ne comporte(nt) pas d'attribut 'uniquepar' permettant de les identifier");
                    // ... UniquePar doit être unique
                    System.Collections.Generic.List<string> tup = new List<string>();
                    foreach (SegmentFichier seg in Segments)
                    {
                        if (!tup.Contains(seg.UniquePar))
                            tup.Add(seg.UniquePar);
                        else
                        {
                            if (msgerr.Equals("")) msgerr = msgerr + ", ";
                            msgerr = msgerr + "'" + seg.UniquePar + "'";
                        }
                    }
                    if (!msgerr.Equals(""))
                        throw new FileIOError(this.GetType().FullName, "Plusieurs segments utilisent " + msgerr + " comme valeur d'attribut 'uniquepar'");

                    // ... il faut obligatoirement des liens.
                    if (Liens.Count == 0)
                        throw new FileIOError(this.GetType().FullName, "Il faut indiquer les liens entre les différents segments");

                    // ... il ne peut y avoir qu'un seul lien obligatoire au niveau 0.
                    int i = 0;
                    foreach (LienFichier lf in Liens)
                    {
                        if (lf.Profondeur == 0 && !lf.Facultatif) i++;
                    }
                    if (i > 1)
                        throw new FileIOError(this.GetType().FullName, "Il est impossible d'avoir plusieurs segments de base obligatoire.");
                }
                // Si un seul segment, on vérifie qu'aucun champ mémo n'est positionné en dernier champ
                else
                {
                    foreach (ChampFichier fp in Segments[0].Champs)
                    {
                        if (fp.TD is Convert.Memo && fp.Position == (segments[0].Champs.Count - 1))
                        {
                            throw new FileIOError(this.GetType().FullName, "Un champ mémo ne peut pas être le dernier champ d'une description.");
                        }
                    }
                }
            }

            /// <summary>
            /// Mise en place des informations.
            /// </summary>
            /// <param name="xn">le noeud fichier à traiter</param>
            private void CreIFI(System.Xml.XmlNode xn)
            {
                // Chargement des informations
                foreach (System.Xml.XmlAttribute xa in xn.Attributes)
                {
                    if (xa.Name.ToLower().Equals("nom")) // Nom du fichier
                    {
                        // Mémorisation du nom
                        Nom = xa.Value.Trim();
                        if (!Nom.Equals(""))
                        {
                            // Test si le répertoire existe dans le cas où il est précisé dans le nom.
                            string dir = Path.GetDirectoryName(Nom);
                            if (!dir.Equals("") && !Directory.Exists(dir))
                                throw new FileIOError(this.GetType().FullName, "Le répertoire '" + dir + "' n'existe pas");
                            // Le nom ne doit pas correspondre à un répertoire
                            if (Directory.Exists(Nom))
                                throw new FileIOError(this.GetType().FullName, "Le nom '" + Nom + "' ne doit pas être un répertoire");
                        }
                    }
                    else if (xa.Name.ToLower().Equals("codage")) // Codage
                    {
                        switch (xa.Value.ToUpper())
                        {
                            case "DEFAULT":
                                Codage = System.Text.Encoding.Default;
                                break;
                            case "ASCII":
                                Codage = System.Text.Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
                                break;
                            case "UTF7":
                                Codage = System.Text.Encoding.UTF7;
                                break;
                            case "UTF8":
                                Codage = System.Text.Encoding.UTF8;
                                break;
                            case "UTF16":
                                Codage = System.Text.Encoding.Unicode;
                                break;
                            case "UTF32":
                                Codage = System.Text.Encoding.UTF32;
                                break;
                        }
                    }
                    else if (xa.Name.ToLower().Equals("sepenr")) // sépérateur d'enregistrement
                    {
                        if (!xa.Value.Equals("")) SepEnr = xa.Value;
                    }
                    else if (xa.Name.ToLower().Equals("sepchamp")) // Sépérateur de champ
                    {
                        if (!xa.Value.Equals("")) SepChamp = xa.Value[0]; // Un seul caractère
                    }
                    else if (xa.Name.ToLower().Equals("delchamp")) // Délimitateur de champ
                    {
                        if (!SepChamp.Equals(Char.MinValue) && !xa.Value.Equals(""))
                            DelChamp = xa.Value[0]; // Un seul caractère, uniquement si le démitateur de champ est renseigné
                    }
                }
            }

            /// <summary>
            /// Mise en place du séparateur d'enregistrement
            /// </summary>
            private void CreSepEnr()
            {
                // Cette information est obligatoire
                if (SepEnr.Length == 0)
                    throw new FileIOError(this.GetType().FullName, "Le séparateur d'enregistrement est obligatoire");
                // Si mode auto, le séparateur de champ sera vérifié à chaque ouverture du fichier
                if (!SepEnr.Equals("auto", StringComparison.CurrentCultureIgnoreCase))
                {
                    // le "\n" donne "\\n" à la récupération de la valeur
                    SepEnr = SepEnr.Replace("\\n", "\n");
                    SepEnr = SepEnr.Replace("\\r", "\r");                    
                }
            }

            /// <summary>
            /// Ajoute un segment dans la collection Segments de la classe Fichier.
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
                    throw new FileIOError(this.GetType().FullName, "Un <segment> dans <fichier> est sans attribut 'nom'.");

                // Est-ce que ce segment existe déja
                if (Segments.Contains(nomsegment))
                    throw new FileIOError(this.GetType().FullName, "Le <segment> '" + nomsegment + "' est présent plusieurs fois dans <fichier>.");

                // Création du segment
                SegmentFichier seg;
                if (uniquepar.Equals(""))
                    seg = new SegmentFichier(nomsegment, SepChamp.Equals(Char.MinValue));
                else
                    seg = new SegmentFichier(nomsegment, SepChamp.Equals(Char.MinValue), uniquepar);

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
                        seg.AddChamp(formatchamp, nomchamp);
                    }
                }

                // Ajoute le segment créé segment créé
                Segments.Add(seg);
            }

            /// <summary>
            /// Création des liens unissant les différent segments
            /// </summary>
            /// <param name="xn">Le noued XML <![CDATA[<liens>]]> à traiter.</param>
            private void CreLIENS(System.Xml.XmlNode xn)
            {
                // Ce noeud ne doit être présent qu'une fois dans <fichier>
                if (Liens.Count > 0)
                    throw new FileIOError(this.GetType().FullName, "Les liens unissant les segments on déja été définis");

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
                    LienFichier lienparent = null;
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
                    bool facultatif = false;
                    bool unique = false;
                    int ins = sc.IndexOf('.', profondeur + 1);
                    if (ins == -1)
                        nomsegment = sc.Substring(profondeur + 1).Trim(); // Pas d'option après le nom du segment.
                    else
                    {
                        // Nom du segment
                        nomsegment = sc.Substring(profondeur + 1, (ins - profondeur - 1)).Trim();
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
                    // Est-ce que le segment existe.
                    if (!Segments.Contains(nomsegment))
                        throw new FileIOError(this.GetType().FullName, "Le segment '" + nomsegment + "' n'existe pas.");
                    // Création du lien
                    if (profondeur == 0)
                    {
                        // Il n'est pas possible d'avoir plusieurs fois le même segment au niveau racine.
                        foreach (LienFichier lf in Liens)
                        {
                            if (lf.Profondeur == 0 && lf.Segment.Nom.Equals(nomsegment))
                                throw new FileIOError(this.GetType().FullName, "Il n'est pas possible d'avoir plusieurs fois le même segment ('" + nomsegment + "') au niveau racine.");
                        }
                        Liens.Add(new LienFichier(Segments[nomsegment], facultatif, unique));
                    }
                    else
                    {
                        // Il n'est pas possible d'avoir plusieurs fois le même segment dépendant d'un même lien.
                        foreach (LienFichier lf in Liens)
                        {
                            if (lf.Profondeur > 0 && lf.DependDe.Equals(lienparent) && lf.Segment.Nom.Equals(nomsegment))
                                throw new FileIOError(this.GetType().FullName, "Il n'est pas possible d'avoir plusieurs fois le même sous-segment ('" + nomsegment + "') au niveau du segment '" + lienparent.Segment.Nom + "'.");
                        }
                        Liens.Add(new LienFichier(Segments[nomsegment], facultatif, unique, lienparent));
                    }
                }
            }
        }
    }
}
