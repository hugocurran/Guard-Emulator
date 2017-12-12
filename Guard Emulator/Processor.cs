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


    /// <summary>
    /// Guard path processor
    /// </summary>
    public class Processor
    {
        // Set a default rule match value
        private string ruleNumber = "NOMATCH";

        /// <summary>
        /// Rule number that matches the message (0 = status/heartbeat message)
        /// </summary>
        public string RuleNumber {  get { return ruleNumber; } }

        /// <summary>
        /// Null Processor object for unit testing only
        /// </summary>
        internal Processor() { }

        /// <summary>
        /// Guard path processor
        /// </summary>
        /// <param name="subscribe">Address:Port for the subscribe socket</param>
        /// <param name="publish">Address:Port for the publish socket</param>
        /// <param name="osp">OSP message protocol</param>
        /// <param name="policy">Policy ruleset to apply</param>
        /// <param name="token">Cancellation token</param>
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
        
        /// <summary>
        /// Test the message against the policy ruleset
        /// </summary>
        /// <param name="message">message in standardised internal format</param>
        /// <returns>True if message permitted, else False</returns>
        internal bool ApplyPolicy(InternalMessage intMessage, XDocument ruleSet)
        {
            // Reset ruleNumber
            ruleNumber = "NOMATCH";

            // Load the policy rules
            IEnumerable<XElement> rules = ruleSet.Descendants("rule");

            // Phase 0: filter out heartbeats etc.
            if (intMessage.Type == MessageType.Status)
            {
                ruleNumber = "0";
                return true;
            }

            // Phase 1: check the message against federates
            IEnumerable<XElement> federateMatches =
                from el in rules
                where (string)el.Element("federate") == intMessage.Federate
                select el;
            if (federateMatches.Count() == 0)
            {
                federateMatches =
                    from el in rules
                    where (string)el.Element("federate") == "*"
                    select el;
                if (federateMatches.Count() == 0)
                    return false;
            }

            // Phase 2: check the message against entities
            IEnumerable<XElement> entityMatches =
                from el in federateMatches
                where (string)el.Element("entity") == intMessage.EntityID
                select el;
            if (entityMatches.Count() == 0)
            {
                entityMatches =
                    from el in federateMatches
                    where (string)el.Element("entity") == "*"
                    select el;
                if (entityMatches.Count() == 0)
                    return false;
            }

            // Phase 3a: check the message against object names
            IEnumerable<XElement> objectMatches = null;
            if ((intMessage.Type == MessageType.ObjectCreate) || (intMessage.Type == MessageType.ObjectDelete) || (intMessage.Type == MessageType.ObjectUpdate))
            {
                objectMatches =
                    from el in entityMatches
                    where (string)el.Element("objectName") == intMessage.ObjectName
                        select el;
                if (objectMatches.Count() == 0)
                {
                    objectMatches =
                        from el in entityMatches
                        where (string)el.Element("objectName") == "*"
                        select el;
                    if (objectMatches.Count() == 0)
                        return false;
                }
                if ((intMessage.Type == MessageType.ObjectCreate) || (intMessage.Type == MessageType.ObjectDelete))
                {
                    ruleNumber = objectMatches.ElementAt(0).Attribute("ruleNumber").Value;
                    return true;
                }
            }

            // Phase 3b: check the message against interaction names
            if (intMessage.Type == MessageType.Interaction)
            {
                IEnumerable<XElement> interactionMatches =
                    from el in entityMatches
                    where (string)el.Element("objectName") == intMessage.InteractionName
                    select el;
                if (interactionMatches.Count() == 0)
                {
                    interactionMatches =
                        from el in entityMatches
                        where (string)el.Element("objectName") == "*"
                        select el;
                }
                if (interactionMatches.Count() == 0)
                    return false;
                else
                {
                    ruleNumber = interactionMatches.ElementAt(0).Attribute("ruleNumber").Value;
                    return true;    // Don't check parameters
                }
            }

            // Phase 4: check the message against attribute names
            if (intMessage.Type == MessageType.ObjectUpdate)
            {
                foreach (string attrib in intMessage.Attribute)
                {
                    IEnumerable<XElement> attribMatches =
                        from el in objectMatches
                        where (string)el.Element("attributeName") == attrib
                        select el;
                    if (attribMatches.Count() == 0)
                    {
                        attribMatches =
                            from el in entityMatches
                            where (string)el.Element("attributeName") == "*"
                            select el;
                        if (attribMatches.Count() == 0)
                            return false;
                    }
                    ruleNumber = objectMatches.ElementAt(0).Attribute("ruleNumber").Value;
                    return true;
                }
            }
            return false;
        }
    }
}
