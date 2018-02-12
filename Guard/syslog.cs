// Copyright Peter Curran (peter@curran.org.uk) 2017

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Guard_Emulator
{
    /// <summary>
    /// Syslog Message Severities - per RFC 5424
    /// <see cref="https://tools.ietf.org/html/rfc5424/">Legacy RFC 3164 values are compatible</see>
    /// </summary>
    public enum Level
    {
        /// <summary>
        /// Emergency: system is unusable
        /// </summary>
        Emergency = 0,
        /// <summary>
        /// Alert: action must be taken immediately
        /// </summary>
        Alert = 1,
        /// <summary>
        /// Critical: critical conditions
        /// </summary>
        Critical = 2,
        /// <summary>
        /// Error: error conditions
        /// </summary>
        Error = 3,
        /// <summary>
        /// Warning: warning conditions
        /// </summary>
        Warning = 4,
        /// <summary>
        /// Notice: normal but significant condition
        /// </summary>
        Notice = 5,
        /// <summary>
        /// Informational: informational messages
        /// </summary>
        Information = 6,
        /// <summary>
        /// Debug: debug-level messages
        /// </summary>
        Debug = 7
    }

    /// <summary>
    /// Syslog Message Facility - per RFC 5424
    /// <see cref="https://tools.ietf.org/html/rfc5424/">Legacy RFC 3164 values are compatible</see>
    /// </summary>
    public enum Facility
    {
        /// <summary>
        /// kernel messages
        /// </summary>
        Kernel = 0,
        /// <summary>
        /// user-level messages
        /// </summary>
        User = 1,
        /// <summary>
        /// mail systems
        /// </summary>
        Mail = 2,
        /// <summary>
        /// system daemons
        /// </summary>
        Daemon = 3,
        /// <summary>
        /// security/authorisation messages
        /// </summary>
        Auth = 4,
        /// <summary>
        /// messages generated internally by syslogd
        /// </summary>
        Syslog = 5,
        /// <summary>
        /// line printer subssystem
        /// </summary>
        Lpr = 6,
        /// <summary>
        /// network news subsystem
        /// </summary>
        News = 7,
        /// <summary>
        /// UUCP subsystem
        /// </summary>
        UUCP = 8,
        /// <summary>
        /// clock daemon
        /// </summary>
        Cron = 9,
        /// <summary>
        /// security/authorization messages
        /// </summary>
        AuthPriv = 10,
        /// <summary>
        /// FTP daemon
        /// </summary>
        FTP = 11,
        /// <summary>
        /// FTP daemon
        /// </summary>
        NTP = 12,
        /// <summary>
        /// log audit
        /// </summary>
        audit = 13,
        /// <summary>
        /// log alert
        /// </summary>
        alert = 14,
        /// <summary>
        /// clock daemon
        /// </summary>
        Clock = 15,
        /// <summary>
        /// local use 0  
        /// </summary>
        Local0 = 16,
        /// <summary>
        /// local use 1
        /// </summary>
        Local1 = 17,
        /// <summary>
        /// local use 2
        /// </summary>
        Local2 = 18,
        /// <summary>
        /// local use 3
        /// </summary>
        Local3 = 19,
        /// <summary>
        /// local use 4
        /// </summary>
        Local4 = 20,
        /// <summary>
        /// local use 5
        /// </summary>
        Local5 = 21,
        /// <summary>
        /// local use 6
        /// </summary>
        Local6 = 22,
        /// <summary>
        /// local use 7
        /// </summary>
        Local7 = 23
    }

    /// <summary>
    /// A syslog message
    /// </summary>
    public class SyslogMessage
    {
        /// <summary>
        /// Facility
        /// </summary>
        public Facility Facility { get; private set; }
        /// <summary>
        /// Level
        /// </summary>
        public Level Level { get; private set; }
        /// <summary>
        /// Message text
        /// </summary>
        public string Text { get; private set; }
        /// <summary>
        /// Syslog version (1 = RFC 5424; 0 = RFC 3164 (legacy))
        /// </summary>
        public int Version { get; private set; }
        /// <summary>
        /// Device or application name (default = NILVALUE)
        /// </summary>
        public string AppName { get; private set; }
        /// <summary>
        /// Process ID (default = NILVALUE)\n
        /// Note used in legacy syslog
        /// </summary>
        public string ProcID { get; private set; }

        public string MsgID { get; private set; }

        public string StructuredData { get; private set; }

        /// <summary>
        /// Create a syslog message
        /// </summary>
        /// <param name="facility">facility </param>
        /// <param name="level">level</param>
        /// <param name="text">message text</param>
        /// <param name="version">version=1 (RFC 5424) is default; for legacy RFC 3164 set version=0</param>
        /// <param name="appName">APP-NAME value (default = NILVALUE)</param>
        /// <param name="procId">PROCID value (default = NILVALUE)</param>
        public SyslogMessage(Facility facility, 
            Level level, 
            string text, 
            int version = 1, 
            string appName="NILVALUE", 
            string procId="NILVALUE", 
            string msgId = "NILVALUE",
            string structData = "NILVALUE")
        {
            Facility = facility;
            Level = level;
            Text = text;
            Version = version;
            AppName = appName;
            ProcID = procId;
            MsgID = msgId;
            StructuredData = structData;
        }

        internal string GetMessage()
        {
            if (Version == 0)
            {
                if (AppName == "NILVALUE")
                    return Text;
                else
                    return String.Format("{0}: {1}", AppName, Text);
            }

            string appName = (AppName == "NILVALUE") ? "-" : AppName;
            string procId = (ProcID == "NILVALUE") ? "-" : ProcID;
            string msgId = (MsgID == "NILVALUE") ? "-" : MsgID;
            string structData = (StructuredData == "NILVALUE") ? "-" : StructuredData;

            return String.Format("{0} {1} {2} {3} {4}",
                appName,
                procId,
                msgId,
                structData,
                Text
                );
        }


    }

    /// <summary>
    /// The syslog client - NOT thread safe unless used with Logger singeton
    /// </summary>
    public class SyslogClient
    {
        private UdpClient syslogClient;
        private string hostname;
        private IPEndPoint ipLocalEndpoint;

        /// <summary>
        /// RFC 5426 Syslog sender (UDP)
        /// </summary>
        /// <param name="logServerIp">IP address of the logging server</param>
        /// <param name="logServerPort">UDP port number</param>
        public SyslogClient(string logServerIp, int logServerPort, string FQDN = "")
        {
            // Per RFC 5424, if the FQDN is not supplied then use an IP address
            if (FQDN == "")
            {
                // Use trivial UDP hack to work out which IP address we will send log messages from
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect(logServerIp, 65530);
                    ipLocalEndpoint = socket.LocalEndPoint as IPEndPoint;
                    hostname = ipLocalEndpoint.Address.ToString();
                }
            }
            else
                hostname = FQDN;

            try
            {
                syslogClient = new UdpClient(logServerIp, logServerPort)
                {
                    DontFragment = false  // RFC 5426
                };
                syslogClient.Client.SendBufferSize = 1440;  // RFC 5426 (MTU - IP/UDP headers)
            }
            catch (Exception e)
            {
                Console.WriteLine("SyslogClient initialisation exception: {0}", e.Message);
            }
        }

        /// <summary>
        /// Dispose of the syslog client
        /// </summary>
        public void Close()
        {
            syslogClient.Close();
        }

        /// <summary>
        /// Asynchronously send a syslog message
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <returns>Task object</returns>
        public async Task SendAsync(SyslogMessage message)
        {
            int priority = (int)message.Facility * 8 + (int)message.Level;

            string msg;
            if (message.Version == 0)
            {
                    msg = String.Format("<{0}>{1} {2} {3}",
                                                      priority,
                                                      LegacyTimestamp(DateTime.Now),
                                                      hostname,
                                                      message.GetMessage());
            }
            else
            {
                msg = String.Format("<{0}>1 {1} {2} {3}",
                                  priority,
                                  DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffzzz"),
                                  hostname,
                                  message.GetMessage());
            }
            byte[] bytes = Encoding.ASCII.GetBytes(msg);

            await syslogClient.SendAsync(bytes, bytes.Length);
        }

        private string LegacyTimestamp(DateTime timestamp)
        {
            string day = (Convert.ToInt32(timestamp.ToString("%d")) < 10) ?
                string.Format(" {0}", timestamp.ToString("%d")) :
                string.Format("{0}", timestamp.ToString("%d"));

            return timestamp.ToString("MMM") + " " + day + " " + timestamp.ToString("HH:mm:ss");
        }
    }
}
