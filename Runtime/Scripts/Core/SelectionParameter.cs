using System.Collections;
using System.Collections.Generic;

namespace CardgameFramework
{
	public abstract class SelectionParameter<T>
	{
		internal abstract bool IsAMatch (T obj);
	}

	#region Card

	internal class CardIDParameter : SelectionParameter<Card>
	{
		internal string value;

		internal CardIDParameter (string value)
		{
			this.value = value;
		}

		internal override bool IsAMatch (Card card)
		{
			return card.id == value;
		}
	}

	internal class MatchStringVariableParameter : SelectionParameter<Card>
	{
		internal string variableName;

		internal MatchStringVariableParameter (string variableName)
		{
			this.variableName = variableName;
		}

		internal override bool IsAMatch (Card card)
		{
			return card.id == Match.GetVariable(variableName);
		}
	}

	internal class CardFieldParameter : SelectionParameter<Card>
	{
		NestedCardFieldConditions cardFieldConditions;

		internal CardFieldParameter (string cardFieldConditions)
		{
			this.cardFieldConditions = new NestedCardFieldConditions(cardFieldConditions);
		}

		internal override bool IsAMatch (Card card)
		{
			return cardFieldConditions.Evaluate(card);
		}
	}

	internal class CardTagParameter : SelectionParameter<Card>
	{
		NestedStrings tags;

		internal CardTagParameter (string tags)
		{
			this.tags = new NestedStrings(tags);
		}

		internal override bool IsAMatch (Card obj)
		{
			return tags.Evaluate(obj.tagArray);
		}
	}

	internal class CardZoneTagParameter : SelectionParameter<Card>
	{
		NestedStrings tags;

		internal CardZoneTagParameter (string tags)
		{
			this.tags = new NestedStrings(tags);
		}

		internal override bool IsAMatch (Card obj)
		{
			if (obj.Zone != null)
			{
				return tags.Evaluate(obj.Zone.tagArray);
			}
			return false;
		}
	}

	internal class CardZoneIDParameter : SelectionParameter<Card>
	{
		string zoneID;

		internal CardZoneIDParameter(string zoneID)
		{
			this.zoneID = zoneID;
		}

		internal override bool IsAMatch(Card obj)
		{
			if (obj.Zone != null)
			{
				return obj.Zone.id == Match.GetVariable(zoneID);
			}
			return false;
		}
	}

	internal class CardIndexParamenter : SelectionParameter<Card>
	{
		NestedCardIndexConditions nestedIndexes;

		internal CardIndexParamenter (string nestedIndexes)
		{
			this.nestedIndexes = new NestedCardIndexConditions(nestedIndexes);
		}

		internal override bool IsAMatch (Card card)
		{
			return nestedIndexes.Evaluate(card);
		}
	}

	#endregion

	#region Zone

	internal class MatchStringZoneVariableParameter : SelectionParameter<Zone>
	{
		internal string variableName;

		internal MatchStringZoneVariableParameter (string variableName)
		{
			this.variableName = variableName;
		}

		internal override bool IsAMatch (Zone zone)
		{
			return zone.id == Match.GetVariable(variableName);
		}
	}

	internal class MatchStringRuleVariableParameter : SelectionParameter<Rule>
	{
		internal string variableName;

		internal MatchStringRuleVariableParameter (string variableName)
		{
			this.variableName = variableName;
		}

		internal override bool IsAMatch (Rule rule)
		{
			return rule.id == Match.GetVariable(variableName);
		}
	}

	internal class ZoneIDParameter : SelectionParameter<Zone>
	{
		internal string value;

		internal ZoneIDParameter (string value)
		{
			this.value = value;
		}

		internal override bool IsAMatch (Zone zone)
		{
			return zone.id == value;
		}
	}

	internal class ZoneTagParameter : SelectionParameter<Zone>
	{
		NestedStrings tags;

		internal ZoneTagParameter (string tags)
		{
			this.tags = new NestedStrings(tags);
		}

		internal override bool IsAMatch (Zone zone)
		{
			return tags.Evaluate(zone.tagArray);
		}
	}

	internal class ZoneByCardsParameter : SelectionParameter<Zone>
	{
		internal CardSelector cardSelector;

		internal ZoneByCardsParameter (CardSelector cardSelector)
		{
			this.cardSelector = cardSelector;
		}

		internal override bool IsAMatch (Zone zone)
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

	internal class RuleIDParameter : SelectionParameter<Rule>
	{
		internal string value;

		internal RuleIDParameter (string value)
		{
			this.value = value;
		}

		internal override bool IsAMatch (Rule rule)
		{
			return rule.id == value;
		}
	}

	internal class RuleTagParameter : SelectionParameter<Rule>
	{
		NestedStrings tags;

		internal RuleTagParameter (NestedStrings tags)
		{
			this.tags = tags;
		}

		internal override bool IsAMatch (Rule rule)
		{
			return tags.Evaluate(rule.tags.Split(','));
		}
	}

	#endregion
}