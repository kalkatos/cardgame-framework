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
		CardFieldDataType dataType;
		public string fieldName;
		public string textFieldValue;
		public double numFieldValue;

		public CardFieldComponent (string fieldClause)
		{
			//TODO Generate Boolean Getter

		}

		//public CardFieldComponent (string fieldName, double numFieldValue)
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
			if (dataType == CardFieldDataType.Number)
				return card.GetNumFieldValue(fieldName) == numFieldValue;
			else if (dataType == CardFieldDataType.Text)
				return card.GetTextFieldValue(fieldName) == textFieldValue;
			return false;
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
			tags.PrepareEvaluation(str.Split(','));
			return tags.Evaluate();
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
			tags.PrepareEvaluation(obj.data.tags.Split(','));
			return tags.Evaluate();
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
			tags.PrepareEvaluation(zone.zoneTags.Split(','));
			return tags.Evaluate();
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
				tags.PrepareEvaluation(obj.zone.zoneTags.Split(','));
				return tags.Evaluate();
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
				tags.PrepareEvaluation(obj.GetTagsFromModifiers().Split(','));
				return tags.Evaluate();
			}
			return false;
		}
	}

}