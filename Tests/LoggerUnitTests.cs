using Microsoft.VisualStudio.TestTools.UnitTesting;
using Guard_Emulator;
using System.Xml.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System;
using System.Text;

namespace UnitTests
{
    [TestClass]
    public class loggerUnitTests
    {
        string solution_dir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;


        [TestMethod]
        public void LoggerInitialisation()    // 
        {
            Logger logger = Logger.Instance;
            logger.Initialise(Facility.Local0, "127.0.0.1", "testGuard", 514);

            Assert.IsTrue(logger.IsInitialised);

            logger.Stop();
        }

        [TestMethod]
        public void LoggerSingletonConfirmation()    // 
        {
            Logger logger = Logger.Instance;
            Logger xlogger = Logger.Instance;
            Logger ylogger = Logger.Instance;

            Assert.AreSame(logger, xlogger);
            Assert.AreSame(logger, ylogger);
            Assert.AreSame(ylogger, xlogger);

            logger.Stop();
        }

        [TestMethod]
        public void LoggerSendLegacyMessage()
        {
            // Need a UDP listener
            byte[] data = new byte[1024];
            IPAddress ip;
            if (!IPAddress.TryParse("127.0.0.1", out ip))
            {
                throw new FormatException("Invalid ip-adress");
            }
            IPEndPoint udpServer = new IPEndPoint(ip, 514);
            UdpClient server = new UdpClient(udpServer);

            // Initialise Logger
            Logger logger = Logger.Instance;
            logger.Initialise(Facility.Local0, "127.0.0.1", "testLogger", 514, 0);

            // Test
            logger.Alert("Test Message");

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            data = server.Receive(ref sender);
            string message = Encoding.ASCII.GetString(data, 0, data.Length);
            Console.WriteLine("Message received from {0}:", sender.ToString());
            Console.WriteLine(message);

            // Extract the message text
            string[] content = message.Split(' ');
            string received = String.Format("{0} {1} {2} {3}", content[3], content[4],content[5], content[6]);

            Assert.AreEqual("127.0.0.1 testLogger: Test Message", received , "Mangled syslog message");
        }

        [TestMethod]
        public void LoggerSendV1Message()
        {
            // Need a UDP listener
            byte[] data = new byte[1024];
            IPAddress ip;
            if (!IPAddress.TryParse("127.0.0.1", out ip))
            {
                throw new FormatException("Invalid ip-adress");
            }
            IPEndPoint udpServer = new IPEndPoint(ip, 514);
            UdpClient server = new UdpClient(udpServer);

            // Initialise Logger
            Logger logger = Logger.Instance;
            logger.Initialise(Facility.Local0, "127.0.0.1", "testLogger", 514, 1);

            // Test
            logger.Alert("Test Message");

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            data = server.Receive(ref sender);
            string message = Encoding.ASCII.GetString(data, 0, data.Length);
            Console.WriteLine("Message received from {0}:", sender.ToString());
            Console.WriteLine(message);

            // Extract the message text
            string[] content = message.Split(' ');
            string received = String.Format("{0} {1} {2} {3} {4} {5}", content[2], content[3], content[5], content[6], content[7], content[8]);

            Assert.AreEqual("127.0.0.1 testLogger - - Test Message", received, "Mangled syslog message");
        }
    }
}


