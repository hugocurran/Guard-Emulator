// Copyright Peter Curran (peter@curran.org.uk) 2017

using System.Diagnostics;
using System.Threading.Tasks;

namespace Hugo.Utility.Syslog
{
    /// <summary>
    /// Thread safe logger
    /// </summary>
    public class Logger
    {
        private static Logger instance = null;
        private Facility facility;
        private string appName;
        private string procId;
        private int version;
        private SyslogClient syslog;

        public bool IsInitialised { get; private set; }

        // Singleton private constructor
        private Logger()
        {
            IsInitialised = false;
        }

        /// <summary>
        /// Reference to the Logger
        /// </summary>
        public static Logger Instance
        {
            get
            {
                if (instance == null)
                    instance = new Logger();
                return instance;
            }
        }

        /// <summary>
        /// Initialise Logger
        /// </summary>
        /// <param name="facility">Default facility</param>
        /// <param name="logServerIp">IP address of the syslog server</param>
        /// <param name="logServerPort">UDP port of the syslog server</param>
        /// <param name="appName">Application name</param>
        /// <param name="syslogVersion">Default = 1 (RFC 5424); set = 0 for legacy RFC 3164</param>
        public void Initialise(Facility facility, string logServerIp, string appName, int logServerPort = 514, int syslogVersion = 1)
        {
            if (!IsInitialised)
            {
                this.facility = facility;
                this.appName = appName;
                version = syslogVersion;
                procId = Process.GetCurrentProcess().Id.ToString();
                syslog = new SyslogClient(logServerIp, logServerPort);
                IsInitialised = true;
            }
        }

        /// <summary>
        /// Stop the logger
        /// </summary>
        public void Stop()
        {
            if (IsInitialised)
                syslog.Close();
        }

        /// <summary>
        /// Log a message at Emergency level
        /// </summary>
        /// <param name="message">Message to log</param>
        public async void Emergency(string message)
        {
            await Log(facility, Level.Emergency, message);
        }

        /// <summary>
        /// Log a message at Alert level
        /// </summary>
        /// <param name="message">Message to log</param>
        public async void Alert(string message)
        {
            await Log(facility, Level.Alert, message);
        }

        /// <summary>
        /// Log a message at Critical level
        /// </summary>
        /// <param name="message">Message to log</param>
        public async void Critical(string message)
        {
            await Log(facility, Level.Critical, message);
        }

        /// <summary>
        /// Log a message at Error level
        /// </summary>
        /// <param name="message">Message to log</param>
        public async void Error(string message)
        {
            await Log(facility, Level.Error, message);
        }

        /// <summary>
        /// Log a message at Warning level
        /// </summary>
        /// <param name="message">Message to log</param>
        public async void Warning(string message)
        {
            await Log(facility, Level.Warning, message);
        }

        /// <summary>
        /// Log a message at Notice level
        /// </summary>
        /// <param name="message">Message to log</param>
        public async void Notice(string message)
        {
            await Log(facility, Level.Notice, message);
        }

        /// <summary>
        /// Log a message at Information level
        /// </summary>
        /// <param name="message">Message to log</param>
        public async void Information(string message)
        {
            await Log(facility, Level.Information, message);
        }

        /// <summary>
        /// Log a message at Debug level
        /// </summary>
        /// <param name="message">Message to log</param>
        public async void Debug(string message)
        {
            await Log(facility, Level.Debug, message);
        }

        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="facility">Syslog facility</param>
        /// <param name="level">Syslog level</param>
        /// <param name="message">Message text</param>
        /// <returns></returns>
        public async Task Log(Facility facility, Level level, string message)
        {
#if DEBUG 
            Console.WriteLine(message);
#endif
            if (version == 0)   //RFC 3614 (legacy syslog)
            {
                message = appName + ": " + message;
                await syslog.SendAsync(new SyslogMessage(facility, level, message, version, procId));
            }
        }
    }
}
