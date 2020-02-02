using System.Collections;
using System.Collections.Generic;

namespace CardGameFramework
{
	public abstract class SelectionComponent<T>
	{
		public abstract bool Match (T obj);
	}

	public class CardIDComponent : SelectionComponent<Card>
	{
		public string value;

		public CardIDComponent (string value)
		{
			this.value = value;
		}

		public override bool Match (Card card)
		{
			return card.ID == value;
		}
	}

	public class MatchStringVariableComponent : SelectionComponent<Card>
	{
		public string variableName;

		public MatchStringVariableComponent (string variableName)
		{
			this.variableName = variableName;
		}

		public override bool Match (Card card)
		{
			return card.ID == (string)CardGameFramework.Match.Current.GetVariable(variableName);
		}
	}

	public class ZoneIDComponent : SelectionComponent<Zone>
	{
		public string value;

		public ZoneIDComponent (string value)
		{
			this.value = value;
		}

		public override bool Match (Zone zone)
		{
			return zone.ID == value;
		}
	}

	public class CardFieldComponent : SelectionComponent<Card>
	{
		NestedCardFieldConditions cardFieldConditions;

		public CardFieldComponent (NestedCardFieldConditions cardFieldConditions)
		{
			this.cardFieldConditions = cardFieldConditions;
		}

		//public CardFieldComponent (string fieldName, float numFieldValue)
		//{
		//	dataType = CardFieldDataType.Number;
		//	this.fieldName = fieldName;
		//	this.numFieldValue = numFieldValue;
		//}

		//public CardFieldComponent (string fieldName, string textFieldValue)
		//{
		//	dataType = CardFieldDataType.Text;
		//	this.fieldName = fieldName;
		//	this.textFieldValue = textFieldValue;
		//}

		public override bool Match (Card card)
		{
			return cardFieldConditions.Evaluate(card);
		}
	}

	public class TagComponent : SelectionComponent<string>
	{
		NestedStrings tags;

		public TagComponent (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool Match (string str)
		{
			return tags.Evaluate(str.Split(','));
		}
	}

	public class CardTagComponent : SelectionComponent<Card>
	{
		NestedStrings tags;

		public CardTagComponent (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool Match (Card obj)
		{
			return tags.Evaluate(obj.data.tags.Split(','));
		}
	}

	public class ZoneTagComponent : SelectionComponent<Zone>
	{
		NestedStrings tags;

		public ZoneTagComponent (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool Match (Zone zone)
		{
			return tags.Evaluate(zone.zoneTags.Split(','));
		}
	}

	public class CardZoneTagComponent : SelectionComponent<Card>
	{
		NestedStrings tags;

		public CardZoneTagComponent (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool Match (Card obj)
		{
			if (obj.zone != null)
			{
				return tags.Evaluate(obj.zone.zoneTags.Split(','));
			}
			return false;
		}
	}

	public class CardModifierTagComponent : SelectionComponent<Card>
	{
		NestedStrings tags;

		public CardModifierTagComponent (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool Match (Card obj)
		{
			if (obj.zone != null)
			{
				return tags.Evaluate(obj.GetTagsFromModifiers().Split(','));
			}
			return false;
		}
	}

}