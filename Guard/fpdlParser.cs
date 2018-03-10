using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using FPDL;
using FPDL.Common;
using FPDL.Deploy;

[assembly: InternalsVisibleTo("Tests")]

namespace Guard_Emulator
{

    /// <summary>
    /// Class and methods for parsing FPDL Deploy files
    /// </summary>

    internal class FpdlParser
    {

        private Component component;
        private ModuleOsp export;
        private ModuleOsp import;
        private RuleSet exportPolicy;
        private RuleSet importPolicy;

        /// <summary>
        /// Load a FPDL Deploy document into the parser
        /// </summary>
        /// <param name="fileName">Deploy document filename</param>
        /// <returns>true if successful, else false</returns>
        internal bool LoadDeployDocument(string fileName)
        {
            try
            {
                IFpdlObject fpdlObject = FpdlReader.Parse(fileName);
                if (fpdlObject.GetType() != typeof(DeployObject))
                {
                    ErrorMsg = "Not a FPDL Deploy document: " + fileName;
                    return false;
                }

                DeployObject deploy = (DeployObject)fpdlObject;

                DesignDocReference = deploy.DesignReference.ToString();

                // We expect to receive a Deploy doc defining a single system, which must contain a 'guard' component.
                if (deploy.Systems.Count != 1)
                {
                    ErrorMsg = "Deploy document ambiguity: Contains more than one system";
                    return false;
                }

                // The only system types that have a guard are HTG and MTG
                if ((deploy.Systems[0].SystemType != Enums.PatternType.htg) && (deploy.Systems[0].SystemType != Enums.PatternType.mtg))
                {
                    ErrorMsg = "Invalid Deploy document: System type does not include a guard";
                    return false;
                }

                // To be useful there must be a single Guard component
                List<Component> components = deploy.Systems[0].Components.FindAll(x => x.ComponentType == Enums.ComponentType.guard);
                if (components.Count !=1)
                {
                    ErrorMsg = "Too many/few Guard specifications in the Deploy document";
                    return false;
                }
                component = components[0];
            }
            catch (FileNotFoundException e)
            {
                ErrorMsg = e.Message;
                return false;
            }
            return (SetLogger() && SetOsp() && setExportPolicy() && setImportPolicy());
        }

        /// <summary>
        /// Guard component export policy
        /// </summary>
        internal XElement ExportPolicy { get { return exportPolicy.GetRuleSet(); } }
        /// <summary>
        /// Guard component import policy
        /// </summary>
        internal XElement ImportPolicy { get { return importPolicy.GetRuleSet(); } }
        /// <summary>
        /// Parser error message
        /// </summary>
        internal string ErrorMsg { get; private set; }
        /// <summary>
        /// Address:port for export path subscribe socket
        /// </summary>
        internal string ExportIn { get { return export.InputPort; } }
        /// <summary>
        /// Address:port for export path publish socket
        /// </summary>
        internal string ExportOut { get { return export.OutputPort; } }
        /// <summary>
        /// Address:port for import path subscribe socket
        /// </summary>
        internal string ImportIn { get { return import.InputPort; } }
        /// <summary>
        /// Address:port for import path publish socket
        /// </summary>
        internal string ImportOut { get { return import.OutputPort; } }
        /// <summary>
        /// OSP messaging protocol
        /// </summary>
        internal ModuleOsp.OspProtocol Protocol { get { return export.Protocol; } }

        internal string DesignDocReference { get; private set; }

        internal string SyslogServerIp { get; private set; }

        private bool SetLogger()
        {
            // Extract Logger settings from the deploy doc

            // There should only be one host module
            List<IModule> hostList = component.Modules.FindAll(x => x.GetModuleType() == Enums.ModuleType.host);
            if (hostList.Count != 1)
            {
                ErrorMsg = "Too many/few Host modules in the Component specification";
                return false;
            }
            // get the host module
            ModuleHost host = (ModuleHost)hostList[0];

            // Extract the FIRST logging entry (dunno what happens if we have 2+ loggers)
            // Check logger is defined
            if (host.Logging.Count < 1)
            {
                ErrorMsg = "No log host defined";
                return false;
            }
            SyslogServerIp = host.Logging[0].Name;
            return true;
        }

