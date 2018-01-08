using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using System.Xml.XPath;

[assembly: InternalsVisibleTo("Tests")]

namespace Guard_Emulator
{

    /// <summary>
    /// Class and methods for parsing FPDL Deploy files
    /// </summary>

    internal class FpdlParser
    {
        //private XmlSchemaSet schema = new XmlSchemaSet();

        private XDocument deploy;
        private XNamespace f = "http://www.niteworks.net/fpdl";
        

        internal FpdlParser() { }

        #region Schema checking
        /*
         * Microsoft STILL does not support XSD 1.1 so we cannot use the .net
         * schema stuff to validate a FPDL Deploy document
         * 
        internal bool LoadSchema(string nameSpace, string schemaLocation)
        {

            XmlUrlResolver resolver = new XmlUrlResolver();
            resolver.ResolveUri(new Uri(schemaLocation), "");
            schema.XmlResolver = resolver;
            try
            {   
                schema.Add(nameSpace, schemaLocation + "FPDL-ver0.2.xsd");
                schema.Compile();
            }
            catch (XmlSchemaException e)
            {
                errorMsg = "Schema load error: " + e.Message;
                return false;
            }
            catch (ArgumentNullException e)
            {
                errorMsg = "Null exception error: " + e.Message;
                return false;
            }
            catch (FileNotFoundException e)
            {
                errorMsg = e.Message;
                return false;
            }
            if (!schema.Contains("http://www.niteworks.net/fpdl"))
            {
                errorMsg = "FPDL namespace not found";
                return false;
            }
            return true;
        
    */
        #endregion

            /// <summary>
            /// Load a FPDL Deploy document into the parser
            /// </summary>
            /// <param name="fileName">Deploy document filename</param>
            /// <returns>true if successful, else false</returns>
        internal bool LoadDeployDocument(string fileName)
        {
            try
            {
                deploy = XDocument.Load(fileName);

                string designDocReference = deploy.Element(f + "Deploy").Element(f + "designDocReference").Value;
                Logger.Log("Loaded Deploy File with Design Document Reference: " + designDocReference);
                
                string systemType = deploy.Element(f + "Deploy").Element(f + "system").Attribute("systemType").Value;
                if (systemType != "Gateway")
                {
                    ErrorMsg = "Invalid system type: " + systemType;
                    return false;
                }
                string pattern = deploy.Element(f + "Deploy").Element(f + "system").Element(f + "pattern").Value;
                if (pattern != "HTG")
                {
                    ErrorMsg = "Invalid deplopyment pattern: " + pattern;
                    return false;
                }
            }
            catch (FileNotFoundException e)
            {
                ErrorMsg = e.Message;
                return false;
            }
            return (ParseOsp());
        }

        /// <summary>
        /// Guard component export policy
        /// </summary>
        internal XDocument ExportPolicy { get { return _exportPolicy(); } }
        /// <summary>
        /// Guard component import policy
        /// </summary>
        internal XDocument ImportPolicy { get { return _importPolicy(); } }
        /// <summary>
        /// Parser error message
        /// </summary>
        internal string ErrorMsg { get; private set; }
        /// <summary>
        /// Address:port for export path subscribe socket
        /// </summary>
        internal string ExportSub { get; private set; }
        /// <summary>
        /// Address:port for export path publish socket
        /// </summary>
        internal string ExportPub { get; private set; }
        /// <summary>
        /// Address:port for import path subscribe socket
        /// </summary>
        internal string ImportSub { get; private set; }
        /// <summary>
        /// Address:port for import path publish socket
        /// </summary>
        internal string ImportPub { get; private set; }
        /// <summary>
        /// OSP messaging protocol
        /// </summary>
        internal OspProtocol Protocol { get; private set; }

