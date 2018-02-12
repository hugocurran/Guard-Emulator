
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Guard_Emulator
{
    /// <summary>
    /// Guard path processor
    /// </summary>
    public abstract class Processor
    {
        // Set a default rule match value
        protected string ruleNumber = "NOMATCH";

        /// <summary>
        /// Rule number that matches the message (0 = status/heartbeat message)
        /// </summary>
        public string RuleNumber { get { return ruleNumber; } }

        protected Logger logger;
        protected string id;
        protected Processor()
        {
            logger = Logger.Instance;
            if (!logger.IsInitialised)
                throw new Exception("Logger has not been initialised");
        }
        
        /// <summary>
        /// Test the message against the policy ruleset
        /// </summary>
        /// <param name="message">message in standardised internal format</param>
        /// <returns>True if message permitted, else False</returns>
        internal bool ApplyPolicy(InternalMessage intMessage, XDocument ruleSet)
        {
            // Reset ruleNumber
            ruleNumber = "NOMATCH";

            // Load the policy rules
            IEnumerable<XElement> rules = ruleSet.Descendants("rule");

            // Phase 0: filter out heartbeats etc.
            if (intMessage.Type == MessageType.Status)
            {
                ruleNumber = "0";
                return true;
            }

            // Phase 1: check the message against federates
            IEnumerable<XElement> federateMatches =
                from el in rules
                where (string)el.Element("federate") == intMessage.Federate
                select el;
            if (federateMatches.Count() == 0)
            {
                federateMatches =
                    from el in rules
                    where (string)el.Element("federate") == "*"
                    select el;
                if (federateMatches.Count() == 0)
                    return false;
            }

            // Phase 2: check the message against entities
            IEnumerable<XElement> entityMatches =
                from el in federateMatches
                where (string)el.Element("entity") == intMessage.EntityID
                select el;
            if (entityMatches.Count() == 0)
            {
                entityMatches =
                    from el in federateMatches
                    where (string)el.Element("entity") == "*"
                    select el;
                if (entityMatches.Count() == 0)
                    return false;
            }

            // Phase 3a: check the message against object names
            IEnumerable<XElement> objectMatches = null;
            if ((intMessage.Type == MessageType.ObjectCreate) || (intMessage.Type == MessageType.ObjectDelete) || (intMessage.Type == MessageType.ObjectUpdate))
            {
                objectMatches =
                    from el in entityMatches
                    where (string)el.Element("objectName") == intMessage.ObjectName
                    select el;
                if (objectMatches.Count() == 0)
                {
                    objectMatches =
                        from el in entityMatches
                        where (string)el.Element("objectName") == "*"
                        select el;
                    if (objectMatches.Count() == 0)
                        return false;
                }
                if ((intMessage.Type == MessageType.ObjectCreate) || (intMessage.Type == MessageType.ObjectDelete))
                {
                    ruleNumber = objectMatches.ElementAt(0).Attribute("ruleNumber").Value;
                    return true;
                }
            }

            // Phase 3b: check the message against interaction names
            if (intMessage.Type == MessageType.Interaction)
            {
                IEnumerable<XElement> interactionMatches =
                    from el in entityMatches
                    where (string)el.Element("objectName") == intMessage.InteractionName
                    select el;
                if (interactionMatches.Count() == 0)
                {
                    interactionMatches =
                        from el in entityMatches
                        where (string)el.Element("objectName") == "*"
                        select el;
                }
                if (interactionMatches.Count() == 0)
                    return false;
                else
                {
                    ruleNumber = interactionMatches.ElementAt(0).Attribute("ruleNumber").Value;
                    return true;    // Don't check parameters
                }
            }

            // Phase 4: check the message against attribute names
            if (intMessage.Type == MessageType.ObjectUpdate)
            {
                foreach (string attrib in intMessage.Attribute)
                {
                    IEnumerable<XElement> attribMatches =
                        from el in objectMatches
                        where (string)el.Element("attributeName") == attrib
                        select el;
                    if (attribMatches.Count() == 0)
                    {
                        attribMatches =
                            from el in entityMatches
                            where (string)el.Element("attributeName") == "*"
                            select el;
                        if (attribMatches.Count() == 0)
                            return false;
                    }
                    ruleNumber = objectMatches.ElementAt(0).Attribute("ruleNumber").Value;
                    return true;
                }
            }
            return false;
        }

        protected string WhoAmI(XDocument policy)
        {
            string policyName = policy.Root.Name.LocalName;
            if (policyName.Contains("export"))
                 return "Export ";
            else
                return "Import ";
        }
    }
}
