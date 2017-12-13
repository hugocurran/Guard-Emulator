using System;
using System.Text;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetMQ;
using NetMQ.Sockets;
using System.Threading.Tasks;
using System.Threading;
using Guard_Emulator;
using Google.Protobuf;
using System.Xml.Linq;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace UnitTests
{
    [TestClass]
    public class TCP_ProcessorUnitTests
    {
        // All testing done on loopback if
        string upstreamPort = "127.0.0.1:5556"; // The port the guard listens on for messages from the upstream
        string downstreamPort = "127.0.0.1:5555";    // The port the guard connects to for messages to the downstream



        [TestMethod]
        public void ProcessorBasicTCPSocketToSocketCopy()
        {
            // Create an empty policy file
            XDocument testPolicy = new XDocument();
            XElement emptyPolicy =
                new XElement("exportPolicy",
                    new XElement("rule",
                        new XAttribute("ruleNumber", "1"),
                        new XElement("federate", "*"),
                        new XElement("entity", "*"),
                        new XElement("objectName", "*"),
                        new XElement("attributeName", "*"))
            );
            testPolicy.Add(emptyPolicy);

            // Processor must run in its own cancellable task
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            OspProtocol protocol = OspProtocol.HPSD_TCP;

            // Start the Processor thread
            var processorTask = Task.Run(() =>
            {
                var processorObj = ProcessorFactory.Create(upstreamPort, downstreamPort, protocol, testPolicy, token);
            }, token);

            // Wait for processor thread to stabilise
            //Thread.Sleep(500);

            // Create a downstream listener for the Guard to connect to
            TcpListener server = new TcpListener(EndPoint(downstreamPort))
            {
                ExclusiveAddressUse = true
            };
            server.Start(1);

            // This will block until the guard connects
            var mesgServer = server.AcceptTcpClient();
            mesgServer.NoDelay = true;
            mesgServer.ReceiveBufferSize = 2048;
            var down = mesgServer.GetStream();


            // Now connect to the Guard as an upstream proxy
            var client = new TcpClient()
            {
                ExclusiveAddressUse = true,
                SendBufferSize = 2048,
                NoDelay = true
            };
            // client.SendTimeout = 100;
            while (!client.Connected)
            {
                try
                {
                    client.Connect(EndPoint(downstreamPort));
                }
                catch (SocketException e)
                {
                    // should filter out not available errors only
                }
                // Retry every 10 seconds
                Thread.Sleep(100);
            }
            NetworkStream up = client.GetStream();

            // Send some test messages
            int counter = 0;
            int received = 0;
            byte[] testData;
            byte[] message = null;
            while (counter < 25)
            {
                testData = statusMessage(counter).ToByteArray();
                WriteMessage(testData, up);
                counter++;
                Thread.Sleep(60);

                message = ReadMessage(down);

                Assert.IsTrue(message.SequenceEqual(testData));
                received++;
            }
            // We expect to lose the first message!
            Assert.IsTrue(received == 25);

            // Tidy up by cancelling the Processor task
            try
            {
                tokenSource.Cancel();
            }
            catch (OperationCanceledException) { }
            finally
            {
                tokenSource.Dispose();
            }
        }
            
        

        static HpsdMessage statusMessage(int sequence)
        {
            // Create an HPSD Status message for testing
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            HpsdMessage statusMessage = new HpsdMessage()
            {
                ProtocolVersion = 81,
                SequenceNumber = sequence,
                Timestamp = timeStamp,
                MessageType = HpsdMessage.Types.MessageType.SessionStatus,
                SessionStatus = new SessionStatus()
                {
                    Active = true,
                    SessionName = "ThisSession"
                }
            };
            return statusMessage;
        }

        /// <summary>
        /// Read a prefix delimited message from a network stream
        /// </summary>
        /// <param name="stream">NetworkStream to read</param>
        /// <returns>A message or null</returns>
        private byte[] ReadMessage(NetworkStream stream)
        {
            try
            {
                byte[] prefix = new byte[4];
                int _read = 0;
                while (_read < 4)
                {
                    // read the first 4 bytes as an int32
                    _read = _read + stream.Read(prefix, 0, 4);
                }
                Int32 length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(prefix, 0));

                // Read the message using the length prescribed
                byte[] message = new byte[length];
                _read = 0;
                while (_read < length)
                {
                    stream.Read(message, 0, length);
                }
                return message;
            }
            catch (IOException e)
            {
                // Do something with the error message
                return null;
            }
        }

        /// <summary>
        /// Write a prefix delimited message to a network stream
        /// </summary>
        /// <param name="message">Message to send<param>
        /// <param name="stream">NetworkStream to write</param>
        /// <returns>Length of message or 0 on failure</returns>
        private int WriteMessage(byte[] message, NetworkStream stream)
        {
            try
            {
                // Determine the message length
                Int32 length = IPAddress.HostToNetworkOrder(message.Length);
                byte[] prefix = BitConverter.GetBytes(length);
                // Send the prefix followed by the message
                stream.Write(prefix, 0, 4);
                stream.Write(message, 0, message.Length);
                return message.Length;
            }
            catch (IOException e)
            {
                // Error message needed
                return 0;
            }
        }

        /// <summary>
        /// Create an IPEndpoint from a string
        /// </summary>
        /// <param name="addrPort">IpAddress:Port</param>
        /// <returns>IPEndpoint</returns>
        private IPEndPoint EndPoint(string addrPort)
        {
            // Server comes from FPDL in the form <IP Address>:<Port>
            string[] parts = addrPort.Split(":");
            IPAddress ipAddress = IPAddress.Parse(parts[0]);
            Int32 port = Convert.ToInt32(parts[1]);
            return new IPEndPoint(ipAddress, port);
        }
    }
}
