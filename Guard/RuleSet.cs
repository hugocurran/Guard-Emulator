using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Guard_Emulator
{
    /// <summary>
    /// Specification for the Guard ruleset
    /// </summary>
    internal class RuleSet
    {
        XDocument ruleSet;
        XElement firstElement;
        int counter;

        /// <summary>
        /// Constructor initialises a new ruleset
        /// </summary>
        /// <param name="ruleSetName">Name for the policy (eg exportPolicy)</param>
        internal RuleSet(string ruleSetName)
        {
            // Initialise policy
            ruleSet = new XDocument();
            ruleSet.AddFirst(new XElement(ruleSetName));
            firstElement = ruleSet.Element(ruleSetName);
            counter = 1;
        }

        /// <summary>
        /// Add a rule to the Guard ruleset
        /// </summary>
        /// <param name="fed">Federate name or *</param>
        /// <param name="ent">EntityID or *</param>
        /// <param name="obj">Object/Interaction classname or *</param>
        /// <param name="attr">Attribute/Parameter name or *</param>
        internal void Add(string fed, string ent, string obj, string attr)
        {
            XElement rule =
                new XElement("rule",
                    new XAttribute("ruleNumber", counter.ToString()),
                    new XElement("federate", fed),
                    new XElement("entity", ent),
                    new XElement("objectName", obj),
                    new XElement("attributeName", attr));
            firstElement.Add(rule);
            counter++;
        }

        /// <summary>
        /// Return the guard ruleset
        /// </summary>
        /// <returns>Ruleset</returns>
        internal XDocument GetRuleSet() { return ruleSet; }
    }
}
