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

			switch (trigger)
			{
				case TriggerLabel.OnMatchStarted:
					Match.AddMatchStartedCallback(conditionObject, IntFuncSignature);
					break;
				case TriggerLabel.OnMatchEnded:
					Match.AddMatchEndedCallback(conditionObject, IntFuncSignature);
					break;
				case TriggerLabel.OnTurnStarted:
					Match.AddTurnStartedCallback(conditionObject, IntFuncSignature);
					break;
				case TriggerLabel.OnTurnEnded:
					Match.AddTurnEndedCallback(conditionObject, IntFuncSignature);
					break;
				case TriggerLabel.OnPhaseStarted:
					Match.AddPhaseStartedCallback(conditionObject, StringFuncSignature);
					break;
				case TriggerLabel.OnPhaseEnded:
					Match.AddPhaseEndedCallback(conditionObject, StringFuncSignature);
					break;
				case TriggerLabel.OnComponentUsed:
					Match.AddComponentUsedCallback(conditionObject, UseCardFuncSignature);
					break;
				case TriggerLabel.OnZoneUsed:
					Match.AddZoneUsedCallback(conditionObject, UseZoneFuncSignature);
					break;
				case TriggerLabel.OnComponentEnteredZone:
					Match.AddComponentEnteredZoneCallback(conditionObject, CardEnteredZoneFuncSignature);
					break;
				case TriggerLabel.OnComponentLeftZone:
					Match.AddComponentLeftZoneCallback(conditionObject, CardLeftZoneFuncSignature);
					break;
				case TriggerLabel.OnMessageSent:
					Match.AddMessageSentCallback(conditionObject, DoubleStringFuncSignature);
					break;
				case TriggerLabel.OnActionUsed:
					Match.AddActionUsedCallback(conditionObject, DoubleStringFuncSignature);
					break;
				case TriggerLabel.OnVariableChanged:
					Match.AddVariableChangedCallback(conditionObject, VariableChangedFuncSignature);
					break;
				case TriggerLabel.OnRuleActivated:
					Match.AddRuleActivatedCallback(conditionObject, RuleActivatedFuncSignature);
					break;
			}
		}

		private IEnumerator IntFuncSignature (int intValue) { yield return Match.ExecuteCommands(commandsList); }
		private IEnumerator StringFuncSignature (string stringValue) { yield return Match.ExecuteCommands(commandsList); }
		private IEnumerator UseCardFuncSignature (CGComponent card, string additionalInfo) { yield return Match.ExecuteCommands(commandsList); }
		private IEnumerator UseZoneFuncSignature (Zone zone, string additionalInfo) { yield return Match.ExecuteCommands(commandsList); }
		private IEnumerator CardEnteredZoneFuncSignature (CGComponent card, Zone newZone, Zone oldZone, string additionalInfo) { yield return Match.ExecuteCommands(commandsList); }
		private IEnumerator CardLeftZoneFuncSignature (CGComponent card, Zone oldZone, string additionalInfo) { yield return Match.ExecuteCommands(commandsList); }
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

	internal class RulePrimitive
	{
		internal TriggerLabel trigger;
		internal NestedBooleans condition;
		internal Delegate callback;
		internal Rule origin;

		internal RulePrimitive (TriggerLabel trigger, NestedBooleans condition, Delegate callback)
		{
			this.trigger = trigger;
			this.condition = condition;
			this.callback = callback;
		}
	}
}