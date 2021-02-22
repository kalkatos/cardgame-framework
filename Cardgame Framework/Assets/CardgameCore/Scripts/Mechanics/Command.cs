using System;
using System.Collections;
using UnityEngine;

namespace CardgameCore
{
	/*
		EndCurrentPhase,
		MoveCardToZone,
		Shuffle,
		UseAction,
		EndTheMatch,
		SendMessage,
		StartSubphaseLoop,
		EndSubphaseLoop,
		SetCardFieldValue,
		SetVariable,
		UseCard,
		UseZone,
		AddTagToCard,
		RemoveTagFromCard


		EndCurrentPhase,
		EndTheMatch,
		EndSubphaseLoop,
		UseAction,
		SendMessage,
		StartSubphaseLoop,
		UseCard,
		Shuffle,
		UseZone,
		SetCardFieldValue,
		SetVariable,
		MoveCardToZone,
		AddTagToCard,
		RemoveTagFromCard

		AddTagToCard = 13,
		EndCurrentPhase = 1,
		EndSubphaseLoop = 3,
		EndTheMatch = 2,
		MoveCardToZone = 12,
		RemoveTagFromCard = 14
		SendMessage = 5,
		SetCardFieldValue = 10,
		SetVariable = 11,
		Shuffle = 8,
		StartSubphaseLoop = 6,
		UseAction = 4,
		UseCard = 7,
		UseZone = 9,
	*/
	public enum CommandType
	{
		EndCurrentPhase = 1,
		EndTheMatch = 2,
		EndSubphaseLoop = 3,
		UseAction = 4,
		SendMessage = 5,
		StartSubphaseLoop = 6,
		UseCard = 7,
		Shuffle = 8,
		//UseZone = 9,
		SetCardFieldValue = 10,
		SetVariable = 11,
		MoveCardToZone = 12,
		AddTagToCard = 13,
		RemoveTagFromCard = 14
	}

	public abstract class Command
	{
		protected CommandType type;
		public abstract IEnumerator Execute ();
	}

	public class SimpleCommand : Command
	{
		Func<IEnumerator> method;

		public SimpleCommand (CommandType type, Func<IEnumerator> method)
		{
			this.type = type;
			this.method = method;
		}

		public override IEnumerator Execute ()
		{
			yield return method();
		}
	}

	public class StringCommand : Command
	{
		Func<string, IEnumerator> method;
		public string strParameter;

		public StringCommand (CommandType type, Func<string, IEnumerator> method, string strParameter)
		{
			this.type = type;
			this.method = method;
			this.strParameter = strParameter;
		}

		public override IEnumerator Execute ()
		{
			yield return method(strParameter);
		}

	}

	public class ZoneCommand : Command
	{
		Func<ZoneSelector, IEnumerator> method;
		ZoneSelector zoneSelector;

		public ZoneCommand (CommandType type, Func<ZoneSelector, IEnumerator> method, ZoneSelector zoneSelector)
		{
			this.type = type;
			this.zoneSelector = zoneSelector;
			this.method = method;
		}

		public override IEnumerator Execute ()
		{
			yield return method(zoneSelector);
		}
	}

	public class CardCommand : Command
	{
		Func<ComponentSelector, IEnumerator> method;
		ComponentSelector cardSelector;

		public CardCommand (CommandType type, Func<ComponentSelector, IEnumerator> method, ComponentSelector cardSelector)
		{
			this.type = type;
			this.method = method;
			this.cardSelector = cardSelector;
		}

		public override IEnumerator Execute ()
		{
			yield return method(cardSelector);
		}
	}

	public class SingleCardCommand : Command
	{
		Component component;
		Func<Component, IEnumerator> method;

		public SingleCardCommand (Func<Component, IEnumerator> method)
		{
			type = CommandType.UseCard;
			this.method = method;
		}

		public void SetCard (Component c)
		{
			component = c;
		}

		public override IEnumerator Execute ()
		{
			yield return method(component);
		}
	}

	//public class SingleZoneCommand : Command
	//{
	//	Zone zone;
	//	Func<Zone, IEnumerator> method;

	//	public SingleZoneCommand (Func<Zone, IEnumerator> method)
	//	{
	//		type = CommandType.UseZone;
	//		this.method = method;
	//	}

	//	public void SetZone (Zone z)
	//	{
	//		zone = z;
	//	}

	//	public override IEnumerator Execute ()
	//	{
	//		yield return method(zone);
	//	}
	//}

	public class WaitCommand : Command
	{
		float seconds;

		public WaitCommand (float seconds)
		{
			this.seconds = seconds;
		}

		public override IEnumerator Execute ()
		{
			yield return new WaitForSeconds(seconds);
		}
	}

	public class CardZoneCommand : Command
	{
		Func<ComponentSelector, ZoneSelector, string[], IEnumerator> method;
		ComponentSelector cardSelector;
		ZoneSelector zoneSelector;
		string[] additionalParams;

		public CardZoneCommand (CommandType type, Func<ComponentSelector, ZoneSelector, string[], IEnumerator> method, ComponentSelector cardSelector, ZoneSelector zoneSelector, string[] additionalParams = null)
		{
			this.type = type;
			this.method = method;
			this.cardSelector = cardSelector;
			this.zoneSelector = zoneSelector;
			this.additionalParams = additionalParams;
		}

		public override IEnumerator Execute ()
		{
			yield return method(cardSelector, zoneSelector, additionalParams);
		}
	}

	public class CardFieldCommand : Command
	{
		Func<ComponentSelector, string, Getter, Getter, Getter, IEnumerator> method;
		ComponentSelector cardSelector;
		string fieldName;
		Getter valueGetter;
		Getter minValue;
		Getter maxValue;

		public CardFieldCommand (CommandType type, Func<ComponentSelector, string, Getter, Getter, Getter, IEnumerator> method, ComponentSelector cardSelector, string fieldName, Getter valueGetter, Getter minValue, Getter maxValue)
		{
			this.method = method;
			this.cardSelector = cardSelector;
			this.fieldName = fieldName;
			this.valueGetter = valueGetter;
			this.minValue = minValue;
			this.maxValue = maxValue;
		}

		public override IEnumerator Execute ()
		{
			yield return method(cardSelector, fieldName, valueGetter, minValue, maxValue);
		}
	}

	public class VariableCommand : Command
	{
		Func<string, Getter, Getter, Getter, IEnumerator> method;
		string variableName;
		Getter value;
		Getter minValue;
		Getter maxValue;

		public VariableCommand (CommandType type, Func<string, Getter, Getter, Getter, IEnumerator> method, string variableName, Getter value, Getter minValue, Getter maxValue)
		{
			this.method = method;
			this.variableName = variableName;
			this.value = value;
			this.minValue = minValue;
			this.maxValue = maxValue;
		}

		public override IEnumerator Execute ()
		{
			yield return method.Invoke(variableName, value, minValue, maxValue);
		}
	}

	public class ChangeCardTagCommand : Command
	{
		Func<ComponentSelector, string, bool, IEnumerator> method;
		ComponentSelector cardSelector;
		string tag;
		bool isAdd;

		public ChangeCardTagCommand (CommandType type, Func<ComponentSelector, string, bool, IEnumerator> method, ComponentSelector cardSelector, string tag, bool isAdd)
		{
			this.method = method;
			this.cardSelector = cardSelector;
			this.tag = tag;
			this.isAdd = isAdd;
		}

		public override IEnumerator Execute ()
		{
			yield return method(cardSelector, tag, isAdd);
		}
	}
}