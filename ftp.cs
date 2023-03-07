using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace llt.FileIO
{

    /// <summary>
    /// Cette classe permet un accès simplifé à un serveur FTP pour envoyer ou recevoir
    /// un ou plusieurs fichiers.
    /// </summary>
    public class BasicFTP
    {
        /// <summary>
        /// Liste des options pour la suppression du fichier source
        /// </summary>
        public enum _DELFICHIERSOURCE : short
        {
            /// <summary>
            /// Le fichier source n'est jamais supprimé
            /// </summary>
            non,
            /// <summary>
            /// Le fichier source est supprimé UNIQUEMENT s'il est copié sur la destination
            /// </summary>
            /// <remarks>
            /// La suppression n'aura pas lieu si le fichier source existe sur la destination
            /// et que OWRFichierCopie est à faux
            /// </remarks>
            sicopie,
            /// <summary>
            /// Le fichier source est supprimé s'il est copié sur la destination ou s'il
            /// existe sur la destination.
            /// </summary>
            sicopieouexiste
        }

        /// <summary>
        /// Liste des options pour le remplacement du fichier destination
        /// </summary>
        public enum _OWRFICHIERDESTINATION : short
        {
            /// <summary>
            /// Le fichier destination n'est jamais remplacé
            /// </summary>
            non,
            /// <summary>
            /// Le fichier destination est systématiquement remplacé
            /// </summary>
            oui,
            /// <summary>
            /// Le fichier destination est remplacé si le fichier source est plus récent
            /// </summary>
            siplusrecent
        }
        /// <summary>
        /// Nom du serveur ou adresse IP
        /// </summary>
        public string Serveur;
        /// <summary>
        /// Le login
        /// </summary>
        public string Utilisateur;
        /// <summary>
        /// Le mot de passe
        /// </summary>
        public string MotdePasse;
        /// <summary>
        /// Le répertoire de travail sur le serveur FTP
        /// </summary>
        public string ServeurPath;
        /// <summary>
        /// Indique si on peut conserver la même connexion pour plusieurs commandes.
        /// </summary>
        private bool SupportKeepAlive;
        /// <summary>
        /// Le répertoir de travail en local
        /// </summary>
        public string LocalPath;
        /// <summary>
        /// Règle de remplacement du fichier destination lors de la copie si il existe
        /// </summary>
        public _OWRFICHIERDESTINATION OWRFichierDestination;
        /// <summary>
        /// Régle de suppression du fichier source
        /// </summary>
        public _DELFICHIERSOURCE DELFichierSource;

        /// <summary>
        /// Nom de fichier utiliser pour l'opération de lecture asynchrone.
        /// </summary>
        //private string fichierasync;
        private System.IO.Stream streamasync;

        /// <summary>
        /// Création de la classe en précisant uniquement le serveur FTP
        /// </summary>
        /// <param name="serveur">nom du serveur ou adresse IP</param>
        /// <param name="utilisateur">Le code utilisateur ou login</param>
        /// <param name="motdepasse">Le mot de passe</param>
        /// <param name="supportkeepalive">
        /// Certain serveur FTP ne supporte pas le KeepAlive qui permet entre chaque
        /// commande de conserver la même connexion. Dans ce cas, il faut que la valeur
        /// soit à Faux sinon le traitement va s'arrêter sur une erreur FTP 502 - Command not implemented
        /// </param>
        /// <remarks>
        /// Le répertoire sur serveur est celui par défaut au moment de la connexion.
        /// Le répertoire en local est le répertoire de travail au moment de l'exécution.
        /// Pas de remplacement d'un fichier existant sur la destination. 
        /// Pas de suppression du fichier source.
        /// </remarks>
        public BasicFTP(string serveur, string utilisateur, string motdepasse,
            bool supportkeepalive)
        {
            Serveur = serveur;
            Utilisateur = utilisateur;
            MotdePasse = motdepasse;
            ServeurPath = "";
            SupportKeepAlive = supportkeepalive;
            LocalPath = ".\\";
            OWRFichierDestination = _OWRFICHIERDESTINATION.non;
            DELFichierSource = _DELFICHIERSOURCE.non;
        }

        /// <summary>
        /// Création de la classe en précisant le serveur FTP et les répertoires de travail
        /// </summary>
        /// <param name="serveur">nom du serveur ou adresse IP</param>
        /// <param name="utilisateur">Le code utilisateur ou login</param>
        /// <param name="motdepasse">Le mot de passe</param>
        /// <param name="supportkeepalive">
        /// Certain serveur FTP ne supporte pas le KeepAlive qui permet entre chaque
        /// commande de conserver la même connexion. Dans ce cas, il faut que la valeur
        /// soit à Faux sinon le traitement va s'arrêter sur une erreur FTP 502 - Command not implemented
        /// </param>
        /// <param name="serveurpath">Le répertoire de travail sur le serveur FTP</param>
        /// <param name="localpath">Le répertoire de travail en local</param>
        /// <remarks>
        /// Pas de remplacement d'un fichier existant sur la destination. 
        /// Pas de suppression du fichier source.
        /// </remarks>
        public BasicFTP(string serveur, string utilisateur, string motdepasse,
            bool supportkeepalive, string serveurpath, string localpath)
            : this(serveur, utilisateur, motdepasse, supportkeepalive)
        {
            ServeurPath = serveurpath;
            LocalPath = localpath;
        }
        /// <summary>
        /// Création de la classe tous les paramètres de traitement
        /// </summary>
        /// <param name="serveur">nom du serveur ou adresse IP</param>
        /// <param name="utilisateur">Le code utilisateur ou login</param>
        /// <param name="motdepasse">Le mot de passe</param>
        /// <param name="supportkeepalive">
        /// Certain serveur FTP ne supporte pas le KeepAlive qui permet entre chaque
        /// commande de conserver la même connexion. Dans ce cas, il faut que la valeur
        /// soit à Faux sinon le traitement va s'arrêter sur une erreur FTP 502 - Command not implemented
        /// </param>
        /// <param name="serveurpath">Le répertoire de travail sur le serveur FTP</param>
        /// <param name="localpath">Le répertoire de travail en local</param>
        /// <param name="owrfichierdestination">Régle de remplacement du fichier destination si il existe</param>
        /// <param name="delfichiersource">Régle de suppression du fichier source</param>
        public BasicFTP(string serveur, string utilisateur, string motdepasse,
            bool supportkeepalive, string serveurpath, string localpath, _OWRFICHIERDESTINATION owrfichierdestination, _DELFICHIERSOURCE delfichiersource)
            : this(serveur, utilisateur, motdepasse, supportkeepalive, serveurpath, localpath)
        {
            OWRFichierDestination = owrfichierdestination;
            DELFichierSource = delfichiersource;
        }

        /// <summary>
        /// Copie UN fichier
        /// </summary>
        /// <param name="localTOserveur">Si vrai, le fichier est envoyé au serveur FTP. Si faux, le fichier est téléchargé du serveur FTP</param>
        /// <param name="fichier">Le nom de fichier (ATTENTION SANS information de répertoire)</param>
        /// <returns>Vrai traitement terminé correctement</returns>
        public bool CopyFile(bool localTOserveur, string fichier)
        {
            try
            {
                // Si fichier source non trouvé, pas de traitement
                if (!ExistFile(localTOserveur, fichier)) return false;

                // Test si le fichier destination existe.
                if (ExistFile(!localTOserveur, fichier))
                {
                    // Si on n'écrase pas le fichier destination
                    if (OWRFichierDestination.Equals(_OWRFICHIERDESTINATION.non))
                    {
                        // Si on supprime le fichier source s' il existe sur la destination,
                        // on effectue la suppression et considère que la copie est faite.
                        if (DELFichierSource.Equals(_DELFICHIERSOURCE.sicopieouexiste))
                        {
                            DelFile(localTOserveur, fichier);
                            return true;
                        }
                        else
                            return false;
                    }
                    // Suppression du fichier sur la destination systématique
                    else if (OWRFichierDestination.Equals(_OWRFICHIERDESTINATION.oui))
                    {
                        DelFile(!localTOserveur, fichier);
                    }
                    // Suppression du fichier destination uniquement si le fichier source est plus récent.
                    else
                    {
                        // Recherche de la date du fichier sur le serveur FTP
                        // L'objet permettant l'accès au serveur
                        System.Net.FtpWebRequest fwr = CreFwr(fichier);
                        fwr.Method = System.Net.WebRequestMethods.Ftp.GetDateTimestamp;
                        // Exécute la requête.
                        System.Net.FtpWebResponse fwp = (System.Net.FtpWebResponse)fwr.GetResponse();

                        // Si le fichier source est plus récent que le fichier destination, on supprime ce dernier
                        if (localTOserveur && System.IO.File.GetLastWriteTime(LocalPath + System.IO.Path.DirectorySeparatorChar + fichier) > fwp.LastModified ||
                            !localTOserveur && System.IO.File.GetLastWriteTime(LocalPath + System.IO.Path.DirectorySeparatorChar + fichier) < fwp.LastModified)
                        {
                            DelFile(!localTOserveur, fichier);
                        }
                        else
                        {
                            // Si on supprime le fichier source s' il existe sur la destination,
                            // on effectue la suppression et considère que la copie est faite.
                            if (DELFichierSource.Equals(_DELFICHIERSOURCE.sicopieouexiste))
                            {
                                DelFile(localTOserveur, fichier);
                                return true;
                            }
                            else
                                return false;
                        }
                    }
                }

                // Exécution de la copie
                ExeCopyFile(localTOserveur, fichier);

                // En final, suppression du fichier source si nécessaire.
                if (DELFichierSource.Equals(_DELFICHIERSOURCE.sicopie) ||
                    DELFichierSource.Equals(_DELFICHIERSOURCE.sicopieouexiste))
                {
                    DelFile(localTOserveur, fichier);
                }

                // Traitement terminé.
                return true;
            }
            catch (FileIOError)
            {
                throw;
            }
            catch (System.Exception eh)
            {
                // Création d'une erreur de type FileIOError
                throw new FileIOError("CopyFile_BasicFTP", "Impossible de copier le fichier '" + fichier + "'", eh);
            }
        }

        /// <summary>
        /// Exécution de la copie
        /// </summary>
        /// <param name="localTOserveur">Si vrai, le fichier est envoyé au serveur FTP. Si faux, le fichier est téléchargé du serveur FTP</param>
        /// <param name="fichier">Le nom de fichier (ATTENTION SANS information de répertoire)</param>
        private void ExeCopyFile(bool localTOserveur, string fichier)
        {
            // Les objets permettant l'accès au serveur
            System.Net.FtpWebRequest fwr = null;
            System.Net.FtpWebResponse fwp = null;
            streamasync = null;

            // L'objet permettant l'accès au fichier.
            BasicFileIO bfio = null;

            try
            {

                if (localTOserveur)
                {
                    // Du fait du traitement asynchrone, il faut utiliser une
                    // variable avec une portée générale.
                    //fichierasync = fichier;
                    // L'objet permettant l'accès au serveur
                    fwr = CreFwr(fichier);
                    fwr.Method = System.Net.WebRequestMethods.Ftp.UploadFile;
                    // Demande du stream d'écriture
                    streamasync = fwr.GetRequestStream();

                    // Lecture de tout le fichier
                    bfio = new BasicFileIO(LocalPath + System.IO.Path.DirectorySeparatorChar + fichier, true);
                    bfio.BasicFileIOEvent += new BasicFileIOEventHandler(bfiocopyfile_BasicFileIOEvent);
                    bfio.ReadFileSeq(1048576);

                    // Une fois terminé, on ferme le stream
                    streamasync.Close();
                }
                else
                {
                    // Création de l'URI avec l'UriBuilder

                    // L'objet permettant l'accès au serveur
                    fwr = CreFwr(fichier);
                    fwr.Method = System.Net.WebRequestMethods.Ftp.DownloadFile;

                    // Exécute la requête.
                    fwp = (System.Net.FtpWebResponse)fwr.GetResponse();
                    // Fichier sur lequel on écrit
                    bfio = new BasicFileIO(LocalPath + System.IO.Path.DirectorySeparatorChar + fichier, true, true);
                    // Lancement de la récupération des données.
                    for (; ; )
                    {
                        System.IO.Stream s = fwp.GetResponseStream();
                        if (s != null && s.CanRead)
                        {
                            // Lecture du stream
                            byte[] rBuffer = new byte[1048576];
                            int rl = s.Read(rBuffer, 0, rBuffer.Length);
                            // Lancement de l'écriture
                            if (rl > 0) bfio.WriteFile(rl, rBuffer);
                        }
                        else
                        {
                            break;
                        }
                    }

                    // on attend que les IO soient terminée.
                    bfio.WaitAllIO();
                }
            }
            finally
            {
                if (streamasync != null)
                {
                    streamasync.Dispose();
                    streamasync = null;
                }
                if (fwp != null)
                {
                    try
                    {
                        fwp.Close();
                    }
                    catch { }
                    fwp = null;
                }
                if (fwr != null) fwr = null;
                if (bfio != null)
                {
                    bfio.Dispose();
                    bfio = null;
                }
            }
        }

        /// <summary>
        /// Cette méthode est exécutée de façon asynchrone lors de l'utilisation
        /// de CopyFile dans le cas d'une copie vers le serveur FTP.
        /// </summary>
        /// <param name="sender">L'objet à l'orogine de l'appel</param>
        /// <param name="e">Les paramètres de l'évènement</param>
        private void bfiocopyfile_BasicFileIOEvent(object sender, BasicFileIOEventArgs e)
        {
            if (e.TypeIOEvent.Equals(TypeIOEventEnum.lecturefaite))
            {
                if (e.NbIO > 0)
                {
                    // Ecriture dans le stream
                    streamasync.Write(e.IOBuffer, 0, e.NbIO);
                    // Transfert sur le serveur FTP
                    streamasync.Flush();
                }
            }
        }

        /// <summary>
        /// Copie les fichiers dont le nom (extension comprise) correspond au modèle fournit
        /// </summary>
        /// <param name="localTOserveur">Si vrai, le fichier est envoyé au serveur FTP. Si faux, le fichier est téléchargé du serveur FTP</param>
        /// <param name="modelenomfichier">Le modèle à appliquer</param>
        /// <param name="minutesMaxTrt">Durée maxiumum en minutes pour effectuer le traitement. Si 0, aucune limite</param>
        /// <returns>Renvoie la liste de fichier(s) copié(s)</returns>
        public string[] CopyPathFiles(bool localTOserveur, string modelenomfichier, int minutesMaxTrt = 0)
        {
            try
            {
                // Recherche des fichiers
                string[] fichiers = PathFiles(localTOserveur, modelenomfichier);
                if (fichiers.Length == 0) return new string[] { };

                // Lancement de la copie
                return CopyPathFiles(localTOserveur, fichiers, minutesMaxTrt);
            }
            catch (FileIOError)
            {
                throw;
            }
            catch (System.Exception eh)
            {
                // Création d'une erreur de type FileIOError
                throw new FileIOError("CopyPathFiles_BasicFTP", "Impossible de copier les fichiers.", eh);
            }
        }
        /// <summary>
        /// Copie la liste de fichiers passée en paramètre.
        /// </summary>
        /// <param name="localTOserveur">Si vrai, le fichier est envoyé au serveur FTP. Si faux, le fichier est téléchargé du serveur FTP</param>
        /// <param name="fichiers">La liste des fichiers à copier</param>
        /// <param name="minutesMaxTrt">Durée maxiumum en minutes pour effectuer le traitement. Si 0, aucune limite</param>
        /// <returns>Renvoie la liste de fichier(s) copié(s)</returns>
        public string[] CopyPathFiles(bool localTOserveur, string[] fichiers, int minutesMaxTrt = 0)
        {
            // La liste des fichiers copiés
            System.Collections.Generic.List<string> fichierscopies =
                new System.Collections.Generic.List<string>();

            // Test paramêtre temps maxi
            if (minutesMaxTrt < 0) minutesMaxTrt = 0;
            // Démarrage du traitement
            DateTime debutTrt = System.DateTime.Now;

            try
            {
                // Copie des fichiers.
                foreach (string fichier in fichiers)
                {
                    if (CopyFile(localTOserveur, fichier))
                        fichierscopies.Add(fichier);
                    if (minutesMaxTrt > 0 && System.DateTime.Now.Subtract(debutTrt).TotalMinutes > minutesMaxTrt) break;
                }

                // Renvoie les fichiers copies
                return fichierscopies.ToArray();
            }
            catch (FileIOError)
            {
                throw;
            }
            catch (System.Exception eh)
            {
                // Création d'une erreur de type FileIOError
                throw new FileIOError("CopyPathFiles_BasicFTP", "Impossible de copier les fichiers.", eh);
            }
        }

        /// <summary>
        /// Copie les fichiers dont le nom (extension comprise) correspond au modèle fournit
        /// </summary>
        /// <param name="localTOserveur">Si vrai, le fichier est envoyé au serveur FTP. Si faux, le fichier est téléchargé du serveur FTP</param>
        /// <param name="modelenomfichier">Le modèle à appliquer</param>
        /// <param name="minutesMaxTrt">Durée maxiumum en minutes pour effectuer le traitement. Si 0, aucune limite</param>
        /// <param name="maxThread">Nombre de copies lancées en simultanée</param>
        /// <remarks>
        /// Cette méthode de copie est à préférer à CopyPathFiles lorsque le nombre de fichiers à traiter est important.
        /// <para>
        /// Elle va lancer en asynchrone jusqu'à <paramref name="maxThread"/> copies de fichier en simultanée.
        /// </para>
        /// <para>
        /// <paramref name="maxThread"/> est limité à 10. Si égal à 1, la méthode CopyFile sera utilisée.
        /// </para>
        /// </remarks>
        public string[] CopyPathFilesEx(bool localTOserveur, string modelenomfichier, int minutesMaxTrt = 0, short maxThread = 5)
        {
            // Récupère fichier(s) dans répertoire source
            System.Collections.Generic.Dictionary<string, DateTime> srcPathFilesEx = PathFilesEx(localTOserveur, modelenomfichier);
            if (srcPathFilesEx.Count == 0) return new string[] { };
            // Récupère fichier(s) dans répertoire destination
            System.Collections.Generic.Dictionary<string, DateTime> desPathFilesEx = PathFilesEx(!localTOserveur, modelenomfichier);
            // Cré la liste des fichiers susceptibles d'être copiés
            string[] fichiers = PathFiles(srcPathFilesEx, desPathFilesEx);
            if (fichiers.Length == 0) return new string[] { };
            // Si un seul fichier, on utilise la copie classique
            if (fichiers.Length == 1) return CopyPathFiles(localTOserveur, fichiers, minutesMaxTrt);

            // Liste des copies en cours.
            System.Collections.Generic.List<asyncCopyFile> asyncCopyFiles = new List<asyncCopyFile>();
            // Liste des fichiers copiés.
            System.Collections.Generic.List<string> fichierscopies = new List<string>();
            // Consignation de l'erreur en cas d'arrêt anormal
            System.Exception resultatCopyFile = null;
            // Démarrage du traitement
            DateTime debutTrt = System.DateTime.Now;
            bool MaxTrt = false;
            // Test paramêtre temps maxi
            if (minutesMaxTrt < 0) minutesMaxTrt = 0;
            // Test du paramètre maxThread
            if (maxThread < 1) maxThread = 1;
            if (maxThread > 10) maxThread = 10;
            // Si un seul thread, on utilise la copie classique
            if (maxThread == 1) return CopyPathFiles(localTOserveur, fichiers, minutesMaxTrt);

            // Lancement de la copie asynchrone
            try
            {
                foreach (string fichier in fichiers)
                {
                    for (; ; )
                    {
                        if (asyncCopyFiles.Count < maxThread)
                        {
                            asyncCopyFiles.Add(new asyncCopyFile(this));
                            if (asyncCopyFiles[asyncCopyFiles.Count - 1].startCopyFile(localTOserveur, fichier)) break;
                            else
                            {
                                // Si l'ajout d'un nouveau thread n'a pas fonctionné, suppression de la nouvelle occurence
                                asyncCopyFiles.Remove(asyncCopyFiles[asyncCopyFiles.Count - 1]);
                                // On diminue le nombre de thread autorisé. Si inférieur à 1, on provoque un arrêt sans erreur
                                // comme si c'était un timeout.
                                maxThread--;
                                if (maxThread < 1)
                                {
                                    MaxTrt = true;
                                    break;
                                }
                            }
                        }
                        // Test si une copie terminée
                        int i;
                        for (i = 0; i < asyncCopyFiles.Count; i++)
                        {
                            if (asyncCopyFiles[i].resultatCopyFile != null) break;
                        }
                        // Si aucune copie terminée
                        if (i == asyncCopyFiles.Count)
                        {
                            System.Threading.Thread.Sleep(500);
                            continue;
                        }
                        // Consigne le fichier copié
                        if (asyncCopyFiles[i].resultatCopyFile is string || asyncCopyFiles[i].resultatCopyFile is bool)
                        {
                            if (asyncCopyFiles[i].resultatCopyFile is string)
                                fichierscopies.Add((string)asyncCopyFiles[i].resultatCopyFile);
                            // Démarrage d'une autre copie si le temps de traitemet le permet
                            if (minutesMaxTrt == 0 || (System.DateTime.Now.Subtract(debutTrt).TotalMinutes < minutesMaxTrt))
                            {
                                if (!asyncCopyFiles[i].startCopyFile(localTOserveur, fichier))
                                {
                                    // Si création du nouveau thread impossible, suppression de l'occurence.
                                    // Pas de diminution du nombre maximum de thread.
                                    asyncCopyFiles.RemoveAt(i);
                                    System.Threading.Thread.Sleep(500);
                                    continue;
                                }
                            }
                            else
                            {
                                // On supprime l'occurence du tableau
                                asyncCopyFiles.RemoveAt(i);
                                // On consigne l'erreur de timeOut
                                MaxTrt = true;
                            }
                        }
                        else
                        {
                            // On signe l'erreur
                            resultatCopyFile = (System.Exception)asyncCopyFiles[i].resultatCopyFile;
                            // On supprime l'occurence du tableau
                            asyncCopyFiles.RemoveAt(i);
                        }
                        // Passe au fichier suivant
                        break;
                    }
                    // Si temps dépassé
                    if (MaxTrt) break;
                    // Avant de passer à un autre fichier, on vérifie qu'on est pas en erreur
                    if (resultatCopyFile != null && resultatCopyFile is System.Exception) break;
                }

                // En fin de traitement, on attend que la copie soit terminée.
                while (asyncCopyFiles.Count > 0)
                {
                    // Recherche si traitement terminé
                    int i;
                    for (i = 0; i < asyncCopyFiles.Count; i++)
                    {
                        if (asyncCopyFiles[i].resultatCopyFile != null) break;
                    }
                    // Si une copie terminée
                    if (i < asyncCopyFiles.Count)
                    {
                        // Consigne le fichier copié
                        if (asyncCopyFiles[i].resultatCopyFile is string || asyncCopyFiles[i].resultatCopyFile is bool)
                        {
                            if (asyncCopyFiles[i].resultatCopyFile is string)
                                fichierscopies.Add((string)asyncCopyFiles[i].resultatCopyFile);
                        }
                        else
                        {
                            // On signe l'erreur
                            if (resultatCopyFile == null)
                                resultatCopyFile = (System.Exception)asyncCopyFiles[i].resultatCopyFile;
                        }
                        // On supprime l'occurence du tableau
                        asyncCopyFiles.RemoveAt(i);
                    }
                    else
                        System.Threading.Thread.Sleep(500);
                }

                // On relaye l'erreur si nécessaire
                if (resultatCopyFile != null) throw resultatCopyFile;

                // Traitement correctement terminé.
                return fichierscopies.ToArray();
            }
            catch (FileIOError)
            {
                throw;
            }
            catch (System.Exception eh)
            {
                // Création d'une erreur de type FileIOError
                throw new FileIOError("CopyPathFiles_BasicFTP", "Impossible de copier les fichiers.", eh);
            }
        }

        /// <summary>
        /// Suppression d'un fichier
        /// </summary>
        /// <param name="local">Si vrai, la suppression a lieu sur LocalPath sinon sur ServeurPath</param>
        /// <param name="fichier">Nom du fichier</param>
        public void DelFile(bool local, string fichier)
        {
            // Lest objets permettant l'accès au serveur
            System.Net.FtpWebRequest fwr = null;
            System.Net.FtpWebResponse fwp = null;

            try
            {
                if (local)
                    System.IO.File.Delete(LocalPath + System.IO.Path.DirectorySeparatorChar + fichier);
                else
                {
                    // L'objet permettant l'accès au serveur
                    fwr = CreFwr(fichier);
                    fwr.Method = System.Net.WebRequestMethods.Ftp.DeleteFile;

                    // Exécute la requête.
                    // NOTA: on ne s'occupe pas du résultat de la réponse.
                    fwp = (System.Net.FtpWebResponse)fwr.GetResponse();
                }
            }
            catch (System.Exception eh)
            {
                // Création d'une erreur de type FileIOError
                throw new FileIOError("DelFile_BasicFTP", "Impossible de supprimer le '" + fichier + "'", eh);
            }
            finally
            {
                if (fwp != null)
                {
                    try
                    {
                        fwp.Close();
                    }
                    catch { }
                    fwp = null;
                }
                if (fwr != null) fwr = null;
            }
        }

        /// <summary>
        /// Renomme un fichier
        /// </summary>
        /// <param name="local">Si vrai, l'opération a lieu sur LocalPath sinon sur ServeurPath</param>
        /// <param name="fichier">Le nom de fichier à renommer</param>
        /// <param name="rfichier">Le nom du fichier renommé</param>
        /// <returns>Vrai si l'opération a réussi</returns>
        public bool RenFile(bool local, string fichier, string rfichier)
        {
            // Lest objets permettant l'accès au serveur
            System.Net.FtpWebRequest fwr = null;
            System.Net.FtpWebResponse fwp = null;

            // Test si le fichier existe
            if (!ExistFile(local, fichier)) return false;
            // Test si le fichier destination existe
            if (ExistFile(local, rfichier)) return false;

            try
            {
                if (local)
                    System.IO.File.Move(LocalPath + System.IO.Path.DirectorySeparatorChar + fichier, LocalPath + System.IO.Path.DirectorySeparatorChar + rfichier);
                else
                {
                    // L'objet permettant l'accès au serveur
                    fwr = CreFwr(fichier);
                    fwr.Method = System.Net.WebRequestMethods.Ftp.Rename;
                    fwr.RenameTo = rfichier;

                    // Exécute la requête.
                    // NOTA: on ne s'occupe pas du résultat de la réponse.
                    fwp = (System.Net.FtpWebResponse)fwr.GetResponse();
                }

                return true;
            }
            catch (FileIOError)
            {
                throw;
            }
            catch (System.Exception eh)
            {
                // Création d'une erreur de type FileIOError
                throw new FileIOError("RenFile_BasicFTP", "Impossible de renommer le fichier '" + fichier + "' en '" + rfichier + "'", eh);
            }
            finally
            {
                if (fwp != null)
                {
                    try
                    {
                        fwp.Close();
                    }
                    catch { }
                    fwp = null;
                }
                if (fwr != null) fwr = null;
            }
        }
        /// <summary>
        /// Test si un fichier existe
        /// </summary>
        /// <param name="local">Si vrai, la recherche a lieu sur LocalPath sinon sur ServeurPath</param>
        /// <param name="fichier">Nom du fichier</param>
        /// <returns>Vrai si le fichier existe</returns>
        public bool ExistFile(bool local, string fichier)
        {
            // Lest objets permettant l'accès au serveur
            System.Net.FtpWebRequest fwr = null;
            System.Net.FtpWebResponse fwp = null;

            try
            {
                if (local)
                {
                    // Recherche en local
                    return System.IO.File.Exists(LocalPath + System.IO.Path.DirectorySeparatorChar + fichier);
                }
                else
                {
                    // Recherche sur le serveur FTP

                    // L'objet permettant l'accès au serveur
                    fwr = CreFwr();
                    fwr.Method = System.Net.WebRequestMethods.Ftp.ListDirectoryDetails;

                    // Exécute la requête.
                    fwp = (System.Net.FtpWebResponse)fwr.GetResponse();
                    for (; ; )
                    {
                        System.IO.Stream s = fwp.GetResponseStream();
                        if (s != null && s.CanRead)
                        {
                            System.IO.StreamReader sr = new System.IO.StreamReader(s);
                            while (!sr.EndOfStream)
                            {
                                string fichierftp = sr.ReadLine();
                                if (!fichierftp.ToLower().StartsWith("d"))
                                {
                                    if (fichierftp.Substring(fichierftp.LastIndexOf(" ") + 1).Equals(fichier)) return true;
                                }
                            }
                        }
                        else
                            break;
                    }

                    // fichier non trouvé.
                    return false;
                }

            }
            catch (System.Exception eh)
            {
                // Création d'une erreur de type FileIOError
                throw new FileIOError("ExistFile_BasicFTP", "Impossible de tester l'existance du fichier '" + fichier + "'", eh);
            }
            finally
            {
                if (fwp != null)
                {
                    try
                    {
                        fwp.Close();
                    }
                    catch { }
                    fwp = null;
                }
                if (fwr != null) fwr = null;
            }
        }

        /// <summary>
        /// Recherche les fichiers présents dans le répertoire respectant le modéle passé en paramètre.
        /// </summary>
        /// <param name="local">Si vrai, recherche en local sinon recherche sur site FTP</param>
        /// <param name="modelenomfichier">Le modéle à rechercher</param>
        /// <returns>La liste des fichiers trouvés</returns>
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
        public string[] PathFiles(bool local, string modelenomfichier)
        {
            // Les objets permettant l'accès au serveur
            System.Net.FtpWebRequest fwr = null;
            System.Net.FtpWebResponse fwp = null;
            ModeleNomFichier mf = null;

            try
            {
                // Création du modèle
                mf = new ModeleNomFichier(modelenomfichier);

                if (local)
                {
                    return mf.ListeFichiers(LocalPath);
                }
                else
                {
                    // La liste de fichier obtenu
                    System.Collections.Generic.List<string> fichiers = new System.Collections.Generic.List<string>();

                    // Recherche sur le serveur FTP
                    // L'objet permettant l'accès au serveur
                    fwr = CreFwr();
                    fwr.Method = System.Net.WebRequestMethods.Ftp.ListDirectoryDetails;

                    // Exécute la requête.
                    fwp = (System.Net.FtpWebResponse)fwr.GetResponse();
                    for (; ; )
                    {
                        System.IO.Stream s = fwp.GetResponseStream();
                        if (s != null && s.CanRead)
                        {
                            System.IO.StreamReader sr = new System.IO.StreamReader(s);
                            while (!sr.EndOfStream)
                            {
                                string fichierftp = sr.ReadLine();
                                if (!fichierftp.ToLower().StartsWith("d"))
                                    fichiers.Add(fichierftp.Substring(fichierftp.LastIndexOf(" ") + 1));
                            }
                        }
                        else
                            break;
                    }

                    // Test la liste de fichier obtenu en tenant compte du modèle
                    mf.TestListeFichiers(fichiers);

                    // Renvoi la liste de fichier modifiée
                    return fichiers.ToArray();
                }
            }
            catch (System.Exception eh)
            {
                // Création d'une erreur de type FileIOError
                throw new FileIOError("PathFiles_BasicFTP", "Impossible de récupérer la liste des fichiers", eh);
            }
            finally
            {
                if (fwp != null)
                {
                    try
                    {
                        fwp.Close();
                    }
                    catch { }
                    fwp = null;
                }
                if (fwr != null) fwr = null;
            }
        }

        /// <summary>
        /// Recherche les fichiers qui seraient copiés de la source vers la destination.
        /// </summary>
        /// <param name="srcPathFilesEx">La liste clé/valeur des fichiers et date de dernière modification du répertoire source</param>
        /// <param name="desPathFilesEx">La liste clé/valeur des fichiers et date de dernière modification du répertoire destination</param>
        /// <remarks>La règle de remplacement du fichier destination lors de la copie si il existe est prise en compte</remarks>
        public string[] PathFiles(System.Collections.Generic.Dictionary<string, DateTime> srcPathFilesEx, System.Collections.Generic.Dictionary<string, DateTime> desPathFilesEx)
        {
            // Liste des fichiers renvoyés
            System.Collections.Generic.List<string> fichiers = new System.Collections.Generic.List<string>();
            try
            {
                // Si aucun fichier en source.
                if (srcPathFilesEx.Count == 0) return fichiers.ToArray();
                // Si aucun fichier en destination ou remplacement systématique
                if (desPathFilesEx.Count == 0 || OWRFichierDestination.Equals(_OWRFICHIERDESTINATION.oui))
                {
                    fichiers.AddRange(srcPathFilesEx.Keys);
                    return fichiers.ToArray();
                }
                // Liste des fichiers à copier en tenant compte des paramètres de remplacement.
                foreach (string fichier in srcPathFilesEx.Keys)
                {
                    if (!desPathFilesEx.ContainsKey(fichier))
                        fichiers.Add(fichier);
                    else if (OWRFichierDestination.Equals(_OWRFICHIERDESTINATION.siplusrecent))
                    {
                        // On ne testera que la date sans l'heure pour tous les fichiers ne datant pas de l'année en cours.
                        if ((srcPathFilesEx[fichier].Year.Equals(DateTime.Today.Year) ? srcPathFilesEx[fichier] : srcPathFilesEx[fichier].Date) >
                            (srcPathFilesEx[fichier].Year.Equals(DateTime.Today.Year) ? desPathFilesEx[fichier] : desPathFilesEx[fichier].Date))
                            fichiers.Add(fichier);
                    }
                }
                return fichiers.ToArray();
            }
            catch (System.Exception eh)
            {
                // Création d'une erreur de type FileIOError
                throw new FileIOError("PathFiles_BasicFTP", "Impossible de récupérer la liste des fichiers", eh);
            }
        }
        /// <summary>
        /// Recherche les fichiers présents dans le répertoire respectant le modéle passé en paramètre.
        /// </summary>
        /// <param name="local">Si vrai, recherche en local sinon recherche sur site FTP</param>
        /// <param name="modelenomfichier">Le modéle à rechercher</param>
        /// <returns>Une tableau clé/valeur des noms de fichiers et de la dernière date de modification</returns>
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
        public System.Collections.Generic.Dictionary<string, DateTime> PathFilesEx(bool local, string modelenomfichier)
        {
            // Les objets permettant l'accès au serveur
            System.Net.FtpWebRequest fwr = null;
            System.Net.FtpWebResponse fwp = null;
            // L'objet permettant d'obtenir la liste des fichiers conforme au modèle
            ModeleNomFichier mf = null;
            // Liste des fichiers avec date de dernière modification
            // NOTA : on part du principe qu'on ne tient pas compte de la casse (ce qui est faux sous LINUX)
            System.Collections.Generic.Dictionary<string, DateTime> ListeFichiersEx = new Dictionary<string, DateTime>(StringComparer.CurrentCultureIgnoreCase);

            try
            {
                // Création du modèle
                mf = new ModeleNomFichier(modelenomfichier);

                if (local)
                {
                    // Recherche des fichiers
                    string[] fichiers = mf.ListeFichiers(LocalPath);
                    if (fichiers.Length == 0) return ListeFichiersEx;
                    // Création du tableau nom de fichier / date de modification
                    foreach (string fichier in fichiers)
                        ListeFichiersEx.Add(fichier, System.IO.File.GetLastAccessTime(LocalPath + System.IO.Path.DirectorySeparatorChar + fichier));
                }
                else
                {
                    // Recherche sur le serveur FTP
                    // L'objet permettant l'accès au serveur
                    fwr = CreFwr();
                    fwr.Method = System.Net.WebRequestMethods.Ftp.ListDirectoryDetails;

                    // Exécute la requête.
                    fwp = (System.Net.FtpWebResponse)fwr.GetResponse();
                    System.Collections.Generic.Dictionary<string, DateTime> tmpListeFichiersEx = new Dictionary<string, DateTime>(StringComparer.CurrentCultureIgnoreCase);
                    for (; ; )
                    {
                        System.IO.Stream s = fwp.GetResponseStream();
                        if (s != null && s.CanRead)
                        {
                            System.IO.StreamReader sr = new System.IO.StreamReader(s);
                            while (!sr.EndOfStream)
                            {
                                string fichierftp = sr.ReadLine();
                                if (!fichierftp.ToLower().StartsWith("d"))
                                {
                                    string[] infos = fichierftp.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    // Détermine le mois
                                    int mois = 0;
                                    if (infos[5].Equals("jan", StringComparison.CurrentCultureIgnoreCase)) mois = 1;
                                    else if (infos[5].Equals("feb", StringComparison.CurrentCultureIgnoreCase)) mois = 2;
                                    else if (infos[5].Equals("mar", StringComparison.CurrentCultureIgnoreCase)) mois = 3;
                                    else if (infos[5].Equals("apr", StringComparison.CurrentCultureIgnoreCase)) mois = 4;
                                    else if (infos[5].Equals("may", StringComparison.CurrentCultureIgnoreCase)) mois = 5;
                                    else if (infos[5].Equals("jun", StringComparison.CurrentCultureIgnoreCase)) mois = 6;
                                    else if (infos[5].Equals("jul", StringComparison.CurrentCultureIgnoreCase)) mois = 7;
                                    else if (infos[5].Equals("aug", StringComparison.CurrentCultureIgnoreCase)) mois = 8;
                                    else if (infos[5].Equals("sep", StringComparison.CurrentCultureIgnoreCase)) mois = 9;
                                    else if (infos[5].Equals("oct", StringComparison.CurrentCultureIgnoreCase)) mois = 10;
                                    else if (infos[5].Equals("nov", StringComparison.CurrentCultureIgnoreCase)) mois = 11;
                                    else if (infos[5].Equals("dec", StringComparison.CurrentCultureIgnoreCase)) mois = 12;
                                    // Détermine année et heure/minute
                                    int annee = System.DateTime.Today.Year;
                                    int hh = 0;
                                    int hm = 0;
                                    if (infos[7].IndexOf(":") > 0)
                                    {
                                        hh = System.Convert.ToInt32(infos[7].Substring(0, 2));
                                        hm = System.Convert.ToInt32(infos[7].Substring(3, 2));
                                        // ATTENTION : si le mois est supérieur au mois de la date système, c'est qu'il s'agit d'un mois
                                        // de l'année précedente.
                                        if (mois > DateTime.Now.Month) annee--;
                                    }
                                    else
                                        annee = System.Convert.ToInt32(infos[7]);
                                    // La postion 8 correspond au nom du fichier. Mais si ce dernier contient des espaces, il faut reconcatener
                                    // le nom.
                                    string fichier = infos[8];
                                    for (int pos = 9; pos < infos.Length; pos++) fichier = fichier + " " + infos[pos];
                                    // Il est possible que le serveur soit casse sensitive.
                                    if (!tmpListeFichiersEx.ContainsKey(fichier))
                                        tmpListeFichiersEx.Add(fichier, new DateTime(annee, mois, System.Convert.ToInt32(infos[6]), hh, hm, 0));
                                    else
                                    {
                                        // Si deux fichiers différents à cause de la casse, on conserve le plus récent.
                                        DateTime dt = new DateTime(annee, mois, System.Convert.ToInt32(infos[6]), hh, hm, 0);
                                        if (dt > tmpListeFichiersEx[fichier])
                                        {
                                            // Le serveur étant sebsible à la casse, il faut garder le nom de fichier tel que présent sur le serveur
                                            tmpListeFichiersEx.Remove(fichier);
                                            tmpListeFichiersEx.Add(fichier, new DateTime(annee, mois, System.Convert.ToInt32(infos[6]), hh, hm, 0));
                                        }
                                    }
                                }
                            }
                        }
                        else
                            break;
                    }

                    // Test la liste de fichiers obtenu en tenant compte du modèle
                    System.Collections.Generic.List<string> fichiers = new System.Collections.Generic.List<string>(tmpListeFichiersEx.Keys);
                    mf.TestListeFichiers(fichiers);
                    if (fichiers.Count > 0)
                    {
                        foreach (string fichier in fichiers)
                            ListeFichiersEx.Add(fichier, tmpListeFichiersEx[fichier]);
                    }
                }

                // Renvoie le résultat
                return ListeFichiersEx;
            }
            catch (System.Exception eh)
            {
                // Création d'une erreur de type FileIOError
                throw new FileIOError("PathFiles_BasicFTP", "Impossible de récupérer la liste des fichiers", eh);
            }
            finally
            {
                if (fwp != null)
                {
                    try
                    {
                        fwp.Close();
                    }
                    catch { }
                    fwp = null;
                }
                if (fwr != null) fwr = null;
            }
        }

        /// <summary>
        /// Creation du FtpWebRequest en fonction des paramètres de connexion
        /// </summary>
        /// <returns>L'objet créé</returns>
        private System.Net.FtpWebRequest CreFwr()
        {
            return CreFwr("");
        }

        /// <summary>
        /// Creation du FtpWebRequest en fonction des paramètres de connexion et
        /// en ajoutant le nom du fichier
        /// </summary>
        /// <param name="fichier">Le nom de fichier à rajouter (SANS information de répertoire)</param>
        /// <returns>L'objet créé</returns>
        private System.Net.FtpWebRequest CreFwr(string fichier)
        {
            // Création de l'URI avec l'UriBuilder
            UriBuilder urb = new UriBuilder
            {
                Scheme = "ftp",
                Host = Serveur,
                UserName = Utilisateur,
                Password = MotdePasse
            };
            if (!ServeurPath.Equals(""))
                urb.Path = ServeurPath;
            if (!urb.Path.EndsWith("/")) urb.Path = urb.Path + "/";
            if (!fichier.Equals("")) urb.Path = urb.Path + fichier;

            // Création de l'objet
            System.Net.FtpWebRequest fwr = (System.Net.FtpWebRequest)System.Net.WebRequest.Create(urb.Uri);
            fwr.UsePassive = true;
            fwr.UseBinary = true;
            fwr.KeepAlive = SupportKeepAlive;

            // Retourne l'objet ainsi créé.
            return fwr;
        }
    }

    /// <summary>
    /// Cette classe permet de déclencher dans un thread à par un CopyFile
    /// </summary>
    internal class asyncCopyFile
    {
        // L'objet BasicFTP pour faire l'opération de copie asynchrone
        private BasicFTP bftp;
        // Le fichier à traiter
        private string Fichier;

        // Résultat de la copie.
        private object resultatcopyfile;
        /// <summary>
        /// Le résultat de la copie.
        /// </summary>
        /// <remarks>
        /// Si Null, la copie est en cours d'éxécution.
        /// Si de type String, c'est que la copie a été effecutée.
        /// Si de type Boolean, c'est que le fichier n'a pas été copié mais ce n'est pas une erreur
        /// Si de type System.Exception, traitement anormalement terminé.
        /// </remarks>
        public object resultatCopyFile
        {
            get { return resultatcopyfile; }
        }

        /// <summary>
        /// Création de la classe pour la copie asynchrone
        /// </summary>
        /// <param name="bftp">L'objet BasicFTP à utiliser</param>
        public asyncCopyFile(BasicFTP bftp)
        {
            this.bftp = new BasicFTP(bftp.Serveur, bftp.Utilisateur, bftp.MotdePasse, false, bftp.ServeurPath, bftp.LocalPath, bftp.OWRFichierDestination, bftp.DELFichierSource);
        }

        /// <summary>
        /// Démarrage de la copie
        /// </summary>
        /// <param name="localTOServeur">Le sens de la copie</param>
        /// <param name="fichier">Le fichier à copier</param>
        /// <returns></returns>
        public bool startCopyFile(bool localTOServeur, string fichier)
        {
            Fichier = fichier;
            resultatcopyfile = null;
            return System.Threading.ThreadPool.QueueUserWorkItem(Run, localTOServeur);
        }

        /// <summary>
        /// Exécution de la copie en asynchrone
        /// </summary>
        /// <param name="Infos">Le sens de la copie</param>
        private void Run(object Infos)
        {
            try
            {
                bool localTOServeur = (bool)Infos;
                if (bftp.CopyFile(localTOServeur, Fichier))
                    resultatcopyfile = Fichier;
                else
                    resultatcopyfile = false;
            }
            catch (System.Exception eh)
            {
                resultatcopyfile = eh;
            }
        }
    }

}
