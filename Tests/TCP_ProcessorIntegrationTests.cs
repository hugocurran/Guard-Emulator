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
using Tests;


namespace UnitTests
{
    [TestClass]
    public class TCP_ProcessorIntegrationTests
    {
        // All testing done on loopback if
        string upstreamPort = "127.0.0.1:5556"; // The port the guard listens on for messages from the upstream
        string downstreamPort = "127.0.0.1:5555";    // The port the guard connects to for messages to the downstream


        #region HPSD over TCP basic test

        [TestMethod]
        public void HPSD_TCP_ProcessorBasicSocketToSocketCopy()
        {
            XDocument testPolicy = Harness.CreateEmptyPolicy();

            // Processor must run in its own cancellable task
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            OspProtocol protocol = OspProtocol.HPSD_TCP;

            // We need an initialised logger object
            Logger logger = Logger.Instance;
            logger.Initialise(Facility.Local1, "127.0.0.1", "testGuard");

            // Start the Processor thread
            var processorTask = Task.Run(() =>
            {
                var processorObj = ProcessorFactory.Create(upstreamPort, downstreamPort, protocol, testPolicy, token);
            }, token);

            // Now connect to the Guard as an upstream proxy
            TcpClient client = new TcpClient();
            ConnectUpstream(client, upstreamPort);
            NetworkStream up = client.GetStream();

            // Connect the Guard downstream
            TcpListener mesgServer = new TcpListener(Harness.EndPoint(downstreamPort)) { ExclusiveAddressUse = true };
            mesgServer.Start(1);
            TcpClient server = ConnectDownstream(mesgServer);
            NetworkStream down = server.GetStream();

            // Send some test messages
            int counter = 0;
            int received = 0;
            byte[] testData;
            byte[] message = null;
            while (counter < 25)
            {
                testData = Harness.HPSD_StatusMessage(counter);
                WriteMessage(testData, up);
                counter++;
                Thread.Sleep(60);

                message = ReadMessage(down);

                Assert.IsTrue(message.SequenceEqual(testData));
                received++;
            }
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
                server.Close();
                client.Close();
                up.Dispose();
                down.Dispose();
                mesgServer.Stop();
            }
        }

        #endregion

        /// <summary>
        /// Connect to the guard upstream server
        /// </summary>
        /// <param name="client">TcpClient reference</param>
        /// <param name="upstreamPort">IPaddr:port the guard is listening on</param>
        private void ConnectUpstream(TcpClient client, string upstreamPort)
        {
            client.ExclusiveAddressUse = true;
            client.SendBufferSize = 8192;
            client.NoDelay = true;
            // client.SendTimeout = 100;
            do
            {
                try
                {
                    client.Connect(Harness.EndPoint(upstreamPort));
                    if (!client.Connected)
                        Thread.Sleep(10000);  // Retry every 10 seconds
                }
                catch (SocketException)
                {
                    // should filter out not available errors only
                }
            } while (!client.Connected);
        }

        /// <summary>
        /// Connect to the Guard downstream client
        /// </summary>
        /// <param name="mesgServer">TcpListener reference</param>
        /// <returns>TcpClient instance for communication with the proxy</returns>
        private TcpClient ConnectDownstream(TcpListener mesgServer)
        {
            mesgServer.Server.Poll(1, SelectMode.SelectError);
            // This will block until the client connects
            TcpClient server = mesgServer.AcceptTcpClient();
            server.NoDelay = true;
            server.ReceiveBufferSize = 8192;
            return server;
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
                    _read = _read + stream.Read(message, 0, length);
                }
                return message;
            }
            catch (IOException)
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
            catch (IOException)
            {
                // Error message needed
                return 0;
            }
        }
    }
}
