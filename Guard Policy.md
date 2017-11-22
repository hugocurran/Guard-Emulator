Guard Policy
============

The guard policy is created from the Deploy document.
The Deploy document must:
	1. Specify a single System with a systemType of 'HTG'
	2. The System must include a Component of Type 'Guard'
	3. The Guard component must define:
		a.  At least 2 interfaces (3 once logging is setup)
		b.  OSP settings for each side of the guard (2 osp)
		c.  Export policy: This defines what is permitted from the high-side to the low-side
		d.  Import policy: this defines what is permitted from the low side to the high side

Guard Policies
==============
1. Each path through the guard (export and import) has a separate policy.
2. The guard uses XML to express the policy.  The general format is:

<policyName>
	<rule ruleNumber=n>
		<federate>name</federate>
		<entity>entityID</entity>
		<object>object.class.name</object>
		<attribute>attribute name</attribute>
	</rule>
</policyName>

All elements of a rule must contain a value; if the value is unknown then the value is '*' which is a generic wildcard.  The treatment of the wildcard depends 
upon the message type being processed (see below).

Message Parsers
===============
1. Message parsers are available for HPSD and WebLVC.
2. The osp entries in the deploy file should specify the message protocol as either 'HPSD' or 'WebLVC'.
	a. The osp entries must specify the same protocol (exit with error if this is not the case).
3. Both parsers translate their messages to a common internal format; this internal format is used for policy validation.

Export Policy
=============

1. The guard parses the export policy from the Deploy file and creates a policy ruleset with the name 'exportPolicy'.
2. Where the export policy is defined against a federate source, the <federate> value in the ruleset is set to the federateName value from the policy; the <entity> value
	is set to '*'.
	a. This is interpreted as 'any entity within the named federate'
3. Where the export policy is defined against an entity source, the <federate> value in the ruleset is set to '*'; the <entity> value is set to the entityID specified in 
	the export policy.
	a. This means that messages will always match the federate wildcard before entities are processed.
4. Export policy reads the object class name, which must be given, and stores this in the <object> value.
	a. Each attribute defined for the object class name results in a separate rule ie <object> <attribute> pair.
	b. If no attribute names are specified in the policy then the <attribute> value is '*' - this permits any attribute with a matching
	<object> name.

Import Policy
=============
1. The guard parses the import policy from the Deploy file and creates a policy ruleset with the name 'importPolicy'.
2. <federate> and <entity> values are always '*' for import policy, as source is not enforced.
3. Export policy reads the object class name, which must be given, and stores this in the <object> value.
	a. Each attribute defined for the object class name results in a separate rule ie <object> <attribute> pair.
	b. If no attribute names are specified in the policy then the <attribute> value is '*' - this permits any attribute with a matching
	<object> name.
	
Guard Ruleset Processing
========================
1. An arriving message is parsed to the internal message format by the appropriate parser
2. The ruleset processor iterates across the entire ruleset until a match is found.  
3. A matching rule results in the original message being released for onward transmission to the proxy.
4. There are multiple shortcuts to avoid parsing the whole ruleset:
	a. The first test is for a matching federate name or '*'.  This produces the federate-set.
		- a null federate-set causes message rejection.
	b. From the federate-set a match is sought for entityID or '*'.  This produces the entity-set.
		- a null entity-set causes message rejection.
	c. From the entity-set a match is sought for the object name or '*'.  This produce the object-set.
		-	a null object-set causes message rejection.
	d. From the object-set a match is sought for the attribute name or '*'. This produces the attribute-set.
		- a null attribute-set causes message rejection.
5. Given that Export Policy must specify the federate or entity source, but Import Policy does not it follows that there is inherently
more of the ruleset parsed for import compared to export.  This can be avoided by optimising the rules to avoid duplicate object names.

Interaction Processing
======================
1. There is no special processing for interaction messages.  The rule <object> value stores the class name of the interaction; the policy
does not currently extend to parameters, so all rules for interactions have <attribute> set to '*'.







