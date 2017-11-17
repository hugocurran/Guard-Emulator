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


            // Processor must run in its own cancellable task
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            OspProtocol protocol = OspProtocol.HPSD;
            var exportTask = Task.Run(() =>
            {
                var exportObj = new Processor(exportSub, exportPub, protocol, token);
            }, token);

            var importTask = Task.Run(() =>
            {
                var importObj = new Processor(importSub, importPub, protocol, token);
            }, token);

            Task.WaitAll();

        }
    }
}
