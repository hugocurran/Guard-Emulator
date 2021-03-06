﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

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

            // Initialise logging
            Logger logger = Logger.Instance;
            logger.Initialise(Facility.Local0, fpdlParser.SyslogServerIp, "guard");

            logger.Information("Loaded Deploy File: " + args[0] + ". Design Document Reference: " + fpdlParser.DesignDocReference);

            // Output the policy files
            XDocument pol = new XDocument(fpdlParser.ExportPolicy);
            pol.Save("exportPolicy.xml");
            pol = new XDocument(fpdlParser.ImportPolicy);
            pol.Save("importPolicy.xml");
            Console.WriteLine("Policy files output");

            // Processor must run in its own cancellable task
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            Task[] tasks = new Task[2];
            try
            {
                logger.Information("Starting export task");
                tasks[0] = Task.Run(() =>
                        {
                            var exportObj = ProcessorFactory.Create(
                                fpdlParser,
                                "Export",
                                 token);
                        }, 
                    token);

                logger.Information("Starting import task");
                tasks[1] = Task.Run(() =>
                        {
                        var importObj = ProcessorFactory.Create(
                                fpdlParser,
                                "Import",
                                token);
                        }, token);
            while (true) { }
            }
            finally
            {
                logger.Information("Stopping");
                tokenSource.Cancel();
                Task.WaitAll(tasks);
                tokenSource.Dispose();
            }
        }
    }
}

