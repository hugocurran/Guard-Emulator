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
            if (args.Count() != 1)
            {
                Console.WriteLine("Usage: Guard deployfile.xml");
                return;
            }

            // Parse the policy file
            FpdlParser fpdlParser = new FpdlParser();
            if (!fpdlParser.LoadDeployDocument(args[0]))
            {
                Console.WriteLine("FPDL Parser error: {0}", fpdlParser.ErrorMsg);
                return;
            }

            // Processor must run in its own cancellable task
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            try
            {
                var exportTask = Task.Run(() =>
                {
                    var exportObj = ProcessorFactory.Create(
                        fpdlParser.ExportSub,
                        fpdlParser.ExportPub,
                        fpdlParser.Protocol,
                        fpdlParser.ExportPolicy,
                        token);
                }, token);

                var importTask = Task.Run(() =>
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
                tokenSource.Cancel();
                Task.WaitAll();
                tokenSource.Dispose();
            }
        }
    }
}

