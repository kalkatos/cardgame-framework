
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
	[DefaultExecutionOrder(-20)]
	public class Match : MonoBehaviour
	{
		private static Match instance;

		public static bool IsRunning => instance != null;

		private static bool DebugLog => instance.debugLog;

		[SerializeField] private Game autoStartGame;
		[SerializeField] private bool debugLog;

		//Match control
		private int componentIDCounter = 1;
		private int zoneIDCounter = 1;
		private int ruleIDCounter = 1;
		private bool endPhase;
		private int matchNumber;
		private int turnNumber;
		private Coroutine matchLoopCoroutine;
		private List<string> phases = new List<string>();
		private List<string> subphases = new List<string>();
		private Queue<Command> commands = new Queue<Command>();
		private Dictionary<string, string> variables = new Dictionary<string, string>();
		//Match information
		private List<Rule> rules = new List<Rule>();
		private List<CGComponent> components = new List<CGComponent>();
		private List<Zone> zones = new List<Zone>();
		private Dictionary<string, CGComponent> componentByID = new Dictionary<string, CGComponent>();
		private Dictionary<string, Zone> zoneByID = new Dictionary<string, Zone>();
		private Dictionary<string, Rule> ruleByID = new Dictionary<string, Rule>();
		//Callbacks
		private Dictionary<Delegate, RuleCore> OnMatchStartedRules = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnMatchEndedRules = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnTurnStartedRules = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnTurnEndedRules = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnPhaseStartedRules = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnPhaseEndedRules = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnComponentUsedRules = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnZoneUsedRules = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnComponentEnteredZoneRules = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnComponentLeftZoneRules = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnMessageSentRules = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnActionUsedRules = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnVariableChangedRules = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnRuleActivatedRules = new Dictionary<Delegate, RuleCore>();
		//Execution queues
		private Queue<RuleCore> OnMatchStartedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnMatchEndedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnTurnStartedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnTurnEndedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnPhaseStartedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnPhaseEndedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnComponentUsedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnZoneUsedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnComponentEnteredZoneActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnComponentLeftZoneActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnMessageSentActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnActionUsedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnVariableChangedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnRuleActivatedActiveRules = new Queue<RuleCore>();

		private void Awake ()
		{
			if (instance == null)
				instance = this;
			else if (instance != this)
			{
				Destroy(this);
				return;
			}

			string[] matchVariables = StringUtility.MatchVariables;
			for (int i = 0; i < matchVariables.Length; i++)
				variables.Add(matchVariables[i], "");
		}

		private void Start ()
		{
			if (autoStartGame)
				StartMatch(autoStartGame, FindObjectsOfType<CGComponent>(), FindObjectsOfType<Zone>());
		}

		// ======================================================================  P R I V A T E  ================================================================================

		private void StartMatchLoop ()
		{
			matchLoopCoroutine = StartCoroutine(MatchLoop());
		}

		private void StopMatchLoop ()
		{
			StopCoroutine(matchLoopCoroutine);
		}

		private IEnumerator MatchLoop ()
		{
			yield return OnMatchStartedTrigger();
			while (true)
			{
				variables["turnNumber"] = (++turnNumber).ToString();

				yield return OnTurnStartedTrigger();
				for (int i = 0; i < phases.Count; i++)
				{
					variables["phase"] = phases[i];
					yield return OnPhaseStartedTrigger(phases[i]);
					if (subphases.Count > 0)
					{
						while (subphases.Count > 0)
						{
							for (int j = 0; j < subphases.Count; j++)
							{
								variables["phase"] = subphases[j];
								yield return OnPhaseStartedTrigger(subphases[j]);
								while (!endPhase)
								{
									if (commands.Count == 0)
										yield return null;
									else
										for (int k = 0; k < commands.Count; k++)
										{
											yield return ExecuteCommand(commands.Dequeue());
											if (subphases.Count == 0)
												break;
										}
									if (subphases.Count == 0)
										break;
								}
								endPhase = false;
								yield return OnPhaseEndedTrigger(subphases[j]);
							}
						}
						subphases.Clear();
					}
					else
					{
						while (!endPhase)
						{
							if (commands.Count == 0)
								yield return null;
							else
								for (int j = 0; j < commands.Count; j++)
								{
									yield return ExecuteCommand(commands.Dequeue());
								}
						}
						endPhase = false;
					}
					variables["phase"] = phases[i];
					yield return OnPhaseEndedTrigger(phases[i]);
				}
				yield return OnTurnEndedTrigger();
			}
		}

		#region ================================================================ T R I G G E R S  =============================================================================

		private IEnumerator OnRuleActivatedTrigger (Rule rule)
		{
#if UNITY_EDITOR
			if (DebugLog)
				Debug.Log($"Trigger: On Rule Activated - rule: {rule}", 1);
#endif
			foreach (var item in OnRuleActivatedRules)
			{
				bool evaluation = item.Value.condition.Evaluate();
#if UNITY_EDITOR
				if (DebugLog)
				{
					if (item.Value.parent != null)
						Debug.Log($"Evaluating rule: {item.Value.name}", 2);
					else
						Debug.Log($"Evaluating callback: {item.Value.name}", 2);
					Debug.Log($"{evaluation} : {item.Value.condition}", 3);
				}
#endif
				if (evaluation)
					OnRuleActivatedActiveRules.Enqueue(item.Value);
			}
			for (int i = 0; i < OnRuleActivatedActiveRules.Count; i++)
				yield return ((Func<Rule, IEnumerator>)OnRuleActivatedActiveRules.Dequeue().callback).Invoke(rule);
		}

		private IEnumerator OnMatchStartedTrigger ()
		{
#if UNITY_EDITOR
			if (DebugLog)
				Debug.Log($"Trigger: On Match Started - matchNumber: {matchNumber}");
#endif
			foreach (var item in OnMatchStartedRules)
			{
				bool evaluation = item.Value.condition.Evaluate();
#if UNITY_EDITOR
				if (DebugLog)
				{
					if (item.Value.parent != null)
						Debug.Log($"Evaluating rule: {item.Value.name}", 1);
					else
						Debug.Log($"Evaluating callback: {item.Value.name}", 1);
					Debug.Log($"{evaluation} : {item.Value.condition}", 2);
				}
#endif
				if (evaluation)
					OnMatchStartedActiveRules.Enqueue(item.Value);
			}
			for (int i = 0; i < OnMatchStartedActiveRules.Count; i++)
			{
				RuleCore ruleCore = OnMatchStartedActiveRules.Dequeue();
				yield return ruleCore.callback.DynamicInvoke(matchNumber);
				if (ruleCore.parent != null)
					yield return OnRuleActivatedTrigger(ruleCore.parent);
			}
		}

		private IEnumerator OnMatchEndedTrigger ()
		{
#if UNITY_EDITOR
			if (DebugLog)
				Debug.Log($"Trigger: On Match Ended - matchNumber: {matchNumber}");
#endif
			foreach (var item in OnMatchEndedRules)
			{
				bool evaluation = item.Value.condition.Evaluate();
#if UNITY_EDITOR
				if (DebugLog)
				{
					if (item.Value.parent != null)
						Debug.Log($"Evaluating rule: {item.Value.name}", 1);
					else
						Debug.Log($"Evaluating callback: {item.Value.name}", 1);
					Debug.Log($"{evaluation} : {item.Value.condition}", 2);
				}
#endif
				if (evaluation)
					OnMatchEndedActiveRules.Enqueue(item.Value);
			}
			for (int i = 0; i < OnMatchEndedActiveRules.Count; i++)
			{
				RuleCore ruleCore = OnMatchEndedActiveRules.Dequeue();
				yield return ruleCore.callback.DynamicInvoke(matchNumber);
				if (ruleCore.parent != null)
					yield return OnRuleActivatedTrigger(ruleCore.parent);
			}
		}

		private IEnumerator OnTurnStartedTrigger ()
		{
#if UNITY_EDITOR
			if (DebugLog)
				Debug.Log($"Trigger: On Turn Started - turnNumber: {turnNumber}");
#endif
			foreach (var item in OnTurnStartedRules)
			{
				bool evaluation = item.Value.condition.Evaluate();
#if UNITY_EDITOR
				if (DebugLog)
				{
					if (item.Value.parent != null)
						Debug.Log($"Evaluating rule: {item.Value.name}", 1);
					else
						Debug.Log($"Evaluating callback: {item.Value.name}", 1);
					Debug.Log($"{evaluation} : {item.Value.condition}", 2);
				}
#endif
				if (evaluation)
					OnTurnStartedActiveRules.Enqueue(item.Value);
			}
			for (int i = 0; i < OnTurnStartedActiveRules.Count; i++)
			{
				RuleCore ruleCore = OnTurnStartedActiveRules.Dequeue();
				yield return ruleCore.callback.DynamicInvoke(turnNumber);
				if (ruleCore.parent != null)
					yield return OnRuleActivatedTrigger(ruleCore.parent);
			}
		}

		private IEnumerator OnTurnEndedTrigger ()
		{
#if UNITY_EDITOR
			if (DebugLog)
				Debug.Log($"Trigger: On Turn Ended - turnNumber: {turnNumber}");
#endif
			foreach (var item in OnTurnEndedRules)
			{
				bool evaluation = item.Value.condition.Evaluate();
#if UNITY_EDITOR
				if (DebugLog)
				{
					if (item.Value.parent != null)
						Debug.Log($"Evaluating rule: {item.Value.name}", 1);
					else
						Debug.Log($"Evaluating callback: {item.Value.name}", 1);
					Debug.Log($"{evaluation} : {item.Value.condition}", 2);
				}
#endif
				if (evaluation)
					OnTurnEndedActiveRules.Enqueue(item.Value);
			}
			for (int i = 0; i < OnTurnEndedActiveRules.Count; i++)
			{
				RuleCore ruleCore = OnTurnEndedActiveRules.Dequeue();
				yield return ruleCore.callback.DynamicInvoke(turnNumber);
				if (ruleCore.parent != null)
					yield return OnRuleActivatedTrigger(ruleCore.parent);
			}
		}

		private IEnumerator OnPhaseStartedTrigger (string phase)
		{
#if UNITY_EDITOR
			if (DebugLog)
				Debug.Log($"Trigger: On Phase Started - phase: {phase}");
#endif
			foreach (var item in OnPhaseStartedRules)
			{
				bool evaluation = item.Value.condition.Evaluate();
#if UNITY_EDITOR
				if (DebugLog)
				{
					if (item.Value.parent != null)
						Debug.Log($"Evaluating rule: {item.Value.name}", 1);
					else
						Debug.Log($"Evaluating callback: {item.Value.name}", 1);
					Debug.Log($"{evaluation} : {item.Value.condition}", 2);
				}
#endif
				if (evaluation)
					OnPhaseStartedActiveRules.Enqueue(item.Value);
			}
			for (int i = 0; i < OnPhaseStartedActiveRules.Count; i++)
			{
				RuleCore ruleCore = OnPhaseStartedActiveRules.Dequeue();
				yield return ruleCore.callback.DynamicInvoke(phase);
				if (ruleCore.parent != null)
					yield return OnRuleActivatedTrigger(ruleCore.parent);
			}
		}

		private IEnumerator OnPhaseEndedTrigger (string phase)
		{
#if UNITY_EDITOR
			if (DebugLog)
				Debug.Log($"Trigger: On Phase Ended - phase: {phase}");
#endif
			foreach (var item in OnPhaseEndedRules)
			{
				bool evaluation = item.Value.condition.Evaluate();
#if UNITY_EDITOR
				if (DebugLog)
				{
					if (item.Value.parent != null)
						Debug.Log($"Evaluating rule: {item.Value.name}", 1);
					else
						Debug.Log($"Evaluating callback: {item.Value.name}", 1);
					Debug.Log($"{evaluation} : {item.Value.condition}", 2);
				}
#endif
				if (evaluation)
					OnPhaseEndedActiveRules.Enqueue(item.Value);
			}
			for (int i = 0; i < OnPhaseEndedActiveRules.Count; i++)
			{
				RuleCore ruleCore = OnPhaseEndedActiveRules.Dequeue();
				yield return ruleCore.callback.DynamicInvoke(phase);
				if (ruleCore.parent != null)
					yield return OnRuleActivatedTrigger(ruleCore.parent);
			}
		}

		private IEnumerator OnComponentUsedTrigger (CGComponent card, string additionalInfo)
		{
#if UNITY_EDITOR
			if (DebugLog)
				Debug.Log($"Trigger: On Component Used - card: {card} - additionalInfo: {additionalInfo}");
#endif
			foreach (var item in OnComponentUsedRules)
			{
				bool evaluation = item.Value.condition.Evaluate();
#if UNITY_EDITOR
				if (DebugLog)
				{
					if (item.Value.parent != null)
						Debug.Log($"Evaluating rule: {item.Value.name}", 1);
					else
						Debug.Log($"Evaluating callback: {item.Value.name}", 1);
					Debug.Log($"{evaluation} : {item.Value.condition}", 2);
				}
#endif
				if (evaluation)
					OnComponentUsedActiveRules.Enqueue(item.Value);
			}
			for (int i = 0; i < OnComponentUsedActiveRules.Count; i++)
			{
				RuleCore ruleCore = OnComponentUsedActiveRules.Dequeue();
				yield return ruleCore.callback.DynamicInvoke(card, additionalInfo);
				if (ruleCore.parent != null)
					yield return OnRuleActivatedTrigger(ruleCore.parent);
			}
		}

		private IEnumerator OnZoneUsedTrigger (Zone zone, string additionalInfo)
		{
#if UNITY_EDITOR
			if (DebugLog)
				Debug.Log($"Trigger: On Zone Used - zone: {zone} - additionalInfo: {additionalInfo}");
#endif
			foreach (var item in OnZoneUsedRules)
			{
				bool evaluation = item.Value.condition.Evaluate();
#if UNITY_EDITOR
				if (DebugLog)
				{
					if (item.Value.parent != null)
						Debug.Log($"Evaluating rule: {item.Value.name}", 1);
					else
						Debug.Log($"Evaluating callback: {item.Value.name}", 1);
					Debug.Log($"{evaluation} : {item.Value.condition}", 2);
				}
#endif
				if (evaluation)
					OnZoneUsedActiveRules.Enqueue(item.Value);
			}
			for (int i = 0; i < OnZoneUsedActiveRules.Count; i++)
			{
				RuleCore ruleCore = OnZoneUsedActiveRules.Dequeue();
				yield return ruleCore.callback.DynamicInvoke(zone, additionalInfo);
				if (ruleCore.parent != null)
					yield return OnRuleActivatedTrigger(ruleCore.parent);
			}
		}

		private IEnumerator OnComponentEnteredZoneTrigger (CGComponent card, Zone newZone, Zone oldZone, string additionalInfo)
		{
#if UNITY_EDITOR
			if (DebugLog)
				Debug.Log($"Trigger: On Card Entered Zone - card: {card.name} - newZone: {newZone.name} - oldZone: {oldZone.name} - additionalInfo: {additionalInfo}");
#endif
			foreach (var item in OnComponentEnteredZoneRules)
			{
				bool evaluation = item.Value.condition.Evaluate();
#if UNITY_EDITOR
				if (DebugLog)
				{
					if (item.Value.parent != null)
						Debug.Log($"Evaluating rule: {item.Value.name}", 1);
					else
						Debug.Log($"Evaluating callback: {item.Value.name}", 1);
					Debug.Log($"{evaluation} : {item.Value.condition}", 2);
				}
#endif
				if (evaluation)
					OnComponentEnteredZoneActiveRules.Enqueue(item.Value);
			}
			for (int i = 0; i < OnComponentEnteredZoneActiveRules.Count; i++)
			{
				RuleCore ruleCore = OnComponentEnteredZoneActiveRules.Dequeue();
				yield return ruleCore.callback.DynamicInvoke(card, newZone, oldZone, additionalInfo);
				if (ruleCore.parent != null)
					yield return OnRuleActivatedTrigger(ruleCore.parent);
			}
		}

		private IEnumerator OnComponentLeftZoneTrigger (CGComponent card, Zone oldZone, string additionalInfo)
		{
#if UNITY_EDITOR
			if (DebugLog)
				Debug.Log($"Trigger: On Card Left Zone - card: {card.name} - oldZone: {oldZone.name} - additionalInfo: {additionalInfo}");
#endif
			foreach (var item in OnComponentLeftZoneRules)
			{
				bool evaluation = item.Value.condition.Evaluate();
#if UNITY_EDITOR
				if (DebugLog)
				{
					if (item.Value.parent != null)
						Debug.Log($"Evaluating rule: {item.Value.name}", 1);
					else
						Debug.Log($"Evaluating callback: {item.Value.name}", 1);
					Debug.Log($"{evaluation} : {item.Value.condition}", 2);
				}
#endif
				if (evaluation)
					OnComponentLeftZoneActiveRules.Enqueue(item.Value);
			}
			for (int i = 0; i < OnComponentLeftZoneActiveRules.Count; i++)
			{
				RuleCore ruleCore = OnComponentLeftZoneActiveRules.Dequeue();
				yield return ruleCore.callback.DynamicInvoke(card, oldZone, additionalInfo);
				if (ruleCore.parent != null)
					yield return OnRuleActivatedTrigger(ruleCore.parent);
			}
		}

		private IEnumerator OnMessageSentTrigger (string message, string additionalInfo)
		{
#if UNITY_EDITOR
			if (DebugLog)
				Debug.Log($"Trigger: On Message Sent - message: {message} - additionalInfo: {additionalInfo}");
#endif
			foreach (var item in OnMessageSentRules)
			{
				bool evaluation = item.Value.condition.Evaluate();
#if UNITY_EDITOR
				if (DebugLog)
				{
					if (item.Value.parent != null)
						Debug.Log($"Evaluating rule: {item.Value.name}", 1);
					else
						Debug.Log($"Evaluating callback: {item.Value.name}", 1);
					Debug.Log($"{evaluation} : {item.Value.condition}", 2);
				}
#endif
				if (evaluation)
					OnMessageSentActiveRules.Enqueue(item.Value);
			}
			for (int i = 0; i < OnMessageSentActiveRules.Count; i++)
			{
				RuleCore ruleCore = OnMessageSentActiveRules.Dequeue();
				yield return ruleCore.callback.DynamicInvoke(message, additionalInfo);
				if (ruleCore.parent != null)
					yield return OnRuleActivatedTrigger(ruleCore.parent);
			}
		}

		private IEnumerator OnActionUsedTrigger (string actionName, string additionalInfo)
		{
#if UNITY_EDITOR
			if (DebugLog)
				Debug.Log($"Trigger: On Action Used - actionName: {actionName} - additionalInfo: {additionalInfo}");
#endif
			foreach (var item in OnActionUsedRules)
			{
				bool evaluation = item.Value.condition.Evaluate();
#if UNITY_EDITOR
				if (DebugLog)
				{
					if (item.Value.parent != null)
						Debug.Log($"Evaluating rule: {item.Value.name}", 1);
					else
						Debug.Log($"Evaluating callback: {item.Value.name}", 1);
					Debug.Log($"{evaluation} : {item.Value.condition}", 2);
				}
#endif
				if (evaluation)
					OnActionUsedActiveRules.Enqueue(item.Value);
			}
			for (int i = 0; i < OnActionUsedActiveRules.Count; i++)
			{
				RuleCore ruleCore = OnActionUsedActiveRules.Dequeue();
				yield return ruleCore.callback.DynamicInvoke(actionName, additionalInfo);
				if (ruleCore.parent != null)
					yield return OnRuleActivatedTrigger(ruleCore.parent);
			}
		}

		private IEnumerator OnVariableChangedTrigger (string variable, string newValue, string oldValue, string additionalInfo)
		{
#if UNITY_EDITOR
			if (DebugLog)
				Debug.Log($"Trigger: On Variable Changed - variable: {variable} - newValue: {newValue} - oldValue: {oldValue} - additionalInfo: {additionalInfo}");
#endif
			foreach (var item in OnVariableChangedRules)
			{
				bool evaluation = item.Value.condition.Evaluate();
#if UNITY_EDITOR
				if (DebugLog)
				{
					if (item.Value.parent != null)
						Debug.Log($"Evaluating rule: {item.Value.name}", 1);
					else
						Debug.Log($"Evaluating callback: {item.Value.name}", 1);
					Debug.Log($"{evaluation} : {item.Value.condition}", 2);
				}
#endif
				if (evaluation)
					OnVariableChangedActiveRules.Enqueue(item.Value);
			}
			for (int i = 0; i < OnVariableChangedActiveRules.Count; i++)
			{
				RuleCore ruleCore = OnVariableChangedActiveRules.Dequeue();
				yield return ruleCore.callback.DynamicInvoke(variable, newValue, oldValue, additionalInfo);
				if (ruleCore.parent != null)
					yield return OnRuleActivatedTrigger(ruleCore.parent);
			}
		}

		#endregion

		#region ==============================================================  C O M M A N D S  =========================================================================

		internal static IEnumerator ExecuteCommand (Command command)
		{
#if UNITY_EDITOR
			if (DebugLog)
			{
				string msg = "- Executing command: " + command.type;
				switch (command.type)
				{
					case CommandType.UseAction:
						msg += $" ({((StringCommand)command).strParameter})";
						break;
					case CommandType.SendMessage:
						msg += $" ({((StringCommand)command).strParameter})";
						break;
					case CommandType.StartSubphaseLoop:
						msg += $" ({((StringCommand)command).strParameter})";
						break;
					case CommandType.UseComponent:
						if (command is ComponentCommand)
							msg += " => " + StringUtility.ListComponentSelection(((ComponentCommand)command).componentSelector, 3);
						else if (command is SingleComponentCommand)
							msg += " => " + ((SingleComponentCommand)command).component;
						break;
					case CommandType.UseZone:
						if (command is ZoneCommand)
							msg += " => " + StringUtility.ListZoneSelection(((ZoneCommand)command).zoneSelector, 2);
						else if (command is SingleZoneCommand)
							msg += " => " + ((SingleZoneCommand)command).zone;
						break;
					case CommandType.Shuffle:
						msg += " => " + StringUtility.ListZoneSelection(((ZoneCommand)command).zoneSelector, 2);
						break;
					case CommandType.SetComponentFieldValue:
						ComponentFieldCommand compFieldCommand = (ComponentFieldCommand)command;
						msg += " => " + StringUtility.ListComponentSelection(compFieldCommand.componentSelector, 1);
						msg += $" - Field: {compFieldCommand.fieldName} : {compFieldCommand.valueGetter.Get()}";
						break;
					case CommandType.SetVariable:
						VariableCommand varCommand = (VariableCommand)command;
						string variableName = varCommand.variableName;
						string value = varCommand.value.Get().ToString();
						msg += $" {variableName} to value {value}";
						break;
					case CommandType.MoveComponentToZone:
						ComponentZoneCommand compZoneCommand = (ComponentZoneCommand)command;
						msg += " => " + StringUtility.ListComponentSelection(compZoneCommand.componentSelector, 2);
						msg += " to " + StringUtility.ListZoneSelection(compZoneCommand.zoneSelector, 2);
						if (compZoneCommand.additionalInfo != null)
							msg += " +Params: " + compZoneCommand.additionalInfo;
						break;
					case CommandType.AddTagToComponent:
						ChangeComponentTagCommand compoAddTagCommand = (ChangeComponentTagCommand)command;
						msg += " => " + StringUtility.ListComponentSelection(compoAddTagCommand.componentSelector, 2) + $" Tag: {compoAddTagCommand.tag}";
						break;
					case CommandType.RemoveTagFromComponent:
						ChangeComponentTagCommand compoRemoveTagCommand = (ChangeComponentTagCommand)command;
						msg += " => " + StringUtility.ListComponentSelection(compoRemoveTagCommand.componentSelector, 2) + $" Tag: {compoRemoveTagCommand.tag}";
						break;
					default:
						break;
				}
				Debug.Log(msg, 1);
			}
#endif
			yield return command.Execute();
		}

		internal static IEnumerator ExecuteCommands (List<Command> commands)
		{
			for (int i = 0; i < commands.Count; i++)
				yield return ExecuteCommand(commands[i]);
		}

		private static IEnumerator EndCurrentPhase ()
		{
			instance.endPhase = true;
			yield return null;
		}

		private static IEnumerator EndTheMatch ()
		{
			instance.StopMatchLoop();
			yield return instance.OnMatchEndedTrigger();
		}

		private static IEnumerator EndSubphaseLoop ()
		{
			instance.subphases.Clear();
			yield return null;
		}

		private static IEnumerator UseActionPrivate (string actionName, string additionalInfo)
		{
			instance.variables["actionName"] = actionName;
			instance.variables["additionalInfo"] = additionalInfo;
			yield return instance.OnActionUsedTrigger(actionName, additionalInfo);
		}

		private static IEnumerator SendMessage (string message, string additionalInfo)
		{
			instance.variables["message"] = message;
			instance.variables["additionalInfo"] = additionalInfo;
			yield return instance.OnMessageSentTrigger(message, additionalInfo);
		}

		private static IEnumerator StartSubphaseLoop (string phases, string additionalInfo) //Doesn't use additional info
		{
			instance.subphases.AddRange(phases.Split(','));
			yield return null;
		}

		private static IEnumerator Shuffle (ZoneSelector zoneSelector, string additionalInfo)
		{
			instance.variables["additionalInfo"] = additionalInfo;
			List<Zone> zones = (List<Zone>)zoneSelector.Get();
			for (int i = 0; i < zones.Count; i++)
				zones[i].Shuffle();
			yield return null;
		}

		private static IEnumerator UseComponentPrivate (CGComponent component, string additionalInfo)
		{
			instance.variables["usedComponent"] = component.id;
			instance.variables["usedCompZone"] = component.Zone ? component.Zone.id : "";
			instance.variables["additionalInfo"] = additionalInfo;
			component.RaiseUsedEvent();
			yield return instance.OnComponentUsedTrigger(component, additionalInfo);
		}

		private static IEnumerator UseComponentPrivate (ComponentSelector componentSelector, string additionalInfo)
		{
			instance.variables["additionalInfo"] = additionalInfo;
			List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
			for (int i = 0; i < components.Count; i++)
			{
				yield return UseComponentPrivate(components[i], additionalInfo);
			}
		}

		private static IEnumerator UseZonePrivate (ZoneSelector zoneSelector, string additionalInfo)
		{
			instance.variables["additionalInfo"] = additionalInfo;
			List<Zone> zones = (List<Zone>)zoneSelector.Get();
			for (int i = 0; i < zones.Count; i++)
			{
				yield return UseZonePrivate(zones[i], additionalInfo);
			}
		}

		private static IEnumerator UseZonePrivate (Zone zone, string additionalInfo)
		{
			instance.variables["usedZone"] = zone.id;
			zone.BeUsed();
			yield return instance.OnZoneUsedTrigger(zone, additionalInfo);
		}

		private static IEnumerator MoveComponentToZone (CGComponent component, Zone zone, MovementAdditionalInfo additionalInfo)
		{
			Zone oldZone = component.Zone;
			if (oldZone)
			{
				instance.variables["oldZone"] = oldZone.id;
				component.RaiseWillLeaveZoneEvent(oldZone);
				oldZone.Pop(component);
				component.RaiseZoneLeftEvent(oldZone);
			}
			else
				instance.variables["oldZone"] = string.Empty;
			instance.variables["movedComponent"] = component.id;
			string addInfoStr = additionalInfo.ToString();
			instance.variables["additionalInfo"] = addInfoStr;
			yield return instance.OnComponentLeftZoneTrigger(component, oldZone, addInfoStr);
			instance.variables["newZone"] = zone.id;
			component.RaiseWillEnterZoneEvent(zone);
			zone.Push(component, additionalInfo);
			component.RaiseEnteredZoneEvent(zone);
			yield return instance.OnComponentEnteredZoneTrigger(component, zone, oldZone, addInfoStr);
		}

		private static IEnumerator MoveComponentToZone (ComponentSelector componentSelector, ZoneSelector zoneSelector, MovementAdditionalInfo additionalInfo)
		{
			List<Zone> zones = (List<Zone>)zoneSelector.Get();
			List<Zone> oldZones = new List<Zone>();
			for (int i = 0; i < zones.Count; i++)
			{
				Zone zoneToMove = zones[i];
				List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
				if (additionalInfo.keepOrder)
					for (int j = components.Count - 1; j >= 0; j--)
					{
						if (components[j].Zone && !oldZones.Contains(components[j].Zone))
							oldZones.Add(components[j].Zone);
						yield return MoveComponentToZone(components[j], zoneToMove, additionalInfo);
					}
				else
					for (int j = 0; j < components.Count; j++)
					{
						if (components[j].Zone && !oldZones.Contains(components[j].Zone))
							oldZones.Add(components[j].Zone);
						yield return MoveComponentToZone(components[j], zoneToMove, additionalInfo);
					}
				for (int j = 0; j < oldZones.Count; j++)
					oldZones[j].Organize();
				zones[i].Organize();
			}
		}

		private static IEnumerator SetComponentFieldValue (ComponentSelector componentSelector, string fieldName, Getter value, string additionalInfo)
		{
			List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
			instance.variables["additionalInfo"] = additionalInfo;
			for (int i = 0; i < components.Count; i++)
			{
				CGComponent component = components[i];
				string valueString = value.opChar != '\0' ? value.opChar + value.Get().ToString() : value.Get().ToString();
				component.SetFieldValue(fieldName, valueString, additionalInfo);
			}
			yield return null;
		}

		private static IEnumerator SetVariable (string variableName, Getter valueGetter, string additionalInfo)
		{
			variableName = ConvertVariableName(variableName);
			string value = valueGetter.Get().ToString();
			if (!instance.variables.ContainsKey(variableName))
				instance.variables.Add(variableName, value);
			else
				instance.variables[variableName] = value;
			instance.variables["variable"] = variableName;
			string oldValue = instance.variables["newValue"];
			instance.variables["oldValue"] = oldValue;
			instance.variables["newValue"] = value;
			instance.variables["additionalInfo"] = additionalInfo;
			yield return instance.OnVariableChangedTrigger(variableName, value, oldValue, additionalInfo);
		}

		private static IEnumerator AddTagToComponent (ComponentSelector componentSelector, string tag, string additionalInfo)
		{
			List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
			instance.variables["additionalInfo"] = additionalInfo;
			for (int i = 0; i < components.Count; i++)
			{
				CGComponent component = components[i];
				component.AddTag(tag);
			}
			yield return null;
		}

		private static IEnumerator RemoveTagFromComponent (ComponentSelector componentSelector, string tag, string additionalInfo)
		{
			List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
			instance.variables["additionalInfo"] = additionalInfo;
			for (int i = 0; i < components.Count; i++)
			{
				CGComponent component = components[i];
				component.RemoveTag(tag);
			}
			yield return null;
		}

		private static IEnumerator OrganizeZonePrivate (Zone zone, string addInfo = "")
		{
			zone.Organize();
			yield return null;
		}

		private static string ConvertVariableName (string variableName)
		{
			if (!string.IsNullOrEmpty(variableName) && variableName[0] == '$')
				variableName = variableName.Substring(1);
			if (variableName.Contains("$"))
			{
				string[] varBreak = variableName.Split(new char[] { '$' }, StringSplitOptions.RemoveEmptyEntries);
				variableName = varBreak[0] + GetVariable(varBreak[1]);
			}
			return variableName;
		}

		internal static Command CreateCommand (string clause)
		{
			Command newCommand = null;
			string additionalInfo = "";
			string[] clauseBreak = StringUtility.ArgumentsBreakdown(clause);
			switch (clauseBreak[0])
			{
				case "EndCurrentPhase":
					newCommand = new SimpleCommand(CommandType.EndCurrentPhase, EndCurrentPhase);
					break;
				case "EndTheMatch":
					newCommand = new SimpleCommand(CommandType.EndTheMatch, EndTheMatch);
					break;
				case "EndSubphaseLoop":
					newCommand = new SimpleCommand(CommandType.EndSubphaseLoop, EndSubphaseLoop);
					break;
				case "UseAction":
					additionalInfo = clauseBreak.Length > 2 ? string.Join(",", clauseBreak.SubArray(2)) : "";
					newCommand = new StringCommand(CommandType.UseAction, UseActionPrivate, clauseBreak[1], additionalInfo);
					break;
				case "SendMessage":
					additionalInfo = clauseBreak.Length > 2 ? string.Join(",", clauseBreak.SubArray(2)) : "";
					newCommand = new StringCommand(CommandType.SendMessage, SendMessage, clauseBreak[1], additionalInfo);
					break;
				case "StartSubphaseLoop":
					newCommand = new StringCommand(CommandType.StartSubphaseLoop, StartSubphaseLoop, clauseBreak[1]);
					break;
				case "UseComponent":
					additionalInfo = clauseBreak.Length > 2 ? string.Join(",", clauseBreak.SubArray(2)) : "";
					newCommand = new ComponentCommand(CommandType.UseComponent, UseComponentPrivate, new ComponentSelector(clauseBreak[1], instance.components), additionalInfo);
					break;
				case "UseZone":
					additionalInfo = clauseBreak.Length > 2 ? string.Join(",", clauseBreak.SubArray(2)) : "";
					newCommand = new ZoneCommand(CommandType.UseZone, UseZonePrivate, new ZoneSelector(clauseBreak[1], instance.zones), additionalInfo);
					break;
				case "Shuffle":
					additionalInfo = clauseBreak.Length > 2 ? string.Join(",", clauseBreak.SubArray(2)) : "";
					newCommand = new ZoneCommand(CommandType.Shuffle, Shuffle, new ZoneSelector(clauseBreak[1], instance.zones), additionalInfo);
					break;
				case "SetComponentFieldValue":
					additionalInfo = clauseBreak.Length > 4 ? string.Join(",", clauseBreak.SubArray(4)) : "";
					newCommand = new ComponentFieldCommand(CommandType.SetComponentFieldValue, SetComponentFieldValue, new ComponentSelector(clauseBreak[1], instance.components), clauseBreak[2], Getter.Build(clauseBreak[3]), additionalInfo);
					break;
				case "SetVariable":
					additionalInfo = clauseBreak.Length > 3 ? string.Join(",", clauseBreak.SubArray(3)) : "";
					char firstVarChar = clauseBreak[2][0];
					if (firstVarChar == '+' || firstVarChar == '*' || firstVarChar == '/' || firstVarChar == '%' || firstVarChar == '^')
						clauseBreak[2] = clauseBreak[1] + clauseBreak[2];
					newCommand = new VariableCommand(CommandType.SetVariable, SetVariable, clauseBreak[1], Getter.Build(clauseBreak[2]), additionalInfo);
					break;
				case "MoveComponentToZone":
					additionalInfo = clauseBreak.Length > 3 ? string.Join(",", clauseBreak.SubArray(3)) : "";
					newCommand = new ComponentZoneCommand(CommandType.MoveComponentToZone, MoveComponentToZone, new ComponentSelector(clauseBreak[1], instance.components), new ZoneSelector(clauseBreak[2], instance.zones), new MovementAdditionalInfo(additionalInfo));
					break;
				case "AddTagToComponent":
					additionalInfo = clauseBreak.Length > 3 ? string.Join(",", clauseBreak.SubArray(3)) : "";
					newCommand = new ChangeComponentTagCommand(CommandType.AddTagToComponent, AddTagToComponent, new ComponentSelector(clauseBreak[1], instance.components), clauseBreak[2], additionalInfo);
					break;
				case "RemoveTagFromComponent":
					additionalInfo = clauseBreak.Length > 3 ? string.Join(",", clauseBreak.SubArray(3)) : "";
					newCommand = new ChangeComponentTagCommand(CommandType.AddTagToComponent, RemoveTagFromComponent, new ComponentSelector(clauseBreak[1], instance.components), clauseBreak[2], additionalInfo);
					break;
				default:
					Debug.LogWarning("Effect not found: " + clauseBreak[0]);
					break;
			}

			if (newCommand == null)
			{
				Debug.LogError("Couldn't build a command with instruction: " + clause);
				return null;
			}
			newCommand.buildingStr = clause;
			return newCommand;
		}

		internal static List<Command> CreateCommands (string clause)
		{
			List<Command> commandSequence = new List<Command>();
			if (string.IsNullOrEmpty(clause))
				return commandSequence;
			string[] commandSequenceClause = clause.Split(';');
			for (int h = 0; h < commandSequenceClause.Length; h++)
			{
				Command newCommand = CreateCommand(commandSequenceClause[h]);
				if (newCommand != null)
					commandSequence.Add(newCommand);
			}
			return commandSequence;
		}

		private static bool IsValidParameters (ref NestedBooleans condition, Delegate callback, string name)
		{
			if (condition == null)
				condition = new NestedBooleans();
			if (callback == null)
			{
				Debug.LogError($"{name} error: Callback cannot be null.");
				return false;
			}
			return true;
		}

		#endregion

		#region =============================================================  C A L L B A C K S  ========================================================================

		internal static void AddMatchStartedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans();
			if (!instance.OnMatchStartedRules.ContainsKey(ruleCore.callback))
				instance.OnMatchStartedRules.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddMatchEndedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans();
			if (!instance.OnMatchEndedRules.ContainsKey(ruleCore.callback))
				instance.OnMatchEndedRules.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddTurnStartedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans();
			if (!instance.OnTurnStartedRules.ContainsKey(ruleCore.callback))
				instance.OnTurnStartedRules.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddTurnEndedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans();
			if (!instance.OnTurnEndedRules.ContainsKey(ruleCore.callback))
				instance.OnTurnEndedRules.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddPhaseStartedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans();
			if (!instance.OnPhaseStartedRules.ContainsKey(ruleCore.callback))
				instance.OnPhaseStartedRules.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddPhaseEndedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans();
			if (!instance.OnPhaseEndedRules.ContainsKey(ruleCore.callback))
				instance.OnPhaseEndedRules.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddComponentUsedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans();
			if (!instance.OnComponentUsedRules.ContainsKey(ruleCore.callback))
				instance.OnComponentUsedRules.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddZoneUsedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans();
			if (!instance.OnZoneUsedRules.ContainsKey(ruleCore.callback))
				instance.OnZoneUsedRules.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddComponentEnteredZoneCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans();
			if (!instance.OnComponentEnteredZoneRules.ContainsKey(ruleCore.callback))
				instance.OnComponentEnteredZoneRules.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddComponentLeftZoneCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans();
			if (!instance.OnComponentLeftZoneRules.ContainsKey(ruleCore.callback))
				instance.OnComponentLeftZoneRules.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddMessageSentCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans();
			if (!instance.OnMessageSentRules.ContainsKey(ruleCore.callback))
				instance.OnMessageSentRules.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddActionUsedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans();
			if (!instance.OnActionUsedRules.ContainsKey(ruleCore.callback))
				instance.OnActionUsedRules.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddVariableChangedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans();
			if (!instance.OnVariableChangedRules.ContainsKey(ruleCore.callback))
				instance.OnVariableChangedRules.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddRuleActivatedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans();
			if (!instance.OnRuleActivatedRules.ContainsKey(ruleCore.callback))
				instance.OnRuleActivatedRules.Add(ruleCore.callback, ruleCore);
		}

		public static void AddMatchStartedCallback (Func<int, IEnumerator> callback, string name = "Custom Match Started Callback") => AddMatchStartedCallback(null, callback);
		public static void AddMatchStartedCallback (NestedBooleans condition, Func<int, IEnumerator> callback, string name = "Custom Match Started Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnMatchStartedRules.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnMatchStarted, condition, callback);
				ruleCore.name = name;
				instance.OnMatchStartedRules.Add(callback, ruleCore);
			}
		}
		public static void RemoveMatchStartedCallback (Func<int, IEnumerator> callback)
		{
			if (instance.OnMatchStartedRules.ContainsKey(callback))
				instance.OnMatchStartedRules.Remove(callback);
		}

		public static void AddMatchEndedCallback (Func<int, IEnumerator> callback, string name = "Custom Match Ended Callback") => AddMatchEndedCallback(null, callback);
		public static void AddMatchEndedCallback (NestedBooleans condition, Func<int, IEnumerator> callback, string name = "Custom Match Ended Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnMatchEndedRules.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnMatchEnded, condition, callback);
				ruleCore.name = name;
				instance.OnMatchEndedRules.Add(callback, ruleCore);
			}
		}
		public static void RemoveMatchEndedCallback (Func<int, IEnumerator> callback)
		{
			if (instance.OnMatchEndedRules.ContainsKey(callback))
				instance.OnMatchEndedRules.Remove(callback);
		}

		public static void AddTurnStartedCallback (Func<int, IEnumerator> callback, string name = "Custom Turn Started Callback") => AddTurnStartedCallback(null, callback);
		public static void AddTurnStartedCallback (NestedBooleans condition, Func<int, IEnumerator> callback, string name = "Custom Turn Started Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnTurnStartedRules.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnTurnStarted, condition, callback);
				ruleCore.name = name;
				instance.OnTurnStartedRules.Add(callback, new RuleCore(TriggerLabel.OnTurnStarted, condition, callback));
			}
		}
		public static void RemoveTurnStartedCallback (Func<int, IEnumerator> callback)
		{
			if (instance.OnTurnStartedRules.ContainsKey(callback))
				instance.OnTurnStartedRules.Remove(callback);
		}

		public static void AddTurnEndedCallback (Func<int, IEnumerator> callback, string name = "Custom Turn Ended Callback") => AddTurnEndedCallback(null, callback);
		public static void AddTurnEndedCallback (NestedBooleans condition, Func<int, IEnumerator> callback, string name = "Custom Turn Ended Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnTurnEndedRules.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnTurnEnded, condition, callback);
				ruleCore.name = name;
				instance.OnTurnEndedRules.Add(callback, ruleCore);
			}
		}
		public static void RemoveTurnEndedCallback (Func<int, IEnumerator> callback)
		{
			if (instance.OnTurnEndedRules.ContainsKey(callback))
				instance.OnTurnEndedRules.Remove(callback);
		}

		public static void AddPhaseStartedCallback (Func<string, IEnumerator> callback, string name = "Custom Phase Started Callback") => AddPhaseStartedCallback(null, callback);
		public static void AddPhaseStartedCallback (NestedBooleans condition, Func<string, IEnumerator> callback, string name = "Custom Phase Started Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnPhaseStartedRules.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnPhaseStarted, condition, callback);
				ruleCore.name = name;
				instance.OnPhaseStartedRules.Add(callback, ruleCore);
			}
		}
		public static void RemovePhaseStartedCallback (Func<string, IEnumerator> callback)
		{
			if (instance.OnPhaseStartedRules.ContainsKey(callback))
				instance.OnPhaseStartedRules.Remove(callback);
		}

		public static void AddPhaseEndedCallback (Func<string, IEnumerator> callback, string name = "Custom Phase Ended Callback") => AddPhaseEndedCallback(null, callback);
		public static void AddPhaseEndedCallback (NestedBooleans condition, Func<string, IEnumerator> callback, string name = "Custom Phase Ended Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnPhaseEndedRules.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnPhaseEnded, condition, callback);
				ruleCore.name = name;
				instance.OnPhaseEndedRules.Add(callback, ruleCore);
			}
		}
		public static void RemovePhaseEndedCallback (Func<string, IEnumerator> callback)
		{
			if (instance.OnPhaseEndedRules.ContainsKey(callback))
				instance.OnPhaseEndedRules.Remove(callback);
		}

		public static void AddComponentUsedCallback (Func<CGComponent, string, IEnumerator> callback, string name = "Custom Component Used Callback") => AddComponentUsedCallback(null, callback);
		public static void AddComponentUsedCallback (NestedBooleans condition, Func<CGComponent, string, IEnumerator> callback, string name = "Custom Component Used Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnComponentUsedRules.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnComponentUsed, condition, callback);
				ruleCore.name = name;
				instance.OnComponentUsedRules.Add(callback, ruleCore);
			}
		}
		public static void RemoveComponentUsedCallback (Func<CGComponent, string, IEnumerator> callback)
		{
			if (instance.OnComponentUsedRules.ContainsKey(callback))
				instance.OnComponentUsedRules.Remove(callback);
		}

		public static void AddZoneUsedCallback (Func<Zone, string, IEnumerator> callback, string name = "Custom Zone Used Callback") => AddZoneUsedCallback(null, callback);
		public static void AddZoneUsedCallback (NestedBooleans condition, Func<Zone, string, IEnumerator> callback, string name = "Custom Zone Used Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnZoneUsedRules.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnZoneUsed, condition, callback);
				ruleCore.name = name;
				instance.OnZoneUsedRules.Add(callback, ruleCore);
			}
		}
		public static void RemoveZoneUsedCallback (Func<Zone, string, IEnumerator> callback)
		{
			if (instance.OnZoneUsedRules.ContainsKey(callback))
				instance.OnZoneUsedRules.Remove(callback);
		}

		public static void AddComponentEnteredZoneCallback (Func<CGComponent, Zone, Zone, string, IEnumerator> callback, string name = "Custom Component Entered Zone Callback") => AddComponentEnteredZoneCallback(null, callback);
		public static void AddComponentEnteredZoneCallback (NestedBooleans condition, Func<CGComponent, Zone, Zone, string, IEnumerator> callback, string name = "Custom Component Entered Zone Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnComponentEnteredZoneRules.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnComponentEnteredZone, condition, callback);
				ruleCore.name = name;
				instance.OnComponentEnteredZoneRules.Add(callback, ruleCore);
			}
		}
		public static void RemoveComponentEnteredZoneCallback (Func<CGComponent, Zone, Zone, string, IEnumerator> callback)
		{
			if (instance.OnComponentEnteredZoneRules.ContainsKey(callback))
				instance.OnComponentEnteredZoneRules.Remove(callback);
		}

		public static void AddComponentLeftZoneCallback (Func<CGComponent, Zone, string, IEnumerator> callback, string name = "Custom Component Left Zone Callback") => AddComponentLeftZoneCallback(null, callback);
		public static void AddComponentLeftZoneCallback (NestedBooleans condition, Func<CGComponent, Zone, string, IEnumerator> callback, string name = "Custom Component Left Zone Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnComponentLeftZoneRules.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnComponentLeftZone, condition, callback);
				ruleCore.name = name;
				instance.OnComponentLeftZoneRules.Add(callback, ruleCore);
			}
		}
		public static void RemoveComponentLeftZoneCallback (Func<CGComponent, Zone, string, IEnumerator> callback)
		{
			if (instance.OnComponentLeftZoneRules.ContainsKey(callback))
				instance.OnComponentLeftZoneRules.Remove(callback);
		}

		public static void AddMessageSentCallback (Func<string, string, IEnumerator> callback, string name = "Custom Message Sent Callback") => AddMessageSentCallback(null, callback);
		public static void AddMessageSentCallback (NestedBooleans condition, Func<string, string, IEnumerator> callback, string name = "Custom Message Sent Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnMessageSentRules.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnMessageSent, condition, callback);
				ruleCore.name = name;
				instance.OnMessageSentRules.Add(callback, ruleCore);
			}
		}
		public static void RemoveMessageSentCallback (Func<string, string, IEnumerator> callback)
		{
			if (instance.OnMessageSentRules.ContainsKey(callback))
				instance.OnMessageSentRules.Remove(callback);
		}

		public static void AddActionUsedCallback (Func<string, string, IEnumerator> callback, string name = "Custom Action Used Callback") => AddActionUsedCallback(null, callback, name);
		public static void AddActionUsedCallback (NestedBooleans condition, Func<string, string, IEnumerator> callback, string name = "Custom Action Used Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnActionUsedRules.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnActionUsed, condition, callback);
				ruleCore.name = name;
				instance.OnActionUsedRules.Add(callback, ruleCore);
			}
		}
		public static void RemoveActionUsedCallback (Func<string, string, IEnumerator> callback)
		{
			if (instance.OnActionUsedRules.ContainsKey(callback))
				instance.OnActionUsedRules.Remove(callback);
		}

		public static void AddVariableChangedCallback (Func<string, string, string, string, IEnumerator> callback, string name = "Custom Variable Changed Callback") => AddVariableChangedCallback(null, callback);
		public static void AddVariableChangedCallback (NestedBooleans condition, Func<string, string, string, string, IEnumerator> callback, string name = "Custom Variable Changed Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnVariableChangedRules.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnVariableChanged, condition, callback);
				ruleCore.name = name;
				instance.OnVariableChangedRules.Add(callback, ruleCore);
			}
		}
		public static void RemoveVariableChangedCallback (Func<string, string, string, string, IEnumerator> callback)
		{
			if (instance.OnVariableChangedRules.ContainsKey(callback))
				instance.OnVariableChangedRules.Remove(callback);
		}

		public static void AddRuleActivatedCallback (Func<Rule, IEnumerator> callback, string name = "Custom Rule Activated Callback") => AddRuleActivatedCallback(null, callback);
		public static void AddRuleActivatedCallback (NestedBooleans condition, Func<Rule, IEnumerator> callback, string name = "Custom Rule Activated Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnRuleActivatedRules.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnRuleActivated, condition, callback);
				ruleCore.name = name;
				instance.OnRuleActivatedRules.Add(callback, ruleCore);
			}
		}
		public static void RemoveRuleActivatedCallback (Func<Rule, IEnumerator> callback)
		{
			if (instance.OnRuleActivatedRules.ContainsKey(callback))
				instance.OnRuleActivatedRules.Remove(callback);
		}

		#endregion

		#region ===============================================================  P U B L I C  ==========================================================================

		public static void UseAction (string actionName, string additionalInfo = "")
		{
			instance.commands.Enqueue(new StringCommand(CommandType.UseAction, UseActionPrivate, actionName, additionalInfo));
		}

		public static void UseComponent (CGComponent component, string additionalInfo = "")
		{
			instance.commands.Enqueue(new SingleComponentCommand(UseComponentPrivate, component, additionalInfo));
		}

		public static void UseZone (Zone zone, string additionalInfo = "")
		{
			instance.commands.Enqueue(new SingleZoneCommand(UseZonePrivate, zone, additionalInfo));
		}

		public static void OrganizeZone (Zone zone)
		{
			instance.commands.Enqueue(new SingleZoneCommand(OrganizeZonePrivate, zone, ""));
		}

		public static void StartMatch (CGComponent[] components, Zone[] zones = null)
		{
			StartMatch(null, null, components, zones);
		}

		public static void StartMatch (Game game, CGComponent[] components, Zone[] zones, int? matchNumber = null)
		{

			List<VariableValuePair> gameVars = game.variablesAndValues;
			for (int i = 0; i < gameVars.Count; i++)
			{
				if (instance.variables.ContainsKey(gameVars[i].variable))
				{
					Debug.LogError("Match already has a variable named " + gameVars[i].variable);
					return;
				}
				instance.variables.Add(gameVars[i].variable, gameVars[i].value);
			}
#if UNITY_EDITOR
			if (DebugLog)
				Debug.Log($"Starting game {game.gameName}");
#endif
			StartMatch(game.rules, game.phases, components, zones, matchNumber);
		}

		public static void StartMatch (List<Rule> gameRules = null, List<string> phases = null, CGComponent[] components = null, Zone[] zones = null, int? matchNumber = null)
		{
			if (!instance)
				instance = FindObjectOfType<Match>();
			if (!instance)
				instance = new GameObject("Match").AddComponent<Match>();
			//Main data
			instance.rules = gameRules;
			if (phases == null)
				phases = new List<string>();
			if (phases.Count == 0)
				phases.Add("Main");
			instance.phases = phases;
			//Rules from game
			if (gameRules != null)
			{
				for (int i = 0; i < gameRules.Count; i++)
				{
					Rule rule = gameRules[i];
					rule.Initialize();
					rule.id = "r" + instance.ruleIDCounter++.ToString().PadLeft(4, '0');
					instance.ruleByID.Add(rule.id, rule);
				}
			}
			//Components
			if (components != null)
			{
				instance.components.AddRange(components);
				for (int i = 0; i < components.Length; i++)
				{
					CGComponent comp = components[i];
					comp.id = "c" + instance.componentIDCounter++.ToString().PadLeft(4, '0');
					//Components by ID
					instance.componentByID.Add(comp.id, comp);
					//Rules from components
					if (comp.rules != null)
						for (int j = 0; j < comp.rules.Count; j++)
						{
							Rule rule = comp.rules[j];
							rule.Initialize();
							rule.id = "r" + instance.ruleIDCounter++.ToString().PadLeft(4, '0');
							rule.origin = comp.id;
							instance.ruleByID.Add(rule.id, rule);
							instance.rules.Add(rule);
						}
				}
			}
			//Zones
			if (zones != null)
			{
				instance.zones.AddRange(zones);
				for (int i = 0; i < zones.Length; i++)
				{
					zones[i].id = "z" + instance.zoneIDCounter++.ToString().PadLeft(3, '0');
					instance.zoneByID.Add(zones[i].id, zones[i]);
				}
			}
			//Match number
			if (matchNumber.HasValue)
				instance.matchNumber = matchNumber.Value;
			else
				instance.matchNumber = 1;
			instance.variables["matchNumber"] = matchNumber.ToString();
			instance.turnNumber = 0;
			//Start match loop
#if UNITY_EDITOR
			if (DebugLog)
				Debug.Log($"Starting match loop");
#endif
			instance.StartMatchLoop();
		}

		public static bool HasVariable (string variableName)
		{
			variableName = ConvertVariableName(variableName);
			bool result = false;
			if (IsRunning)
				result = instance.variables.ContainsKey(variableName);
			else
				StringUtility.MatchVariables.Contains(variableName);
			return result;
		}

		public static string GetVariable (string variableName)
		{
			variableName = ConvertVariableName(variableName);
			if (HasVariable(variableName))
				return instance.variables[variableName];
			return string.Empty;
		}

		public static CGComponent GetComponentByID (string id)
		{
			if (instance.componentByID.ContainsKey(id))
				return instance.componentByID[id];
			Debug.LogWarning("Couldn't find component with id " + id);
			return null;
		}

		public static Zone GetZoneByID (string id)
		{
			if (instance.zoneByID.ContainsKey(id))
				return instance.zoneByID[id];
			Debug.LogWarning("Couldn't find zone with id " + id);
			return null;
		}

		public static List<Zone> GetAllZones ()
		{
			if (IsRunning)
				return instance.zones;
			return new List<Zone>();
		}

		public static List<CGComponent> GetAllComponents ()
		{
			if (IsRunning)
				return instance.components;
			return new List<CGComponent>();
		}

		public static List<Rule> GetAllRules ()
		{
			if (IsRunning)
				return instance.rules;
			return new List<Rule>();
		}

		#endregion
	}

	public enum TriggerLabel
	{
		OnMatchStarted,
		OnMatchEnded,
		OnTurnStarted,
		OnTurnEnded,
		OnPhaseStarted,
		OnPhaseEnded,
		OnComponentUsed,
		OnZoneUsed,
		OnComponentEnteredZone,
		OnComponentLeftZone,
		OnMessageSent,
		OnActionUsed,
		OnVariableChanged,
		OnRuleActivated
	}
}
