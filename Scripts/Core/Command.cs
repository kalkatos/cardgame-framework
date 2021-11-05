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
	internal enum CommandType
	{
		Undefined = 0,
		EndCurrentPhase = 1,
		EndTheMatch = 2,
		EndSubphaseLoop = 3,
		UseAction = 4,
		SendMessage = 5,
		StartSubphaseLoop = 6,
		UseCard = 7,
		Shuffle = 8,
		UseZone = 9,
		SetCardFieldValue = 10,
		SetVariable = 11,
		MoveCardToZone = 12,
		AddTagToCard = 13,
		RemoveTagFromCard = 14,
		OrganizeZone = 15
	}

	[Serializable]
	internal abstract class Command
	{
		internal string buildingStr;
		internal CommandType type;
		internal Command (CommandType type)
		{
			this.type = type;
		}
		internal abstract IEnumerator Execute ();
	}

	internal class CustomCommand : Command
	{
		internal Func<IEnumerator> method;
		internal CustomCommand (Func<IEnumerator> method) : base(CommandType.Undefined)
		{
			this.method = method;
		}
		internal override IEnumerator Execute()
		{
			yield return method;
		}
	}

	internal class SimpleCommand : Command
	{
		internal Func<IEnumerator> method;
		internal SimpleCommand (CommandType type, Func<IEnumerator> method) : base(type)
		{
			this.type = type;
			this.method = method;
		}
		internal override IEnumerator Execute ()
		{
			yield return method();
		}
	}

	internal class StringCommand : Command
	{
		internal Func<string, string, IEnumerator> method;
		internal string strParameter;
		internal string additionalInfo;
		internal StringCommand (CommandType type, Func<string, string, IEnumerator> method, string strParameter, string additionalInfo = "") : base(type)
		{
			this.type = type;
			this.method = method;
			this.strParameter = strParameter;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(strParameter, additionalInfo);
		}
	}

	internal class ZoneCommand : Command
	{
		internal Func<ZoneSelector, string, IEnumerator> method;
		internal ZoneSelector zoneSelector;
		internal string additionalInfo;
		internal ZoneCommand (CommandType type, Func<ZoneSelector, string, IEnumerator> method, ZoneSelector zoneSelector, string additionalInfo) : base(type)
		{
			this.type = type;
			this.zoneSelector = zoneSelector;
			this.method = method;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(zoneSelector, additionalInfo);
		}
	}

	internal class CardCommand : Command
	{
		internal Func<CardSelector, string, IEnumerator> method;
		internal CardSelector cardSelector;
		internal string additionalInfo;
		internal CardCommand (CommandType type, Func<CardSelector, string, IEnumerator> method, CardSelector cardSelector, string additionalInfo) : base(type)
		{
			this.type = type;
			this.method = method;
			this.cardSelector = cardSelector;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(cardSelector, additionalInfo);
		}
	}

	internal class SingleCardCommand : Command
	{
		internal Card card;
		internal Func<Card, string, IEnumerator> method;
		internal string additionalInfo;
		internal SingleCardCommand (Func<Card, string, IEnumerator> method, Card card, string additionalInfo) : base(CommandType.UseCard)
		{
			this.card = card;
			this.method = method;
			this.additionalInfo = additionalInfo;
		}
		internal void SetCard (Card c)
		{
			card = c;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(card, additionalInfo);
		}
	}

	internal class SingleZoneCommand : Command
	{
		internal Zone zone;
		internal Func<Zone, string, IEnumerator> method;
		internal string additionalInfo;
		internal SingleZoneCommand(CommandType type, Func<Zone, string, IEnumerator> method, Zone zone, string additionalInfo) : base(type)
		{
			this.method = method;
			this.zone = zone;
			this.additionalInfo = additionalInfo;
		}
		internal void SetZone(Zone z)
		{
			zone = z;
		}
		internal override IEnumerator Execute()
		{
			yield return method(zone, additionalInfo);
		}
	}

	//internal class WaitCommand : Command
	//{
	//	float seconds;
	//
	//	internal WaitCommand (float seconds) : base(type)
	//	{
	//		this.seconds = seconds;
	//	}
	//
	//	internal override IEnumerator Execute ()
	//	{
	//		yield return new WaitForSeconds(seconds);
	//	}
	//}

	internal class CardZoneCommand : Command
	{
		internal Func<CardSelector, ZoneSelector, MovementAdditionalInfo, IEnumerator> method;
		internal CardSelector cardSelector;
		internal ZoneSelector zoneSelector;
		internal MovementAdditionalInfo additionalInfo;
		internal CardZoneCommand (CommandType type, Func<CardSelector, ZoneSelector, MovementAdditionalInfo, IEnumerator> method, CardSelector cardSelector, 
			ZoneSelector zoneSelector, MovementAdditionalInfo additionalInfo) : base(type)
		{
			this.type = type;
			this.method = method;
			this.cardSelector = cardSelector;
			this.zoneSelector = zoneSelector;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(cardSelector, zoneSelector, additionalInfo);
		}
	}

	internal class CardFieldCommand : Command
	{
		internal Func<CardSelector, string, Getter, string, IEnumerator> method;
		internal CardSelector cardSelector;
		internal string fieldName;
		internal Getter valueGetter;
		internal string additionalInfo;
		internal CardFieldCommand (CommandType type, Func<CardSelector, string, Getter, string, IEnumerator> method, CardSelector cardSelector, 
			string fieldName, Getter valueGetter, string additionalInfo) : base(type)
		{
			this.method = method;
			this.cardSelector = cardSelector;
			this.fieldName = fieldName;
			this.valueGetter = valueGetter;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(cardSelector, fieldName, valueGetter, additionalInfo);
		}
	}

	internal class VariableCommand : Command
	{
		internal Func<string, Getter, string, IEnumerator> method;
		internal string variableName;
		internal Getter value;
		internal string additionalInfo;
		internal VariableCommand (CommandType type, Func<string, Getter, string, IEnumerator> method, string variableName, Getter value, string additionalInfo) : base(type)
		{
			this.method = method;
			this.variableName = variableName;
			this.value = value;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method.Invoke(variableName, value, additionalInfo);
		}
	}

	internal class ChangeCardTagCommand : Command
	{
		internal Func<CardSelector, string, string, IEnumerator> method;
		internal CardSelector cardSelector;
		internal string tag;
		internal string additionalInfo;
		internal ChangeCardTagCommand (CommandType type, Func<CardSelector, string, string, IEnumerator> method, CardSelector cardSelector, string tag, string additionalInfo) : base(type)
		{
			this.method = method;
			this.cardSelector = cardSelector;
			this.tag = tag;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(cardSelector, tag, additionalInfo);
		}
	}
}