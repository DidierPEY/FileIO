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
            OWRFichierDestination=_OWRFICHIERDESTINATION.non;
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
                            int rl = 0;
                            for (; ; )
                            {
                                rl = s.Read(rBuffer, rl, rl + rBuffer.Length);
                                if (rl == 0) break;
                                // Lancement de l'écriture
                                bfio.WriteFile(rl, rBuffer);
                                // Si le nombre d'octet est inférieur au nombre total demandé
                                // on sort.
                                if (rl < rBuffer.Length) break;
                            }
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
                    /*
                    // Création de l'URI avec l'UriBuilder
                    UriBuilder urb = new UriBuilder();
                    urb.Scheme = "ftp";
                    urb.Host = Serveur;
                    urb.UserName = Utilisateur;
                    urb.Password = MotdePasse;
                    if (!ServeurPath.Equals(""))
                        urb.Path = ServeurPath;

                    // L'objet permettant l'accès au serveur
                    System.Net.FtpWebRequest fwr = (System.Net.FtpWebRequest)System.Net.WebRequest.Create(urb.Uri + "/" + fichierasync);
                     */
                    /*
                    // L'objet permettant l'accès au serveur
                    System.Net.FtpWebRequest fwr = CreFwr(fichierasync);
                    fwr.Method = System.Net.WebRequestMethods.Ftp.AppendFile;

                    // Demande du stream d'écriture
                    System.IO.Stream s = fwr.GetRequestStream();
                    // Ecriture dans le stream
                    s.Write(e.IOBuffer, 0, e.NbIO);
                    // Transfert sur le serveur FTP
                    s.Flush();
                    // Fermeture du stream (opération obligatoire).
                    s.Close();
                    */
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
        /// <returns>Renvoie la liste de fichier(s) copié(s)</returns>
        public string[] CopyPathFiles(bool localTOserveur, string modelenomfichier)
        {
            try
            {
                // Recherche des fichiers
                string[] fichiers = PathFiles(localTOserveur, modelenomfichier);
                if (fichiers.Length == 0) return new string[] { };

                // La liste des fichiers copiés
                System.Collections.Generic.List<string> fichierscopies =
                    new System.Collections.Generic.List<string>();

                // Copie des fichiers.
                foreach (string fichier in fichiers)
                {
                    if (CopyFile(localTOserveur, fichier))
                        fichierscopies.Add(fichier);
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
        /// Recherche les fichiers présents dans le répertoire respectant le
        /// modéle passé en paramètre.
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
            UriBuilder urb = new UriBuilder();
            urb.Scheme = "ftp";
            urb.Host = Serveur;
            urb.UserName = Utilisateur;
            urb.Password = MotdePasse;
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
}
