using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using NetMQ;
using System.Net.Sockets;
using System.Net;
using System.IO;

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
        public TcpProcessor(string upstreamPort, string downstreamPort, OspProtocol osp, XDocument policy, CancellationToken token)
        {
            // Create a timer to check for task cancellation
            var timer = new NetMQTimer(TimeSpan.FromMilliseconds(500));
            timer.Elapsed += (sender, args) => { token.ThrowIfCancellationRequested(); };

            using (var poller = new NetMQPoller { timer })
            {
                // Start monitoring the cancellation token
                poller.RunAsync();

                // Setup connection to downstream proxy
                //TcpClient client = new TcpClient(AddressFamily.InterNetwork);
                TcpClient client = ConnectDownstream(downstreamPort);
                //ConnectDownstream(client, downstreamPort);
                NetworkStream downstream = client.GetStream();

                // Setup connection to upstream proxy
                TcpListener mesgServer = new TcpListener(EndPoint(upstreamPort)) { ExclusiveAddressUse = true };
                mesgServer.Start(1);
                TcpClient server = ConnectUpstream(mesgServer);
                NetworkStream upstream = server.GetStream();

                // Message processing loop
                InternalMessage iMesg = null;
                byte[] message = null;
                while (true)
                {
                    while (client.Connected && server.Connected)
                    {
                        message = ReadMessage(upstream);
                        if (message != null)
                        {
                            Console.WriteLine("Message read");
                            switch (osp)
                            {
                                case OspProtocol.HPSD_TCP:
                                    iMesg = HpsdParser.ParseMessage(HpsdMessage.Parser.ParseFrom(message));
                                    break;

                                case OspProtocol.WebLVC_TCP:
                                    iMesg = WeblvcParser.ParseMessage(message);
                                    break;
                            }
                            if (ApplyPolicy(iMesg, policy))
                            {
                                if (WriteMessage(message, downstream) == message.Length)
                                {
                                    // Log an event message
                                    Console.WriteLine("Message written to downstream");
                                }
                                else    // Write timeout returns message length == 0
                                {
                                    // Check we are still actually connected - should reset client.Connected state
                                    client.Client.Poll(1, SelectMode.SelectWrite);
                                    Console.WriteLine("Downstream Disconnected #1");
                                    // Log an event message
                                }
                            }
                            else
                            {
                                // Log an event message - Message does not comply with policy
                                Console.WriteLine("Message invalid");
                                continue;
                            }
                        }
                        else  // A read timeout will bring us here
                        {
                            // Check we are still actually connected
                            if (server.Client.Poll(1, SelectMode.SelectRead) && !upstream.DataAvailable)
                            {
                                // We have been disconnected
                                // Log an event message
                                Console.WriteLine("Upstream disconnected #1");
                                server.Close(); // Dispose the old connection
                                // Log an event message
                            }
                            else  // Nah - just a timeout
                            {
                                Console.WriteLine("Read Timeout");
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
                        // Log an event message
                        Console.WriteLine("Upstream disconnected #2");
                        server.Close(); // Dispose the old connection
                        upstream.Dispose();
                        server = ConnectUpstream(mesgServer);
                        upstream = server.GetStream();
                        upstreamReconnected = true;
                        // Log an event message
                    }
                    // Check the downstream
                    if ((!client.Connected) || (!client.Client.Poll(1, SelectMode.SelectWrite)))
                    {
                        // Downstream has died, close upstream and restart
                        Console.WriteLine("Downstream disconnected #2");
                        // Log an event message
                        client.Close();
                        downstream.Dispose();
                        client = ConnectDownstream(downstreamPort);
                        downstream = client.GetStream();

                        // Only restart the upstream if it has not been restarted
                        if (!upstreamReconnected && client.Connected)
                        {
                            server.Close();
                            upstream.Dispose();                        
                            // Log an event message
                            server = ConnectUpstream(mesgServer);
                            upstream = server.GetStream();
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
                    if (!client.Connected)
                        Thread.Sleep(10000);  // Retry every 10 seconds
                }
                catch (SocketException e)
                {
                    // should filter out not available errors only
                }
            } while (!client.Connected);
            client.ExclusiveAddressUse = true;
            client.NoDelay = true;
            client.SendTimeout = 50;  // 50 millisecond timeout on writing
            Console.WriteLine("Downstream connected");
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
            Console.WriteLine("Upstream connected");
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
                        throw new IOException("I/O error occurred.");
                }
                Int32 length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(prefix, 0));

                // Read the message using the length prescribed
                byte[] message = new byte[length];
                _read = 0;
                while (_read < length)
                {                    
                    _read += stream.Read(message, 0, length);
                    if (_read == 0)  // No more data available (disconnected)
                        throw new IOException("I/O error occurred.");
                }
                return message;
            }
            catch (IOException)
            {
                // Read has timed out
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
            // addrPort comes from FPDL in the form <IP Address>:<Port>
            string[] parts = addrPort.Split(":");
            IPAddress ipAddress = IPAddress.Parse(parts[0]);
            Int32 port = Convert.ToInt32(parts[1]);
            return new IPEndPoint(ipAddress, port);
        }
    }
}

    
    


