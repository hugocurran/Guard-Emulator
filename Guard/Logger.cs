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
        private Facility facility = Facility.Local0;
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
        /// Initialise Logger facility
        /// </summary>
        /// <param name="logServerIp">IP address of the syslog server</param>
        /// <param name="logServerPort">UDP port of the syslog server</param>
        public void Initialise(string logServerIp, int logServerPort = 514)
        {
            syslog = new SyslogClient(logServerIp, logServerPort);
            IsInitialised = true;
        }

        /// <summary>
        /// Stop the logger
        /// </summary>
        public void Stop()
        {
            if (IsInitialised)
                syslog.Close();
        }

        public async void Log(Level level, string message)
        {
            await syslog.SendAsync(new SyslogMessage((int)facility, (int)level, message));           
        }
    }
}
