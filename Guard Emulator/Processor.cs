using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;

namespace Guard_Emulator
{
    public class Processor
    {
        /*
        static void Main(string[] args)
        {
            string subscribe = args[0];
            string publish = args[1];
            XDocument policy args[2];
            */

        public Processor(string subscribe, string publish, CancellationToken token)
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
                Console.WriteLine("Subscriber socket binding...");

                pubSocket.Options.SendHighWatermark = 1000;
                pubSocket.Connect("tcp://" + publish);
                Console.WriteLine("Publisher socket connecting...");

                // Start monitoring the cancellation token
                poller.RunAsync();

                byte[] message = null;
                while (true)
                {
                    message = subSocket.ReceiveFrameBytes();
                    Console.WriteLine("Got one: {0}", System.Text.Encoding.ASCII.GetString(message));

                    // Do some message checking
                    pubSocket.SendFrame(message);
                }
            }
        }

        /// <summary>
        /// Parse a HPSD message and return a standardised internal message
        /// </summary>
        /// <param name="message">HPSD message</param>
        /// <returns>Standardised internal message</returns>
        //InternalMessage hpsdParser(byte[] message)
        //{
        //    return new objectUpdateMessage();
        //}

        /// <summary>
        /// Test the message against the policy
        /// </summary>
        /// <param name="message">message in standardised internal format</param>
        /// <returns>True if message permitted, else False</returns>
        //bool applyPolicy(InternalMessage intMessage)
        //{
        // Phase 1: check the message against federates


        // Phase 2: check the message against entities


        // Phase 3a: check the message against object names


        // Phase 3b: check the message against interaction names


        // Phase 4: check the message against attribute names

        //  return true;
        //}
    }
}
