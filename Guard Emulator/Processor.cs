using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using Google.Protobuf;

namespace Guard_Emulator
{
    public enum OspProtocol
    {
        HPSD,
        WebLVC
    }



    public class Processor
    {

        public Processor(string subscribe, string publish, OspProtocol osp, XDocument policy, CancellationToken token)
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
                //Console.WriteLine("Subscriber socket binding...");

                pubSocket.Options.SendHighWatermark = 1000;
                pubSocket.Connect("tcp://" + publish);
                //Console.WriteLine("Publisher socket connecting...");

                // Start monitoring the cancellation token
                poller.RunAsync();

                InternalMessage iMesg = null;
                byte[] message = null;
                while (true)
                {
                    message = subSocket.ReceiveFrameBytes();
                    //Console.WriteLine("Got one: {0}", System.Text.Encoding.ASCII.GetString(message));

                    switch (osp)
                    {
                        case OspProtocol.HPSD:
                            //Console.WriteLine("About to Parse message ");
                            iMesg = HpsdParser.ParseMessage(HpsdMessage.Parser.ParseFrom(message));
                            //Console.WriteLine("Parsed message: Sequence = {0}", iMesg.SequenceNumber);
                            break;

                        case OspProtocol.WebLVC:
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


        /// <summary>
        /// Test the message against the policy
        /// </summary>
        /// <param name="message">message in standardised internal format</param>
        /// <returns>True if message permitted, else False</returns>
        bool ApplyPolicy(InternalMessage intMessage, XDocument policy)
        {
            // Phase 0: filter out heartbeats etc.
            if (intMessage.Type == MessageType.Status)
            {
                return true;
            }

            // Phase 1: check the message against federates
           // IEnumerable<XElement> list = policy.
            from policy in root.Elements().Elements("federate")
                    where (string)typeElement.Attribute("Value") == "Yes"
                    select (string)typeElement.Parent.Element("Text");


            // Phase 2: check the message against entities


            // Phase 3a: check the message against object names


            // Phase 3b: check the message against interaction names


            // Phase 4: check the message against attribute names

            return true;
        }
    }
}
