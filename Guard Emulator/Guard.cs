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
            string policy = args[0];        // Policy file

            // Parse the policy file
            FpdlParser fpdlParser = new FpdlParser();
            if (!fpdlParser.LoadDeployDocument(args[4]))
            {
                Console.WriteLine("FPDL Parser error: {0}", fpdlParser.ErrorMsg);
            }

            // Processor must run in its own cancellable task
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            var exportTask = Task.Run(() =>
            {
                var exportObj = new Processor(
                    fpdlParser.ExportSub, 
                    fpdlParser.ExportPub,
                    fpdlParser.ExportProtocol,
                    fpdlParser.ExportPolicy, 
                    token);
            }, token);

            var importTask = Task.Run(() =>
            {
                var importObj = new Processor(
                    fpdlParser.ImportSub,
                    fpdlParser.ImportPub,
                    fpdlParser.ImportProtocol,
                    fpdlParser.ImportPolicy, 
                    token);
            }, token);

            Task.WaitAll();

        }
    }
}
