using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace llt.FileIO
{
    /// <summary>
    /// Le gestionnaire de l'évènement BasicFileIOEvent
    /// </summary>
    /// <param name="sender">L'objet à l'origine du déclenchement</param>
    /// <param name="e">Les paramètres associès à l'évènement</param>
    public delegate void BasicFileIOEventHandler(object sender,BasicFileIOEventArgs e);
    /// <summary>
    /// Le gestionnaire de l'évènement TextFileIOEvent
    /// </summary>
    /// <param name="sender">L'objet à l'origine du déclenchement</param>
    /// <param name="e">Les paramètres associès à l'évènement</param>
    public delegate void TextFileIOEventHandler(object sender, TextFileIOEventArgs e);

    /// <summary>
    /// Cette classe gère l'accès à un fichier de facon simplifié.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Il faut créer un objet par fichier à traiter.
    /// </para>
    /// <para>
    /// Si le fichier est ouvert en lecture seule, il est partagé en lecture.
    /// Si le fichier est ouvert en lecture-écriture, il est ouvert en exclusif.
    /// </para>
    /// <para>
    /// IMPORTANT : que l'opération d'entrée/sortie soit faite en synchrone ou asynchone
    /// cette classe est SYSTEMATIQUEMENT multi-thread avec un thread de traitement
    /// à part déclenchant les évènement 'lecturefaite' et 'ecriturefaite'.
    /// </para>
    /// </remarks>
    public class BasicFileIO : IDisposable
    {
        /// <summary>
        /// Le buffer d'entrée/sortie utilisé pour l'opération d'entrèe/sortie.
        /// </summary>
        private byte[] IOBuffer;
        /// <summary>
        /// Le FileStream utilisé pour l'accès au fichier.
        /// </summary>
        private FileStream fs;
        /// <summary>
        /// Indique que plus aucune entrèe/sortie ne peut-être demandée.
        /// </summary>
        private System.Threading.ManualResetEvent wstop;
        /// <summary>
        /// Dans le cas du traitement asynchrone, une erreur peut se produire.
        /// Dans ce cas, wstop est signalé, et un message d'erreur est envoyé
        /// depuis le thread principal.
        /// </summary>
        private System.Exception asyncErreur;

        /// <summary>
        /// Le type de traitement à effectuer
        /// </summary>
        private enum TypeTrtEnum : short
        {
            lecturefaite,
            ecritureafaire
        }
        /// <summary>
        /// L'information compléte sur le traitement à effectuer
        /// </summary>
        private struct _INFOTRT
        {
            public TypeTrtEnum typetrt;
            public int nbio;
            public long offset;
            public byte[] buffer;
        }
        /// <summary>
        /// La file d'attente des traitements.
        /// </summary>
        private System.Collections.Generic.Queue<_INFOTRT> qTrt;
        /// <summary>
        /// Indique au thread qui gère la file d'attente des traitements
        /// qu'il y a un traitement à faire
        /// </summary>
        private System.Threading.ManualResetEvent qcontinue;
        /// <summary>
        /// Indique au thread qui gère la file d'attente des traitements
        /// qu'il doit s'arrêter.
        /// </summary>
        private System.Threading.ManualResetEvent qstop;
        /// <summary>
        /// Indique au thread qui gère la file d'attente des traitements
        /// qu'il doit arrêter tous les traitements non effectués suite
        /// à une erreur.
        /// </summary>
        private System.Threading.ManualResetEvent qstoptrt;
        /// <summary>
        /// Indique au thread qui gère la file d'attente des traitements
        /// s'il faut qu'il signale lorsque la file d'attente est vide
        /// après le dernier traitement effectué
        /// </summary>
        bool qsignalltrt;
        /// <summary>
        /// Indique que tous les traitements ont été effectués par le
        /// thread qui gère la file d'attente.
        /// </summary>
        private System.Threading.ManualResetEvent qalltrt;

        /// <summary>
        /// L'évènement indiquant l'état de l'opération d'entrée/sortie
        /// </summary>
        /// <remarks>
        /// IMPORTANT : cet évènement peut être déclenché dans un thread séparé.
        /// </remarks>
        public event BasicFileIOEventHandler BasicFileIOEvent;

        /// <summary>
        /// Création de l'objet pour un accès en lecture seule en mode synchrone.
        /// </summary>
        /// <param name="nomfichier">Le nom du fichier respectant les régles de nommage du système d'explotation</param>
         public BasicFileIO(string nomfichier)
            : this(nomfichier, false)
        {
        }
        /// <summary>
        /// Création de l'objet pour un accès en lecture seule, en synchrone/asynchrone
        /// </summary>
        /// <param name="nomfichier">Le nom du fichier respectant les régles de nommage du système d'explotation</param>
        /// <param name="async">Si vrai, traitement en asynchrone</param>
        public BasicFileIO(string nomfichier, bool async)
            : this(nomfichier, async, false)
        {
        }
        /// <summary>
        /// Création de l'objet pour un accès en lecture seule,lecture/écriture, en synchrone/asynchrone
        /// </summary>
        /// <param name="nomfichier">Le nom du fichier respectant les régles de nommage du système d'explotation</param>
        /// <param name="async">Si vrai, traitement en asynchrone</param>
        /// <param name="lectureecriture">Si vrai, l'accès du fichier se fait en lecture/écriture</param>
        public BasicFileIO(string nomfichier, bool async, bool lectureecriture)
            : this(nomfichier, async, lectureecriture, false)
        {
        }
        /// <summary>
        /// Création de l'objet pour un accès en lecture seule,lecture/écriture, en synchrone/asynchrone
        /// </summary>
        /// <param name="nomfichier">Le nom du fichier respectant les régles de nommage du système d'explotation</param>
        /// <param name="async">Si vrai, traitement en asynchrone</param>
        /// <param name="lectureecriture">Si vrai, l'accès du fichier se fait en lecture/écriture</param>
        /// <param name="appendmode">Si vrai, les enregistrements sont ajoutés à la fin du fichier</param>
        public BasicFileIO(string nomfichier, bool async, bool lectureecriture, bool appendmode)
        {
            // Test si le nom dde fichier est renseigné
            if (nomfichier.Equals(""))
                throw new FileIOError("new_BasicFileIO","Le nom de fichier est obligatoire.");
            // Détermine les différents paramètres.
            System.IO.FileMode fm;
            System.IO.FileAccess fa;
            System.IO.FileShare fs;
            if (lectureecriture)
            {
                fm = appendmode ? FileMode.Append : FileMode.OpenOrCreate;
                fa= appendmode ? FileAccess.Write : FileAccess.ReadWrite;
                fs=FileShare.None;
            }
            else
            {
                fm=FileMode.Open;
                fa=FileAccess.Read;
                fs=FileShare.Read;
            }
            // Initialisation de la taille par défaut pour la mémoire tampon.
            // REMARQUE : cette valeur est indépendante de la taille du buffer
            // utilisé pour les entrès/sorties (IOBuffer)
            int buffersize;
            if (!async)
            {
                buffersize = 32 * 1024; // En lecture synchrone, le buffer est de 32ko
                wstop = null;
            }
            else
            {
                buffersize = 512 * 1024; // En lecture asynchrone, le buffer est de 512ko
                wstop = new System.Threading.ManualResetEvent(false);
            }

            // Ouverture du fichier.
            try
            {
                this.fs = new FileStream(nomfichier,fm,fa,fs,buffersize,async);
            }
            catch (System.Exception eh)
            {
                throw new FileIOError("new_BasicFileIO","Impossible d'ouvrir le fichier " + nomfichier, eh);
            }

            // Initialisation pour le thread de traitement.
            qcontinue = new System.Threading.ManualResetEvent(false);
            qstop = new System.Threading.ManualResetEvent(false);
            qstoptrt = new System.Threading.ManualResetEvent(false);
            qalltrt = new System.Threading.ManualResetEvent(false);
            qsignalltrt = false;
            qTrt = new Queue<_INFOTRT>();
            // Création du thread
            if (!System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(qthrdTrt)))
                throw new FileIOError("new_BasicFileIO","Impossible de démarrer le thread de traitement");
        }

        /// <summary>
        /// Cette méthode permet de savoir si à partir de la position 'index', sur une
        /// longueur de 'acomparer', 'buffer' contient 'acomparer'
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="acomparer"></param>
        /// <returns></returns>
        public static bool Contient(ref byte[] buffer, int index, ref byte[] acomparer)
        {
            try
            {
                // Si rien à comparer, on sort.
                if (acomparer.Length == 0) return false;
                // Création du tableau
                byte[] b = new byte[acomparer.Length];
                System.Array.Copy(buffer, index, b, 0, b.Length);
                // Effectue la comparaison.
                for (int i=0;i<b.Length;i++)
                {
                    if (b[i]!=acomparer[i]) return false;
                }
                // Tableau de byte identique.
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lecture du fichier
        /// </summary>
        /// <param name="nbioread">Le nombre d'octet à traiter à chaque opération de lecture</param>
        /// <returns>Vrai si la lecture a pu se faire, faux si la lecture n'a pas été effectué</returns>
        /// <remarks>
        /// ReadFile va renvoyer 'Faux' pour deux raisons :
        /// - fin de fichier atteinte
        /// - erreur dans le traitement asynchone.
        /// </remarks>
        public bool ReadFile(int nbioread)
        {
            // Test si les paramètres sont correctes.
            if (fs == null) return false;
            if (nbioread == 0) return false;

            // Lancement de la lecture.
            try
            {
                // Initialisation du buffer
                if (IOBuffer == null)
                    IOBuffer = new byte[nbioread];
                else
                {
                    if (IOBuffer.Length<nbioread)
                        IOBuffer = new byte[nbioread]; // Création d'un nouveau si nécessaire
                    else
                        System.Array.Clear(IOBuffer,0,IOBuffer.Length); // Initialisation
                }

                // Traitement de la lecture
                int nbio = 0;
                long offset = fs.Position;
                if (!fs.IsAsync)
                {
                    // Lecture en mode synchrone
                    nbio = fs.Read(IOBuffer, 0, nbioread);
                }
                else
                {
                    // Mémorisation de la position
                    long position=fs.Position;
                    System.Threading.Tasks.Task<int> t = fs.ReadAsync(IOBuffer, 0, nbioread);
                    // Prévient que la lecture asynchrone est en cours.
                    OnBasicFileIOEvent(new BasicFileIOEventArgs(TypeIOEventEnum.lectureencours, nbioread, position, null));
                    // Attend la lecture
                    t.Wait();
                    nbio = t.Result;
                }
                // La lecture est effectuée.
                if (nbio > 0)
                {
                    _INFOTRT trt = new _INFOTRT();
                    trt.typetrt = TypeTrtEnum.lecturefaite;
                    trt.nbio = nbio;
                    trt.offset = offset;
                    trt.buffer = (byte[])IOBuffer.Clone();
                    return qAdd(trt);
                }
                else
                {
                    // NOTA : il peut s'agir d'une fin de fichier. On envoie
                    // donc une lecturefaite mais avec nbio à 0 afin de la signaler.
                    _INFOTRT trt = new _INFOTRT();
                    trt.typetrt = TypeTrtEnum.lecturefaite;
                    trt.nbio = 0;
                    trt.offset = 0;
                    trt.buffer = new byte[] { };
                    qAdd(trt);
                    return false;
                }
            }
            catch (FileIOError)
            {
                // On déclenche l'arrêt de tous les traitements en attente
                if (!qstoptrt.WaitOne(0)) qstoptrt.Set();
                // On relaie le message d'erreur tel quel
                throw;
            }
            catch (System.Exception eh)
            {
                // On déclenche l'arrêt de tous les traitements en attente
                if (!qstoptrt.WaitOne(0)) qstoptrt.Set();
                // Création d'une erreur de tpe FileIOError
                throw new FileIOError("ReadFile_BasicFileIO","Impossible de lire le fichier '" + fs.Name + "'", eh);
            }
        }
        /// <summary>
        /// Lecture du fichier
        /// </summary>
        /// <param name="nbioread">Le nombre d'octet à traiter à chaque opération de lecture</param>
        /// <param name="offset">Le déplacement à effectuer depuis le début de fichier avant la lecture</param>
        /// <returns>Vrai si la lecture a pu se faire, faux si la lecture n'a pas été effectué</returns>
        public bool ReadFile(int nbioread, long offset)
        {
            try
            {
                if (fs.Position != offset) fs.Position = offset;
                return ReadFile(nbioread);
            }
            catch (FileIOError)
            {
                throw;
            }
            catch (System.Exception eh)
            {
                // On déclenche l'arrêt de tous les traitements en attente
                if (!qstoptrt.WaitOne(0)) qstoptrt.Set();
                // Création d'une erreur de tpe FileIOError
                throw new FileIOError("ReadFile_BasicFileIO","Impossible d'effectuer le positionnement demandé pour le fichier '" + fs.Name + "'", eh);
            }
        }
        /// <summary>
        /// Lecture de tout le fichier en mode séquentiel.
        /// </summary>
        /// <param name="nbioread">Le nombre d'octet à traiter à chaque opération de lecture</param>
        /// <remarks>
        /// La lecture a lieu SYSTEMATIQUEMENT depuis le début du fichier.
        /// Cette méthode attend automatiquement que toutes les lectures soient terminées.
        /// </remarks>
        public void ReadFileSeq(int nbioread)
        {
            try
            {
                // On se positionne en début de fichier
                if (fs.Position != 0) fs.Position = 0;
                // Initialition du message d'erreur
                if (asyncErreur != null) asyncErreur = null;
                // On désactive le signalement de fin de traitement
                qsignalltrt = false;
                qalltrt.Reset();
                // Lecture du fichier
                while (ReadFile(nbioread))
                {
                    // Vérifie si le traitement n'est pas arrêté.
                    if (wstop != null && wstop.WaitOne(0)) break;
                }
                // On attend que tous les traitements soient terminés
                WaitAllIO();
            }
                // Erreur déja mise en forme. On la renvoie telle quelle.
            catch (FileIOError)
            {
                throw;
            }
                // Erreur non prévue. Mise en forme
            catch (System.Exception eh)
            {
                throw new FileIOError("ReadFileSeq_BasicFileIO","Arrêt anormal de la lecture séquentielle", eh);
            }
        }

        /// <summary>
        /// Place une demande d'écriture dans la file d'attente des travaux.
        /// </summary>
        /// <param name="nbiowrite">Le nombre de byte à écrire</param>
        /// <param name="buffer">Le buffer à utiliser</param>
        /// <returns>Vrai si l'ajout a été effectué</returns>
        /// <remarks>
        /// IMPORTANT : 
        /// <para>
        /// - à ce stade, l'écriture réelle sur le disque n'est pas encore faite
        /// </para>
        /// <para>
        /// - l'ecriture dans le fichier se fera à partir de la position courante
        /// au moment ou se fait l'écriture.
        /// </para>
        /// </remarks>
        public bool WriteFile(int nbiowrite, byte[] buffer)
        {
            return WriteFile(nbiowrite, long.MinValue, buffer);
        }

        /// <summary>
        /// Place une demande d'écriture dans la file d'attente des travaux.
        /// </summary>
        /// <param name="nbiowrite">Le nombre de byte à écrire</param>
        /// <param name="offset">L'emplacement dans le fichier</param>
        /// <param name="buffer">Le buffer à utiliser</param>
        /// <returns>Vrai si l'ajout a été effectué</returns>
        /// <remarks>IMPORTANT : à ce stade, l'écriture réelle sur le disque n'est pas encore faite</remarks>
        public bool WriteFile(int nbiowrite, long offset, byte[] buffer)
        {
            try
            {
                // ON ajoute la demande d'écriture.
                _INFOTRT trt = new _INFOTRT();
                trt.typetrt = TypeTrtEnum.ecritureafaire;
                trt.nbio = nbiowrite;
                trt.offset = offset;
                trt.buffer = (byte[])buffer.Clone();
                return qAdd(trt);
            }
            catch (System.Exception eh)
            {
                // On déclenche l'arrêt de tous les traitements en attente
                if (!qstoptrt.WaitOne(0)) qstoptrt.Set();
                // Création d'une erreur de tpe FileIOError
                throw new FileIOError("WriteFile_BasicFileIO","Impossible d'ajouter une demande d'écriture à la file d'attente", eh);
            }
        }

        /// <summary>
        /// Ecriture dans le fichier, à l'offset indiqué, du contenu du buffer
        /// </summary>
        /// <param name="nbio">Le nombre de byte à écrire</param>
        /// <param name="offset">L'emplacement dans le fichier</param>
        /// <param name="buffer">Le buffer à utiliser</param>
        /// <returns>Vrai si l'écriture s'est bien passé</returns>
        /// <remarks>
        /// La taille totale du buffer peut être supérieur au nombre de byte à écrire
        /// </remarks>
        private void ExeWriteFile(int nbio, long offset, byte[] buffer)
        {
            // Test si les paramètres sont correctes.
            if (fs == null) return;
            if (nbio == 0) return;
            // Test position
            if (offset != long.MinValue)
            {
                if (fs.Position != offset) fs.Position = offset;
            }
            else
                offset = fs.Position;
            // Traitement de l'écriture
            if (!fs.IsAsync)
            {
                // Ecriture en mode synchrone
                fs.Write(buffer, 0, nbio);
                // L'écriture est effectuée.
                OnBasicFileIOEvent(new BasicFileIOEventArgs(TypeIOEventEnum.ecriturefaite,nbio,offset,null));
                // Avertit si dernier traitement effectué
                qSignalAll();
            }
            else
            {
                // Ecriture en mode asynchrone.
                System.Threading.Tasks.Task t = fs.WriteAsync(buffer, 0, nbio);
                // Prévient que la lecture asynchrone est en cours.
                OnBasicFileIOEvent(new BasicFileIOEventArgs(TypeIOEventEnum.ecritureencours, nbio, offset, null));
                t.Wait();
                // L'écriture est effectuée.
                OnBasicFileIOEvent(new BasicFileIOEventArgs(TypeIOEventEnum.ecriturefaite, nbio, offset, null));
                // Avertit si dernier traitement effectué
                qSignalAll();
            }
        }

        /// <summary>
        /// Déclenchement de l'évènement BasicFileIOEvent
        /// </summary>
        /// <param name="bfioargs">L'argument utilisé pour déclencher l'évènement</param>
        protected void OnBasicFileIOEvent(BasicFileIOEventArgs bfioargs)
        {
            if (BasicFileIOEvent != null) BasicFileIOEvent(this, bfioargs);
        }

        /// <summary>
        /// Ajoute un traitement dans la file d'attente et le signal.
        /// </summary>
        /// <returns>Vrai si l'ajout a pu se faire</returns>
        private bool qAdd(_INFOTRT trt)
        {
            // Test si arrêt en cours.
            if (qstop.WaitOne(0)) return false;
            if (qstoptrt.WaitOne(0)) return false;

            // Ajout de la demande.
            lock (qTrt)
            {
                qTrt.Enqueue(trt);
                // Demande de lancer le traitement si nécessaire.
                if (!qcontinue.WaitOne(0)) qcontinue.Set();
            }

            // Si plus de 20 traitements en attente, c'est que le temps
            // de traitement de l'opération E/S est long. On patiente pour éviter une
            // surcharge inutile.
            for (; ; )
            {
                if (qTrt.Count <= 20) break;

                // On attend qu'il redescende à 10.
                for (; ; )
                {
                    System.Threading.Thread.Sleep(100);
                    if (qTrt.Count <= 10) break;
                }
            }

            // Ajout fait.
            return true;
        }

        /// <summary>
        /// Attend que le dernier traitement soit pris en compte. Transmet aussi
        /// le message d'erreur généré suite à un problème dans le thread de traitement.
        /// </summary>
        /// <remarks>
        /// Il est INDISPENSABLE d'appeller cette méthode pour un traitement stable dans
        /// le cas de l'utilisation de ReadFile et WriteFile. Cet appel doit se faire
        /// sur la DERNIERE opération de lecture ou d'écriture.
        /// Il est INUTILE d'appeler cette méthode à la suite de ReadFileSeq().
        /// </remarks>
        public void WaitAllIO()
        {
            // Si le traitement n'a pas déjà été arrêté
            if (wstop == null || !wstop.WaitOne(0))
            {
                // Il faut signaler lorsque tous les traitements fait
                if (!qsignalltrt) qsignalltrt = true;
                // On signal qcontinue pour forcer un traitement même à vide.
                lock (qTrt)
                {
                    if (!qcontinue.WaitOne(0)) qcontinue.Set();
                }
                // Attente fin de traitement
                qalltrt.WaitOne();
            }
            // Si erreur asynchrone, on la signale.
            if (asyncErreur != null) throw asyncErreur;
        }

        /// <summary>
        /// Arrête la lecture asynchrone et supprime tous les traitements en attente.
        /// </summary>
        /// <remarks>
        /// Le thread d'attente n'est pas arrêtée. D'autre IO peuvent donc être déclenchées.
        /// </remarks>
        public void StopAllIO()
        {
            lock (qstoptrt)
            {
                qstoptrt.Set();
            }
        }

        /// <summary>
        /// Signal que tous les traitements ont été effectués.
        /// </summary>
        private void qSignalAll()
        {
            // Test si signalement nécessaire.
            if (qsignalltrt)
            {
                lock (qTrt)
                {
                    if (qTrt.Count == 0) qalltrt.Set();
                }
            }
        }

        /// <summary>
        /// La méthode gérant la file d'attente des traitements.
        /// </summary>
        /// <param name="stateinfo">Ce paramètre n'est pas utilisé</param>
        /// <remarks>Elle s'exécute dans un thread specifique</remarks>
        private void qthrdTrt(object stateinfo)
        {
            for (; ; )
            {
                try
                {
                    // Attente évènement
                    // REMARQUE IMPORTANTE : si plusieurs évènements sont signalés, c'est celui
                    // correspondant au plus petit indice qui est renvoyé.
                    int q = System.Threading.WaitHandle.WaitAny(new System.Threading.WaitHandle[] { qstop, qstoptrt, qcontinue });
                    // Si demande d'arrêt du thread
                    if (q == 0)
                    {
                        // Suppression de tous les traitements.
                        lock (qTrt)
                        {
                            qTrt.Clear();
                            if (wstop != null && !wstop.WaitOne(0)) wstop.Set();
                        }
                        // Signal la fin à WaitAllIO si ce dernier est utilisé.
                        qSignalAll();
                        // Arrête la boucle de traitement.
                        break;
                    }
                    // Si demande d'arrêt des traitements en attente suite à une erreur
                    else if (q == 1)
                    {
                        // Suppression de tous les traitements.
                        lock (qTrt)
                        {
                            qTrt.Clear();
                            if (wstop != null && !wstop.WaitOne(0)) wstop.Set();
                        }
                        // On reste dans la boucle en attente d'une prochaine action.
                        qstoptrt.Reset();
                    }
                    // Traitement normal
                    else if (q == 2)
                    {
                        // Récupére le traitement à effectuer.
                        _INFOTRT trt;
                        lock (qTrt)
                        {
                            if (qTrt.Count > 0)
                                trt = qTrt.Dequeue();
                            else
                            {
                                trt = new _INFOTRT();
                                trt.nbio = -1;
                            }
                            // Si plus rien à traiter on initialise.
                            if (qTrt.Count == 0) qcontinue.Reset();
                        }
                        // Si quelque-chose à faire
                        // NOTA : dans le cas de la lecture, on signale même
                        // si rien n'a été lu pour gérer la fin de fichier.
                        if (trt.nbio >= 0)
                        {
                            // On initialise le message d'erreur asnchrone
                            asyncErreur = null;
                            // On lance le traitement liè à l'opération d'I/O.
                            if (trt.typetrt.Equals(TypeTrtEnum.lecturefaite))
                            {
                                // Avertit qu'une lecture est faite.
                                OnBasicFileIOEvent(new BasicFileIOEventArgs(TypeIOEventEnum.lecturefaite, trt.nbio, trt.offset, trt.buffer));
                            }
                            else if (trt.typetrt.Equals(TypeTrtEnum.ecritureafaire))
                            {
                                // Déclenche l'écriture physique
                                if (trt.nbio > 0)
                                    ExeWriteFile(trt.nbio, trt.offset, trt.buffer);
                            }
                        }
                    }
                    // Signal la fin à WaitAllIO si ce dernier est utilisé.
                    qSignalAll();
                }
                catch (System.Exception eh)
                {
                    // On arrête toute opération asynchrone.
                    if (wstop != null) wstop.Set();
                    // On arrête tous les opérations sur le thread de traitement.
                    if (qstoptrt != null) qstoptrt.Set();
                    // Consignation de l'erreur asynchrone
                    asyncErreur = eh;
                }
            }
        }

        /// <summary>
        /// Le destructeur appelé automatiquement
        /// </summary>
        ~BasicFileIO()
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
                // Arrêt du thread de traitement
                if (qstop != null)
                {
                    // Si une demande d'arrêt n'a pas été déja faite.
                    if (!qstop.WaitOne(0))
                    {
                        // Arrêt thread de traitement.
                        qstop.Set();
                        // On attend une seconde
                        System.Threading.Thread.Sleep(1000);
                    }
                    // On ferme tous les pointeurs.
                    if (dispose)
                    {
                        qstop.Close();
                        if (qstoptrt != null) qstoptrt.Close();
                        if (qcontinue != null) qcontinue.Close();
                    }
                    // On efface la liste des traitements.
                    if (qTrt != null) qTrt.Clear();
                }
                // Suppression du filestream
                if (fs != null)
                {
                    fs.Close();
                    if (dispose) fs.Dispose();
                }
                // Fermeture des pointeurs de lecture.
                if (dispose)
                {
                    //if (wcontinue != null) wcontinue.Close();
                    if (wstop != null) wstop.Close();
                }
            }
            catch
            {
            }
            finally
            {
                fs = null;
                //wcontinue = null;
                wstop = null;
                qcontinue = null;
                qstop = null;
                qstoptrt = null;
                qTrt = null;
                BasicFileIOEvent = null;
            }
        }
    }

    /// <summary>
    /// L'agument utilisé par l'évenement BasicFileIOEvent
    /// </summary>
    /// <remarks>Attention aux remarques lièes au thread d'exécution</remarks>
    public class BasicFileIOEventArgs : EventArgs
    {
        private TypeIOEventEnum typeioevent;
        /// <summary>
        /// Le type d'évènement traité
        /// </summary>
        public TypeIOEventEnum TypeIOEvent
        {
            get { return typeioevent; }
        }

        private int nbio;
        /// <summary>
        /// Le nombre de byte traité par l'opération d'entrèe/sortie
        /// </summary>
        public int NbIO
        {
            get { return nbio; }
        }

        private long offset;
        /// <summary>
        /// L'emplacement dans le fichier avant l'opération d'entrée/sortie
        /// </summary>
        /// <remarks>Renseigné uniquement en lecture</remarks>
        public long Offset
        {
            get { return offset; }
        }

        /// <summary>
        /// Le buffer d'entrée/sortie.
        /// </summary>
        public byte[] IOBuffer;

        /// <summary>
        /// Création de la liste d'arguments dans le cas d'une opération en cours.
        /// </summary>
        /// <param name="typeiovent">Le type d'évènement</param>
        public BasicFileIOEventArgs(TypeIOEventEnum typeiovent)
            : this(typeiovent, 0,0, null)
        {
        }
        /// <summary>
        /// Créaton de la liste d'arguments dans le cas d'une opération effectuée.
        /// </summary>
        /// <param name="typeiovent">Le type d'évènement</param>
        /// <param name="nbio">Le nombre de byte traité</param>
        /// <param name="offset">La position dans le fichier</param>
        /// <param name="iobuffer">Le buffer contenant les données</param>
        public BasicFileIOEventArgs(TypeIOEventEnum typeiovent,int nbio,long offset,byte[] iobuffer)
        {
            this.typeioevent = typeiovent;
            this.nbio = nbio;
            this.offset = offset;
            this.IOBuffer = iobuffer;
        }
    }


    /// <summary>
    /// L'énumération des types d'évènements.
    /// </summary>
    public enum TypeIOEventEnum : short
    {
        /// <summary>
        /// La lecture est faite (opération terminée)
        /// Cet évènement est exécuté dans le thread gérant la file d'attente des traitements.
        /// </summary>
        lecturefaite,
        /// <summary>
        /// Prévient que la lecture est en cours, en mode asynchrone.
        /// Cet évènement est exécuté dans le thread principal.
        /// </summary>
        lectureencours,
        /// <summary>
        /// L'écriture est faite (opération terminée)
        /// Cet évènement est exécuté dans le thread gérant la file d'attente des traitements
        /// ou dans le thread crée par l'opération asynchrone
        /// </summary>
        ecriturefaite,
        /// <summary>
        /// Prévient que l'écriture est en cours, en mode asynchrone.
        /// Cet évènement est exécuté dans le thread gérant la file d'attente des traitements.
        /// </summary>
        ecritureencours
    }

    /// <summary>
    /// Les erreurs envoyés par cette classe.
    /// </summary>
    public class FileIOError : System.Exception
    {
        /// <summary>
        /// Génère une erreur avec un simple message
        /// </summary>
        /// <param name="source">La source ayant crée l'erreur</param>
        /// <param name="message">Le message envoyé</param>
        public FileIOError(string source,string message) : base(message)
        {
            this.Source = source;
        }
        /// <summary>
        /// Génère une erreur avec un message et l'erreur d'origine.
        /// </summary>
        /// <param name="source">La source ayant crée l'erreur</param>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public FileIOError(string source,string message, System.Exception innerException) : base(message,innerException)
        {
            this.Source = source;
        }
    }
}
