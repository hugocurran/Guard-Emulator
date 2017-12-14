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

namespace UnitTests
{
    [TestClass]
    public class ProcessorIntegrationTests
    {
        // All testing done on loopback if
        string subscriber = "127.0.0.1:5556";
        string publisher = "127.0.0.1:5555";

        // Processor setup
        OspProtocol protocol = OspProtocol.HPSD_ZMQ;

        // Sequence number for messages
        // Note that the test harness consumes seq 0 and 1
        int sequence = 0;   

        [TestMethod]
        public void ProcessorStatusMessage()
        {
            XDocument testPolicy = policy();
            HpsdMessage testMessage = statusMessage(2);
            
            Assert.IsTrue(processorDriver(testPolicy, testMessage));
        }

        [TestMethod]
        public void ProcessorObjectCreateMessagePositive()
        {
            XDocument testPolicy = policy();
            HpsdMessage testMessage = objectCreateGood(2);
            
            Assert.IsTrue(processorDriver(testPolicy, testMessage));
        }

        [TestMethod]
        public void ProcessorObjectCreateMessageNegative()
        {
            XDocument testPolicy = policy();
            HpsdMessage testMessage = objectCreateBad(2);
            
            Assert.IsFalse(processorDriver(testPolicy, testMessage));
        }

        #region Test Harness

        // Test harness
        public bool processorDriver(XDocument policy, HpsdMessage sendMessage)
        {
            bool success = true;
            // Processor must run in its own cancellable task
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            // Start the Processor thread
            var processorTask = Task.Run(() =>
            {
                var processorObj = ProcessorFactory.Create(subscriber, publisher, protocol, policy, token);
            }, token);

            // Wait for processor thread to stabilise
            Thread.Sleep(500);

            // Create publish + subscribe sockets
            using (var subSocket = new SubscriberSocket())
            using (var pubSocket = new PublisherSocket())
            {
                // Pull data from the receive socket
                subSocket.Options.ReceiveHighWatermark = 1000;
                subSocket.Bind("tcp://" + publisher);
                subSocket.SubscribeToAnyTopic();

                // Push test data to the subscribe socket
                pubSocket.Options.SendHighWatermark = 1000;
                pubSocket.Connect("tcp://" + subscriber);

                // Send starter
                pubSocket.SendFrame(statusMessage(sequence++).ToByteArray());

                // Wait for the zmq connections to stabilise
                Thread.Sleep(50);
                pubSocket.SendFrame(statusMessage(sequence++).ToByteArray());

                byte[] receivedMessage = null;
                TimeSpan timeout = new TimeSpan(1000000);    // 10 msec
                pubSocket.SendFrame(sendMessage.ToByteArray());

                if (subSocket.TryReceiveFrameBytes(timeout, out receivedMessage))
                {
                    if (receivedMessage.SequenceEqual(sendMessage.ToByteArray()))
                        success = true;
                    else
                        success = false;
                    
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
                return success;
            }
        }

        // Test data sets

        XDocument policy()
        {
            XDocument testPolicy = new XDocument();
            testPolicy.Add(new XElement("exportPolicy"));
            int number = 1;
            XElement rule;
            /*
            rule =
                new XElement("exportPolicy",
                    new XElement("rule",
                        new XAttribute("ruleNumber", number.ToString()),
                        new XElement("federate", "*"),
                        new XElement("entity", "*"),
                        new XElement("objectName", "*"),
                        new XElement("attributeName", "*"))
            );
            testPolicy.Element("exportPolicy").Add(rule);
            number++;
            */

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", number.ToString()),
                new XElement("federate", "CGF"),
                new XElement("entity", "*"),
                new XElement("objectName", "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"),
                new XElement("attributeName", "Marking")
            );
            testPolicy.Element("exportPolicy").Add(rule);
            number++;

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", number.ToString()),
                new XElement("federate", "CGF"),
                new XElement("entity", "2B93915C-116C-43F4-BF61-5295FFD5F82A"),
                new XElement("objectName", "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"),
                new XElement("attributeName", "Marking")
            );
            testPolicy.Element("exportPolicy").Add(rule);
            number++;

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", number.ToString()),
                new XElement("federate", "CGF"),
                new XElement("entity", "*"),
                new XElement("objectName", "HLAinteractionRoot.WeaponFire"),
                new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);
            number++;

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", number.ToString()),
                new XElement("federate", "CGF"),
                new XElement("entity", "2B93915C-116C-43F4-BF61-5295FFD5F82A"),
                new XElement("objectName", "HLAinteractionRoot.Detonation"),
                new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);
            number++;

            return testPolicy;
        }

        HpsdMessage statusMessage(int sequence)
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

        HpsdMessage objectCreateGood(int sequence)
        {
            // Create an HPSD ObjectCreate message for testing
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            HpsdMessage objectCreateMessage = new HpsdMessage()
            {
                ProtocolVersion = 81,
                SequenceNumber = sequence,
                Timestamp = timeStamp,
                MessageType = HpsdMessage.Types.MessageType.ObjectCreate,
                ObjectCreate = new ObjectCreate()
                {
                    ProducingFederate = "CGF",
                    InstanceId = "2B93915C-116C-43F4-BF61-5295FFD5F82A",
                    ObjectClassName = "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"
                }
            };
            return objectCreateMessage;
        }

        HpsdMessage objectCreateBad(int sequence)
        {
            // Create an HPSD ObjectCreate message for testing
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            HpsdMessage objectCreateMessage = new HpsdMessage()
            {
                ProtocolVersion = 81,
                SequenceNumber = sequence,
                Timestamp = timeStamp,
                MessageType = HpsdMessage.Types.MessageType.ObjectCreate,
                ObjectCreate = new ObjectCreate()
                {
                    ProducingFederate = "Typhoon",      // Invalid federate
                    InstanceId = "2B93915C-116C-43F4-BF61-5295FFD5F82A",
                    ObjectClassName = "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"
                }
            };
            return objectCreateMessage;
        }
        #endregion

    }
}

