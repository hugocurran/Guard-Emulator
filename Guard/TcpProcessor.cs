using System;
using System.Threading;
using System.Xml.Linq;
using NetMQ;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Linq;

namespace Guard_Emulator
{
    class TcpProcessor : Processor
    {
        /// <summary>
        /// Null Processor object for unit testing only
        /// </summary>
        internal TcpProcessor() { }

        /// <summary>
        /// Guard path processor using TCP
        /// </summary>
        /// <param name="upstreamPort">Address:Port for the upstream socket; guard listens on this</param>
        /// <param name="downstreamPort">Address:Port for the downstream socket; guard connects to this</param>
        /// <param name="osp">OSP message protocol</param>
        /// <param name="policy">Policy ruleset to apply</param>
        /// <param name="token">Cancellation token</param>
        public TcpProcessor(string upstreamPort, string downstreamPort, OspProtocol osp, XDocument policy, CancellationToken token) : base()
        {
            // Get identity
            this.id = WhoAmI(policy);

            // Create a timer to check for task cancellation
            var timer = new NetMQTimer(TimeSpan.FromMilliseconds(500));
            timer.Elapsed += (sender, args) => { token.ThrowIfCancellationRequested(); };

            using (var poller = new NetMQPoller { timer })
            {
                // Start monitoring the cancellation token
                poller.RunAsync();

                // Setup connection to upstream proxy
                TcpListener mesgServer = new TcpListener(EndPoint(upstreamPort)) { ExclusiveAddressUse = true };
                logger.Information(id + "Listening on " + upstreamPort);
                mesgServer.Start(1);
                TcpClient server = ConnectUpstream(mesgServer);
                logger.Information(id + "Upstream connected on " + upstreamPort);
                NetworkStream upstream = server.GetStream();

                // Setup connection to downstream proxy
                logger.Information(id + "Connecting to " + downstreamPort);
                TcpClient client = ConnectDownstream(downstreamPort);
                NetworkStream downstream = client.GetStream();
                logger.Information(id + "Downstream connected on " + downstreamPort);

                // Message processing loop
                InternalMessage iMesg = null;
                byte[] message = null;
                logger.Debug(id + "Starting message loop...");
                while (true)
                {
                    while (client.Connected && server.Connected)
                    {
                        message = ReadMessage(upstream);
                        if (message != null)
                        {
                            logger.Debug(id + "Message read from: " + upstreamPort);
                            switch (osp)
                            {
                                case OspProtocol.HPSD_TCP:
                                    iMesg = HpsdParser.ParseMessage(HpsdMessage.Parser.ParseFrom(message));
                                    break;

                                case OspProtocol.WebLVC_TCP:
                                    iMesg = WeblvcParser.ParseMessage(message);
                                    break;
                            }
                            logger.Information(id + "Message: " + iMesg.ToString());
                            if (ApplyPolicy(iMesg, policy))
                            {
                                logger.Information(id + "Valid message Sequence: " + iMesg.SequenceNumber + " Rule: " + ruleNumber);
                                if (WriteMessage(message, downstream) == message.Length)
                                {
                                    logger.Debug(id + "Message written to downstream Sequence: " + iMesg.SequenceNumber);
                                }
                                else    // Write timeout returns message length == 0
                                {
                                    // Check we are still actually connected - should reset client.Connected state
                                    client.Client.Poll(1, SelectMode.SelectWrite);
                                    logger.Warning(id + "Downstream Disconnected #1");
                                }
                            }
                            else
                            {
                                // Message does not comply with policy
                                logger.Alert(id + "Invalid message Sequence: " + iMesg.SequenceNumber);
                                continue;
                            }
                        }
                        else  // A read timeout will put us here
                        {
                            // Check we are still actually connected
                            if (server.Client.Poll(1, SelectMode.SelectRead) && !upstream.DataAvailable)
                            {
                                // We have been disconnected
                                logger.Warning(id + "Upstream disconnected #1");
                                server.Close(); // Dispose the old connection
                            }
                            else  // Nah - just a timeout
                            {
                                //Console.WriteLine(id + "Read Timeout");
                                continue;
                            }
                        }
                    }
                    // One of the connections has died, so reset and start again
                    // Check the upstream first
                    bool upstreamReconnected = false;
                    if ((!server.Connected) || (server.Client.Poll(1, SelectMode.SelectRead) && !upstream.DataAvailable))
                    {
                        // If the upstream has died, keep the downstream connection and restart upstream
                        logger.Warning(id + "Upstream disconnected #2");
                        server.Close(); // Dispose the old connection
                        upstream.Dispose();
                        server = ConnectUpstream(mesgServer);
                        upstream = server.GetStream();
                        upstreamReconnected = true;
                        logger.Information(id + "Upstream reconnected");
                    }
                    // Check the downstream
                    if ((!client.Connected) || (!client.Client.Poll(1, SelectMode.SelectWrite)))
                    {
                        // Downstream has died, close upstream and restart
                        logger.Warning (id + "Downstream disconnected #2");
                        client.Close();
                        downstream.Dispose();
                        client = ConnectDownstream(downstreamPort);
                        downstream = client.GetStream();
                        logger.Information(id + "Downstream reconnected");

                        // Only restart the upstream if it has not been restarted
                        if (!upstreamReconnected && client.Connected)
                        {
                            server.Close();
                            upstream.Dispose();                        
                            server = ConnectUpstream(mesgServer);
                            upstream = server.GetStream();
                            logger.Information(id + "Upstream reconnected");
                            // Log an event message
                        }
                    }
                }   // End of infinite loop
            }
        }

