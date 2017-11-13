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
            string exportSub = args[0];     // Export subscribe socket
            string exportPub = args[1];     // Export publish socket
            string importSub = args[2];     // Import subscribe socket
            string importPub = args[3];     // Import publish soscket
            string policy = args[4];        // Policy file

            /*
            string processorPath = @"C:\Users\peter\Source\Repos\guard_cli\Processor\bin\Debug\netcoreapp1.1\Processor.dll";
            Process exportProcessor = new Process();
            exportProcessor.StartInfo.FileName = "dotnet.exe";
            exportProcessor.StartInfo.Arguments = processorPath + " " + exportSub + " " + exportPub;
            exportProcessor.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            exportProcessor.Start();
            Console.WriteLine("Spawning export processor\n");

            Process importProcessor = new Process();
            importProcessor.StartInfo.FileName = "dotnet.exe";
            importProcessor.StartInfo.Arguments = processorPath + " " + exportSub + " " + exportPub;
            importProcessor.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            importProcessor.Start();
            Console.WriteLine("Spawning import processor\n");
            */
            // Processor must run in its own cancellable task
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            var exportTask = Task.Run(() =>
            {
                var exportObj = new Processor(exportSub, exportPub, token);
            }, token);

            var importTask = Task.Run(() =>
            {
                var importObj = new Processor(importSub, importPub, token);
            }, token);

            Task.WaitAll();

        }
    }
}
