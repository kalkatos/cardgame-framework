using System;
using System.Collections;
using System.Collections.Generic;
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
		RemoveTagFromCard = 14
	}

	[Serializable]
	public abstract class Command
	{
		internal string buildingStr;
		internal CommandType type;
		internal Action<Command> enqueueCallback;
		internal Command (CommandType type)
		{
			this.type = type;
		}
		internal abstract IEnumerator Execute ();
		internal abstract void Initialize (Delegate method, params object[] additionalParameters);

		internal virtual void Set (params object[] setParams) { }

		internal static Command Build (string clause)
		{
			Command newCommand = null;
			string additionalInfo;
			string[] clauseBreak = StringUtility.ArgumentsBreakdown(clause);
			switch (clauseBreak[0])
			{
				case "EndCurrentPhase":
					newCommand = EndCurrentPhase();
					break;
				case "EndTheMatch":
					newCommand = new SimpleCommand(CommandType.EndTheMatch);
					break;
				case "EndSubphaseLoop":
					newCommand = new SimpleCommand(CommandType.EndSubphaseLoop);
					break;
				case "UseAction":
					additionalInfo = clauseBreak.Length > 2 ? string.Join(",", clauseBreak.SubArray(2)) : "";
					newCommand = new StringCommand(CommandType.UseAction, clauseBreak[1], additionalInfo);
					break;
				case "SendMessage":
					additionalInfo = clauseBreak.Length > 2 ? string.Join(",", clauseBreak.SubArray(2)) : "";
					newCommand = new StringCommand(CommandType.SendMessage, clauseBreak[1], additionalInfo);
					break;
				case "StartSubphaseLoop":
					newCommand = new StringCommand(CommandType.StartSubphaseLoop, clauseBreak[1]);
					break;
				case "UseCard":
					additionalInfo = clauseBreak.Length > 2 ? string.Join(",", clauseBreak.SubArray(2)) : "";
					newCommand = new CardCommand(CommandType.UseCard, clauseBreak[1], additionalInfo);
					break;
				case "UseZone":
					additionalInfo = clauseBreak.Length > 2 ? string.Join(",", clauseBreak.SubArray(2)) : "";
					newCommand = new ZoneCommand(CommandType.UseZone, clauseBreak[1], additionalInfo);
					break;
				case "Shuffle":
					additionalInfo = clauseBreak.Length > 2 ? string.Join(",", clauseBreak.SubArray(2)) : "";
					newCommand = new ZoneCommand(CommandType.Shuffle, clauseBreak[1], additionalInfo);
					break;
				case "SetCardFieldValue":
					additionalInfo = clauseBreak.Length > 4 ? string.Join(",", clauseBreak.SubArray(4)) : "";
					newCommand = new CardFieldCommand(CommandType.SetCardFieldValue, clauseBreak[1], clauseBreak[2], Getter.Build(clauseBreak[3]), additionalInfo);
					break;
				case "SetVariable":
					additionalInfo = clauseBreak.Length > 3 ? string.Join(",", clauseBreak.SubArray(3)) : "";
					char firstVarChar = clauseBreak[2][0];
					if (firstVarChar == '+' || firstVarChar == '*' || firstVarChar == '/' || firstVarChar == '%' || firstVarChar == '^')
						clauseBreak[2] = clauseBreak[1] + clauseBreak[2];
					newCommand = new VariableCommand(CommandType.SetVariable, clauseBreak[1], Getter.Build(clauseBreak[2]), additionalInfo);
					break;
				case "MoveCardToZone":
					additionalInfo = clauseBreak.Length > 3 ? string.Join(",", clauseBreak.SubArray(3)) : "";
					newCommand = new CardZoneCommand(CommandType.MoveCardToZone, clauseBreak[1], clauseBreak[2], new MovementAdditionalInfo(additionalInfo));
					break;
				case "AddTagToCard":
					additionalInfo = clauseBreak.Length > 3 ? string.Join(",", clauseBreak.SubArray(3)) : "";
					newCommand = new ChangeCardTagCommand(CommandType.AddTagToCard, clauseBreak[1], clauseBreak[2], additionalInfo);
					break;
				case "RemoveTagFromCard":
					additionalInfo = clauseBreak.Length > 3 ? string.Join(",", clauseBreak.SubArray(3)) : "";
					newCommand = new ChangeCardTagCommand(CommandType.RemoveTagFromCard, clauseBreak[1], clauseBreak[2], additionalInfo);
					break;
				default:
					CustomDebug.LogWarning("Effect not found: " + clauseBreak[0]);
					break;
			}

			if (newCommand == null)
			{
				CustomDebug.LogError("Couldn't build a command with instruction: " + clause);
				return null;
			}
			newCommand.buildingStr = clause;
			return newCommand;
		}

		internal static List<Command> BuildSequence (string clause)
		{
			List<Command> list = new List<Command>();
			if (string.IsNullOrEmpty(clause))
				return list;
			string[] commandSequenceClause = clause.Split(';');
			for (int index = 0; index < commandSequenceClause.Length; index++)
			{
				Command newCommand = Build(commandSequenceClause[index]);
				if (newCommand != null)
					list.Add(newCommand);
			}
			return list;
		}

		public static Command EndCurrentPhase ()
		{
			return new SimpleCommand(CommandType.EndCurrentPhase);
		}
	}

	internal class SimpleCommand : Command
	{
		internal Func<IEnumerator> method;
		internal SimpleCommand (CommandType type) : base(type)
		{
			this.type = type;
		}
		internal override IEnumerator Execute ()
		{
			yield return method();
		}
		internal override void Initialize (Delegate method, params object[] additionalParameters)
		{
			this.method = (Func<IEnumerator>)method;
		}
	}

	internal class StringCommand : Command
	{
		internal Func<string, string, IEnumerator> method;
		internal string strParameter;
		internal string additionalInfo;
		internal StringCommand (CommandType type, string strParameter, string additionalInfo = "") : base(type)
		{
			this.type = type;
			this.strParameter = strParameter;
			this.additionalInfo = additionalInfo;
		}
		internal StringCommand (CommandType type, Func<string, string, IEnumerator> method) : base(type)
		{
			this.method = method;
		}
		internal override void Set (params object[] setParams)
		{
			strParameter = (string)setParams[0];
			if (setParams.Length > 1)
				additionalInfo = (string)setParams[1];
		}
		internal override IEnumerator Execute ()
		{
			yield return method(strParameter, additionalInfo);
		}
		internal override void Initialize (Delegate method, params object[] additionalParameters)
		{
			this.method = (Func<string, string, IEnumerator>)method;
		}
	}

	internal class ZoneCommand : Command
	{
		internal Func<ZoneSelector, string, IEnumerator> method;
		internal ZoneSelector zoneSelector;
		internal string additionalInfo;
		internal ZoneCommand (CommandType type, string zoneSelectorClause, string additionalInfo) : base(type)
		{
			this.type = type;
			zoneSelector = new ZoneSelector(zoneSelectorClause);
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(zoneSelector, additionalInfo);
		}
		internal override void Initialize (Delegate method, params object[] additionalParameters)
		{
			this.method = (Func<ZoneSelector, string, IEnumerator>)method;
			zoneSelector = (ZoneSelector)additionalParameters[0];
		}
	}

	internal class CardCommand : Command
	{
		internal Func<CardSelector, string, IEnumerator> method;
		internal CardSelector cardSelector;
		internal string additionalInfo;
		internal CardCommand (CommandType type, string cardSelectorClause, string additionalInfo) : base(type)
		{
			cardSelector = new CardSelector(cardSelectorClause);
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(cardSelector, additionalInfo);
		}
		internal override void Initialize (Delegate method, params object[] additionalParameters)
		{
			this.method = (Func<CardSelector, string, IEnumerator>)method;
			cardSelector.SetPool((List<Card>)additionalParameters[0]);
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
		internal SingleCardCommand (Func<Card, string, IEnumerator> method) : base(CommandType.UseCard)
		{
			this.method = method;
		}
		internal override void Set (params object[] setParams)
		{
			card = (Card)setParams[0];
			if (setParams.Length > 1)
				additionalInfo = (string)setParams[1];
		}
		internal override IEnumerator Execute ()
		{
			yield return method(card, additionalInfo);
		}
		internal override void Initialize (Delegate method, params object[] additionalParameters)
		{
			this.method = (Func<Card, string, IEnumerator>)method;
		}
	}

	internal class SingleZoneCommand : Command
	{
		internal Zone zone;
		internal Func<Zone, string, IEnumerator> method;
		internal string additionalInfo;
		internal SingleZoneCommand(Func<Zone, string, IEnumerator> method, Zone zone, string additionalInfo) : base(CommandType.UseZone)
		{
			this.method = method;
			this.zone = zone;
			this.additionalInfo = additionalInfo;
		}
		internal SingleZoneCommand (Func<Zone, string, IEnumerator> method) : base(CommandType.UseZone)
		{
			this.method = method;
		}
		internal override void Set (params object[] setParams)
		{
			zone = (Zone)setParams[0];
			if (setParams.Length > 1)
				additionalInfo = (string)setParams[1];
		}
		internal override IEnumerator Execute()
		{
			yield return method(zone, additionalInfo);
		}
		internal override void Initialize (Delegate method, params object[] additionalParameters)
		{
			this.method = (Func<Zone, string, IEnumerator>)method;
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
		internal CardZoneCommand (CommandType type, string cardSelectorClause,	string zoneSelectorClause, MovementAdditionalInfo additionalInfo) : base(type)
		{
			this.type = type;
			cardSelector = new CardSelector(cardSelectorClause);
			zoneSelector = new ZoneSelector(zoneSelectorClause);
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(cardSelector, zoneSelector, additionalInfo);
		}
		internal override void Initialize (Delegate method, params object[] additionalParameters)
		{
			this.method = (Func<CardSelector, ZoneSelector, MovementAdditionalInfo, IEnumerator>)method;
			cardSelector.SetPool((List<Card>)additionalParameters[0]);
			zoneSelector.SetPool((List<Zone>)additionalParameters[1]);
		}
	}

	internal class CardFieldCommand : Command
	{
		internal Func<CardSelector, string, Getter, string, IEnumerator> method;
		internal CardSelector cardSelector;
		internal string fieldName;
		internal Getter valueGetter;
		internal string additionalInfo;
		internal CardFieldCommand (CommandType type, string cardSelectorClause, string fieldName, Getter valueGetter, string additionalInfo) : base(type)
		{
			cardSelector = new CardSelector(cardSelectorClause);
			this.fieldName = fieldName;
			this.valueGetter = valueGetter;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(cardSelector, fieldName, valueGetter, additionalInfo);
		}
		internal override void Initialize (Delegate method, params object[] additionalParameters)
		{
			this.method = (Func<CardSelector, string, Getter, string, IEnumerator>)method;
			cardSelector.SetPool((List<Card>)additionalParameters[0]);
		}
	}

	internal class VariableCommand : Command
	{
		internal Func<string, Getter, string, IEnumerator> method;
		internal string variableName;
		internal Getter value;
		internal string additionalInfo;
		internal VariableCommand (CommandType type, string variableName, Getter value, string additionalInfo) : base(type)
		{
			this.variableName = variableName;
			this.value = value;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method.Invoke(variableName, value, additionalInfo);
		}

		internal override void Initialize (Delegate method, params object[] additionalParameters)
		{
			this.method = (Func<string, Getter, string, IEnumerator>)method;
		}
	}

	internal class ChangeCardTagCommand : Command
	{
		internal Func<CardSelector, string, string, IEnumerator> method;
		internal CardSelector cardSelector;
		internal string tag;
		internal string additionalInfo;
		internal ChangeCardTagCommand (CommandType type, string cardSelectorClause, string tag, string additionalInfo) : base(type)
		{
			cardSelector = new CardSelector(cardSelectorClause);
			this.tag = tag;
			this.additionalInfo = additionalInfo;
		}
		internal override IEnumerator Execute ()
		{
			yield return method(cardSelector, tag, additionalInfo);
		}
		internal override void Initialize (Delegate method, params object[] additionalParameters)
		{
			this.method = (Func<CardSelector, string, string, IEnumerator>)method;
			cardSelector.SetPool((List<Card>)additionalParameters[0]);
		}
	}
}