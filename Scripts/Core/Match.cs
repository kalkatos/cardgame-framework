
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

		private Dictionary<Delegate, RulePrimitive> OnMatchStartedRules = new Dictionary<Delegate, RulePrimitive>();
		private Dictionary<Delegate, RulePrimitive> OnMatchEndedRules = new Dictionary<Delegate, RulePrimitive>();
		private Dictionary<Delegate, RulePrimitive> OnTurnStartedRules = new Dictionary<Delegate, RulePrimitive>();
		private Dictionary<Delegate, RulePrimitive> OnTurnEndedRules = new Dictionary<Delegate, RulePrimitive>();
		private Dictionary<Delegate, RulePrimitive> OnPhaseStartedRules = new Dictionary<Delegate, RulePrimitive>();
		private Dictionary<Delegate, RulePrimitive> OnPhaseEndedRules = new Dictionary<Delegate, RulePrimitive>();
		private Dictionary<Delegate, RulePrimitive> OnComponentUsedRules = new Dictionary<Delegate, RulePrimitive>();
		private Dictionary<Delegate, RulePrimitive> OnZoneUsedRules = new Dictionary<Delegate, RulePrimitive>();
		private Dictionary<Delegate, RulePrimitive> OnComponentEnteredZoneRules = new Dictionary<Delegate, RulePrimitive>();
		private Dictionary<Delegate, RulePrimitive> OnComponentLeftZoneRules = new Dictionary<Delegate, RulePrimitive>();
		private Dictionary<Delegate, RulePrimitive> OnMessageSentRules = new Dictionary<Delegate, RulePrimitive>();
		private Dictionary<Delegate, RulePrimitive> OnActionUsedRules = new Dictionary<Delegate, RulePrimitive>();
		private Dictionary<Delegate, RulePrimitive> OnVariableChangedRules = new Dictionary<Delegate, RulePrimitive>();
		private Dictionary<Delegate, RulePrimitive> OnRuleActivatedRules = new Dictionary<Delegate, RulePrimitive>();

		private Queue<RulePrimitive> OnMatchStartedActiveRules = new Queue<RulePrimitive>();
		private Queue<RulePrimitive> OnMatchEndedActiveRules = new Queue<RulePrimitive>();
		private Queue<RulePrimitive> OnTurnStartedActiveRules = new Queue<RulePrimitive>();
		private Queue<RulePrimitive> OnTurnEndedActiveRules = new Queue<RulePrimitive>();
		private Queue<RulePrimitive> OnPhaseStartedActiveRules = new Queue<RulePrimitive>();
		private Queue<RulePrimitive> OnPhaseEndedActiveRules = new Queue<RulePrimitive>();
		private Queue<RulePrimitive> OnComponentUsedActiveRules = new Queue<RulePrimitive>();
		private Queue<RulePrimitive> OnZoneUsedActiveRules = new Queue<RulePrimitive>();
		private Queue<RulePrimitive> OnComponentEnteredZoneActiveRules = new Queue<RulePrimitive>();
		private Queue<RulePrimitive> OnComponentLeftZoneActiveRules = new Queue<RulePrimitive>();
		private Queue<RulePrimitive> OnMessageSentActiveRules = new Queue<RulePrimitive>();
		private Queue<RulePrimitive> OnActionUsedActiveRules = new Queue<RulePrimitive>();
		private Queue<RulePrimitive> OnVariableChangedActiveRules = new Queue<RulePrimitive>();
		private Queue<RulePrimitive> OnRuleActivatedActiveRules = new Queue<RulePrimitive>();

		public static bool DebugLog => instance.debugLog;
		public static bool IsRunning => instance != null;

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
		private Dictionary<TriggerLabel, List<Rule>> gameRulesByTrigger = new Dictionary<TriggerLabel, List<Rule>>();
		private Dictionary<TriggerLabel, List<Rule>> compRulesByTrigger = new Dictionary<TriggerLabel, List<Rule>>();
		private Dictionary<string, CGComponent> componentByID = new Dictionary<string, CGComponent>();
		private Dictionary<string, Zone> zoneByID = new Dictionary<string, Zone>();
		private Dictionary<string, Rule> ruleByID = new Dictionary<string, Rule>();

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

		private void AddRulePrimitive (RulePrimitive rulePrimitive)
		{
			switch (rulePrimitive.trigger)
			{
				case TriggerLabel.OnMatchStarted:
					break;
				case TriggerLabel.OnMatchEnded:
					break;
				case TriggerLabel.OnTurnStarted:
					break;
				case TriggerLabel.OnTurnEnded:
					break;
				case TriggerLabel.OnPhaseStarted:
					break;
				case TriggerLabel.OnPhaseEnded:
					break;
				case TriggerLabel.OnComponentUsed:
					break;
				case TriggerLabel.OnZoneUsed:
					break;
				case TriggerLabel.OnComponentEnteredZone:
					break;
				case TriggerLabel.OnComponentLeftZone:
					break;
				case TriggerLabel.OnMessageSent:
					break;
				case TriggerLabel.OnActionUsed:
					break;
				case TriggerLabel.OnVariableChanged:
					break;
				case TriggerLabel.OnRuleActivated:
					break;
			}
		}

		private IEnumerator OnRuleActivatedTrigger (Rule rule)
		{
			foreach (var item in OnRuleActivatedRules)
				if (item.Value.condition.Evaluate())
					OnRuleActivatedActiveRules.Enqueue(item.Value);
			for (int i = 0; i < OnRuleActivatedActiveRules.Count; i++)
				yield return OnRuleActivatedActiveRules.Dequeue().callback.DynamicInvoke(rule);
		}

		private IEnumerator OnMatchStartedTrigger ()
		{
			foreach (var item in OnMatchStartedRules)
				if (item.Value.condition.Evaluate())
					OnMatchStartedActiveRules.Enqueue(item.Value);
			for (int i = 0; i < OnMatchStartedActiveRules.Count; i++)
				yield return OnMatchStartedActiveRules.Dequeue().callback.DynamicInvoke(matchNumber);
		}

		private IEnumerator OnMatchEndedTrigger ()
		{
			foreach (var item in OnMatchEndedRules)
				if (item.Value.condition.Evaluate())
					OnMatchEndedActiveRules.Enqueue(item.Value);
			for (int i = 0; i < OnMatchEndedActiveRules.Count; i++)
				yield return OnMatchEndedActiveRules.Dequeue().callback.DynamicInvoke(matchNumber);
		}

		private IEnumerator OnTurnStartedTrigger ()
		{
			foreach (var item in OnTurnStartedRules)
				if (item.Value.condition.Evaluate())
					OnTurnStartedActiveRules.Enqueue(item.Value);
			for (int i = 0; i < OnTurnStartedActiveRules.Count; i++)
				yield return OnTurnStartedActiveRules.Dequeue().callback.DynamicInvoke(turnNumber);
		}

		private IEnumerator OnTurnEndedTrigger ()
		{
			foreach (var item in OnTurnEndedRules)
				if (item.Value.condition.Evaluate())
					OnTurnEndedActiveRules.Enqueue(item.Value);
			for (int i = 0; i < OnTurnEndedActiveRules.Count; i++)
				yield return OnTurnEndedActiveRules.Dequeue().callback.DynamicInvoke(turnNumber);
		}

		private IEnumerator OnPhaseStartedTrigger (string phase)
		{
			foreach (var item in OnPhaseStartedRules)
				if (item.Value.condition.Evaluate())
					OnPhaseStartedActiveRules.Enqueue(item.Value);
			for (int i = 0; i < OnPhaseStartedActiveRules.Count; i++)
				yield return OnPhaseStartedActiveRules.Dequeue().callback.DynamicInvoke(phase);
		}

		private IEnumerator OnPhaseEndedTrigger (string phase)
		{
			foreach (var item in OnPhaseEndedRules)
				if (item.Value.condition.Evaluate())
					OnPhaseEndedActiveRules.Enqueue(item.Value);
			for (int i = 0; i < OnPhaseEndedActiveRules.Count; i++)
				yield return OnPhaseEndedActiveRules.Dequeue().callback.DynamicInvoke(phase);
		}
		private IEnumerator OnComponentUsedTrigger (CGComponent card, string additionalInfo)
		{
			foreach (var item in OnComponentUsedRules)
				if (item.Value.condition.Evaluate())
					OnComponentUsedActiveRules.Enqueue(item.Value);
			for (int i = 0; i < OnComponentUsedActiveRules.Count; i++)
				yield return OnComponentUsedActiveRules.Dequeue().callback.DynamicInvoke(card, additionalInfo);
		}

		private IEnumerator OnZoneUsedTrigger (Zone zone, string additionalInfo)
		{
			foreach (var item in OnZoneUsedRules)
				if (item.Value.condition.Evaluate())
					OnZoneUsedActiveRules.Enqueue(item.Value);
			for (int i = 0; i < OnZoneUsedActiveRules.Count; i++)
				yield return OnZoneUsedActiveRules.Dequeue().callback.DynamicInvoke(zone, additionalInfo);
		}

		private IEnumerator OnComponentEnteredZoneTrigger (CGComponent card, Zone newZone, Zone oldZone, string additionalInfo)
		{
			foreach (var item in OnComponentEnteredZoneRules)
				if (item.Value.condition.Evaluate())
					OnComponentEnteredZoneActiveRules.Enqueue(item.Value);
			for (int i = 0; i < OnComponentEnteredZoneActiveRules.Count; i++)
				yield return OnComponentEnteredZoneActiveRules.Dequeue().callback.DynamicInvoke(card, newZone, oldZone, additionalInfo);
		}

		private IEnumerator OnComponentLeftZoneTrigger (CGComponent card, Zone oldZone, string additionalInfo)
		{
			foreach (var item in OnComponentLeftZoneRules)
				if (item.Value.condition.Evaluate())
					OnComponentLeftZoneActiveRules.Enqueue(item.Value);
			for (int i = 0; i < OnComponentLeftZoneActiveRules.Count; i++)
				yield return OnComponentLeftZoneActiveRules.Dequeue().callback.DynamicInvoke(card, oldZone, additionalInfo);
		}

		private IEnumerator OnMessageSentTrigger (string message, string additionalInfo)
		{
			foreach (var item in OnMessageSentRules)
				if (item.Value.condition.Evaluate())
					OnMessageSentActiveRules.Enqueue(item.Value);
			for (int i = 0; i < OnMessageSentActiveRules.Count; i++)
				yield return OnMessageSentActiveRules.Dequeue().callback.DynamicInvoke(message, additionalInfo);
		}

		private IEnumerator OnActionUsedTrigger (string actionName, string additionalInfo)
		{
			foreach (var item in OnActionUsedRules)
				if (item.Value.condition.Evaluate())
					OnActionUsedActiveRules.Enqueue(item.Value);
			for (int i = 0; i < OnActionUsedActiveRules.Count; i++)
				yield return OnActionUsedActiveRules.Dequeue().callback.DynamicInvoke(actionName, additionalInfo);
		}

		private IEnumerator OnVariableChangedTrigger (string variable, string newValue, string oldValue, string additionalInfo)
		{
			foreach (var item in OnVariableChangedRules)
				if (item.Value.condition.Evaluate())
					OnVariableChangedActiveRules.Enqueue(item.Value);
			for (int i = 0; i < OnVariableChangedActiveRules.Count; i++)
				yield return OnVariableChangedActiveRules.Dequeue().callback.DynamicInvoke(variable, newValue, oldValue, additionalInfo);
		}

		#endregion

		#region ================================================================  C O M M A N D S  ============================================================================

		internal static IEnumerator ExecuteCommand (Command command)
		{
			if (DebugLog)
			{
				string msg = "   * Executing command: " + command.type;
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
				Debug.Log(msg);
			}
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
					Debug.LogWarning("[CGEngine] Effect not found: " + clauseBreak[0]);
					break;
			}

			if (newCommand == null)
			{
				Debug.LogError("[CGEngine] Couldn't build a command with instruction: " + clause);
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
			if (DebugLog)
				Debug.Log($"Starting game {game.gameName}");
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
					if (!instance.gameRulesByTrigger.ContainsKey(rule.trigger))
						instance.gameRulesByTrigger.Add(rule.trigger, new List<Rule>());
					instance.gameRulesByTrigger[rule.trigger].Add(rule);
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
							if (!instance.compRulesByTrigger.ContainsKey(rule.trigger))
								instance.compRulesByTrigger.Add(rule.trigger, new List<Rule>());
							instance.compRulesByTrigger[rule.trigger].Add(rule);
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
			//Func

			//Start match loop
			if (DebugLog)
				Debug.Log($"Starting match loop");
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

		private static bool IsValidParameters (ref NestedBooleans condition, Delegate callback)
		{
			if (condition == null)
				condition = new NestedBooleans();
			if (callback == null)
			{
				Debug.LogError("[Match] Callback cannot be null.");
				return false;
			}
			return true;
		}

		public static void AddMatchStartedCallback (Func<int, IEnumerator> callback) => AddMatchStartedCallback(null, callback);
		public static void AddMatchStartedCallback (NestedBooleans condition, Func<int, IEnumerator> callback)
		{
			if (!IsValidParameters(ref condition, callback))
				return;
			if (!instance.OnMatchStartedRules.ContainsKey(callback))
				instance.OnMatchStartedRules.Add(callback, new RulePrimitive(TriggerLabel.OnMatchStarted, condition, callback));
		}
		public static void RemoveMatchStartedCallback (Func<int, IEnumerator> callback)
		{
			if (instance.OnMatchStartedRules.ContainsKey(callback))
				instance.OnMatchStartedRules.Remove(callback);
		}

		public static void AddMatchEndedCallback (Func<int, IEnumerator> callback) => AddMatchEndedCallback(null, callback);
		public static void AddMatchEndedCallback (NestedBooleans condition, Func<int, IEnumerator> callback)
		{
			if (!IsValidParameters(ref condition, callback))
				return;
			if (!instance.OnMatchEndedRules.ContainsKey(callback))
				instance.OnMatchEndedRules.Add(callback, new RulePrimitive(TriggerLabel.OnMatchEnded, condition, callback));
		}
		public static void RemoveMatchEndedCallback (Func<int, IEnumerator> callback)
		{
			if (instance.OnMatchEndedRules.ContainsKey(callback))
				instance.OnMatchEndedRules.Remove(callback);
		}

		public static void AddTurnStartedCallback (Func<int, IEnumerator> callback) => AddTurnStartedCallback(null, callback);
		public static void AddTurnStartedCallback (NestedBooleans condition, Func<int, IEnumerator> callback)
		{
			if (!IsValidParameters(ref condition, callback))
				return;
			if (!instance.OnTurnStartedRules.ContainsKey(callback))
				instance.OnTurnStartedRules.Add(callback, new RulePrimitive(TriggerLabel.OnTurnStarted, condition, callback));
		}
		public static void RemoveTurnStartedCallback (Func<int, IEnumerator> callback)
		{
			if (instance.OnTurnStartedRules.ContainsKey(callback))
				instance.OnTurnStartedRules.Remove(callback);
		}

		public static void AddTurnEndedCallback (Func<int, IEnumerator> callback) => AddTurnEndedCallback(null, callback);
		public static void AddTurnEndedCallback (NestedBooleans condition, Func<int, IEnumerator> callback)
		{
			if (!IsValidParameters(ref condition, callback))
				return;
			if (!instance.OnTurnEndedRules.ContainsKey(callback))
				instance.OnTurnEndedRules.Add(callback, new RulePrimitive(TriggerLabel.OnTurnEnded, condition, callback));
		}
		public static void RemoveTurnEndedCallback (Func<int, IEnumerator> callback)
		{
			if (instance.OnTurnEndedRules.ContainsKey(callback))
				instance.OnTurnEndedRules.Remove(callback);
		}

		public static void AddPhaseStartedCallback (Func<string, IEnumerator> callback) => AddPhaseStartedCallback(null, callback);
		public static void AddPhaseStartedCallback (NestedBooleans condition, Func<string, IEnumerator> callback)
		{
			if (!IsValidParameters(ref condition, callback))
				return;
			if (!instance.OnPhaseStartedRules.ContainsKey(callback))
				instance.OnPhaseStartedRules.Add(callback, new RulePrimitive(TriggerLabel.OnPhaseStarted, condition, callback));
		}
		public static void RemovePhaseStartedCallback (Func<string, IEnumerator> callback)
		{
			if (instance.OnPhaseStartedRules.ContainsKey(callback))
				instance.OnPhaseStartedRules.Remove(callback);
		}

		public static void AddPhaseEndedCallback (Func<string, IEnumerator> callback) => AddPhaseEndedCallback(null, callback);
		public static void AddPhaseEndedCallback (NestedBooleans condition, Func<string, IEnumerator> callback)
		{
			if (!IsValidParameters(ref condition, callback))
				return;
			if (!instance.OnPhaseEndedRules.ContainsKey(callback))
				instance.OnPhaseEndedRules.Add(callback, new RulePrimitive(TriggerLabel.OnPhaseEnded, condition, callback));
		}
		public static void RemovePhaseEndedCallback (Func<string, IEnumerator> callback)
		{
			if (instance.OnPhaseEndedRules.ContainsKey(callback))
				instance.OnPhaseEndedRules.Remove(callback);
		}

		public static void AddComponentUsedCallback (Func<CGComponent, string, IEnumerator> callback) => AddComponentUsedCallback(null, callback);
		public static void AddComponentUsedCallback (NestedBooleans condition, Func<CGComponent, string, IEnumerator> callback)
		{
			if (!IsValidParameters(ref condition, callback))
				return;
			if (!instance.OnComponentUsedRules.ContainsKey(callback))
				instance.OnComponentUsedRules.Add(callback, new RulePrimitive(TriggerLabel.OnComponentUsed, condition, callback));
		}
		public static void RemoveComponentUsedCallback (Func<CGComponent, string, IEnumerator> callback)
		{
			if (instance.OnComponentUsedRules.ContainsKey(callback))
				instance.OnComponentUsedRules.Remove(callback);
		}

		public static void AddZoneUsedCallback (Func<Zone, string, IEnumerator> callback) => AddZoneUsedCallback(null, callback);
		public static void AddZoneUsedCallback (NestedBooleans condition, Func<Zone, string, IEnumerator> callback)
		{
			if (!IsValidParameters(ref condition, callback))
				return;
			if (!instance.OnZoneUsedRules.ContainsKey(callback))
				instance.OnZoneUsedRules.Add(callback, new RulePrimitive(TriggerLabel.OnZoneUsed, condition, callback));
		}
		public static void RemoveZoneUsedCallback (Func<Zone, string, IEnumerator> callback)
		{
			if (instance.OnZoneUsedRules.ContainsKey(callback))
				instance.OnZoneUsedRules.Remove(callback);
		}

		public static void AddComponentEnteredZoneCallback (Func<CGComponent, Zone, Zone, string, IEnumerator> callback) => AddComponentEnteredZoneCallback(null, callback);
		public static void AddComponentEnteredZoneCallback (NestedBooleans condition, Func<CGComponent, Zone, Zone, string, IEnumerator> callback)
		{
			if (!IsValidParameters(ref condition, callback))
				return;
			if (!instance.OnComponentEnteredZoneRules.ContainsKey(callback))
				instance.OnComponentEnteredZoneRules.Add(callback, new RulePrimitive(TriggerLabel.OnComponentEnteredZone, condition, callback));
		}
		public static void RemoveComponentEnteredZoneCallback (Func<CGComponent, Zone, Zone, string, IEnumerator> callback)
		{
			if (instance.OnComponentEnteredZoneRules.ContainsKey(callback))
				instance.OnComponentEnteredZoneRules.Remove(callback);
		}

		public static void AddComponentLeftZoneCallback (Func<CGComponent, Zone, string, IEnumerator> callback) => AddComponentLeftZoneCallback(null, callback);
		public static void AddComponentLeftZoneCallback (NestedBooleans condition, Func<CGComponent, Zone, string, IEnumerator> callback)
		{
			if (!IsValidParameters(ref condition, callback))
				return;
			if (!instance.OnComponentLeftZoneRules.ContainsKey(callback))
				instance.OnComponentLeftZoneRules.Add(callback, new RulePrimitive(TriggerLabel.OnComponentLeftZone, condition, callback));
		}
		public static void RemoveComponentLeftZoneCallback (Func<CGComponent, Zone, string, IEnumerator> callback)
		{
			if (instance.OnComponentLeftZoneRules.ContainsKey(callback))
				instance.OnComponentLeftZoneRules.Remove(callback);
		}

		public static void AddMessageSentCallback (Func<string, string, IEnumerator> callback) => AddMessageSentCallback(null, callback);
		public static void AddMessageSentCallback (NestedBooleans condition, Func<string, string, IEnumerator> callback)
		{
			if (!IsValidParameters(ref condition, callback))
				return;
			if (!instance.OnMessageSentRules.ContainsKey(callback))
				instance.OnMessageSentRules.Add(callback, new RulePrimitive(TriggerLabel.OnMessageSent, condition, callback));
		}
		public static void RemoveMessageSentCallback (Func<string, string, IEnumerator> callback)
		{
			if (instance.OnMessageSentRules.ContainsKey(callback))
				instance.OnMessageSentRules.Remove(callback);
		}

		public static void AddActionUsedCallback (Func<string, string, IEnumerator> callback) => AddActionUsedCallback(null, callback);
		public static void AddActionUsedCallback (NestedBooleans condition, Func<string, string, IEnumerator> callback)
		{
			if (!IsValidParameters(ref condition, callback))
				return;
			if (!instance.OnActionUsedRules.ContainsKey(callback))
				instance.OnActionUsedRules.Add(callback, new RulePrimitive(TriggerLabel.OnActionUsed, condition, callback));
		}
		public static void RemoveActionUsedCallback (Func<string, string, IEnumerator> callback)
		{
			if (instance.OnActionUsedRules.ContainsKey(callback))
				instance.OnActionUsedRules.Remove(callback);
		}

		public static void AddVariableChangedCallback (Func<string, string, string, string, IEnumerator> callback) => AddVariableChangedCallback(null, callback);
		public static void AddVariableChangedCallback (NestedBooleans condition, Func<string, string, string, string, IEnumerator> callback)
		{
			if (!IsValidParameters(ref condition, callback))
				return;
			if (!instance.OnVariableChangedRules.ContainsKey(callback))
				instance.OnVariableChangedRules.Add(callback, new RulePrimitive(TriggerLabel.OnVariableChanged, condition, callback));
		}
		public static void RemoveVariableChangedCallback (Func<string, string, string, string, IEnumerator> callback)
		{
			if (instance.OnVariableChangedRules.ContainsKey(callback))
				instance.OnVariableChangedRules.Remove(callback);
		}

		public static void AddRuleActivatedCallback (Func<Rule, IEnumerator> callback) => AddRuleActivatedCallback(null, callback);
		public static void AddRuleActivatedCallback (NestedBooleans condition, Func<Rule, IEnumerator> callback)
		{
			if (!IsValidParameters(ref condition, callback))
				return;
			if (!instance.OnRuleActivatedRules.ContainsKey(callback))
				instance.OnRuleActivatedRules.Add(callback, new RulePrimitive(TriggerLabel.OnRuleActivated, condition, callback));
		}
		public static void RemoveRuleActivatedCallback (Func<Rule, IEnumerator> callback)
		{
			if (instance.OnRuleActivatedRules.ContainsKey(callback))
				instance.OnRuleActivatedRules.Remove(callback);
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
