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
using Tests;

namespace UnitTests
{
    [TestClass]
    public class ZMQ_ProcessorIntegrationTests
    {
        // All testing done on loopback if
        string subscriber = "127.0.0.1:5556";  //Socket that guard will subscribe to for upstream traffic
        string publisher = "127.0.0.1:5555";   //Socket guard will publish to for downstream traffic

        #region HPSD over ZMQ

        [TestMethod]
        public void HPSD_ZMQ_ProcessorBasicSocketToSocketCopy()
        {
            XDocument testPolicy = Harness.CreateEmptyPolicy();
            ZMQ_MessageTestLoop(OspProtocol.HPSD_ZMQ, testPolicy, Harness.HPSD_StatusMessage, Harness.HPSD_StatusMessage);
        }

        #endregion

        #region WebLVC over ZMQ

        [TestMethod]
        public void WebLVC_ZMQ_ProcessorBasicSocketToSocketCopy()
        {
            XDocument testPolicy = Harness.CreateEmptyPolicy();
            ZMQ_MessageTestLoop(OspProtocol.WebLVC_ZMQ, testPolicy, Harness.WebLVC_StatusMessage, Harness.WebLVC_StatusMessage);
        }

        [TestMethod]
        public void WebLVC_ZMQ_ProcessorUpdate()
        {
            XDocument testPolicy = Harness.CreateEmptyPolicy();
            ZMQ_MessageTestLoop(OspProtocol.WebLVC_ZMQ, testPolicy, Harness.WebLVC_StatusMessage, Harness.WebLVC_UpdateMessage);
        }

        #endregion

        public void ZMQ_MessageTestLoop(OspProtocol protocol, XDocument policy, Func<int, byte[]> statusMsg, Func<int, byte[]> testMsg)
        {
            // We need an initialised logger object
            Logger logger = Logger.Instance;
            logger.Initialise(Facility.Local1, "127.0.0.1");

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
                // UPSTREAM: Publish data on the guards subscribe socket
                pubSocket.Options.SendHighWatermark = 1000;
                pubSocket.Bind("tcp://" + subscriber);

                // DOWNSTREAM Subscribe to test data on the guards publish socket                
                subSocket.Options.ReceiveHighWatermark = 1000;
                subSocket.Connect("tcp://" + publisher);
                subSocket.SubscribeToAnyTopic();

                // Send starter
                int counter = 0;
                pubSocket.SendFrame(statusMsg(counter++));
                // Wait for the zmq connections to stabilise
                Thread.Sleep(50);
                pubSocket.SendFrame(statusMsg(counter++));
                Thread.Sleep(50);

                int received = 0;
                int missed = 0;
                byte[] testData;
                byte[] message = null;
                TimeSpan timeout = new TimeSpan(1000000);    // 10 msec
                while (counter < 25)
                {
                    testData = testMsg(counter++);

                    pubSocket.SendFrame(testData);
                    if (subSocket.TryReceiveFrameBytes(timeout, out message))
                    {
                        Assert.IsTrue(message.SequenceEqual(testData), "Received message does not match transmitted");
                        received++;
                    }
                    else
                    {
                        missed++;
                    }
                }
                // We shouldnt lose any messages!
                //Console.WriteLine("recieved: {0}, missed: {1}", received, missed);
                Assert.IsTrue((received == 23) && (missed == 0), "Received (" + received.ToString() + ")/Missed (" + missed.ToString() + ") mismatch");

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
    }
}