        /// <summary>
        /// Determine the OSP settings from the Deploy document
        /// </summary>
        /// <returns>true if successful</returns>
        private bool ParseOsp()
        {
            // Extract OSP settings from the deploy doc
            // Build import policy from the deploy doc
            IEnumerable<XElement> componentList = deploy.Element(f + "Deploy").Element(f + "system").Elements(f + "component");

            IEnumerable<XElement> interfaceList = null;
            IEnumerable<XElement> ospList = null;
            foreach (XElement component in componentList)
            {
                if (component.Attribute("componentType").Value == "Guard")
                {
                    interfaceList = component.Descendants(f + "interface");
                    ospList = component.Descendants(f + "osp");
                    break;
                }
            }
            if ((interfaceList == null) || (ospList == null))
            {
                ErrorMsg = "No component defining Guard found";
                return false;
            }
            if ((interfaceList.Count() < 2) || (ospList.Count() != 2))
            {
                ErrorMsg = "Invalid component specification for Guard (wrong interface or osp count";
                return false;
            }

            Dictionary<string, string> interfaces = new Dictionary<string, string> ();
            foreach (XElement iface in interfaceList)
            {
                interfaces.Add(iface.Element(f + "interfaceName").Value, iface.Element(f + "ipAddress").Value);
            }

            OspProtocol exportProtocol = OspProtocol.INVALID, importProtocol = OspProtocol.INVALID;
            foreach (XElement osp in ospList)
            {
                // Export
                if (osp.Element(f + "path").Value == "ExportPath")
                {
                    ExportSub = osp.Element(f + "inputPort").Value;
                    ExportPub = osp.Element(f + "outputPort").Value;
                    switch(osp.Element(f+ "protocol").Value)
                    {
                        case "HPSD_ZMQ":
                            exportProtocol = OspProtocol.HPSD_ZMQ;
                            break;
                        case "HPSD_TCP":
                            exportProtocol = OspProtocol.HPSD_TCP;
                            break;
                        case "WebLVC_ZMQ":
                            exportProtocol = OspProtocol.WebLVC_ZMQ;
                            break;
                        case "WebLVC_TCP":
                            exportProtocol = OspProtocol.WebLVC_TCP;
                            break;
                        default:
                            exportProtocol = OspProtocol.INVALID;
                            break;
                    }
                }
                // Import
                else
                {
                    ImportSub = osp.Element(f + "inputPort").Value;
                    ImportPub = osp.Element(f + "outputPort").Value;
                    switch (osp.Element(f + "protocol").Value)
                    {
                        case "HPSD_ZMQ":
                            importProtocol = OspProtocol.HPSD_ZMQ;
                            break;
                        case "HPSD_TCP":
                            importProtocol = OspProtocol.HPSD_TCP;
                            break;
                        case "WebLVC_ZMQ":
                            importProtocol = OspProtocol.WebLVC_ZMQ;
                            break;
                        case "WebLVC_TCP":
                            importProtocol = OspProtocol.WebLVC_TCP;
                            break;
                        default:
                            importProtocol = OspProtocol.INVALID;
                            break;
                    }
                }
            }
            if ((exportProtocol == OspProtocol.INVALID) || (exportProtocol == OspProtocol.INVALID))
            {
                ErrorMsg = "Invalid import or export messaging protocol";
                return false;
            }
            if (exportProtocol != importProtocol)
            {
                ErrorMsg = "Mismatch between import and export messaging protocol";
                return false;
            }
            Protocol = exportProtocol;
            return true;
        }

