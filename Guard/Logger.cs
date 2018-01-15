using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Guard_Emulator
{
    /// <summary>
    /// Thread safe logger to be shared amongst each processor
    /// </summary>
    public class Logger
    {
        private static Logger instance = null;
        private Facility facility;
        private string process;
        private SyslogClient syslog;

        public bool IsInitialised = false;

        private Logger() { }

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
        public void Initialise(Facility facility, string logServerIp, int logServerPort = 514, string process = "guard")
        {
            if (!IsInitialised)
            {
                this.facility = facility;
                this.process = process;
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
            message = process + ": " + message;
            await syslog.SendAsync(new SyslogMessage((int)facility, (int)level, message));           
        }
    }
}
