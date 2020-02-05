using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace EzoGateway
{
    public static class Logger
    {
        public const string LOG_FOLDER = "Log";

        static BackgroundWorker bw;

        static DateTime m_LogFileCreationTime;
        static bool m_DequeuerIsActive = false;
        static ConcurrentQueue<LogMessage> m_MessageQueue;

        public static async Task<StorageFile> GetCurrentLogFile()
        {
            var localFolder = ApplicationData.Current.LocalFolder;

            var logFolder = (StorageFolder)await localFolder.TryGetItemAsync(LOG_FOLDER);
            if (logFolder == null)
                return null;

            return (StorageFile)await logFolder.TryGetItemAsync(GetCurrentLogFileName());
        }

        public static async void Write(Exception ex, SubSystem source, LoggerLevel level = LoggerLevel.Error)
        {
            Write(ex.Message, source, level);
        }

        public static async void Write(string message, SubSystem source, LoggerLevel level = LoggerLevel.Info)
        {
            Debug.WriteLine(level.ToString() + ": " + message);

            if (m_MessageQueue == null)
                m_MessageQueue = new ConcurrentQueue<LogMessage>();

            m_MessageQueue.Enqueue(new LogMessage(message, level, source));

            if (bw == null || !bw.IsBusy)
                StartBackgroundWorker();
        }

        public static async void Flush()
        {
            if (bw != null && bw.IsBusy)
            {
                bw.CancelAsync();
                while (bw.IsBusy)
                {
                    await Task.Delay(100);
                }

                bw = null;
            }

            await Task.Delay(500);

            while (m_MessageQueue.Count > 0)
            {
                if (m_MessageQueue.TryDequeue(out var log))
                {
                    if (log != null)
                        await WriteToFile(new LogMessage[1] { log });
                }
            }

            StartBackgroundWorker();
        }

        /// <summary>
        /// Print a "EzoGateway" ASCII artwork to the logfile, subtitled with "IS NOW RUNNING"
        /// </summary>
        public static void LogWatermark()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::");
            sb.AppendLine(":'########:'########::'#######:::'######::::::'###::::'########:'########:'##:::::'##::::'###::::'##:::'##:");
            sb.AppendLine(": ##.....::..... ##::'##.... ##:'##... ##::::'## ##:::... ##..:: ##.....:: ##:'##: ##:::'## ##:::. ##:'##::");
            sb.AppendLine(": ##::::::::::: ##::: ##:::: ##: ##:::..::::'##:. ##::::: ##:::: ##::::::: ##: ##: ##::'##:. ##:::. ####:::");
            sb.AppendLine(": ######:::::: ##:::: ##:::: ##: ##::'####:'##:::. ##:::: ##:::: ######::: ##: ##: ##:'##:::. ##:::. ##::::");
            sb.AppendLine(": ##...:::::: ##::::: ##:::: ##: ##::: ##:: #########:::: ##:::: ##...:::: ##: ##: ##: #########:::: ##::::");
            sb.AppendLine(": ##:::::::: ##:::::: ##:::: ##: ##::: ##:: ##.... ##:::: ##:::: ##::::::: ##: ##: ##: ##.... ##:::: ##::::");
            sb.AppendLine(": ########: ########:. #######::. ######::: ##:::: ##:::: ##:::: ########:. ###. ###:: ##:::: ##:::: ##::::");
            sb.AppendLine(":........::........:::.......::::......::::..:::::..:::::..:::::........:::...::...:::..:::::..:::::..:::::");
            sb.AppendLine("...........................................................................................................");
            sb.AppendLine(":::::::::::::::::::::::::::::::::::::::::::::: IS NOW RUNNING :::::::::::::::::::::::::::::::::::::::::::::");

            Write(sb.ToString(), SubSystem.App, LoggerLevel.Info);
        }

        private static void StartBackgroundWorker()
        {
            if (bw == null)
            {
                bw = new BackgroundWorker
                {
                    WorkerSupportsCancellation = true
                };
                bw.DoWork += Bw_DoWork;
                bw.RunWorkerAsync();
            }
        }

        private static async void Bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            while (true)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                else if (m_MessageQueue.TryDequeue(out var log))
                {
                    if (log != null)
                        await WriteToFile(new LogMessage[1] { log });
                }
            }
        }

        private static async void DequeueMessages()
        {
            m_DequeuerIsActive = true;

            var logMessages = new List<LogMessage>();

            while (!m_MessageQueue.IsEmpty)
            {
                if (m_MessageQueue.TryDequeue(out var log))
                    logMessages.Add(log);
            }

            await WriteToFile(logMessages.ToArray());

            if (!m_MessageQueue.IsEmpty)
                DequeueMessages();

            m_DequeuerIsActive = false;
        }

        private static string GetCurrentLogFileName()
        {
            if (m_LogFileCreationTime.DayOfYear != DateTime.Now.DayOfYear) //New logfile for each day
                m_LogFileCreationTime = DateTime.Now;
            return $"{m_LogFileCreationTime.ToString("yyyyMMdd-HHmmss")}.log";
        }

        private static async Task WriteToFile(LogMessage[] logMessage)
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;

                var logFolder = (StorageFolder)await localFolder.TryGetItemAsync(LOG_FOLDER);
                if (logFolder == null)
                    logFolder = await localFolder.CreateFolderAsync(LOG_FOLDER);

                var logFile = await logFolder.CreateFileAsync(GetCurrentLogFileName(), CreationCollisionOption.OpenIfExists);

                await FileIO.AppendLinesAsync(logFile, logMessage.Select(x => x.ToString()));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in logger: " + ex.Message);
                Write(ex, SubSystem.Logger);
            }
        }
    }

    /// <summary>
    /// Log message
    /// </summary>
    public class LogMessage
    {
        const int POS_LEVEL = 23;
        const int POS_SUBSYSTEM = 38;
        public const int POS_MESSAGE = 55;

        public string Message { get; set; }

        public LoggerLevel Level { get; set; }

        public SubSystem Source { get; set; }

        public DateTime Timestamp { get; set; }

        public LogMessage(string message, LoggerLevel level, SubSystem source) : this(message, level, source, DateTime.Now)
        {

        }

        public LogMessage(string message, LoggerLevel level, SubSystem source, DateTime timestamp)
        {
            Message = message;
            Level = level;
            Source = source;
            Timestamp = timestamp;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Timestamp);
            sb = Fill(sb, POS_LEVEL);
            sb.Append(Level);
            sb = Fill(sb, POS_SUBSYSTEM);
            sb.Append(Source);
            sb = Fill(sb, POS_MESSAGE);
            sb.Append(Message);

            var space = "".PadLeft(POS_MESSAGE, ' ');

            return sb.ToString().Replace("\n", "\n" + space);
        }

        private StringBuilder Fill(StringBuilder sb, int charAmount)
        {
            while (sb.Length < (charAmount - 1))
                sb.Append(" ");

            sb.Append(";");

            return sb;
        }
    }

    /// <summary>
    /// Level of a log message
    /// </summary>
    public enum LoggerLevel
    {
        /// <summary>
        /// Info message (e.g. debug messages etc.)
        /// </summary>
        Info,
        /// <summary>
        /// Message that reports an error that could be corrected (e.g. connection terminated)
        /// </summary>
        Warning,
        /// <summary>
        /// Message reporting a bug that could not be fixed.
        /// </summary>
        Error,
        /// <summary>
        /// Message reporting an error that is critical and leads, for example, to the termination of the application. 
        /// </summary>
        CriticalError
    }

    /// <summary>
    /// Message source
    /// </summary>
    public enum SubSystem
    {
        /// <summary>
        /// General app
        /// </summary>
        App,
        /// <summary>
        /// Built-in HTTP server
        /// </summary>
        HttpServer,
        /// <summary>
        /// Configuration of the EzoGateway app
        /// </summary>
        Configuration,
        /// <summary>
        /// REST API
        /// </summary>
        RestApi,
        /// <summary>
        /// Plc interface (Siemens LOGO!)
        /// </summary>
        Plc,
        /// <summary>
        /// Hardware (EZO module)
        /// </summary>
        LowLevel,
        /// <summary>
        /// This logger
        /// </summary>
        Logger,
    }
}
