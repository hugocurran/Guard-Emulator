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
    public class ProcessorUnitTests
    {
        // All testing done on loopback if
        string subscriber = "127.0.0.1:5556";
        string publisher = "127.0.0.1:5555";

        

        [TestMethod]
        public void ProcessorBasicSocketToSocketCopy()
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
            OspProtocol protocol = OspProtocol.HPSD;

            // Start the Processor thread
            var processorTask = Task.Run(() =>
            {
                var processorObj = new Processor(subscriber, publisher, protocol, testPolicy, token);
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
                pubSocket.SendFrame(statusMessage(0).ToByteArray());
                // pubSocket.SendFrame(statusMessage(1).ToByteArray());

                // Wait for the zmq connections to stabilise
                Thread.Sleep(50);
                
                int counter = 1;
                int received = 0;
                int missed = 0;
                byte[] testData;
                byte[] message = null;
                TimeSpan timeout = new TimeSpan(1000000);    // 10 msec
                while (counter < 25)
                {
                    testData = statusMessage(counter).ToByteArray();
                    pubSocket.SendFrame(testData);
                    counter++;
                    //Thread.Sleep(60);

                    if (subSocket.TryReceiveFrameBytes(timeout, out message))
                    {
                        Assert.IsTrue(message.SequenceEqual(testData));
                        received++;
                    }
                    else
                    {
                        missed++;
                    }
                }
                // We expect to lose the first message!
                Assert.IsTrue((received == 23) && (missed == 1));

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
        }

        [TestMethod]
        public void ObjectCreatePolicyProcessing1()
        {
            // Create a matching policy file
            XDocument testPolicy = new XDocument();
            testPolicy.Add(new XElement("exportPolicy"));
            XElement rule =
                new XElement("rule",
                        new XAttribute("ruleNumber", "1"),
                        new XElement("federate", "*"),
                        new XElement("entity", "*"),
                        new XElement("objectName", "*"),
                        new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            // Create an internal message for an Object Update
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            InternalMessage intMessage = new InternalMessage()
            {
                Federate = "CGF",
                EntityID = "2B93915C-116C-43F4-BF61-5295FFD5F82A",
                ObjectName = "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft",
                Type = MessageType.ObjectCreate,
                TimeStamp = calcTime,
                SequenceNumber = 1
            };

            // Create a 'null' processor for testing
            Processor processor = new Processor();

            Assert.IsTrue(processor.ApplyPolicy(intMessage, testPolicy));
            Assert.AreEqual(processor.RuleNumber, "1");
        }

        [TestMethod]
        public void ObjectCreatePolicyProcessing2()
        {
            // Create a matching policy file
            XDocument testPolicy = new XDocument();
            testPolicy.Add(new XElement("exportPolicy"));
            XElement rule =
                new XElement("rule",
                        new XAttribute("ruleNumber", "1"),
                        new XElement("federate", "*"),
                        new XElement("entity", "*"),
                        new XElement("objectName", "*"),
                        new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            rule =
                new XElement("rule",
                    new XAttribute("ruleNumber", "2"),
                    new XElement("federate", "CGF"),
                    new XElement("entity", "*"),
                    new XElement("objectName", "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"),
                    new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            // Create an internal message for an Object Update
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            InternalMessage intMessage = new InternalMessage()
            {
                Federate = "CGF",
                EntityID = "2B93915C-116C-43F4-BF61-5295FFD5F82A",
                ObjectName = "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft",
                Type = MessageType.ObjectCreate,
                TimeStamp = calcTime,
                SequenceNumber = 1
            };

            // Create a 'null' processor for testing
            Processor processor = new Processor();

            Assert.IsTrue(processor.ApplyPolicy(intMessage, testPolicy));
            Assert.AreEqual(processor.RuleNumber, "2");
        }

        [TestMethod]
        public void ObjectCreatePolicyProcessing3()
        {
            // Create a matching policy file
            XDocument testPolicy = new XDocument();
            testPolicy.Add(new XElement("exportPolicy"));
            XElement rule =
                new XElement("rule",
                        new XAttribute("ruleNumber", "1"),
                        new XElement("federate", "*"),
                        new XElement("entity", "*"),
                        new XElement("objectName", "*"),
                        new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            rule =
                new XElement("rule",
                    new XAttribute("ruleNumber", "2"),
                    new XElement("federate", "CGF"),
                    new XElement("entity", "*"),
                    new XElement("objectName", "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"),
                    new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "3"),
                new XElement("federate", "CGF"),
                new XElement("entity", "2B93915C-116C-43F4-BF61-5295FFD5F82A"),
                new XElement("objectName", "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"),
                new XElement("attributeName", "*")
);
            testPolicy.Element("exportPolicy").Add(rule);

            // Create an internal message for an Object Update
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            InternalMessage intMessage = new InternalMessage()
            {
                Federate = "CGF",
                EntityID = "2B93915C-116C-43F4-BF61-5295FFD5F82A",
                ObjectName = "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft",
                Type = MessageType.ObjectCreate,
                TimeStamp = calcTime,
                SequenceNumber = 1
            };

            // Create a 'null' processor for testing
            Processor processor = new Processor();

            Assert.IsTrue(processor.ApplyPolicy(intMessage, testPolicy));
            Assert.AreEqual(processor.RuleNumber, "3");
        }

        [TestMethod]
        public void ObjectUpdatePolicyProcessing1()
        {
            // Create a matching policy file
            XDocument testPolicy = new XDocument();
            testPolicy.Add(new XElement("exportPolicy"));

            XElement rule =
                new XElement("exportPolicy",
                    new XElement("rule",
                        new XAttribute("ruleNumber", "1"),
                        new XElement("federate", "*"),
                        new XElement("entity", "*"),
                        new XElement("objectName", "*"),
                        new XElement("attributeName", "*"))
            );
            testPolicy.Element("exportPolicy").Add(rule);

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "2"),
                new XElement("federate", "CGF"),
                new XElement("entity", "*"),
                new XElement("objectName", "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"),
                new XElement("attributeName", "Marking")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "3"),
                new XElement("federate", "CGF"),
                new XElement("entity", "2B93915C-116C-43F4-BF61-5295FFD5F82A"),
                new XElement("objectName", "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"),
                new XElement("attributeName", "Marking")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            // Create an internal message for an Object Update
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            InternalMessage intMessage = new InternalMessage()
            {
                Federate = "CGF",
                EntityID = "2B93915C-116C-43F4-BF61-5295FFD5F82A",
                ObjectName = "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft",
                AddAttribute = "Marking",
                Type = MessageType.ObjectUpdate,
                TimeStamp = calcTime,
                SequenceNumber = 1
            };

            // Create a 'null' processor for testing
            Processor processor = new Processor();

            Assert.IsTrue(processor.ApplyPolicy(intMessage, testPolicy));
            Assert.AreEqual(processor.RuleNumber, "3");
        }

        [TestMethod]
        public void ObjectUpdatePolicyProcessing2()
        {
            // Create a matching policy file
            XDocument testPolicy = new XDocument();
            testPolicy.Add(new XElement("exportPolicy"));

            XElement rule =
                new XElement("exportPolicy",
                    new XElement("rule",
                        new XAttribute("ruleNumber", "1"),
                        new XElement("federate", "*"),
                        new XElement("entity", "*"),
                        new XElement("objectName", "*"),
                        new XElement("attributeName", "*"))
            );
            testPolicy.Element("exportPolicy").Add(rule);

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "2"),
                new XElement("federate", "CGF"),
                new XElement("entity", "*"),
                new XElement("objectName", "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"),
                new XElement("attributeName", "Marking")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "3"),
                new XElement("federate", "CGF"),
                new XElement("entity", "2B93915C-116C-43F4-BF61-5295FFD5F82A"),
                new XElement("objectName", "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"),
                new XElement("attributeName", "Marking")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            // Create an internal message for an Object Update
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            InternalMessage intMessage = new InternalMessage()
            {
                Federate = "CGF",
                EntityID = "2B93915C-116C-43F4-BF61-5295FFD5F82A",
                ObjectName = "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft",
                AddAttribute = "Type",
                Type = MessageType.ObjectUpdate,
                TimeStamp = calcTime,
                SequenceNumber = 1
            };

            // Create a 'null' processor for testing
            Processor processor = new Processor();

            Assert.IsFalse(processor.ApplyPolicy(intMessage, testPolicy));
            Assert.AreEqual(processor.RuleNumber, "NOMATCH");
        }

        [TestMethod]
        public void InteractionPolicyProcessing1()
        {
            // Create a matching policy file
            XDocument testPolicy = new XDocument();
            testPolicy.Add(new XElement("exportPolicy"));

            XElement rule =
                new XElement("exportPolicy",
                    new XElement("rule",
                        new XAttribute("ruleNumber", "1"),
                        new XElement("federate", "*"),
                        new XElement("entity", "*"),
                        new XElement("objectName", "*"),
                        new XElement("attributeName", "*"))
            );
            testPolicy.Element("exportPolicy").Add(rule);

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "2"),
                new XElement("federate", "CGF"),
                new XElement("entity", "*"),
                new XElement("objectName", "HLAinteractionRoot.WeaponFire"),
                new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "3"),
                new XElement("federate", "CGF"),
                new XElement("entity", "2B93915C-116C-43F4-BF61-5295FFD5F82A"),
                new XElement("objectName", "HLAinteractionRoot.Detonation"),
                new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            // Create an internal message for an Interaction
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            InternalMessage intMessage = new InternalMessage()
            {
                Federate = "CGF",
                EntityID = "2B93915C-116C-43F4-BF61-5295FFD5F82A",
                InteractionName = "HLAinteractionRoot.Detonation",
                Type = MessageType.Interaction,
                TimeStamp = calcTime,
                SequenceNumber = 1
            };

            // Create a 'null' processor for testing
            Processor processor = new Processor();

            Assert.IsTrue(processor.ApplyPolicy(intMessage, testPolicy));
            Assert.AreEqual(processor.RuleNumber, "3");
        }

        [TestMethod]
        public void InteractionPolicyProcessing2()
        {
            // Create a matching policy file
            XDocument testPolicy = new XDocument();
            testPolicy.Add(new XElement("exportPolicy"));

            XElement rule =
                new XElement("exportPolicy",
                    new XElement("rule",
                        new XAttribute("ruleNumber", "1"),
                        new XElement("federate", "*"),
                        new XElement("entity", "*"),
                        new XElement("objectName", "*"),
                        new XElement("attributeName", "*"))
            );
            testPolicy.Element("exportPolicy").Add(rule);

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "2"),
                new XElement("federate", "CGF"),
                new XElement("entity", "*"),
                new XElement("objectName", "HLAinteractionRoot.WeaponFire"),
                new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "3"),
                new XElement("federate", "CGF"),
                new XElement("entity", "2B93915C-116C-43F4-BF61-5295FFD5F82A"),
                new XElement("objectName", "HLAinteractionRoot.Detonation"),
                new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            // Create an internal message for an Interaction
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            InternalMessage intMessage = new InternalMessage()
            {
                Federate = "CGF",
                EntityID = "2B93915C-116C-43F4-BF61-5295FFD5F82A",
                InteractionName = "HLAinteractionRoot.WeaponFire",
                Type = MessageType.Interaction,
                TimeStamp = calcTime,
                SequenceNumber = 1
            };

            // Create a 'null' processor for testing
            Processor processor = new Processor();

            Assert.IsFalse(processor.ApplyPolicy(intMessage, testPolicy));
            Assert.AreEqual(processor.RuleNumber, "NOMATCH");
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

        static HpsdMessage objectCreate(int sequence)
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

        }
    }

