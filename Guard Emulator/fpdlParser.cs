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
                    ErrorMsg = "Invalid pattern: " + pattern;
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

        internal XDocument ExportPolicy { get { return _exportPolicy(); } }
        internal XDocument ImportPolicy { get { return _importPolicy(); } }
        internal string ErrorMsg { get; private set; }
        internal string ExportSub { get; private set; }
        internal string ExportPub { get; private set; }
        internal string ImportSub { get; private set; }
        internal string ImportPub { get; private set; }
        internal OspProtocol ExportProtocol { get; private set; }
        internal OspProtocol ImportProtocol { get; private set; }


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

            foreach (XElement osp in ospList)
            {
                // Export
                if (osp.Element(f + "path").Value == "ExportPath")
                {
                    ExportSub = osp.Element(f + "inputPort").Value;
                    ExportPub = osp.Element(f + "outputPort").Value;
                    if (osp.Element(f+ "protocol").Value == "HPSD")
                        ExportProtocol = OspProtocol.HPSD;
                    else
                        ExportProtocol = OspProtocol.WebLVC;
                }
                // Import
                else
                {
                    ImportSub = osp.Element(f + "inputPort").Value;
                    ImportPub = osp.Element(f + "outputPort").Value;
                    if (osp.Element(f+ "protocol").Value == "HPSD")
                        ImportProtocol = OspProtocol.HPSD;
                    else
                        ImportProtocol = OspProtocol.WebLVC;
                }
            }
            if (ExportProtocol != ImportProtocol)
            {
                ErrorMsg = "Mismatch between import and export messaging protocol";
                return false;
            }
            return true;
        }

        private XDocument _exportPolicy()
        {
            // Initialise export policy
            Policy exportPolicy = new Policy("exportPolicy");

            // Build export policy from the deploy doc
            IEnumerable<XElement> componentList = deploy.Element(f+"Deploy").Element(f + "system").Elements(f + "component");
            
            IEnumerable<XElement> sourceList = null;
            foreach (XElement component in componentList)
            {
                if (component.Attribute("componentType").Value == "guard")
                {
                    sourceList = component.Element(f + "export").Descendants(f+"source");
                    break;
                }
            }
            if (sourceList == null)
            {
                ErrorMsg = "No component defining Guard found";
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
            return exportPolicy.GetPolicy();
        }
        private XDocument _importPolicy()
        {
            // Initialise import policy
            Policy importPolicy = new Policy("importPolicy");
            
            // Build import policy from the deploy doc
            IEnumerable<XElement> componentList = deploy.Element(f + "Deploy").Element(f + "system").Elements(f + "component");

            IEnumerable<XElement> importList = null;
            foreach (XElement component in componentList)
            {
                if (component.Attribute("componentType").Value == "guard")
                {
                    importList = component.Descendants(f + "import");
                    break;
                }
            }
            if (importList == null)
            {
                ErrorMsg = "No component defining Guard found";
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
            return importPolicy.GetPolicy();
        }
    }

    internal class Policy
    {
        XDocument policy;
        XElement firstElement;
        int counter;

        internal Policy(string policyName)
        {
            // Initialise policy
            policy = new XDocument();
            policy.AddFirst(new XElement(policyName));
            firstElement = policy.Element(policyName);
            counter = 1;
        }

        internal void Add(string fed, string ent, string obj, string attr)
        {
            XElement policyLine =
                new XElement("rule",
                    new XAttribute("ruleNumber", counter.ToString()),
                    new XElement("federate", fed),
                    new XElement("entity", ent),
                    new XElement("objectName", obj),
                    new XElement("attributeName", attr));
            firstElement.Add(policyLine);
            counter++;
        }
        
        internal XDocument GetPolicy() {  return policy; }
    }
}
