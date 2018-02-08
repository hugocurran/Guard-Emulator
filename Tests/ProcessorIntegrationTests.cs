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
using Hugo.Utility.Syslog;

namespace UnitTests
{
    [TestClass]
    public class ProcessorIntegrationTests
    {
        // All testing done on loopback if
        string subscriber = "127.0.0.1:5556";
        string publisher = "127.0.0.1:5555";
        OspProtocol protocol;

        // Sequence number for messages
        // Note that the test harness consumes seq 0 and 1
        int sequence = 0;   

        [TestMethod]
        public void ProcessorStatusMessage_HPSD()
        {
            protocol = OspProtocol.HPSD_ZMQ;
            XDocument testPolicy = policy();
            
            Assert.IsTrue(processorDriver(testPolicy, statusMessage_HPSD));
        }

        //[TestMethod]
        //public void ProcessorObjectCreateMessagePositive()
        //{
        //    XDocument testPolicy = policy();
        //    HpsdMessage testMessage = objectCreateGood(2);
            
        //    Assert.IsTrue(processorDriver(testPolicy, testMessage));
        //}
/*
        // Illogical test - we should not receive anything
        [TestMethod]
        public void ProcessorObjectCreateMessageNegative()
        {
            XDocument testPolicy = policy();
            HpsdMessage testMessage = objectCreateBad(2);
            
            Assert.IsFalse(processorDriver(testPolicy, testMessage));
        }
        */

        #region Test Harness - processor driver

        // Test harness
        public bool processorDriver(XDocument policy, Func<int, byte[]> mesgType)
        {
            bool success = false;
            // Processor must run in its own cancellable task
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            // We need an initialised logger object
            Logger logger = Logger.Instance;
            logger.Initialise(Facility.Local1, "127.0.0.1", "testGuard");

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
                // UPSTREAM: Publish data on the guards subscribe socket
                pubSocket.Options.SendHighWatermark = 1000;
                pubSocket.Bind("tcp://" + subscriber);

                // DOWNSTREAM Subscribe to test data on the guards publish socket                
                subSocket.Options.ReceiveHighWatermark = 1000;
                subSocket.Connect("tcp://" + publisher);
                subSocket.SubscribeToAnyTopic();

                // Send starter
                pubSocket.SendFrame(statusMessage_HPSD(sequence++));

                // Wait for the zmq connections to stabilise
                Thread.Sleep(50);
                pubSocket.SendFrame(statusMessage_HPSD(sequence++));

                byte[] receivedMessage = null;
                byte[] sendMessage = mesgType(sequence);
                TimeSpan timeout = new TimeSpan(1000000);    // 10 msec
                pubSocket.SendFrame(sendMessage);


                //if (subSocket.TryReceiveFrameBytes(timeout, out receivedMessage))
                //{
                receivedMessage = subSocket.ReceiveFrameBytes();
                    if (receivedMessage.SequenceEqual(sendMessage))
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

                //}
                return success;
            }
        }

        #endregion

        #region Test Policies

        // Test data sets

        XDocument allowAllPolicy()
        {
            XDocument testPolicy = new XDocument();
            testPolicy.Add(new XElement("exportPolicy"));
            int number = 1;
            XElement rule;

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
            return testPolicy;
        }

        XDocument policy()
        {
            XDocument testPolicy = new XDocument();
            testPolicy.Add(new XElement("exportPolicy"));
            int number = 1;
            XElement rule;
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

        #endregion

        #region HPSD messages

        byte[] statusMessage_HPSD(int sequence)
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
            return statusMessage.ToByteArray();
        }

        byte[] objectCreateMessage(int sequence)
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
            return objectCreateMessage.ToByteArray();
        }


        #endregion

        

    }
}

