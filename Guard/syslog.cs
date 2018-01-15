﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Guard_Emulator
{
    /// <summary>
    /// Syslog Level - per RFC 3164
    /// </summary>
    public enum Level
    {
        Emergency = 0,
        Alert = 1,
        Critical = 2,
        Error = 3,
        Warning = 4,
        Notice = 5,
        Information = 6,
        Debug = 7
    }

    /// <summary>
    /// Syslog Facility - per RFC 3164
    /// </summary>
    public enum Facility
    {
        Kernel = 0,
        User = 1,
        Mail = 2,
        Daemon = 3,
        Auth = 4,
        Syslog = 5,
        Lpr = 6,
        News = 7,
        UUCP = 8,
        Cron = 9,
        AuthPriv = 10,
        FTP = 11,
        Local0 = 16,
        Local1 = 17,
        Local2 = 18,
        Local3 = 19,
        Local4 = 20,
        Local5 = 21,
        Local6 = 22,
        Local7 = 23
    }

    /// <summary>
    /// A syslog message
    /// </summary>
    public class SyslogMessage
    {
        public int Facility;
        public int Level;
        public string Text;

        public SyslogMessage() { }

        public SyslogMessage(int facility, int level, string text)
        {
            this.Facility = facility;
            this.Level = level;
            this.Text = text;
        }
    }

    /// <summary>
    /// The syslog client - thread safe
    /// </summary>
    class SyslogClient
    {
        private UdpClient syslogClient;
        private IPEndPoint ipLocalEndpoint;

        public SyslogClient(string logServerIp, int logServerPort)
        {
            // Use trivial UDP hack to work out which IP address we will send log messages from
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect(logServerIp, 65530);
                ipLocalEndpoint = socket.LocalEndPoint as IPEndPoint;
            }

            try
            {
                syslogClient = new UdpClient(logServerIp, logServerPort);
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
            int priority = message.Facility * 8 + message.Level;

            string msg = String.Format("<{0}>{1} {2} {3}",
                                              priority,
                                              DateTime.Now.ToString("MMM dd HH:mm:ss"),
                                              ipLocalEndpoint.Address.ToString(),
                                              message.Text);

            byte[] bytes = Encoding.ASCII.GetBytes(msg);

            await syslogClient.SendAsync(bytes, bytes.Length);
        }
    }
}
