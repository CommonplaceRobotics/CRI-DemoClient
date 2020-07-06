using System;
using System.Collections.Generic;

using System.Text;

using log4net;

    /// <summary>
    /// Die Klasse kann als log4net-Appender eingebunden werden und bekommt dadurch alle log-Meldungen mit.
    /// Ein Zugriff ist möglich, da die Klasse als Singleton implementiert ist. Man besorgt sich die Instance
    /// und darüber ruft man GetMessage auf.
    /// Drawbacks: nicht threadsave, der erste Aufruf (instanziierung) muss vom Log4net kommen.
    /// 
    /// </summary>
    class ListViewAppender : log4net.Appender.IAppender 
    {
        public static ListViewAppender instance = null;

        public int nrNewMessages = 0;
        private String[] msgArray = new string[100];
        private int indexWrite = 0;
        private int indexRead = 0;

        /// <summary>
        /// Zugriff auf das Singleton über die Instanz.
        /// </summary>
        public static ListViewAppender Instance
        {
            get
            {
                return instance;
            }
        }



        public string Name { set; get; }


        public void DoAppend(log4net.Core.LoggingEvent loggingEvent)
        {
            // wird von log4net mit einer neuen Nachricht aufgerufen.
            // diese wird hier anwendungsspezifisch gespeichert

            // und um das singleton erreichbar zu machen wird beim ersten Zugriff die Instanz gespeichert
            if (instance == null)
            {
                instance = this;
            }

            // neue Nachrichten werden ringförmig in einen Puffer von 100 Strings geschrieben
            try
            {
                if (indexWrite < 100)
                {
                    // msgArray[indexWrite] = loggingEvent.TimeStamp + "\t" + loggingEvent.Level + "\t" + loggingEvent.LoggerName + "\t" + loggingEvent.RenderedMessage;
                    msgArray[indexWrite] = loggingEvent.TimeStamp.Hour + ":" + loggingEvent.TimeStamp.Minute + ":" + loggingEvent.TimeStamp.Second + "." + loggingEvent.TimeStamp.Millisecond + "" +"\t" + loggingEvent.Level + "\t" + loggingEvent.RenderedMessage;
                    indexWrite++;
                    if (indexWrite >= 100)
                        indexWrite = 0;
                    nrNewMessages++;
                }
            }
            catch (Exception e)
            {
            }
        }


         /// <summary>
         /// Provides the oldest message that has not been read. Storage is limited to 100 messages,
         /// afterwards data loss.
         /// </summary>
         /// <returns></returns>
        public string GetMessage()
        {
            // auch das Auslesen der Nachrichten erfolgt über den Ringpuffer mit
            // dem Index indexRead. Der sollte 
            string msg = "";
            if (nrNewMessages > 0)
            {
                msg = msgArray[indexRead];
                indexRead++;
                if (indexRead >= 100)
                    indexRead = 0;
                nrNewMessages--;
            }
            return msg;
        }

        public void Close()
        {
        }

    }

