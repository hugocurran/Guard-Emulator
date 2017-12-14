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
            using (var client = new TcpClient(AddressFamily.InterNetwork))
            {
                // Start monitoring the cancellation token
                poller.RunAsync();

                // Setup connection to downstream proxy
                client.ExclusiveAddressUse = true;
                client.SendBufferSize = 2048;
                client.NoDelay = true;
                // client.SendTimeout = 100;
                do
                {
                    try
                    {
                        client.Connect(EndPoint(downstreamPort));
                        if (!client.Connected)
                            Thread.Sleep(10000);  // Retry every 10 seconds
                    }
                    catch (SocketException e)
                    {
                        // should filter out not available errors only
                    }
                    
                    
                } while (!client.Connected);

                NetworkStream downstream = client.GetStream();

                // Setup connection to the upstream proxy
                TcpListener mesgServer = new TcpListener(EndPoint(upstreamPort))
                {
                    ExclusiveAddressUse = true
                };
                mesgServer.Start(1);

                TcpClient server = null;
                NetworkStream upstream = null;
                if (client.Connected)
                {
                    // This will block until the upstream connects
                    server = mesgServer.AcceptTcpClient();
                    server.NoDelay = true;
                    server.ReceiveBufferSize = 2048;
                    upstream = server.GetStream();
                }

                // Message processing loop
                InternalMessage iMesg = null;
                byte[] message = null;
                while (client.Connected && server.Connected)
                {
                    message = ReadMessage(upstream);
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
                        }
                        else
                        {
                            // Log an error message
                        }
                    }
                    else
                    {
                        // Log an event message
                        continue;
                    }
                }
            }
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

    
    