        /// <summary>
        /// Determine the OSP settings from the Deploy document
        /// </summary>
        /// <returns>true if successful</returns>
        private bool SetOsp()
        {
            // We require two OSP modules to be defined
            List<ModuleOsp> ospList = component.Modules.FindAll(x => x.GetModuleType() == Enums.ModuleType.osp).Cast<ModuleOsp>().ToList();
            if (ospList.Count != 2)
            {
                ErrorMsg = "Too many/few OSP modules in the Component specification";
                return false;
            }
            export = ospList.Find(x => x.Path.ToUpper() == "EXPORTPATH");
            if (export == null)
            {
                ErrorMsg = "OSP: Export path not defined";
                return false;
            }
            import = ospList.Find(x => x.Path.ToUpper() == "IMPORTPATH");
            if (import == null)
            {
                ErrorMsg = "OSP: Import path not defined";
                return false;
            }

            // Extract OSP settings from the deploy doc
            if ((export.Protocol == ModuleOsp.OspProtocol.INVALID) || (import.Protocol == ModuleOsp.OspProtocol.INVALID))
            {
                ErrorMsg = "OSP: Protocol not set for exportPath or importPath";
                return false;
            }
            if (export.Protocol != import.Protocol)
            {
                ErrorMsg = "OSP: Mismatch between importPath and exportPath protocol";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determine the export policy 
        /// </summary>
        /// <returns>true if no errors</returns>
        private bool setExportPolicy()
        {
            // Initialise export policy
            exportPolicy = new RuleSet("exportPolicy");

            // Build export policy from the deploy doc
            List<IModule> exportList = component.Modules.FindAll(x => x.GetModuleType() == Enums.ModuleType.export);
            if (exportList.Count != 1)
            {
                ErrorMsg = "No Export module in the Component specification";
                return false;
            }
            ModuleExport exportPol = (ModuleExport)exportList[0];
            // Get the export module


            foreach (Source source in exportPol.Sources)
            {
                string _fed, _ent, _obj, _attr;

                switch (source.SourceType)
                {
                    case Source.Type.Federate:
                        _fed = source.FederateName;
                        _ent = "*";
                        break;
                    case Source.Type.Entity:
                        _fed = (source.FederateName == "") ? "*" : source.FederateName;
                        _ent = source.EntityId;
                        break;
                    default:
                        ErrorMsg = "Export: Invalid source type found in Export Module";
                        return false;
                }
                foreach (HlaObject obj in source.Objects)
                {
                    _obj = obj.ObjectClassName;
                    if (obj.Attributes.Count > 0)
                        foreach (HlaAttribute attrib in obj.Attributes)
                        {
                            _attr = attrib.AttributeName;
                            exportPolicy.Add(_fed, _ent, _obj, _attr);
                        }
                    else
                        exportPolicy.Add(_fed, _ent, _obj, "*");
                }
                foreach (HlaInteraction inter in source.Interactions)
                {
                    _obj = inter.InteractionClassName;
                    exportPolicy.Add(_fed, _ent, _obj, "*");
                }
            }
            return true;
        }

        /// <summary>
        /// Determine the import policy
        /// </summary>
        /// <returns>true if no errors</returns>
        private bool setImportPolicy()
        {
            // Initialise import policy
            importPolicy = new RuleSet("importPolicy");

            // Build export policy from the deploy doc
            List<IModule> importList = component.Modules.FindAll(x => x.GetModuleType() == Enums.ModuleType.import);
            if (importList.Count != 1)
            {
                ErrorMsg = "No Import module in the Component specification";
                return false;
            }
            ModuleImport importPol = (ModuleImport)importList[0];

            string _fed = "*", _ent = "*", _obj, _attr;

            foreach (HlaObject obj in importPol.Objects)
            {
                _obj = obj.ObjectClassName;
                if (obj.Attributes.Count > 0)
                    foreach (HlaAttribute attrib in obj.Attributes)
                    {
                        _attr = attrib.AttributeName;
                        importPolicy.Add(_fed, _ent, _obj, _attr);
                    }
                else
                    importPolicy.Add(_fed, _ent, _obj, "*");
            }
            foreach (HlaInteraction inter in importPol.Interactions)
            {
                _obj = inter.InteractionClassName;
                importPolicy.Add(_fed, _ent, _obj, "*");
            }
            return true;
        }
    }
}
