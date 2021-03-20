
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
	[DefaultExecutionOrder(-20)]
	public class Match : MonoBehaviour
	{
		//      [Serializable]
		//      public class MatchData
		//{
		//          public int matchNumber;
		//          public int turnNumber;
		//          public string phase;
		//          public string actionName;
		//          public string message;
		//          public string variable;
		//          public string oldValue;
		//          public string newValue;
		//          public Rule rule;
		//          public CGComponent usedComponent;
		//          public CGComponent movedComponent;
		//          public Zone usedZone;
		//          public Zone newZone;
		//          public Zone oldZone;
		//      }

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

		//public static MatchData Data { get { return instance.data; } }
		public static bool DebugLog { get { return instance.debugLog; } }

		public bool debugLog;

		[SerializeField] private Game autoStartGame;

		//Match data
		//private MatchData data = new MatchData();

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

			//data = new MatchData();
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
			yield return OnMatchStartedTrigger();
			while (!endMatch)
			{
				variables["turnNumber"] = (++turnNumber).ToString();
				yield return OnTurnStartedTrigger();
				for (int i = 0; i < phases.Count; i++)
				{
					if (endMatch)
						break;
					variables["phase"] = phases[i];
					yield return OnPhaseStartedTrigger();
					if (subphases.Count > 0)
					{
						while (subphases.Count > 0)
						{
							for (int j = 0; j < subphases.Count; j++)
							{
								variables["phase"] = subphases[j];
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
					yield return OnPhaseEndedTrigger();
				}
				if (endMatch)
					break;
				yield return OnTurnEndedTrigger();
			}
			yield return OnMatchEndedTrigger();
		}

		#region ======================================================================  T R I G G E R S  ================================================================================

		private IEnumerator TriggerRules (TriggerLabel type)
		{
			if (gameRulesByTrigger.ContainsKey(type))
			{
				List<Rule> rules = gameRulesByTrigger[type];
				for (int i = 0; i < rules.Count; i++)
				{
					if (rules[i].conditionObject.Evaluate())
					{
						if (type != TriggerLabel.OnRuleActivated)
						{
							variables["rule"] = rules[i].id;
							yield return OnRuleActivatedTrigger();
						}
						for (int j = 0; j < rules[i].trueCommandsList.Count; j++)
							yield return ExecuteCommand(rules[i].trueCommandsList[j]);
					}
					else
						for (int j = 0; j < rules[i].falseCommandsList.Count; j++)
							yield return ExecuteCommand(rules[i].falseCommandsList[j]);
				}
			}
			if (compRulesByTrigger.ContainsKey(type))
			{
				List<Rule> rules = compRulesByTrigger[type];
				for (int i = 0; i < rules.Count; i++)
				{
					if (rules[i].conditionObject.Evaluate())
					{
						if (type != TriggerLabel.OnRuleActivated)
						{
							variables["rule"] = rules[i].id;
							yield return OnRuleActivatedTrigger();
						}
						for (int j = 0; j < rules[i].trueCommandsList.Count; j++)
							yield return ExecuteCommand(rules[i].trueCommandsList[j]);
					}
					else
						for (int j = 0; j < rules[i].falseCommandsList.Count; j++)
							yield return ExecuteCommand(rules[i].falseCommandsList[j]);
				}
			}
		}

		private IEnumerator Invoke (Func<IEnumerator> trigger)
		{
			if (trigger != null)
				foreach (var func in trigger.GetInvocationList())
					yield return func.DynamicInvoke();
		}

		private IEnumerator OnRuleActivatedTrigger ()
		{
			if (debugLog)
				Debug.Log("Rule Activated: " + instance.ruleByID[variables["rule"]].name);
			yield return TriggerRules(TriggerLabel.OnRuleActivated);
			yield return Invoke(OnRuleActivated);
		}

		private IEnumerator OnMatchStartedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnMatchStarted - matchNumber = " + instance.variables["matchNumber"]);
			yield return TriggerRules(TriggerLabel.OnMatchStarted);
			yield return Invoke(OnMatchStarted);
		}

		private IEnumerator OnMatchEndedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnMatchEnded - matchNumber = " + instance.variables["matchNumber"]);
			yield return TriggerRules(TriggerLabel.OnMatchEnded);
			yield return Invoke(OnMatchEnded);
		}

		private IEnumerator OnTurnStartedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnTurnStarted - turnNumber = " + instance.variables["turnNumber"]);
			yield return TriggerRules(TriggerLabel.OnTurnStarted);
			yield return Invoke(OnTurnStarted);
		}

		private IEnumerator OnTurnEndedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnTurnEnded - turnNumber = " + instance.variables["turnNumber"]);
			yield return TriggerRules(TriggerLabel.OnTurnEnded);
			yield return Invoke(OnTurnEnded);
		}

		private IEnumerator OnPhaseStartedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnPhaseStarted - phase = " + instance.variables["phase"]);
			yield return TriggerRules(TriggerLabel.OnPhaseStarted);
			yield return Invoke(OnPhaseStarted);
		}

		private IEnumerator OnPhaseEndedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnPhaseEnded - phase = " + instance.variables["phase"]);
			yield return TriggerRules(TriggerLabel.OnPhaseEnded);
			yield return Invoke(OnPhaseEnded);
		}

		private IEnumerator OnComponentUsedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnComponentUsed - " + instance.componentByID[instance.variables["usedComponent"]]);
			yield return TriggerRules(TriggerLabel.OnComponentUsed);
			yield return Invoke(OnComponentUsed);
		}

		private IEnumerator OnZoneUsedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnZoneUsed - " + instance.zoneByID[instance.variables["usedZone"]]);
			yield return TriggerRules(TriggerLabel.OnZoneUsed);
			yield return Invoke(OnZoneUsed);
		}

		private IEnumerator OnComponentEnteredZoneTrigger ()
		{
			if (debugLog)
				Debug.Log($"Triggering: OnComponentEnteredZone - {instance.componentByID[instance.variables["movedComponent"]]} - {instance.zoneByID[instance.variables["oldZone"]]} - {instance.zoneByID[instance.variables["newZone"]]}");
			yield return TriggerRules(TriggerLabel.OnComponentEnteredZone);
			yield return Invoke(OnComponentEnteredZone);
		}

		private IEnumerator OnComponentLeftZoneTrigger ()
		{
			if (debugLog)
				Debug.Log($"Triggering: OnComponentLeftZone - {instance.componentByID[instance.variables["movedComponent"]]} - {instance.zoneByID[instance.variables["oldZone"]]}");
			yield return TriggerRules(TriggerLabel.OnComponentLeftZone);
			yield return Invoke(OnComponentLeftZone);
		}

		private IEnumerator OnMessageSentTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnMessageSent - " + variables["message"]);
			yield return TriggerRules(TriggerLabel.OnMessageSent);
			yield return Invoke(OnMessageSent);
		}

		private IEnumerator OnActionUsedTrigger ()
		{
			if (debugLog)
				Debug.Log("Triggering: OnActionUsed - " + variables["actionName"]);
			yield return TriggerRules(TriggerLabel.OnActionUsed);
			yield return Invoke(OnActionUsed);
		}

		private IEnumerator OnVariableChangedTrigger ()
		{
			if (debugLog)
				Debug.Log($"Triggering: OnVariableChanged - variable: {variables["variable"]} - value: {variables["newValue"]}");
			yield return TriggerRules(TriggerLabel.OnVariableChanged);
			yield return Invoke(OnVariableChanged);
		}

		#endregion

		#region ======================================================================  C O M M A N D S  ================================================================================

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
					char firstVarChar = clauseBreak[1][0];
					if (firstVarChar == '+' || firstVarChar == '*' || firstVarChar == '/' || firstVarChar == '%' || firstVarChar == '^')
						clauseBreak[1] = clauseBreak[0] + clauseBreak[1];
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
							msg += ((SingleComponentCommand)command).component;
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
						msg += $" {variableName} Value: {value}";
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

		public static IEnumerator EndCurrentPhase ()
		{
			instance.endPhase = true;
			yield return null;
		}

		public static IEnumerator EndTheMatch ()
		{
			instance.endMatch = true;
			yield return null;
		}

		public static IEnumerator EndSubphaseLoop ()
		{
			instance.subphases.Clear();
			yield return null;
		}

		public static IEnumerator UseAction (string actionName, string additionalInfo)
		{
			instance.variables["actionName"] = actionName;
			instance.variables["additionalInfo"] = additionalInfo;
			yield return instance.OnActionUsedTrigger();
		}


		public static IEnumerator SendMessage (string message, string additionalInfo)
		{
			instance.variables["message"] = message;
			yield return instance.OnMessageSentTrigger();
		}

		public static IEnumerator StartSubphaseLoop (string phases, string additionalInfo)
		{
			instance.subphases.AddRange(phases.Split(','));
			yield return null;
		}

		public static IEnumerator Shuffle (ZoneSelector zoneSelector, string additionalInfo)
		{
			List<Zone> zones = (List<Zone>)zoneSelector.Get();
			for (int i = 0; i < zones.Count; i++)
				zones[i].Shuffle();
			yield return null;
		}

		public static IEnumerator UseComponent (CGComponent component, string additionalInfo)
		{
			instance.variables["usedComponent"] = component.id;
			component.BeUsed();
			yield return instance.OnComponentUsedTrigger();
		}

		public static IEnumerator UseComponent (ComponentSelector componentSelector, string additionalInfo)
		{
			List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
			for (int i = 0; i < components.Count; i++)
			{
				yield return UseComponent(components[i], additionalInfo);
			}
		}

		public static IEnumerator UseZone (ZoneSelector zoneSelector, string additionalInfo)
		{
			List<Zone> zones = (List<Zone>)zoneSelector.Get();
			for (int i = 0; i < zones.Count; i++)
			{
				yield return UseZone(zones[i], additionalInfo);
			}
		}

		public static IEnumerator UseZone (Zone zone, string additionalInfo)
		{
			instance.variables["usedZone"] = zone.id;
			zone.BeUsed();
			yield return instance.OnZoneUsedTrigger();
		}

		public static IEnumerator MoveComponentToZone (CGComponent component, Zone zone, MovementAdditionalInfo additionalInfo)
		{
			Zone oldZone = component.Zone;
			if (oldZone)
			{
				instance.variables["oldZone"] = oldZone.id;
				oldZone.Pop(component);
			}
			else
				instance.variables["oldZone"] = string.Empty;
			instance.variables["movedComponent"] = component.id;
			yield return instance.OnComponentLeftZoneTrigger();
			instance.variables["newZone"] = zone.id;
			zone.Push(component, additionalInfo);
			yield return instance.OnComponentEnteredZoneTrigger();
		}

		public static IEnumerator MoveComponentToZone (List<CGComponent> components, Zone zone, MovementAdditionalInfo additionalInfo)
		{
			for (int i = 0; i < components.Count; i++)
			{
				yield return MoveComponentToZone(components[i], zone, additionalInfo);
			}
		}

		public static IEnumerator MoveComponentToZone (ComponentSelector componentSelector, ZoneSelector zoneSelector, MovementAdditionalInfo additionalInfo)
		{
			List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
			List<Zone> zones = (List<Zone>)zoneSelector.Get();
			yield return MoveComponentToZone(components, zones, additionalInfo);
		}

		public static IEnumerator MoveComponentToZone (List<CGComponent> components, List<Zone> zones, MovementAdditionalInfo additionalInfo)
		{
			for (int h = 0; h < zones.Count; h++)
			{
				Zone zoneToMove = zones[h];
				for (int i = 0; i < components.Count; i++)
					yield return MoveComponentToZone(components[i], zoneToMove, additionalInfo);
			}
		}

		public static IEnumerator SetComponentFieldValue (ComponentSelector componentSelector, string fieldName, Getter value, string additionalInfo)
		{
			List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
			for (int i = 0; i < components.Count; i++)
			{
				CGComponent component = components[i];
				string valueString = value.opChar != '\0' ? value.opChar + value.Get().ToString() : value.Get().ToString();
				component.SetFieldValue(fieldName, valueString, additionalInfo);
				yield return null;
			}
		}

		public static IEnumerator SetVariable (string variableName, Getter valueGetter, string additionalInfo)
		{
			string value = valueGetter.Get().ToString();
			if (!instance.variables.ContainsKey(variableName))
				instance.variables.Add(variableName, value);
			else
				instance.variables[variableName] = value;
			instance.variables["variable"] = variableName;
			instance.variables["oldValue"] = instance.variables["newValue"];
			instance.variables["newValue"] = value;
			yield return instance.OnVariableChangedTrigger();
		}

		public static IEnumerator AddTagToComponent (ComponentSelector componentSelector, string tag, string additionalInfo)
		{
			List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
			for (int i = 0; i < components.Count; i++)
			{
				CGComponent component = components[i];
				component.AddTag(tag);
				yield return null;
			}
		}

		public static IEnumerator RemoveTagFromComponent (ComponentSelector componentSelector, string tag, string additionalInfo)
		{
			List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
			for (int i = 0; i < components.Count; i++)
			{
				CGComponent component = components[i];
				component.RemoveTag(tag);
				yield return null;
			}
		}

		public static void ReceiveAction (string actionName, string additionalInfo = "")
		{
			instance.commands.Enqueue(new StringCommand(CommandType.UseAction, UseAction, actionName, additionalInfo));
		}

		public static void ReceiveComponentUse (CGComponent component, string additionalInfo = "")
		{
			instance.commands.Enqueue(new SingleComponentCommand(UseComponent, component, additionalInfo));
		}

		public static void ReceiveZoneUse (Zone zone, string additionalInfo = "")
		{
			instance.commands.Enqueue(new SingleZoneCommand(UseZone, zone, additionalInfo));
		}

		#endregion

		#region ======================================================================  P U B L I C  ================================================================================

		public static void StartMatch (CGComponent[] components, Zone[] zones = null)
		{
			StartMatch(null, null, components, zones);
		}

		public static void StartMatch (Game game, CGComponent[] components, Zone[] zones, int? matchNumber = null)
		{

			List<string> gameVars = game.variables;
			List<string> gameValues = game.values;
			for (int i = 0; i < gameVars.Count; i++)
			{
				if (instance.variables.ContainsKey(gameVars[i]))
				{
					Debug.LogError("Match already has a variable named " + gameVars[i]);
					return;
				}
				instance.variables.Add(gameVars[i], gameValues[i]);
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
			//Rules from game
			if (gameRules != null)
			{
				for (int i = 0; i < gameRules.Count; i++)
				{
					Rule rule = gameRules[i];
					if (!instance.gameRulesByTrigger.ContainsKey(rule.type))
						instance.gameRulesByTrigger.Add(rule.type, new List<Rule>());
					instance.gameRulesByTrigger[rule.type].Add(rule);
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
							if (!instance.compRulesByTrigger.ContainsKey(rule.type))
								instance.compRulesByTrigger.Add(rule.type, new List<Rule>());
							instance.compRulesByTrigger[rule.type].Add(rule);
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
			//Match number
			if (matchNumber.HasValue)
				instance.variables["matchNumber"] = matchNumber.Value.ToString();
			else
				instance.variables["matchNumber"] = "1";
			instance.turnNumber = 0;
			//Start match loop
			instance.StartCoroutine(instance.MatchLoop());
		}

		public static bool HasVariable (string variableName)
		{
			return instance.variables.ContainsKey(variableName);
		}

		public static string GetVariable (string variableName)
		{
			return instance.variables[variableName];
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
