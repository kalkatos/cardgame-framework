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

	public class CardFieldParameter : SelectionParameter<CGComponent>
	{
		NestedCardFieldConditions cardFieldConditions;

		public CardFieldParameter (NestedCardFieldConditions cardFieldConditions)
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

	public class CardTagParameter : SelectionParameter<CGComponent>
	{
		NestedStrings tags;

		public CardTagParameter (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool IsAMatch (CGComponent obj)
		{
			return tags.Evaluate(obj.tags.Split(','));
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
			return tags.Evaluate(zone.tags);
		}
	}

	public class CardZoneTagParameter : SelectionParameter<CGComponent>
	{
		NestedStrings tags;

		public CardZoneTagParameter (NestedStrings tags)
		{
			this.tags = tags;
		}

		public override bool IsAMatch (CGComponent obj)
		{
			if (obj.zone != null)
			{
				return tags.Evaluate(obj.zone.tags);
			}
			return false;
		}
	}

	public class CardZoneIDParameter : SelectionParameter<CGComponent>
	{
		string zoneID;

		public CardZoneIDParameter(string zoneID)
		{
			this.zoneID = zoneID;
		}

		public override bool IsAMatch(CGComponent obj)
		{
			if (obj.zone != null)
			{
				return obj.zone.id == Match.GetVariable(zoneID);
			}
			return false;
		}
	}

	//public class CardRuleTagParameter : SelectionParameter<Component>
	//{
	//	NestedStrings tags;

	//	public CardRuleTagParameter(NestedStrings tags)
	//	{
	//		this.tags = tags;
	//	}

	//	public override bool IsAMatch(Component obj)
	//	{
	//		if (obj.zone != null)
	//		{
	//			return tags.Evaluate(obj.GetTagsFromRules().Split(','));
	//		}
	//		return false;
	//	}
	//}

	//public class CardZoneSlotParameter : SelectionParameter<Component>
	//{
	//	Getter slotGetter;

	//	public CardZoneSlotParameter(Getter slotGetter)
	//	{
	//		this.slotGetter = slotGetter;
	//	}

	//	public override bool IsAMatch(Component obj)
	//	{
	//		object valueGot = slotGetter.Get();
	//		if (valueGot is float)
	//			return obj.slotInZone == (int)(float)valueGot;
	//		return false;
	//	}
	//}

}