        /// <summary>
        /// Determine the export policy 
        /// </summary>
        /// <returns>true if no errors</returns>
        private XDocument _exportPolicy()
        {
            // Initialise export policy
            RuleSet exportPolicy = new RuleSet("exportPolicy");

            // Build export policy from the deploy doc
            IEnumerable<XElement> componentList = deploy.Element(f+"Deploy").Element(f + "system").Elements(f + "component");
            
            IEnumerable<XElement> sourceList = null;
            foreach (XElement component in componentList)
            {
                if (component.Attribute("componentType").Value == "Guard")
                {
                    sourceList = component.Element(f + "export").Descendants(f+"source");
                    break;
                }
            }
            if (sourceList == null)
            {
                ErrorMsg = "No component defining Guard export policy found";
                return null;
            }

            foreach (XElement source in sourceList)
            { 
                string _fed, _ent, _obj, _attr;
                
                if (source.Element(f + "federateSource") != null)
                {
                    _fed = source.Element(f + "federateSource").Value;
                    _ent = "*";
                }
                else
                {
                    _fed = "*";
                    _ent = source.Element(f + "entitySource").Value;
                }
                if (source.Element(f + "object") != null)
                {
                    IEnumerable<XElement> objectList = source.Descendants(f + "object");
                    foreach (XElement objectName in objectList)
                    {
                        _obj = objectName.Element(f + "objectClassName").Value;
                        IEnumerable<XElement> attributeList = objectName.Descendants(f + "attributeName");
                        if (attributeList.Count() > 0)
                        {
                            foreach (XElement attribute in attributeList)
                            {
                                _attr = attribute.Value;
                                exportPolicy.Add(_fed, _ent, _obj, _attr);
                            }
                        }
                        else  //If no attributes defined then all attributes permitted
                        {
                            _attr = "*";
                            exportPolicy.Add(_fed, _ent, _obj, _attr);
                        }
                    }
                }
                if (source.Element(f + "interaction") != null)
                {
                    IEnumerable<XElement> interactionList = source.Descendants(f + "interaction");
                    foreach (XElement interactionName in interactionList)
                    {
                        _obj = interactionName.Element(f + "interactionClassName").Value;
                        exportPolicy.Add(_fed, _ent, _obj, "*");
                    }
                }
            }
            return exportPolicy.GetRuleSet();
        }

        /// <summary>
        /// Determine the import policy
        /// </summary>
        /// <returns>true if no errors</returns>
        private XDocument _importPolicy()
        {
            // Initialise import policy
            RuleSet importPolicy = new RuleSet("importPolicy");
            
            // Build import policy from the deploy doc
            IEnumerable<XElement> componentList = deploy.Element(f + "Deploy").Element(f + "system").Elements(f + "component");

            IEnumerable<XElement> importList = null;
            foreach (XElement component in componentList)
            {
                if (component.Attribute("componentType").Value == "Guard")
                {
                    importList = component.Descendants(f + "import");
                    break;
                }
            }
            if (importList == null)
            {
                ErrorMsg = "No component defining Guard import policy found";
                return null;
            }

            foreach (XElement source in importList)
            {
                string _fed = "*", _ent = "*", _obj, _attr;

                if (source.Element(f + "object") != null)
                {
                    IEnumerable<XElement> objectList = source.Descendants(f + "object");
                    foreach (XElement objectName in objectList)
                    {
                        _obj = objectName.Element(f + "objectClassName").Value;
                        IEnumerable<XElement> attributeList = objectName.Descendants(f + "attributeName");
                        if (attributeList.Count() > 0)
                        {
                            foreach (XElement attribute in attributeList)
                            {
                                _attr = attribute.Value;
                                importPolicy.Add(_fed, _ent, _obj, _attr);
                            }
                        }
                        else   // If no attribute defined then all are permitted
                        {
                            _attr = "*";
                            importPolicy.Add(_fed, _ent, _obj, _attr);
                        }
                    }
                }
                if (source.Element(f + "interaction") != null)
                {
                    IEnumerable<XElement> interactionList = source.Descendants(f + "interaction");
                    foreach (XElement interactionName in interactionList)
                    {
                        _obj = interactionName.Element(f + "interactionClassName").Value;
                        importPolicy.Add(_fed, _ent, _obj, "*");
                    }
                }
            }
            return importPolicy.GetRuleSet();
        }
    }
}