        /// <summary>
        /// Connect to the downstream proxy
        /// </summary>
        /// <param name="client">TcpClient reference</param>
        /// <param name="downstreamPort">IPaddr:port the downstream is listening on</param>
        //private void ConnectDownstream(TcpClient client, string downstreamPort)
        private TcpClient ConnectDownstream(string downstreamPort)
        {
            TcpClient client = null;
            do
            {
                try
                {
                    client = new TcpClient(AddressFamily.InterNetwork);
                    client.Connect(EndPoint(downstreamPort));
                    // Console.WriteLine(id + "Connect call returned {0}", downstreamPort);
                }
                catch (SocketException e)
                {
                    logger.Debug(id + "loop exception: " + e.Message);
                    // should filter out not available errors only
                    Thread.Sleep(10000);  // Retry every 10 seconds
                }
            } while (!client.Connected);

            client.NoDelay = true;
            client.SendTimeout = 10;
            return client;
        }

        /// <summary>
        /// Connect to the upstream proxy
        /// </summary>
        /// <param name="mesgServer">TcpListener reference</param>
        /// <returns>TcpClient instance for communication with the proxy</returns>
        private TcpClient ConnectUpstream(TcpListener mesgServer)
        {
            // This will block until the upstream connects
            TcpClient server = mesgServer.AcceptTcpClient();
            server.NoDelay = true;
            server.ReceiveTimeout = 1000;   //  second timeout on reads
            return server;
        }

        /// <summary>
        /// Read a prefix delimited message from a network stream
        /// </summary>
        /// <param name="stream">NetworkStream to read</param>
        /// <returns>A message or null</returns>
        private byte[] ReadMessage(NetworkStream stream)
        {
            // Read timout is set on the socket which will throw IOException
            try
            {
                // read the first 4 bytes as an int32
                byte[] prefix = new byte[4];
                int _read = 0;
                while (_read < 4)
                {                    
                    _read += stream.Read(prefix, 0, 4);
                    if (_read == 0)  // No more data available (disconnected)
                    {
                        logger.Debug(id + "Disconnect detected");
                        throw new IOException(id + "I/O error occurred.");
                    }
                }
                Int32 length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(prefix, 0));

                // Read the message using the length prescribed
                byte[] message = new byte[length];
                _read = 0;
                while (_read < length)
                {                    
                    _read += stream.Read(message, 0, length);
                    if (_read == 0)  // No more data available (disconnected)
                    {
                        logger.Debug(id + "Disconnect detected");
                        throw new IOException(id + "I/O error occurred.");
                    }
                }
                return message;
            }
            catch (IOException)
            {
                // Read has timed out or disconnected
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
            // Write timeout is set on the socket which should throw an IOException
            try
            {
                // Determine the message length
                Int32 length = IPAddress.HostToNetworkOrder(message.Length);
                byte[] prefix = BitConverter.GetBytes(length);
                // Send the prefix followed by the message
                stream.Write(prefix.Concat(message).ToArray<byte>(), 0, message.Length + 4);
                return message.Length;
            }
            catch (IOException)
            {
                logger.Debug(id + "Write error (disconnect detected?)");
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
            // addrPort comes from FPDL in the form <IP Address>:<Port>
            string[] parts = addrPort.Split(":");
            IPAddress ipAddress = IPAddress.Parse(parts[0]);
            Int32 port = Convert.ToInt32(parts[1]);
            return new IPEndPoint(ipAddress, port);
        }
    }
}

    
    


