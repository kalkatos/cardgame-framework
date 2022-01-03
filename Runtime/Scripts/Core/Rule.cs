using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameFramework
{

	[CreateAssetMenu(fileName = "New Rule", menuName = "Cardgame/Rule", order = 4)]
	public class Rule : ScriptableObject
	{
		[HideInInspector] public Game game;
		[HideInInspector] public Rule self;
		[HideInInspector] public string id;
		[HideInInspector] public string origin;
		public string tags;
		public TriggerLabel trigger;
		public string condition;
		public string commands;
		public NestedBooleans conditionObject;
		public List<TriggerConditionPair> additionalTriggerConditions = new List<TriggerConditionPair>();
		internal List<Command> commandsList;

		public void Initialize ()
		{
			conditionObject = new NestedConditions(condition);
			commandsList = Command.BuildList(commands, ToString());
			Register(trigger, conditionObject);
			for (int i = 0; i < additionalTriggerConditions.Count; i++)
			{
				TriggerConditionPair pair = additionalTriggerConditions[i];
				pair.Initialize();
				Register(pair.trigger, pair.conditionObj, i);
			}
		}

		private void Register (TriggerLabel trigger, NestedBooleans conditionObject, int index = -1)
		{
			RuleCore rulePrimitive = null;
			switch (trigger)
			{
				case TriggerLabel.OnMatchStarted:
					Func<int, IEnumerator> matchStartedFunc = IntFuncSignature;
					rulePrimitive = new RuleCore(trigger, (Func<bool>)conditionObject.Evaluate, matchStartedFunc);
					Match.AddMatchStartedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnMatchEnded:
					Func<int, IEnumerator> matchEndedFunc = IntFuncSignature;
					rulePrimitive = new RuleCore(trigger, (Func<bool>)conditionObject.Evaluate, matchEndedFunc);
					Match.AddMatchEndedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnTurnStarted:
					Func<int, IEnumerator> turnStartedFunc = IntFuncSignature;
					rulePrimitive = new RuleCore(trigger, (Func<bool>)conditionObject.Evaluate, turnStartedFunc);
					Match.AddTurnStartedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnTurnEnded:
					Func<int, IEnumerator> turnEndedFunc = IntFuncSignature;
					rulePrimitive = new RuleCore(trigger, (Func<bool>)conditionObject.Evaluate, turnEndedFunc);
					Match.AddTurnEndedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnPhaseStarted:
					Func<string, IEnumerator> phaseStartedFunc = StringFuncSignature;
					rulePrimitive = new RuleCore(trigger, (Func<bool>)conditionObject.Evaluate, phaseStartedFunc);
					Match.AddPhaseStartedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnPhaseEnded:
					Func<string, IEnumerator> phaseEndedFunc = StringFuncSignature;
					rulePrimitive = new RuleCore(trigger, (Func<bool>)conditionObject.Evaluate, phaseEndedFunc);
					Match.AddPhaseEndedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnCardUsed:
					Func<Card, string, IEnumerator> useCardFunc = UseCardFuncSignature;
					rulePrimitive = new RuleCore(trigger, (Func<bool>)conditionObject.Evaluate, useCardFunc);
					Match.AddCardUsedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnZoneUsed:
					Func<Zone, string, IEnumerator> useZoneFunc = UseZoneFuncSignature;
					rulePrimitive = new RuleCore(trigger, (Func<bool>)conditionObject.Evaluate, useZoneFunc);
					Match.AddZoneUsedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnCardEnteredZone:
					Func<Card, Zone, Zone, string, IEnumerator> cardEnteredZoneFunc = CardEnteredZoneFuncSignature;
					rulePrimitive = new RuleCore(trigger, (Func<bool>)conditionObject.Evaluate, cardEnteredZoneFunc);
					Match.AddCardEnteredZoneCallback(rulePrimitive);
					break;
				case TriggerLabel.OnCardLeftZone:
					Func<Card, Zone, string, IEnumerator> cardLeftZoneFunc = CardLeftZoneFuncSignature;
					rulePrimitive = new RuleCore(trigger, (Func<bool>)conditionObject.Evaluate, cardLeftZoneFunc);
					Match.AddCardLeftZoneCallback(rulePrimitive);
					break;
				case TriggerLabel.OnMessageSent:
					Func<string, string, IEnumerator> messageSentFunc = SendMessageFuncSignature;
					rulePrimitive = new RuleCore(trigger, (Func<bool>)conditionObject.Evaluate, messageSentFunc);
					Match.AddMessageSentCallback(rulePrimitive);
					break;
				case TriggerLabel.OnActionUsed:
					Func<string, string, IEnumerator> actionUsedFunc = UseActionFuncSignature;
					rulePrimitive = new RuleCore(trigger, (Func<bool>)conditionObject.Evaluate, actionUsedFunc);
					Match.AddActionUsedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnVariableChanged:
					Func<string, string, string, string, IEnumerator> variableChangedFunc = VariableChangedFuncSignature;
					rulePrimitive = new RuleCore(trigger, (Func<bool>)conditionObject.Evaluate, variableChangedFunc);
					Match.AddVariableChangedCallback(rulePrimitive);
					break;
				case TriggerLabel.OnRuleActivated:
					Func<Rule, IEnumerator> ruleFunc = RuleActivatedFuncSignature;
					rulePrimitive = new RuleCore(trigger, (Func<bool>)conditionObject.Evaluate, ruleFunc);
					Match.AddRuleActivatedCallback(rulePrimitive);
					break;
			}
			rulePrimitive.parent = this;
			rulePrimitive.triggerConditionIndex = index;
			rulePrimitive.name = ToString();
		}

		private IEnumerator IntFuncSignature (int intValue) { yield return Match.ExecuteInitializedCommands(commandsList); }
		private IEnumerator StringFuncSignature (string stringValue) { yield return Match.ExecuteInitializedCommands(commandsList); }
		private IEnumerator UseCardFuncSignature (Card card, string additionalInfo) { yield return Match.ExecuteInitializedCommands(commandsList); }
		private IEnumerator UseZoneFuncSignature (Zone zone, string additionalInfo) { yield return Match.ExecuteInitializedCommands(commandsList); }
		private IEnumerator CardEnteredZoneFuncSignature (Card card, Zone newZone, Zone oldZone, string additionalInfo) { yield return Match.ExecuteInitializedCommands(commandsList); }
		private IEnumerator CardLeftZoneFuncSignature (Card card, Zone oldZone, string additionalInfo) { yield return Match.ExecuteInitializedCommands(commandsList); }
		private IEnumerator SendMessageFuncSignature (string mainString, string additionalInfo) { yield return Match.ExecuteInitializedCommands(commandsList); }
		private IEnumerator UseActionFuncSignature (string mainString, string additionalInfo) { yield return Match.EnqueueCommandsCoroutine(commandsList); }
		private IEnumerator VariableChangedFuncSignature (string variable, string newValue, string oldValue, string additionalInfo) { yield return Match.ExecuteInitializedCommands(commandsList); }
		private IEnumerator RuleActivatedFuncSignature (Rule rule) { yield return Match.ExecuteInitializedCommands(commandsList); }

		public override string ToString ()
		{
			return $"{name} (Rule)";
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
		internal string conditionLog;
		internal TriggerLabel trigger;
		internal Delegate condition;
		internal Delegate callback;
		internal Rule parent;
		internal int triggerConditionIndex = -1;

		internal RuleCore (TriggerLabel trigger, Delegate condition, Delegate callback)
		{
			this.trigger = trigger;
			this.condition = condition;
			this.callback = callback;
		}

		internal bool EvaluateAndLogCondition ()
		{
			bool evaluation = (bool)condition.DynamicInvoke();
			if (parent != null)
				conditionLog = triggerConditionIndex == -1 ? parent.conditionObject.ToString() : parent.additionalTriggerConditions[triggerConditionIndex].conditionObj.ToString();
			else
				evaluation.ToString();
			return evaluation;
		}
	}

	[Serializable]
	public class TriggerConditionPair
	{
		public TriggerLabel trigger;
		public string condition;
		public NestedBooleans conditionObj;

		public void Initialize ()
		{
			conditionObj = new NestedConditions(condition);
		}
	}
}