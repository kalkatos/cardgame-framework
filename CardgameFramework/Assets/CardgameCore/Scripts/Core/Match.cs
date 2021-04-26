
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

		public static Func<IEnumerator> OnMatchStarted;
		public static Func<IEnumerator> OnMatchEnded;
		public static Func<IEnumerator> OnTurnStarted;
		public static Func<IEnumerator> OnTurnEnded;
		public static Func<IEnumerator> OnPhaseStarted;
		public static Func<IEnumerator> OnPhaseEnded;
		public static Func<IEnumerator> OnComponentUsed;
		public static Func<IEnumerator> OnZoneUsed;
		public static Func<IEnumerator> OnComponentEnteredZone;
		public static Func<IEnumerator> OnComponentLeftZone;
		public static Func<IEnumerator> OnMessageSent;
		public static Func<IEnumerator> OnActionUsed;
		public static Func<IEnumerator> OnVariableChanged;
		public static Func<IEnumerator> OnRuleActivated;

		public static bool DebugLog { get { return instance.debugLog; } }

		public bool debugLog;

		[SerializeField] private Game autoStartGame;

		//Match control
		private int componentIDCounter = 1;
		private int zoneIDCounter = 1;
		private int ruleIDCounter = 1;
		private bool endMatch;
		private bool endPhase;
		private int turnNumber;
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
		private Dictionary<TriggerLabel, Func<IEnumerator>> funcByTrigger = new Dictionary<TriggerLabel, Func<IEnumerator>>();
		private Dictionary<string, CGComponent> componentByID = new Dictionary<string, CGComponent>();
		private Dictionary<string, Zone> zoneByID = new Dictionary<string, Zone>();
		private Dictionary<string, Rule> ruleByID = new Dictionary<string, Rule>();
		private int activatedTriggers = 0;
		private long activatedGameRules = 0;
		private long activatedCompRules = 0;

		private void Awake ()
		{
			if (instance == null)
				instance = this;
			else if (instance != this)
			{
				Destroy(this);
				return;
			}

			variables.Add("matchNumber", "");
			variables.Add("turnNumber", "");
			variables.Add("phase", "");
			variables.Add("actionName", "");
			variables.Add("message", "");
			variables.Add("variable", "");
			variables.Add("newValue", "");
			variables.Add("oldValue", "");
			variables.Add("rule", "");
			variables.Add("usedComponent", "");
			variables.Add("usedCompZone", "");
			variables.Add("movedComponent", "");
			variables.Add("newZone", "");
			variables.Add("oldZone", "");
			variables.Add("usedZone", "");
			variables.Add("additionalInfo", "");
		}

		private void Start ()
		{
			if (autoStartGame)
				StartMatch(autoStartGame, FindObjectsOfType<CGComponent>(), FindObjectsOfType<Zone>());
		}

		// ======================================================================  P R I V A T E  ================================================================================

		private IEnumerator MatchLoop ()
		{
			if (HasTriggers(TriggerLabel.OnMatchStarted))
				yield return OnMatchStartedTrigger();
			while (!endMatch)
			{
				variables["turnNumber"] = (++turnNumber).ToString();
				if (HasTriggers(TriggerLabel.OnTurnStarted))
					yield return OnTurnStartedTrigger();
				for (int i = 0; i < phases.Count; i++)
				{
					if (endMatch)
						break;
					variables["phase"] = phases[i];
					if (HasTriggers(TriggerLabel.OnPhaseStarted))
						yield return OnPhaseStartedTrigger();
					if (subphases.Count > 0)
					{
						while (subphases.Count > 0)
						{
							for (int j = 0; j < subphases.Count; j++)
							{
								variables["phase"] = subphases[j];
								if (HasTriggers(TriggerLabel.OnPhaseStarted))
									yield return OnPhaseStartedTrigger();
								while (!endPhase)
								{
									if (endMatch)
										break;
									if (commands.Count == 0)
										yield return null;
									else
										for (int k = 0; k < commands.Count; k++)
										{
											yield return ExecuteCommand(commands.Dequeue());
											if (endMatch || subphases.Count == 0)
												break;
										}
									if (endMatch || subphases.Count == 0)
										break;
								}
								endPhase = false;
								if (endMatch)
									break;
								if (HasTriggers(TriggerLabel.OnPhaseEnded))
									yield return OnPhaseEndedTrigger();
							}
							if (endMatch)
								break;
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
									if (endMatch)
										break;
								}
							if (endMatch)
								break;
						}
						endPhase = false;
					}
					if (endMatch)
						break;
					variables["phase"] = phases[i];
					if (HasTriggers(TriggerLabel.OnPhaseEnded))
						yield return OnPhaseEndedTrigger();
				}
				if (endMatch)
					break;
				if (HasTriggers(TriggerLabel.OnTurnEnded))
					yield return OnTurnEndedTrigger();
			}
			if (HasTriggers(TriggerLabel.OnMatchEnded))
				yield return OnMatchEndedTrigger();
		}

		#region ================================================================ T R I G G E R S  =============================================================================

		private static bool HasTriggers (TriggerLabel type)
		{
			instance.activatedTriggers = 0;
			instance.activatedGameRules = 0;
			instance.activatedCompRules = 0;
			if (instance.gameRulesByTrigger.ContainsKey(type))
			{
				instance.activatedTriggers += 1;
				List<Rule> rules = instance.gameRulesByTrigger[type];
				for (int i = 0; i < rules.Count; i++)
					if (rules[i].conditionObject.Evaluate())
						instance.activatedGameRules = 1 << i;
			}
			if (instance.compRulesByTrigger.ContainsKey(type))
			{
				instance.activatedTriggers += 2;
				List<Rule> rules = instance.compRulesByTrigger[type];
				for (int i = 0; i < rules.Count; i++)
					if (rules[i].conditionObject.Evaluate())
						instance.activatedCompRules = 1 << i;
			}
			if (instance.funcByTrigger[type] != null)
				instance.activatedTriggers += 4;

			return (instance.activatedTriggers & 4) > 0 || instance.activatedGameRules > 0 || instance.activatedCompRules > 0;
		}

		private IEnumerator TriggerRules (TriggerLabel type)
		{
			int activatedTriggers = this.activatedTriggers;

			if ((activatedTriggers & 1) > 0)
			{
				List<Rule> rules = gameRulesByTrigger[type];
				for (int i = 0; i < rules.Count; i++)
				{
					if ((activatedGameRules & (1 << i)) > 0)
					{
						variables["rule"] = rules[i].id;
						if (debugLog)
							Debug.Log("Rule Activated: " + instance.ruleByID[variables["rule"]].name);
						if (type != TriggerLabel.OnRuleActivated && HasTriggers(TriggerLabel.OnRuleActivated))
							yield return OnRuleActivatedTrigger();
						for (int j = 0; j < rules[i].commandsList.Count; j++)
							yield return ExecuteCommand(rules[i].commandsList[j]);
					}
				}
			}
			if ((activatedTriggers & 2) > 0)
			{
				List<Rule> rules = compRulesByTrigger[type];
				for (int i = 0; i < rules.Count; i++)
				{
					if ((activatedCompRules & (1 << i)) > 0)
					{
						variables["rule"] = rules[i].id;
						if (debugLog)
							Debug.Log("Rule Activated: " + instance.ruleByID[variables["rule"]].name);
						if (type != TriggerLabel.OnRuleActivated && HasTriggers(TriggerLabel.OnRuleActivated))
							yield return OnRuleActivatedTrigger();
						for (int j = 0; j < rules[i].commandsList.Count; j++)
							yield return ExecuteCommand(rules[i].commandsList[j]);
					}
				}
			}


			if ((activatedTriggers & 4) > 0)
				yield return Invoke(funcByTrigger[type]);
		}

		private IEnumerator Invoke (Func<IEnumerator> trigger)
		{
			if (trigger != null)
				foreach (var func in trigger.GetInvocationList())
					yield return func.DynamicInvoke();
		}

		private IEnumerator OnRuleActivatedTrigger ()
		{
			yield return TriggerRules(TriggerLabel.OnRuleActivated);
		}

		private IEnumerator OnMatchStartedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnMatchStarted - matchNumber = " + instance.variables["matchNumber"]);
			yield return TriggerRules(TriggerLabel.OnMatchStarted);
		}

		private IEnumerator OnMatchEndedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnMatchEnded - matchNumber = " + instance.variables["matchNumber"]);
			yield return TriggerRules(TriggerLabel.OnMatchEnded);
		}

		private IEnumerator OnTurnStartedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnTurnStarted - turnNumber = " + instance.variables["turnNumber"]);
			yield return TriggerRules(TriggerLabel.OnTurnStarted);
		}

		private IEnumerator OnTurnEndedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnTurnEnded - turnNumber = " + instance.variables["turnNumber"]);
			yield return TriggerRules(TriggerLabel.OnTurnEnded);
		}

		private IEnumerator OnPhaseStartedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnPhaseStarted - phase = " + instance.variables["phase"]);
			yield return TriggerRules(TriggerLabel.OnPhaseStarted);
		}

		private IEnumerator OnPhaseEndedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnPhaseEnded - phase = " + instance.variables["phase"]);
			yield return TriggerRules(TriggerLabel.OnPhaseEnded);
		}

		private IEnumerator OnComponentUsedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnComponentUsed - " + instance.componentByID[instance.variables["usedComponent"]]);
			yield return TriggerRules(TriggerLabel.OnComponentUsed);
		}

		private IEnumerator OnZoneUsedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnZoneUsed - " + instance.zoneByID[instance.variables["usedZone"]]);
			yield return TriggerRules(TriggerLabel.OnZoneUsed);
		}

		private IEnumerator OnComponentEnteredZoneTrigger ()
		{
			if (debugLog)
				Debug.Log($"Triggering: OnComponentEnteredZone - {instance.componentByID[instance.variables["movedComponent"]]} - {instance.zoneByID[instance.variables["oldZone"]]} - {instance.zoneByID[instance.variables["newZone"]]}");
			yield return TriggerRules(TriggerLabel.OnComponentEnteredZone);
		}

		private IEnumerator OnComponentLeftZoneTrigger ()
		{
			if (debugLog)
				Debug.Log($"Triggering: OnComponentLeftZone - {instance.componentByID[instance.variables["movedComponent"]]} - {instance.zoneByID[instance.variables["oldZone"]]}");
			yield return TriggerRules(TriggerLabel.OnComponentLeftZone);
		}

		private IEnumerator OnMessageSentTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnMessageSent - " + variables["message"]);
			yield return TriggerRules(TriggerLabel.OnMessageSent);
		}

		private IEnumerator OnActionUsedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnActionUsed - " + variables["actionName"]);
			yield return TriggerRules(TriggerLabel.OnActionUsed);
		}

		private IEnumerator OnVariableChangedTrigger ()
		{
			if (debugLog)
				Debug.Log($"Triggering: OnVariableChanged - variable: {variables["variable"]} - value: {variables["newValue"]}");
			yield return TriggerRules(TriggerLabel.OnVariableChanged);
		}

		#endregion

		#region ================================================================  C O M M A N D S  ============================================================================

		private IEnumerator ExecuteCommand (Command command)
		{
			if (instance.debugLog)
			{
				string msg = "* Executing command: " + command.type;
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

		private static IEnumerator EndCurrentPhase ()
		{
			instance.endPhase = true;
			yield return null;
		}

		private static IEnumerator EndTheMatch ()
		{
			instance.endMatch = true;
			yield return null;
		}

		private static IEnumerator EndSubphaseLoop ()
		{
			instance.subphases.Clear();
			yield return null;
		}

		private static IEnumerator UseAction (string actionName, string additionalInfo)
		{
			instance.variables["actionName"] = actionName;
			instance.variables["additionalInfo"] = additionalInfo;
			if (HasTriggers(TriggerLabel.OnActionUsed))
				yield return instance.OnActionUsedTrigger();
		}

		private static IEnumerator SendMessage (string message, string additionalInfo)
		{
			instance.variables["message"] = message;
			if (HasTriggers(TriggerLabel.OnMessageSent))
				yield return instance.OnMessageSentTrigger();
		}

		private static IEnumerator StartSubphaseLoop (string phases, string additionalInfo)
		{
			instance.subphases.AddRange(phases.Split(','));
			yield return null;
		}

		private static IEnumerator Shuffle (ZoneSelector zoneSelector, string additionalInfo)
		{
			List<Zone> zones = (List<Zone>)zoneSelector.Get();
			for (int i = 0; i < zones.Count; i++)
				zones[i].Shuffle();
			yield return null;
		}

		private static IEnumerator UseComponent (CGComponent component, string additionalInfo)
		{
			instance.variables["usedComponent"] = component.id;
			instance.variables["usedCompZone"] = component.Zone ? component.Zone.id : "";
			component.BeUsed();
			if (HasTriggers(TriggerLabel.OnComponentUsed))
				yield return instance.OnComponentUsedTrigger();
		}

		private static IEnumerator UseComponent (ComponentSelector componentSelector, string additionalInfo)
		{
			List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
			for (int i = 0; i < components.Count; i++)
			{
				yield return UseComponent(components[i], additionalInfo);
			}
		}

		private static IEnumerator UseZone (ZoneSelector zoneSelector, string additionalInfo)
		{
			List<Zone> zones = (List<Zone>)zoneSelector.Get();
			for (int i = 0; i < zones.Count; i++)
			{
				yield return UseZone(zones[i], additionalInfo);
			}
		}

		private static IEnumerator UseZone (Zone zone, string additionalInfo)
		{
			instance.variables["usedZone"] = zone.id;
			zone.BeUsed();
			if (HasTriggers(TriggerLabel.OnZoneUsed))
				yield return instance.OnZoneUsedTrigger();
		}

		private static IEnumerator MoveComponentToZone (CGComponent component, Zone zone, MovementAdditionalInfo additionalInfo)
		{
			Zone oldZone = component.Zone;
			if (oldZone)
			{
				instance.variables["oldZone"] = oldZone.id;
				oldZone.Pop(component);
				oldZone.Organize();
			}
			else
				instance.variables["oldZone"] = string.Empty;
			instance.variables["movedComponent"] = component.id;
			if (HasTriggers(TriggerLabel.OnComponentLeftZone))
				yield return instance.OnComponentLeftZoneTrigger();
			instance.variables["newZone"] = zone.id;
			zone.Push(component, additionalInfo);
			if (HasTriggers(TriggerLabel.OnComponentEnteredZone))
				yield return instance.OnComponentEnteredZoneTrigger();
			zone.Organize();
		}

		private static IEnumerator MoveComponentToZone (ComponentSelector componentSelector, ZoneSelector zoneSelector, MovementAdditionalInfo additionalInfo)
		{
			List<Zone> zones = (List<Zone>)zoneSelector.Get();
			for (int i = 0; i < zones.Count; i++)
			{
				Zone zoneToMove = zones[i];
				List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
				if (additionalInfo.keepOrder)
					for (int j = components.Count - 1; j >= 0; j--)
						yield return MoveComponentToZone(components[j], zoneToMove, additionalInfo);
				else
					for (int j = 0; j < components.Count; j++)
						yield return MoveComponentToZone(components[j], zoneToMove, additionalInfo);
			}
		}

		private static IEnumerator SetComponentFieldValue (ComponentSelector componentSelector, string fieldName, Getter value, string additionalInfo)
		{
			List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
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
			instance.variables["oldValue"] = instance.variables["newValue"];
			instance.variables["newValue"] = value;
			if (HasTriggers(TriggerLabel.OnVariableChanged))
				yield return instance.OnVariableChangedTrigger();
		}

		private static IEnumerator AddTagToComponent (ComponentSelector componentSelector, string tag, string additionalInfo)
		{
			List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
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
			for (int i = 0; i < components.Count; i++)
			{
				CGComponent component = components[i];
				component.RemoveTag(tag);
			}
			yield return null;
		}

		private static IEnumerator OrganizeZone (Zone zone, string addInfo = "")
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

		public static Command CreateCommand (string clause)
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
					newCommand = new StringCommand(CommandType.UseAction, UseAction, clauseBreak[1], additionalInfo);
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
					newCommand = new ComponentCommand(CommandType.UseComponent, UseComponent, new ComponentSelector(clauseBreak[1], instance.components), additionalInfo);
					break;
				case "UseZone":
					additionalInfo = clauseBreak.Length > 2 ? string.Join(",", clauseBreak.SubArray(2)) : "";
					newCommand = new ZoneCommand(CommandType.UseZone, UseZone, new ZoneSelector(clauseBreak[1], instance.zones), additionalInfo);
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

		public static List<Command> CreateCommands (string clause)
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

		public static void EnqueueActionUse (string actionName, string additionalInfo = "")
		{
			instance.commands.Enqueue(new StringCommand(CommandType.UseAction, UseAction, actionName, additionalInfo));
		}

		public static void EnqueueComponentUse (CGComponent component, string additionalInfo = "")
		{
			instance.commands.Enqueue(new SingleComponentCommand(UseComponent, component, additionalInfo));
		}

		public static void EnqueueZoneUse (Zone zone, string additionalInfo = "")
		{
			instance.commands.Enqueue(new SingleZoneCommand(UseZone, zone, additionalInfo));
		}

		public static void EnqueueZoneOrganization (Zone zone)
		{
			instance.commands.Enqueue(new SingleZoneCommand(OrganizeZone, zone, ""));
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
			//Match number
			if (matchNumber.HasValue)
				instance.variables["matchNumber"] = matchNumber.Value.ToString();
			else
				instance.variables["matchNumber"] = "1";
			instance.turnNumber = 0;
			//Func
			instance.funcByTrigger.Add(TriggerLabel.OnMatchStarted, OnMatchStarted);
			instance.funcByTrigger.Add(TriggerLabel.OnMatchEnded, OnMatchEnded);
			instance.funcByTrigger.Add(TriggerLabel.OnTurnStarted, OnTurnStarted);
			instance.funcByTrigger.Add(TriggerLabel.OnTurnEnded, OnTurnEnded);
			instance.funcByTrigger.Add(TriggerLabel.OnPhaseStarted, OnPhaseStarted);
			instance.funcByTrigger.Add(TriggerLabel.OnPhaseEnded, OnPhaseEnded);
			instance.funcByTrigger.Add(TriggerLabel.OnComponentUsed, OnComponentUsed);
			instance.funcByTrigger.Add(TriggerLabel.OnZoneUsed, OnZoneUsed);
			instance.funcByTrigger.Add(TriggerLabel.OnComponentEnteredZone, OnComponentEnteredZone);
			instance.funcByTrigger.Add(TriggerLabel.OnComponentLeftZone, OnComponentLeftZone);
			instance.funcByTrigger.Add(TriggerLabel.OnMessageSent, OnMessageSent);
			instance.funcByTrigger.Add(TriggerLabel.OnActionUsed, OnActionUsed);
			instance.funcByTrigger.Add(TriggerLabel.OnVariableChanged, OnVariableChanged);
			instance.funcByTrigger.Add(TriggerLabel.OnRuleActivated, OnRuleActivated);
			//Start match loop
			instance.StartCoroutine(instance.MatchLoop());
		}

		public static bool HasVariable (string variableName)
		{
			variableName = ConvertVariableName(variableName);
			return instance.variables.ContainsKey(variableName);
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
			return instance.zones;
		}

		public static List<CGComponent> GetAllComponents ()
		{
			return instance.components;
		}

		public static List<Rule> GetAllRules ()
		{
			return instance.rules;
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
