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
        /// <param name="subscribe">Address:Port for the upstream (subscribe) socket</param>
        /// <param name="publish">Address:Port for the downstream (publish) socket</param>
        /// <param name="osp">OSP message protocol</param>
        /// <param name="policy">Policy ruleset to apply</param>
        /// <param name="token">Cancellation token</param>
        public TcpProcessor(string subscribe, string publish, OspProtocol osp, XDocument policy, CancellationToken token)
            : base(subscribe, publish, osp, policy, token)
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
                while (!client.Connected)
                {
                    try
                    {
                        client.Connect(EndPoint(publish));
                    }
                    catch (SocketException e)
                    {
                        // should filter out not available errors only
                    }
                    // Retry every 10 seconds
                    Thread.Sleep(10000);
                }
                NetworkStream down = client.GetStream();

                // Setup connection to the upstream proxy
                TcpListener server = new TcpListener(EndPoint(subscribe))
                {
                    ExclusiveAddressUse = true
                };
                server.Start(1);

                TcpClient mesgServer = null;
                NetworkStream up = null;
                if (client.Connected)
                {
                    // This will block until the upstream connects
                    mesgServer = server.AcceptTcpClient();
                    mesgServer.NoDelay = true;
                    mesgServer.ReceiveBufferSize = 2048;
                    up = mesgServer.GetStream();
                }

                // Message processing loop
                InternalMessage iMesg = null;
                byte[] message = null;
                while (client.Connected && mesgServer.Connected)
                {
                    message = ReadMessage(up);
                    switch (osp)
                    {
                        case OspProtocol.HPSD_ZMQ:
                            iMesg = HpsdParser.ParseMessage(HpsdMessage.Parser.ParseFrom(message));
                            break;

                        case OspProtocol.WebLVC_ZMQ:
                            iMesg = WeblvcParser.ParseMessage(message);
                            break;
                    }

                    if (ApplyPolicy(iMesg, policy))
                    {
                        if (WriteMessage(message, down) == message.Length)
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
                        // Log an error message
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
                // read the first 2 bytes as an int32
                byte[] prefix = new byte[4];
                stream.Read(prefix, 0, 4);
                Int32 length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(prefix, 0));
                // Read the message using the length prescribed
                byte[] message = new byte[length];
                stream.Read(message, 0, length);
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

    
    


