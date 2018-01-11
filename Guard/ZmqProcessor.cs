using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace Guard_Emulator
{
    class ZmqProcessor : Processor
    {
        /// <summary>
        /// Null Processor object for unit testing only
        /// </summary>
        internal ZmqProcessor() { }

        /// <summary>
        /// Guard path processor using ZeroMQ
        /// </summary>
        /// <param name="subscribe">Address:Port for the subscribe socket</param>
        /// <param name="publish">Address:Port for the publish socket</param>
        /// <param name="osp">OSP message protocol</param>
        /// <param name="policy">Policy ruleset to apply</param>
        /// <param name="token">Cancellation token</param>
        public ZmqProcessor(string subscribe, string publish, OspProtocol osp, XDocument policy, CancellationToken token) : base()
        {
            // Create a timer to check for task cancellation
            var timer = new NetMQTimer(TimeSpan.FromMilliseconds(500));
            timer.Elapsed += (sender, args) => { token.ThrowIfCancellationRequested(); };

            using (var poller = new NetMQPoller { timer })
            using (var subSocket = new SubscriberSocket())
            using (var pubSocket = new PublisherSocket())
            {
                subSocket.Options.ReceiveHighWatermark = 1000;
                subSocket.Bind("tcp://" + subscribe);
                subSocket.SubscribeToAnyTopic();

                pubSocket.Options.SendHighWatermark = 1000;
                pubSocket.Connect("tcp://" + publish);

                // Start monitoring the cancellation token
                poller.RunAsync();

                InternalMessage iMesg = null;
                byte[] message = null;
                while (true)
                {
                    message = subSocket.ReceiveFrameBytes();

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
                        pubSocket.SendFrame(message);
                        // Log an event message
                    }
                    else
                    {
                        // Log an error message
                        continue;
                    }
                }
            }
        }
    }
}
