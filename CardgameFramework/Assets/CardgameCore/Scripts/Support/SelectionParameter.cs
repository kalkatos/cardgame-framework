using System.Collections;
using System.Collections.Generic;

namespace CardgameCore
{
	public abstract class SelectionParameter<T>
	{
		public abstract bool IsAMatch (T obj);
	}

	public class CardIDParameter : SelectionParameter<CGComponent>
	{
		public string value;

		public CardIDParameter (string value)
		{
			this.value = value;
		}

		public override bool IsAMatch (CGComponent component)
		{
			return component.id == value;
		}
	}

	public class MatchStringVariableParameter : SelectionParameter<CGComponent>
	{
		public string variableName;

		public MatchStringVariableParameter (string variableName)
		{
			this.variableName = variableName;
		}

		public override bool IsAMatch (CGComponent component)
		{
			return component.id == Match.GetVariable(variableName);
		}
	}

	public class MatchStringZoneVariableParameter : SelectionParameter<Zone>
	{
		public string variableName;

		public MatchStringZoneVariableParameter (string variableName)
		{
			this.variableName = variableName;
		}

		public override bool IsAMatch (Zone zone)
		{
			return zone.id == Match.GetVariable(variableName);
		}
	}

	public class MatchStringRuleVariableParameter : SelectionParameter<Rule>
	{
		public string variableName;

		public MatchStringRuleVariableParameter (string variableName)
		{
			this.variableName = variableName;
		}

		public override bool IsAMatch (Rule rule)
		{
			return rule.id == Match.GetVariable(variableName);
		}
	}

	public class ZoneIDParameter : SelectionParameter<Zone>
	{
		public string value;

		public ZoneIDParameter (string value)
		{
			this.value = value;
		}

		public override bool IsAMatch (Zone zone)
		{
			return zone.id == value;
		}
	}

	public class RuleIDParameter : SelectionParameter<Rule>
	{
		public string value;

		public RuleIDParameter (string value)
		{
			this.value = value;
		}

		public override bool IsAMatch (Rule rule)
		{
			return rule.id == value;
		}
	}

	public class ComponentFieldParameter : SelectionParameter<CGComponent>
	{
		NestedComponentFieldConditions cardFieldConditions;

		public ComponentFieldParameter (NestedComponentFieldConditions cardFieldConditions)
		{
			this.cardFieldConditions = cardFieldConditions;
		}

		public override bool IsAMatch (CGComponent component)
		{
			return cardFieldConditions.Evaluate(component);
		}
	}

	public class TagParameter : SelectionParameter<string>
	{
		NestedStrings tags;

		public TagParameter (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool IsAMatch (string str)
		{
			return tags.Evaluate(str.Split(','));
		}
	}

	public class ComponentTagParameter : SelectionParameter<CGComponent>
	{
		NestedStrings tags;

		public ComponentTagParameter (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool IsAMatch (CGComponent obj)
		{
			return tags.Evaluate(obj.tagArray);
		}
	}

	public class ZoneTagParameter : SelectionParameter<Zone>
	{
		NestedStrings tags;

		public ZoneTagParameter (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool IsAMatch (Zone zone)
		{
			return tags.Evaluate(zone.tagArray);
		}
	}

	public class RuleTagParameter : SelectionParameter<Rule>
	{
		NestedStrings tags;

		public RuleTagParameter (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool IsAMatch (Rule rule)
		{
			return tags.Evaluate(rule.tags.Split(','));
		}
	}

	public class ComponentZoneTagParameter : SelectionParameter<CGComponent>
	{
		NestedStrings tags;

		public ComponentZoneTagParameter (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool IsAMatch (CGComponent obj)
		{
			if (obj.Zone != null)
			{
				return tags.Evaluate(obj.Zone.tagArray);
			}
			return false;
		}
	}

	public class ComponentZoneIDParameter : SelectionParameter<CGComponent>
	{
		string zoneID;

		public ComponentZoneIDParameter(string zoneID)
		{
			this.zoneID = zoneID;
		}

		public override bool IsAMatch(CGComponent obj)
		{
			if (obj.Zone != null)
			{
				return obj.Zone.id == Match.GetVariable(zoneID);
			}
			return false;
		}
	}

	public class ComponentIndexParamenter : SelectionParameter<CGComponent>
	{
		NestedComponentIndexConditions nestedIndexes;

		public ComponentIndexParamenter (NestedComponentIndexConditions nestedIndexes)
		{
			this.nestedIndexes = nestedIndexes;
		}

		public override bool IsAMatch (CGComponent component)
		{
			return nestedIndexes.Evaluate(component);
		}
	}
}