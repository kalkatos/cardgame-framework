using System.Collections;
using System.Collections.Generic;

namespace CardgameCore
{
	public abstract class SelectionParameter<T>
	{
		public abstract bool IsAMatch (T obj);
	}

	#region Card

	public class CardIDParameter : SelectionParameter<Card>
	{
		public string value;

		public CardIDParameter (string value)
		{
			this.value = value;
		}

		public override bool IsAMatch (Card card)
		{
			return card.id == value;
		}
	}

	public class MatchStringVariableParameter : SelectionParameter<Card>
	{
		public string variableName;

		public MatchStringVariableParameter (string variableName)
		{
			this.variableName = variableName;
		}

		public override bool IsAMatch (Card card)
		{
			return card.id == Match.GetVariable(variableName);
		}
	}

	public class CardFieldParameter : SelectionParameter<Card>
	{
		NestedCardFieldConditions cardFieldConditions;

		public CardFieldParameter (NestedCardFieldConditions cardFieldConditions)
		{
			this.cardFieldConditions = cardFieldConditions;
		}

		public override bool IsAMatch (Card card)
		{
			return cardFieldConditions.Evaluate(card);
		}
	}

	public class CardTagParameter : SelectionParameter<Card>
	{
		NestedStrings tags;

		public CardTagParameter (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool IsAMatch (Card obj)
		{
			return tags.Evaluate(obj.tagArray);
		}
	}

	public class CardZoneTagParameter : SelectionParameter<Card>
	{
		NestedStrings tags;

		public CardZoneTagParameter (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool IsAMatch (Card obj)
		{
			if (obj.Zone != null)
			{
				return tags.Evaluate(obj.Zone.tagArray);
			}
			return false;
		}
	}

	public class CardZoneIDParameter : SelectionParameter<Card>
	{
		string zoneID;

		public CardZoneIDParameter(string zoneID)
		{
			this.zoneID = zoneID;
		}

		public override bool IsAMatch(Card obj)
		{
			if (obj.Zone != null)
			{
				return obj.Zone.id == Match.GetVariable(zoneID);
			}
			return false;
		}
	}

	public class CardIndexParamenter : SelectionParameter<Card>
	{
		NestedCardIndexConditions nestedIndexes;

		public CardIndexParamenter (NestedCardIndexConditions nestedIndexes)
		{
			this.nestedIndexes = nestedIndexes;
		}

		public override bool IsAMatch (Card card)
		{
			return nestedIndexes.Evaluate(card);
		}
	}

	#endregion

	#region Zone

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

	public class ZoneByCardsParameter : SelectionParameter<Zone>
	{
		public CardSelector cardSelector;

		public ZoneByCardsParameter (CardSelector cardSelector)
		{
			this.cardSelector = cardSelector;
		}

		public override bool IsAMatch (Zone zone)
		{
			List<Card> selection = (List<Card>)cardSelector.Get();
			for (int i = 0; i < selection.Count; i++)
				if (selection[i].Zone == zone)
					return true;
			return false;
		}
	}

	#endregion

	#region Rule

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

	#endregion
}