using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace EnrollmentProcessorApp
{
    /// <summary>
    /// Logger version 1.2
    /// 
    /// NOTE: If you need to customize the log location directory, overwrite the return value for GetDefaultLogLocation() to be what you need.
    /// </summary>

    public interface ILogger
    {
        void WriteStartMessageToLog();
        void WriteStopMessageToLog();
        void WriteToLog(string value, [CallerFilePath]string callerFilePath = "", [CallerMemberName]string callerMemberName = "", [CallerLineNumber]int callerLineNumber = 0);
        void WriteToLog(string value, bool includeCallerDetails, [CallerFilePath]string callerFilePath = "", [CallerMemberName]string callerMemberName = "", [CallerLineNumber]int callerLineNumber = 0);
        void WriteExceptionToLog(Exception exception, [CallerFilePath]string callerFilePath = "", [CallerMemberName]string callerMemberName = "", [CallerLineNumber]int callerLineNumber = 0);
        void WriteExceptionToLog(Exception exception, bool includeCallerDetails, [CallerFilePath]string callerFilePath = "", [CallerMemberName]string callerMemberName = "", [CallerLineNumber]int callerLineNumber = 0);
    }

    public class Logger : ILogger
    {

        //protected class TandTConsoleTraceListener : ConsoleTraceListener
        //{
        //    public override void Write(string message)
        //    {
        //        // Do not write the output. This is the logging prefix added by the Trace Source that we don't want to log.
        //    }
        //}


        //protected class TandTTextWriterTraceListener : TextWriterTraceListener
        //{
        //    public TandTTextWriterTraceListener(string fileName) : base(fileName) { }
        //    public override void Write(string message)
        //    {
        //        // Do not write the output. This is the logging prefix added by the Trace Source that we don't want to log.
        //    }
        //}


        public enum FileType
        {
            PerRun = 0,
            PerDay = 1
        }

        protected const FileType DEFAULT_LOG_TYPE = FileType.PerRun;
        protected const string DEFAULT_TRACE_SOURCE_NAME = "DefaultLog";
        protected const string DEFAULT_LOG_DIRECTORY_NAME = "Logs";
        protected const string PER_RUN_LOG_FILE_DATE_FORMAT = "yyyy-MM-dd_HH.mm.ss";
        protected const string PER_DAY_LOG_FILE_DATE_FORMAT = "yyyy-MM-dd";
        protected const bool DEFAULT_INCLUDE_CALLER_DETAILS = true;
        protected const string TIME_STAMP_DATE_FORMAT = "MM-dd-yyyy hh:mm:ss tt";
        protected const string DELIMITER = " | ";
        protected const string MESSAGE_START = "**************Start**************";
        protected const string MESSAGE_STOP = "**************Stop**************";
        protected const string MESSAGE_EXCEPTION = "**************Exception**************";
        public const string LOG_FILE_EXTENSION = ".log";

        protected static readonly Lazy<Logger> _defaultLogger = null;
        protected TraceSource _traceSource = null;
        protected string _logFileDateTime = string.Empty;
        protected string _logFilePath = string.Empty;
        protected string _logFileId = string.Empty;
        protected string _logFileDirectoryPath = string.Empty;
        protected bool _hasPermissionToWriteToFile = false;
        protected FileType _logFileType = DEFAULT_LOG_TYPE;
        protected bool _includeCallerDetails = DEFAULT_INCLUDE_CALLER_DETAILS;
        protected object _lockObject = new object();

        /// <summary>
        /// A reference to the default logger.
        /// </summary>
        public static Logger Default
        {
            get
            {
                return _defaultLogger.Value;
            }
        }

        /// <summary>
        /// The path to the log file.
        /// </summary>
        public string LogFilePath
        {
            get
            {
                return _logFilePath;
            }
        }

        /// <summary>
        /// The ID of the log file.
        /// </summary>
        public string LogFileId
        {
            get
            {
                return _logFileId;
            }
            set
            {
                // Lock for thread safety.
                lock (_lockObject)
                {
                    _logFileId = value;

                    // Set up the new log file path.
                    SetUpLogFilePath(false);
                }
            }
        }

        /// <summary>
        /// The path to the directory containing the log file.
        /// </summary>
        public string LogFileDirectoryPath
        {
            get
            {
                return _logFileDirectoryPath;
            }
            set
            {
                // Lock for thread safety.
                lock (_lockObject)
                {
                    _logFileDirectoryPath = value;

                    // Set up the new log file path.
                    SetUpLogFilePath(false);
                }
            }
        }

        /// <summary>
        /// The type of log file.
        /// </summary>
        public FileType LogFileType
        {
            get
            {
                return _logFileType;
            }
            set
            {
                if (value != _logFileType)
                {
                    // Lock for thread safety.
                    lock (_lockObject)
                    {
                        _logFileType = value;

                        // Set up the new log file path.
                        _logFileDateTime = DetermineLogFileDateTime(_logFileType);
                        SetUpLogFilePath(false);
                    }
                }
            }
        }

        /// <summary>
        /// A flag indicating if the details of the caller should be written out to the log.
        /// </summary>
        public bool IncludeCallerDetails
        {
            get
            {
                return _includeCallerDetails;
            }
            set
            {
                // Lock for thread safety.
                lock (_lockObject)
                {
                    _includeCallerDetails = value;
                }
            }

        }

        public bool HasPermissionToWriteToFile
        {
            get { return _hasPermissionToWriteToFile; }
        }

        /// <summary>
        /// The static default construction which sets up lazy loading of the default logger so it is only created if and when needed.
        /// </summary>
        static Logger()
        {
            // Provide a lazy loading of the logger so it is only instantiated if referenced.
            _defaultLogger = new Lazy<Logger>(() => new Logger());
        }

        /// <summary>
        /// Creates a new logger using the logFileId specified.
        /// </summary>
        /// <param name="logFileId">The ID used to identify this log file.</param>
        [System.Diagnostics.DebuggerStepThrough()]
        public Logger(string logFileId) : this(logFileId, GetDefaultLogLocation(), DEFAULT_LOG_TYPE) { }
        /// <summary>
        /// Creates a new logger using the parameters specified.
        /// </summary>
        /// <param name="logFileId">The ID used to identify this log file.</param>
        /// <param name="logFileDirectoryPath">The path to the directory that is to contain the log file.</param>
        /// <param name="logFileType">The type of log file.</param>
        [System.Diagnostics.DebuggerStepThrough()]
        public Logger(string logFileId, string logFileDirectoryPath, FileType logFileType) : this(logFileId, logFileDirectoryPath, logFileType, string.Empty, false, logFileId, DEFAULT_INCLUDE_CALLER_DETAILS) { }
        private Logger() : this(string.Empty, GetDefaultLogLocation(), DEFAULT_LOG_TYPE, string.Empty, true, DEFAULT_TRACE_SOURCE_NAME, DEFAULT_INCLUDE_CALLER_DETAILS) { }
        private Logger(string logFileId, string logFileDirectoryPath, FileType logFileType, string logFileDateTime, bool logToConsoleWindowIfItExists, string traceSourceName, bool includeCallerDetails)
        {
            // Store the values.
            _logFileId = logFileId;
            _logFileDirectoryPath = logFileDirectoryPath;
            _logFileType = logFileType;
            if (string.IsNullOrEmpty(logFileDateTime))
            {
                _logFileDateTime = DetermineLogFileDateTime(_logFileType);
            }
            else
            {
                _logFileDateTime = logFileDateTime;
            }
            _includeCallerDetails = includeCallerDetails;

            // Set up the trace source.
            _traceSource = new TraceSource(traceSourceName);
            _traceSource.Switch.Level = SourceLevels.All;

            // Set up the new log file path.
            SetUpLogFilePath(true);

            // Add console listening, if needed.
            //if (logToConsoleWindowIfItExists && !string.IsNullOrEmpty(Console.Title)) // i.e. If there is a console window, add a console listener.
            //{
            //    _consoleTraceListener = new TandTConsoleTraceListener();
            //    _traceSource.Listeners.Add(_consoleTraceListener);
            //}
        }


        /// <summary>
        /// Writes a given value to the default output.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough()]
        public static void Write(string value, [CallerFilePath]string callerFilePath = "", [CallerMemberName]string callerMemberName = "", [CallerLineNumber]int callerLineNumber = 0)
        {
            Write(value, Default.IncludeCallerDetails, callerFilePath, callerMemberName, callerLineNumber);
        }

        /// <summary>
        /// Writes a given value to the default output.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough()]
        public static void Write(string value, bool includeCallerDetails, [CallerFilePath]string callerFilePath = "", [CallerMemberName]string callerMemberName = "", [CallerLineNumber]int callerLineNumber = 0)
        {
            Default.WriteToLog(value, includeCallerDetails, callerFilePath, callerMemberName, callerLineNumber);
        }

        /// <summary>
        /// Writes an exception to the default output.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough()]
        public static void WriteException(Exception exception, [CallerFilePath]string callerFilePath = "", [CallerMemberName]string callerMemberName = "", [CallerLineNumber]int callerLineNumber = 0)
        {
            WriteException(exception, Default.IncludeCallerDetails, callerFilePath, callerMemberName, callerLineNumber);
        }

        /// <summary>
        /// Writes an exception to the default output.
        /// </summary>
        [DebuggerStepThrough()]
        public static void WriteException(Exception exception, bool includeCallerDetails, [CallerFilePath]string callerFilePath = "", [CallerMemberName]string callerMemberName = "", [CallerLineNumber]int callerLineNumber = 0)
        {
            Default.WriteExceptionToLog(exception, includeCallerDetails, callerFilePath, callerMemberName, callerLineNumber);
        }

        /// <summary>
        /// Writes an application start message to the default output.
        /// </summary>
        [DebuggerStepThrough()]
        public static void WriteStartMessage()
        {
            Default.WriteStartMessageToLog();
        }

        /// <summary>
        /// Writes an application stop message to the default output.
        /// </summary>
        [DebuggerStepThrough()]
        public static void WriteStopMessage()
        {
            Default.WriteStopMessageToLog();
        }

        /// <summary>
        /// Deletes application logs older than the specified time.
        /// </summary>
        [DebuggerStepThrough()]
        public static void DeleteOldLogFiles(int daysToKeep)
        {
            //Default.DeleteOldLogs(daysToKeep);
        }

        /// <summary>
        /// Creates a new instance of the logger using the same parameters except for the new logFileId passed in.
        /// </summary>
        /// <param name="logFileId">The ID used to identify the new log file.</param>
        /// <returns></returns>
        [System.Diagnostics.DebuggerStepThrough()]
        public static Logger CreateNewLogger(string logFileId)
        {
            return Default.CreateLogger(logFileId);
        }

        /// <summary>
        /// Retrieves the default location to use for log files.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough()]
        public static string GetDefaultLogLocation()
        {
            return Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName, DEFAULT_LOG_DIRECTORY_NAME);
        }


        /// <summary>
        /// Writes a given value to the output.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough()]
        public void WriteToLog(string value, [CallerFilePath]string callerFilePath = "", [CallerMemberName]string callerMemberName = "", [CallerLineNumber]int callerLineNumber = 0)
        {
            WriteToLog(value, this.IncludeCallerDetails, callerFilePath, callerMemberName, callerLineNumber);
        }

        /// <summary>
        /// Writes a given value to the output.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough()]
        public void WriteToLog(string value, bool includeCallerDetails, [CallerFilePath]string callerFilePath = "", [CallerMemberName]string callerMemberName = "", [CallerLineNumber]int callerLineNumber = 0)
        {
            // Lock for thread safety.
            lock (_lockObject)
            {
                _traceSource.TraceInformation(PrepareValueForLogging(value, includeCallerDetails, callerFilePath, callerMemberName, callerLineNumber));
            }
        }

        /// <summary>
        /// Writes an exception to the output.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough()]
        public void WriteExceptionToLog(Exception exception, [CallerFilePath]string callerFilePath = "", [CallerMemberName]string callerMemberName = "", [CallerLineNumber]int callerLineNumber = 0)
        {
            WriteExceptionToLog(exception, this.IncludeCallerDetails, callerFilePath, callerMemberName, callerLineNumber);
        }

        /// <summary>
        /// Writes an exception to the output.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough()]
        public void WriteExceptionToLog(Exception exception, bool includeCallerDetails, [CallerFilePath]string callerFilePath = "", [CallerMemberName]string callerMemberName = "", [CallerLineNumber]int callerLineNumber = 0)
        {
            WriteToLog(string.Concat(MESSAGE_EXCEPTION, Environment.NewLine, exception.ToString()), includeCallerDetails, callerFilePath, callerMemberName, callerLineNumber);
        }

        /// <summary>
        /// Writes an application start message to the output.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough()]
        public void WriteStartMessageToLog()
        {
            WriteToLog(MESSAGE_START, false);
        }

        /// <summary>
        /// Writes an application stop message to the output.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough()]
        public void WriteStopMessageToLog()
        {
            WriteToLog(MESSAGE_STOP, false);
        }

        /// <summary>
        /// Creates a new instance of the logger using the same parameters except for the new logFileId passed in.
        /// </summary>
        /// <param name="logFileId">The ID used to identify the new log file.</param>
        /// <returns></returns>
        [System.Diagnostics.DebuggerStepThrough()]
        public Logger CreateLogger(string logFileId)
        {
            return new Logger(logFileId, _logFileDirectoryPath, _logFileType, _logFileDateTime, false, logFileId, _includeCallerDetails);
        }


        private string PrepareValueForLogging(string value, bool includeCallerDetails, string callerFilePath, string callerMemberName, int callerLineNumber)
        {
            string callerDetails = string.Empty;
            if (includeCallerDetails)
            {
                callerDetails = BuildCallerDetailPrefix(callerFilePath, callerMemberName, callerLineNumber);
            }
            return string.Concat(DateTime.Now.ToString(TIME_STAMP_DATE_FORMAT), DELIMITER, callerDetails, value);
        }

        private string BuildCallerDetailPrefix([CallerFilePath]string callerFilePath = "", [CallerMemberName]string callerMemberName = "", [CallerLineNumber]int callerLineNumber = 0)
        {
            return string.Concat(Path.GetFileName(callerFilePath), "_", callerMemberName, " line ", callerLineNumber, DELIMITER);
        }

        private string DetermineLogFileDateTime(FileType logFileType)
        {
            string logFileDateTime = string.Empty;

            // Determine the date time to be applied for the file.
            switch (logFileType)
            {
                case FileType.PerDay:
                    logFileDateTime = DateTime.Now.ToString(PER_DAY_LOG_FILE_DATE_FORMAT);
                    break;
                default:
                    logFileDateTime = DateTime.Now.ToString(PER_RUN_LOG_FILE_DATE_FORMAT);
                    break;
            }

            return logFileDateTime;
        }

        private void SetUpLogFilePath(bool isInitialSetUp)
        {
            string logFileIdToApply = string.Empty;
            string newLogFilePath = string.Empty;
            bool logFilePathChanged = false;
            bool directoryExists = false;

            // Build the log file ID to apply, if needed.  The underscore provides a visual separation in the file name.
            if (!string.IsNullOrEmpty(_logFileId))
            {
                logFileIdToApply = string.Concat("_", _logFileId);
            }

            // Build the log file path, and determine if it changed.
            newLogFilePath = Path.Combine(_logFileDirectoryPath, string.Concat(_logFileDateTime, logFileIdToApply, LOG_FILE_EXTENSION));
            logFilePathChanged = _logFilePath != newLogFilePath;
            _logFilePath = newLogFilePath;

            // Create the directory needed to store the log file, if needed.
            // NOTE: This must be done before the TandTTextWriterTraceListener is set up.
            if (!Directory.Exists(_logFileDirectoryPath))
            {
                try
                {
                    Directory.CreateDirectory(_logFileDirectoryPath);
                    directoryExists = true;
                }
                catch (Exception)
                {
                    // The directory could not be created.  We need a directory.  Ignore the exception as we can't do much about this.
                    directoryExists = false;
                }
            }
            else
            {
                directoryExists = true;
            }

            // If the log file path changed, reset the file trace listener.
            if (directoryExists && (isInitialSetUp || logFilePathChanged))
            {
               // ResetTandTTextWriterTraceListener();
            }
        }

        //private void ResetTandTTextWriterTraceListener()
        //{
        //    if (_fileTraceListener != null)
        //    {
        //        // Remove the listener from the trace source if needed.
        //        if (_traceSource != null && _traceSource.Listeners.Contains(_fileTraceListener))
        //        {
        //            _traceSource.Listeners.Remove(_fileTraceListener);
        //        }

        //        // Close and dispose of the listener.
        //        _fileTraceListener.Close();
        //        _fileTraceListener.Dispose();
        //    }

        //    // Create a new file trace listener.
        //    _fileTraceListener = new TandTTextWriterTraceListener(this.LogFilePath);
        //    if (_fileTraceListener.Writer == null)
        //    {
        //        // The .Writer will be null if the app process account does not have permission to write the log file to the directory.
        //        _hasPermissionToWriteToFile = false;
        //    }
        //    else
        //    {
        //        ((System.IO.StreamWriter)_fileTraceListener.Writer).AutoFlush = true;
        //        _hasPermissionToWriteToFile = true;
        //    }

        //    // Add the listener to the trace source.
        //    _traceSource.Listeners.Add(_fileTraceListener);
        //}

    }

}
