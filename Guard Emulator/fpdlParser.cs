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

    internal class fpdlParser
    {
        private XmlSchemaSet schema = new XmlSchemaSet();
        
        private XDocument deploy;

        internal string errorMsg { get; private set; }

        internal fpdlParser() { }

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

        internal bool LoadDeployDocument(string fileName)
        {
            /*
             * No schema validation support
             * 
            if (!schema.Contains("http://www.niteworks.net/fpdl"))
            {
                errorMsg = "No FPDL schema loaded";
                return false;
            }
            */
            try
            {
                deploy = XDocument.Load(fileName);
                // deploy.Validate(schema, null);
            }
            catch (FileNotFoundException e)
            {
                errorMsg = e.Message;
                return false;
            }
            catch (XmlSchemaValidationException e)
            {
                errorMsg = "Validation error: " + e.Message;
                return false;
            }

            // Validate the file

            if (deploy.Element("Deploy").Element("system").Attribute("systemType").Value != "Gateway")
            {
                errorMsg = "Invalid systemType = " + deploy.Element("Deploy").Element("system").Attribute("systemType").Value;
                return false;
            }
            return true;
        }

        internal XDocument ExportPolicy()
        {
            XDocument exportPolicy = new XDocument();
            exportPolicy.AddFirst(new XElement("exportPolicy"));
            int counter = 1;

            // Get a set of objects from the deploy doc
            IEnumerable<XElement> sourceList =
                from item in deploy.Descendants("export")
                select (XElement)item.Element("source");

            foreach (XElement source in sourceList)
            { 
                string _fed, _ent, _obj, _attr;
                
                if (source.Element("federateSource") != null)
                {
                    _fed = source.Element("federateSource").Value;
                    _ent = "*";
                }
                else
                {
                    _fed = "*";
                    _ent = source.Element("entitySource").Value;
                }
                IEnumerable<XElement> objectList =
                    from item in source.Descendants("object")
                    select (XElement)item.Element("object");
                foreach (XElement objectName in objectList)
                {
                    _obj = objectName.Element("objectClassName").Value;
                    IEnumerable<XElement> attributeList =
                        from item in objectName.Descendants("attributeName")
                        select (XElement)item.Element("attributeName");
                    foreach(XElement attribute in attributeList)
                    {
                        _attr = attribute.Element("attributeName").Value;
                        XElement policyLine =
                            new XElement("Rule",
                                new XAttribute("ruleNumber", counter.ToString()),
                                new XElement("federate", _fed),
                                new XElement("entity", _ent),
                                new XElement("objectName", _obj),
                                new XElement("attributeName", _attr));

                        exportPolicy.AddAfterSelf(policyLine);
                        counter++;
                    }
                }
            }

            return exportPolicy;
        }

    }
}
