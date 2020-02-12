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
    /// <summary>
    /// Lightweigth threadsafe logger
    /// </summary>
    public static class Logger
    {
        #region Constants
        public const string LOG_FOLDER = "Log";

        #endregion Constants

        #region Members
        static BackgroundWorker m_Worker;
        static DateTime m_LogFileCreationTime;
        static ConcurrentQueue<LogMessage> m_MessageQueue;

        /// <summary>
        /// The logger is deactivated
        /// </summary>
        static bool m_LoggerIsDisabled = false;

        /// <summary>
        /// Subsystems which are excluded from logging
        /// </summary>
        static SubSystem m_ExcludedSubSystems = SubSystem.None;

        /// <summary>
        /// Log depth from which log messages are saved 
        /// </summary>
        static LoggerLevel m_MinimumLogLevel = LoggerLevel.Info;

        #endregion Members

        #region Services
        public static void ApplyConfig(bool isActive, SubSystem excludedSubSystems, LoggerLevel minimumLogLevel)
        {
            Write("Logger configuration changed!", SubSystem.Logger, LoggerLevel.Info);
            if (isActive)
                Write("Logger:                Enabled", SubSystem.Logger, LoggerLevel.Info);
            else
                Write("Logger:                Disabled", SubSystem.Logger, LoggerLevel.Info);
            Write($"Excluded sub-systems: {excludedSubSystems}", SubSystem.Logger, LoggerLevel.Info);
            Write($"Minimum loglevel:     {minimumLogLevel}", SubSystem.Logger, LoggerLevel.Info);

            m_LoggerIsDisabled = !isActive;
            m_ExcludedSubSystems = excludedSubSystems;
            m_MinimumLogLevel = minimumLogLevel;
        }


        /// <summary>
        /// Gets the current log file as StorageFile
        /// </summary>
        /// <returns>The current log file</returns>
        public static async Task<StorageFile> GetCurrentLogFile()
        {
            var localFolder = ApplicationData.Current.LocalFolder;

            var logFolder = (StorageFolder)await localFolder.TryGetItemAsync(LOG_FOLDER);
            if (logFolder == null)
                return null;

            return (StorageFile)await logFolder.TryGetItemAsync(GetCurrentLogFileName());
        }

        /// <summary>
        /// Write a exception to the log file
        /// </summary>
        /// <param name="message">Exception</param>
        /// <param name="source">Source (subsystem)</param>
        /// <param name="level">Log-level</param>
        public static async void Write(Exception ex, SubSystem source, LoggerLevel level = LoggerLevel.Error)
        {
            Write(ex.Message, source, level);
        }

        /// <summary>
        /// Write a message to the log file
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="source">Source (subsystem)</param>
        /// <param name="level">Log-level</param>
        public static async void Write(string message, SubSystem source, LoggerLevel level = LoggerLevel.Info)
        {
            Debug.WriteLine(level.ToString() + ": " + message);

            if (!m_LoggerIsDisabled) //TODO: Testen was passiert wenn der Logger aus ist (SocketListener Problem!)
            {
                if (level >= m_MinimumLogLevel && !m_ExcludedSubSystems.HasFlag(source)) //Limit log depth
                {
                    if (m_MessageQueue == null)
                        m_MessageQueue = new ConcurrentQueue<LogMessage>();

                    m_MessageQueue.Enqueue(new LogMessage(message, level, source));

                    if (m_Worker == null || !m_Worker.IsBusy)
                        StartBackgroundWorker();
                }
            }
        }

        public static async void Flush()
        {
            if (m_Worker != null && m_Worker.IsBusy)
            {
                m_Worker.CancelAsync();
                while (m_Worker.IsBusy)
                {
                    await Task.Delay(100);
                }

                m_Worker = null;
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

        #endregion Services

        #region Internal services
        private static void StartBackgroundWorker()
        {
            if (m_Worker == null)
            {
                m_Worker = new BackgroundWorker
                {
                    WorkerSupportsCancellation = true
                };
                m_Worker.DoWork += Bw_DoWork;
                m_Worker.RunWorkerAsync();
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
            var logMessages = new List<LogMessage>();

            while (!m_MessageQueue.IsEmpty)
            {
                if (m_MessageQueue.TryDequeue(out var log))
                    logMessages.Add(log);
            }

            await WriteToFile(logMessages.ToArray());

            if (!m_MessageQueue.IsEmpty)
                DequeueMessages();
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

        /// <summary>
        /// Deleting old log files
        /// </summary>
        /// <param name="timeLimit">Time until which log files should be deleted</param>
        private static async void CleanUpLogFolder(DateTime timeLimit)
        {
            throw new NotImplementedException(); //TODO:
        }

        #endregion Internal services
    }

    /// <summary>
    /// Object to transport log-messages
    /// </summary>
    public class LogMessage
    {
        #region Constants
        const int POS_LEVEL = 23;
        const int POS_SUBSYSTEM = 38;
        public const int POS_MESSAGE = 55;

        #endregion Constants

        #region Properties
        /// <summary>
        /// Log-message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Log-level
        /// </summary>
        public LoggerLevel Level { get; set; }

        /// <summary>
        /// Source of the log entrie (describes also the subsystem)
        /// </summary>
        public SubSystem Source { get; set; }

        /// <summary>
        /// Timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        #endregion Properties

        #region Constructors
        /// <summary>
        /// New LogMessage, obtain DateTime.Now as timestamp 
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="level">Log-Level</param>
        /// <param name="source">Source (subsystem)</param>
        public LogMessage(string message, LoggerLevel level, SubSystem source) : this(message, level, source, DateTime.Now)
        {

        }

        /// <summary>
        /// New LogMessage
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="level">Log-Level</param>
        /// <param name="source">Source (subsystem)</param>
        /// <param name="timestamp">Timestamp</param>
        public LogMessage(string message, LoggerLevel level, SubSystem source, DateTime timestamp)
        {
            Message = message;
            Level = level;
            Source = source;
            Timestamp = timestamp;
        }

        #endregion Constructors

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
        Info = 0,
        /// <summary>
        /// Message that reports an error that could be corrected (e.g. connection terminated)
        /// </summary>
        Warning = 10,
        /// <summary>
        /// Message reporting a bug that could not be fixed.
        /// </summary>
        Error = 20,
        /// <summary>
        /// Message reporting an error that is critical and leads, for example, to the termination of the application. 
        /// </summary>
        CriticalError = 30
    }

    /// <summary>
    /// Message source
    /// </summary>
    [Flags]
    public enum SubSystem
    {
        /// <summary>
        /// Empty entrie, to exclude no subsystem
        /// </summary>
        None = 0x0,
        /// <summary>
        /// General app
        /// </summary>
        App = 0x1,
        /// <summary>
        /// Built-in HTTP server
        /// </summary>
        HttpServer = 0x2,
        /// <summary>
        /// Configuration of the EzoGateway app
        /// </summary>
        Configuration = 0x4,
        /// <summary>
        /// REST API
        /// </summary>
        RestApi = 0x8,
        /// <summary>
        /// Plc interface (Siemens LOGO!)
        /// </summary>
        Plc = 0x10,
        /// <summary>
        /// Hardware (EZO module)
        /// </summary>
        LowLevel = 0x20,
        /// <summary>
        /// The logger itself
        /// </summary>
        Logger = 0x40,
    }
}
