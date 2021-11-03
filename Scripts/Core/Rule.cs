using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
	public class Rule : ScriptableObject
	{
		[HideInInspector] public Game game;
		[HideInInspector] public string id;
		[HideInInspector] public string origin;
		public string tags;
		public TriggerLabel trigger;
		public string condition;
		public string commands;
		public NestedBooleans conditionObject;
		internal List<Command> commandsList = new List<Command>();

		public void Initialize ()
		{
			conditionObject = new NestedConditions(condition);
			commandsList = Match.CreateCommands(commands);

			RuleCore rulePrimitive = null;
			switch (trigger)
			{
				case TriggerLabel.OnMatchStarted:
					Func<int, IEnumerator> matchStartedFunc = IntFuncSignature;
					rulePrimitive = new RuleCore(trigger, conditionObject, matchStartedFunc);
					Match.AddMatchStartedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnMatchEnded:
					Func<int, IEnumerator> matchEndedFunc = IntFuncSignature;
					rulePrimitive = new RuleCore(trigger, conditionObject, matchEndedFunc);
					Match.AddMatchEndedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnTurnStarted:
					Func<int, IEnumerator> turnStartedFunc = IntFuncSignature;
					rulePrimitive = new RuleCore(trigger, conditionObject, turnStartedFunc);
					Match.AddTurnStartedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnTurnEnded:
					Func<int, IEnumerator> turnEndedFunc = IntFuncSignature;
					rulePrimitive = new RuleCore(trigger, conditionObject, turnEndedFunc);
					Match.AddTurnEndedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnPhaseStarted:
					Func<string, IEnumerator> phaseStartedFunc = StringFuncSignature;
					rulePrimitive = new RuleCore(trigger, conditionObject, phaseStartedFunc);
					Match.AddPhaseStartedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnPhaseEnded:
					Func<string, IEnumerator> phaseEndedFunc = StringFuncSignature;
					rulePrimitive = new RuleCore(trigger, conditionObject, phaseEndedFunc);
					Match.AddPhaseEndedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnCardUsed:
					Func<Card, string, IEnumerator> useCardFunc = UseCardFuncSignature;
					rulePrimitive = new RuleCore(trigger, conditionObject, useCardFunc);
					Match.AddCardUsedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnZoneUsed:
					Func<Zone, string, IEnumerator> useZoneFunc = UseZoneFuncSignature;
					rulePrimitive = new RuleCore(trigger, conditionObject, useZoneFunc);
					Match.AddZoneUsedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnCardEnteredZone:
					Func<Card, Zone, Zone, string, IEnumerator> cardEnteredZoneFunc = CardEnteredZoneFuncSignature;
					rulePrimitive = new RuleCore(trigger, conditionObject, cardEnteredZoneFunc);
					Match.AddCardEnteredZoneCallback(rulePrimitive);
					break;
				case TriggerLabel.OnCardLeftZone:
					Func<Card, Zone, string, IEnumerator> cardLeftZoneFunc = CardLeftZoneFuncSignature;
					rulePrimitive = new RuleCore(trigger, conditionObject, cardLeftZoneFunc);
					Match.AddCardLeftZoneCallback(rulePrimitive);
					break;
				case TriggerLabel.OnMessageSent:
					Func<string, string, IEnumerator> messageSentFunc = DoubleStringFuncSignature;
					rulePrimitive = new RuleCore(trigger, conditionObject, messageSentFunc);
					Match.AddMessageSentCallback(rulePrimitive);
					break;
				case TriggerLabel.OnActionUsed:
					Func<string, string, IEnumerator> actionUsedFunc = DoubleStringFuncSignature;
					rulePrimitive = new RuleCore(trigger, conditionObject, actionUsedFunc);
					Match.AddActionUsedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnVariableChanged:
					Func<string, string, string, string, IEnumerator> variableChangedFunc = VariableChangedFuncSignature;
					rulePrimitive = new RuleCore(trigger, conditionObject, variableChangedFunc);
					Match.AddVariableChangedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnRuleActivated:
					Func<Rule, IEnumerator> ruleFunc = RuleActivatedFuncSignature;
					rulePrimitive = new RuleCore(trigger, conditionObject, ruleFunc);
					Match.AddRuleActivatedCallback(rulePrimitive);
					break;
			}
			rulePrimitive.parent = this;
			rulePrimitive.name = ToString();
		}

		private IEnumerator IntFuncSignature (int intValue) { yield return Match.ExecuteCommands(commandsList); }
		private IEnumerator StringFuncSignature (string stringValue) { yield return Match.ExecuteCommands(commandsList); }
		private IEnumerator UseCardFuncSignature (Card card, string additionalInfo) { yield return Match.ExecuteCommands(commandsList); }
		private IEnumerator UseZoneFuncSignature (Zone zone, string additionalInfo) { yield return Match.ExecuteCommands(commandsList); }
		private IEnumerator CardEnteredZoneFuncSignature (Card card, Zone newZone, Zone oldZone, string additionalInfo) { yield return Match.ExecuteCommands(commandsList); }
		private IEnumerator CardLeftZoneFuncSignature (Card card, Zone oldZone, string additionalInfo) { yield return Match.ExecuteCommands(commandsList); }
		private IEnumerator DoubleStringFuncSignature (string mainString, string additionalInfo) { yield return Match.ExecuteCommands(commandsList); }
		private IEnumerator VariableChangedFuncSignature (string variable, string newValue, string oldValue, string additionalInfo) { yield return Match.ExecuteCommands(commandsList); }
		private IEnumerator RuleActivatedFuncSignature (Rule rule) { yield return Match.ExecuteCommands(commandsList); }

		public override string ToString ()
		{
			return $"{name} (id: {id})";
		}

		public void Copy (Rule other)
		{
			name = other.name;
			game = other.game;
			id = other.id;
			origin = other.origin;
			tags = other.tags;
			trigger = other.trigger;
			condition = other.condition;
			commands = other.commands;
		}
	}

	internal class RuleCore
	{
		internal string name = "Unnamed Rule";
		internal TriggerLabel trigger;
		internal NestedBooleans condition;
		internal Delegate callback;
		internal Rule parent;

		internal RuleCore (TriggerLabel trigger, NestedBooleans condition, Delegate callback)
		{
			this.trigger = trigger;
			this.condition = condition;
			this.callback = callback;
		}
	}
}