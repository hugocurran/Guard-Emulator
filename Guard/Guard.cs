using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Guard_Emulator
{
    class Program
    {
        static void Main(string[] args)
        {
            if ((args.Count() != 1) || (args.Count() > 2))
            {
                Console.WriteLine("Usage: Guard deployfile.xml");
                return;
            }

            // Parse the policy file
            Console.WriteLine("Loading Deploy file");
            FpdlParser fpdlParser = new FpdlParser();
            if (!fpdlParser.LoadDeployDocument(args[0]))
            {
                Console.WriteLine("FPDL Parser error: {0}", fpdlParser.ErrorMsg);
                return;
            }

            // Processor must run in its own cancellable task
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            Task[] tasks = new Task[2];
            try
            {
                Console.WriteLine("Starting export task");
                tasks[0] = Task.Run(() =>
                        {
                            var exportObj = ProcessorFactory.Create(
                                fpdlParser.ExportSub,
                                fpdlParser.ExportPub,
                                fpdlParser.Protocol,
                                fpdlParser.ExportPolicy,
                                token);
                        }, 
                    token);

                Console.WriteLine("Starting import task");
                tasks[1] = Task.Run(() =>
                        {
                            var importObj = ProcessorFactory.Create(
                                fpdlParser.ImportSub,
                                fpdlParser.ImportPub,
                                fpdlParser.Protocol,
                                fpdlParser.ImportPolicy,
                                token);
                        }, token);
            }
            finally
            {
                Console.WriteLine("Stopping...");
                tokenSource.Cancel();
                Task.WaitAll(tasks);
                tokenSource.Dispose();
            }
        }
    }
}

