using System;
using System.Text;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetMQ;
using NetMQ.Sockets;
using System.Threading.Tasks;
using System.Threading;
using Guard_Emulator;

namespace UnitTests
{
    [TestClass]
    public class ProcessorUnitTests
    {
        // All testing done on loopback if
        string subscriber = "127.0.0.1:5556";
        string publisher = "127.0.0.1:5555";

        [TestMethod]
        public void TestProcessorBasicSocketToSocketCopy()
        {
            byte[] testData = System.Text.Encoding.ASCII.GetBytes("Hello World!");

            // Processor must run in its own cancellable task
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            var processorTask = Task.Run(() =>
            {
                var processorObj = new Processor(subscriber, publisher, token);
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
                pubSocket.SendFrame(Encoding.ASCII.GetBytes("Starter 1"));
                //pubSocket.SendFrame(Encoding.ASCII.GetBytes("Starter 2"));
                // Wait for the zmq connections to stabilise
                Thread.Sleep(50);

                int counter = 0;
                int received = 0;
                int missed = 0;
                byte[] message = null;
                TimeSpan timeout = new TimeSpan(1000000);    // 10 msec
                while (counter < 25)
                {
                    testData = System.Text.Encoding.ASCII.GetBytes("Hello World! " + counter.ToString());
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
                Assert.IsTrue((received == 24) && (missed == 1));

                // Tidy up by cancelling the Processor task
                try
                {
                    tokenSource.Cancel();
                }
                catch(OperationCanceledException) {}
                finally
                {
                    tokenSource.Dispose();
                }
            }

        }
    }
}
