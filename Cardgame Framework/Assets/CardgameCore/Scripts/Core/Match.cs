  
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
        //          public string variableName;
        //          public string variableValue;
        //          public Rule activatedRule;
        //          public CGComponent usedComponent;
        //          public CGComponent movedComponent;
        //          public Zone newZone;
        //          public Zone oldZone;
        //      }

        private static Match instance;

        public static Func<IEnumerator> OnMatchStarted; //matchNumber (int)
        public static Func<IEnumerator> OnMatchEnded; //matchNumber (int)
        public static Func<IEnumerator> OnTurnStarted; //turnNumber (int)
        public static Func<IEnumerator> OnTurnEnded; //turnNumber (int)
        public static Func<IEnumerator> OnPhaseStarted; //phase (string)
        public static Func<IEnumerator> OnPhaseEnded; //phase (string)
        public static Func<IEnumerator> OnComponentUsed; //componentUsed (id)
        public static Func<IEnumerator> OnComponentEnteredZone; //movedComponent (id), newZone (id), oldZone (id)
        public static Func<IEnumerator> OnComponentLeftZone; //movedComponent (id), oldZone (id)
        public static Func<IEnumerator> OnMessageSent; //message
        public static Func<IEnumerator> OnActionUsed; //actionName
        public static Func<IEnumerator> OnVariableChanged; //variable (name), value
        public static Func<IEnumerator> OnRuleActivated;

        //public static MatchData Data { get { return instance.data; } }

        [SerializeField] private bool debugLog;
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
        private List<Rule> gameRules = new List<Rule>();
        private List<CGComponent> components = new List<CGComponent>();
        private List<Zone> zones = new List<Zone>();
        private Dictionary<TriggerLabel, List<Rule>> gameRulesByTrigger = new Dictionary<TriggerLabel, List<Rule>>();
        private Dictionary<TriggerLabel, List<Rule>> compRulesByTrigger = new Dictionary<TriggerLabel, List<Rule>>();
        private Dictionary<string, CGComponent> componentByID = new Dictionary<string, CGComponent>();
        private Dictionary<string, Zone> zoneByID = new Dictionary<string, Zone>();
        private Dictionary<string, Rule> ruleByID = new Dictionary<string, Rule>();

        private void Awake()
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
            variables.Add("variableName", "");
            variables.Add("variableValue", "");
            variables.Add("activatedRule", "");
            variables.Add("usedComponent", "");
            variables.Add("movedComponent", "");
            variables.Add("newZone", "");
            variables.Add("oldZone", "");
        }

        private void Start()
        {
            if (autoStartGame)
                StartMatch(autoStartGame, FindObjectsOfType<CGComponent>(), FindObjectsOfType<Zone>());
        }

        // ======================================================================  P R I V A T E  ================================================================================

        private IEnumerator MatchLoop()
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

        private IEnumerator TriggerRules(TriggerLabel type)
        {
            if (gameRulesByTrigger.ContainsKey(type))
            {
                List<Rule> rules = gameRulesByTrigger[type];
                for (int i = 0; i < rules.Count; i++)
                {
                    if (rules[i].conditionObject.Evaluate())
                    {
                        variables["activatedRule"] = rules[i].id;
                        yield return OnRuleActivatedTrigger();
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
                        variables["activatedRule"] = rules[i].id;
                        yield return OnRuleActivatedTrigger();
                        for (int j = 0; j < rules[i].trueCommandsList.Count; j++)
                            yield return ExecuteCommand(rules[i].trueCommandsList[j]);
                    }
                    else
                        for (int j = 0; j < rules[i].falseCommandsList.Count; j++)
                            yield return ExecuteCommand(rules[i].falseCommandsList[j]);
                }
            }
        }

        private IEnumerator Invoke(Func<IEnumerator> trigger)
        {
            if (trigger != null)
                foreach (var func in trigger.GetInvocationList())
                    yield return func.DynamicInvoke();
        }

        private IEnumerator OnRuleActivatedTrigger()
        {
            if (debugLog)
                Debug.Log("Rule Activated: " + instance.ruleByID[variables["activatedRule"]].name);
            yield return Invoke(OnRuleActivated);
        }

        private IEnumerator OnMatchStartedTrigger()
        {
            if (debugLog)
                Debug.Log("Triggering: OnMatchStarted - matchNumber = " + instance.variables["matchNumber"]);
            yield return TriggerRules(TriggerLabel.OnMatchStarted);
            yield return Invoke(OnMatchStarted);
        }

        private IEnumerator OnMatchEndedTrigger()
        {
            if (debugLog)
                Debug.Log("Triggering: OnMatchEnded - matchNumber = " + instance.variables["matchNumber"]);
            yield return TriggerRules(TriggerLabel.OnMatchEnded);
            yield return Invoke(OnMatchEnded);
        }

        private IEnumerator OnTurnStartedTrigger()
        {
            if (debugLog)
                Debug.Log("Triggering: OnTurnStarted - turnNumber = " + instance.variables["turnNumber"]);
            yield return TriggerRules(TriggerLabel.OnTurnStarted);
            yield return Invoke(OnTurnStarted);
        }

        private IEnumerator OnTurnEndedTrigger()
        {
            if (debugLog)
                Debug.Log("Triggering: OnTurnEnded - turnNumber = " + instance.variables["turnNumber"]);
            yield return TriggerRules(TriggerLabel.OnTurnEnded);
            yield return Invoke(OnTurnEnded);
        }

        private IEnumerator OnPhaseStartedTrigger()
        {
            if (debugLog)
                Debug.Log("Triggering: OnPhaseStarted - phase = " + instance.variables["phase"]);
            yield return TriggerRules(TriggerLabel.OnPhaseStarted);
            yield return Invoke(OnPhaseStarted);
        }

        private IEnumerator OnPhaseEndedTrigger()
        {
            if (debugLog)
                Debug.Log("Triggering: OnPhaseEnded - phase = " + instance.variables["phase"]);
            yield return TriggerRules(TriggerLabel.OnPhaseEnded);
            yield return Invoke(OnPhaseEnded);
        }

        private IEnumerator OnComponentUsedTrigger()
        {
            if (debugLog)
                Debug.Log("Triggering: OnComponentUsed - " + instance.componentByID[instance.variables["usedComponent"]]);
            yield return TriggerRules(TriggerLabel.OnComponentUsed);
            yield return Invoke(OnComponentUsed);
        }

        private IEnumerator OnComponentEnteredZoneTrigger()
        {
            if (debugLog)
                Debug.Log($"Triggering: OnComponentEnteredZone - {instance.componentByID[instance.variables["movedComponent"]]} - {instance.zoneByID[instance.variables["oldZone"]]} - {instance.zoneByID[instance.variables["newZone"]]}");
            yield return TriggerRules(TriggerLabel.OnComponentEnteredZone);
            yield return Invoke(OnComponentEnteredZone);
        }

        private IEnumerator OnComponentLeftZoneTrigger()
        {
            if (debugLog)
                Debug.Log($"Triggering: OnComponentLeftZone - {instance.componentByID[instance.variables["movedComponent"]]} - {instance.zoneByID[instance.variables["oldZone"]]}");
            yield return TriggerRules(TriggerLabel.OnComponentLeftZone);
            yield return Invoke(OnComponentLeftZone);
        }

        private IEnumerator OnMessageSentTrigger()
        {
            if (debugLog)
                Debug.Log("Triggering: OnMessageSent - " + variables["message"]);
            yield return TriggerRules(TriggerLabel.OnMessageSent);
            yield return Invoke(OnMessageSent);
        }

        private IEnumerator OnActionUsedTrigger()
        {
            if (debugLog)
                Debug.Log("Triggering: OnActionUsed - " + variables["actionName"]);
            yield return TriggerRules(TriggerLabel.OnActionUsed);
            yield return Invoke(OnActionUsed);
        }

        private IEnumerator OnVariableChangedTrigger()
        {
            if (debugLog)
                Debug.Log($"Triggering: OnVariableChanged - variable: {variables["variableName"]} - value: {variables["variableValue"]}");
            yield return TriggerRules(TriggerLabel.OnVariableChanged);
            yield return Invoke(OnVariableChanged);
        }

        #endregion

        #region ======================================================================  C O M M A N D S  ================================================================================

        public static Command CreateCommand(string clause)
        {
            Command newCommand = null;
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
                    if (clauseBreak.Length != 2) break;
                    newCommand = new StringCommand(CommandType.UseAction, UseAction, clauseBreak[1]);
                    break;
                case "SendMessage":
                    if (clauseBreak.Length != 2) break;
                    newCommand = new StringCommand(CommandType.SendMessage, SendMessage, clauseBreak[1]);
                    break;
                case "StartSubphaseLoop":
                    if (clauseBreak.Length != 2) break;
                    newCommand = new StringCommand(CommandType.StartSubphaseLoop, StartSubphaseLoop, clauseBreak[1]);
                    break;
                case "UseComponent":
                    if (clauseBreak.Length != 2) break;
                    newCommand = new ComponentCommand(CommandType.UseComponent, UseComponent, new ComponentSelector(clauseBreak[1], instance.components));
                    break;
                case "Shuffle":
                    if (clauseBreak.Length != 2) break;
                    newCommand = new ZoneCommand(CommandType.Shuffle, Shuffle, new ZoneSelector(clauseBreak[1], instance.zones));
                    break;
                case "SetComponentFieldValue":
                    if (clauseBreak.Length != 4 && clauseBreak.Length != 6) break;
                    newCommand = new ComponentFieldCommand(CommandType.SetComponentFieldValue, SetComponentFieldValue, new ComponentSelector(clauseBreak[1], instance.components), clauseBreak[2], Getter.Build(clauseBreak[3]));
                    break;
                case "SetVariable":
                    if (clauseBreak.Length != 3 && clauseBreak.Length != 5) break;
                    char firstVarChar = clauseBreak[2][0];
                    if (firstVarChar == '+' || firstVarChar == '*' || firstVarChar == '/' || firstVarChar == '%' || firstVarChar == '^')
                    {
                        clauseBreak[2] = clauseBreak[1] + clauseBreak[2];
                    }
                    newCommand = new VariableCommand(CommandType.SetVariable, SetVariable, clauseBreak[1], Getter.Build(clauseBreak[2]));
                    break;
                case "MoveComponentToZone":
                    string[] additionalInfo = null;
                    if (clauseBreak.Length > 3)
                    {
                        additionalInfo = new string[clauseBreak.Length - 3];
                        for (int i = 3; i < clauseBreak.Length; i++)
                            additionalInfo[i - 3] = clauseBreak[i];
                    }
                    newCommand = new ComponentZoneCommand(CommandType.MoveComponentToZone, MoveComponentToZone, new ComponentSelector(clauseBreak[1], instance.components), new ZoneSelector(clauseBreak[2], instance.zones), additionalInfo);
                    break;
                case "AddTagToComponent":
                    newCommand = new ChangeComponentTagCommand(CommandType.AddTagToComponent, AddTagToComponent, new ComponentSelector(clauseBreak[1], instance.components), clauseBreak[2]);
                    break;
                case "RemoveTagFromComponent":
                    newCommand = new ChangeComponentTagCommand(CommandType.AddTagToComponent, RemoveTagFromComponent, new ComponentSelector(clauseBreak[1], instance.components), clauseBreak[2]);
                    break;

                default: //=================================================================
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

        public static List<Command> CreateCommands(string clause)
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

        private IEnumerator ExecuteCommand(Command command)
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
                        msg += " => ";
                        if (command is ComponentCommand)
                        {
                            ComponentCommand compCommand = (ComponentCommand)command;
                            List<CGComponent> compList = (List<CGComponent>)compCommand.cardSelector.Get();
							for (int i = 0; i < compList.Count; i++)
							{
                                if (i == 3)
                                {
                                    msg += $" and {compList.Count - 3} more.";
                                    break;
                                }
                                if (i > 0)
                                    msg += ", ";
                                msg += compList[i];
							}
                        }
                        else if (command is SingleComponentCommand)
						{
                            msg += ((SingleComponentCommand)command).component;
						}
						break;
					case CommandType.Shuffle:
                        //TODO finish command debug logs
						break;
					case CommandType.SetComponentFieldValue:
						break;
					case CommandType.SetVariable:
						break;
					case CommandType.MoveComponentToZone:
						break;
					case CommandType.AddTagToComponent:
						break;
					case CommandType.RemoveTagFromComponent:
						break;
					default:
						break;
				}
                Debug.Log(msg);
            }
            yield return command.Execute();
        }

        public static IEnumerator EndCurrentPhase()
        {
            instance.endPhase = true;
            yield return null;
        }

        public static IEnumerator EndTheMatch()
        {
            instance.endMatch = true;
            yield return null;
        }

        public static IEnumerator EndSubphaseLoop()
        {
            instance.subphases.Clear();
            yield return null;
        }

        public static IEnumerator UseAction(string actionName)
        {
            instance.variables["actionName"] = actionName;
            yield return instance.OnActionUsedTrigger();
        }


        public static new IEnumerator SendMessage(string message)
        {
            instance.variables["message"] = message;
            yield return instance.OnMessageSentTrigger();
        }

        public static IEnumerator StartSubphaseLoop(string phases)
        {
            instance.subphases.AddRange(phases.Split(','));
            yield return null;
        }

        public static IEnumerator Shuffle(ZoneSelector zoneSelector)
        {
            List<Zone> zones = (List<Zone>)zoneSelector.Get();
            for (int i = 0; i < zones.Count; i++)
                zones[i].Shuffle();
            yield return null;
        }

        public static IEnumerator UseComponent(CGComponent component)
        {
            instance.variables["usedComponent"] = component.id;
            component.BeUsed();
            yield return instance.OnComponentUsedTrigger();
        }

        public static IEnumerator UseComponent(ComponentSelector componentSelector)
        {
            List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
            for (int i = 0; i < components.Count; i++)
            {
                yield return UseComponent(components[i]);
            }
        }

        public static IEnumerator MoveComponentToZone(CGComponent component, Zone zone, string[] additionalInfo)
        {
            bool toBottom = false;
            if (additionalInfo != null)
            {
                for (int j = 0; j < additionalInfo.Length; j++)
                {
                    if (additionalInfo[j] == "Bottom")
                    {
                        toBottom = true;
                        break;
                    }
                }
            }
            instance.variables["movedComponent"] = component.id;
            zone.Push(component, toBottom);
            instance.variables["newZone"] = zone.id;
            yield return instance.OnComponentEnteredZoneTrigger();
        }

        public static IEnumerator MoveComponentToZone(List<CGComponent> components, Zone zone, string[] additionalInfo)
        {
            bool toBottom = false;
            if (additionalInfo != null)
            {
                for (int j = 0; j < additionalInfo.Length; j++)
                {
                    if (additionalInfo[j] == "Bottom")
                    {
                        toBottom = true;
                        break;
                    }
                }
            }
            for (int i = 0; i < components.Count; i++)
            {
                zone.Push(components[i], toBottom);
                instance.variables["movedComponent"] = components[i].id;
                instance.variables["newZone"] = zone.id;
                yield return instance.OnComponentEnteredZoneTrigger();
            }
        }

        public static IEnumerator MoveComponentToZone(ComponentSelector componentSelector, ZoneSelector zoneSelector, string[] additionalInfo)
        {
            List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
            List<Zone> zones = (List<Zone>)zoneSelector.Get();
            yield return MoveComponentToZone(components, zones, additionalInfo);
        }

        public static IEnumerator MoveComponentToZone(List<CGComponent> components, List<Zone> zones, string[] additionalInfo)
        {
            bool toBottom = false;
            if (additionalInfo != null)
            {
                for (int j = 0; j < additionalInfo.Length; j++)
                {
                    if (additionalInfo[j] == "Bottom")
                    {
                        toBottom = true;
                        break;
                    }
                }
            }
            for (int h = 0; h < zones.Count; h++)
            {
                Zone zoneToMove = zones[h];

                for (int i = 0; i < components.Count; i++)
                {
                    CGComponent component = components[i];
                    Zone oldZone = component.Zone;
                    instance.variables["movedComponent"] = component.id;
                    if (oldZone != null)
                    {
                        oldZone.Pop(component);
                        instance.variables["oldZone"] = oldZone.id;
                        yield return instance.OnComponentLeftZoneTrigger();
                    }
                    zoneToMove.Push(component, toBottom);
                    instance.variables["newZone"] = zoneToMove.id;
                    yield return instance.OnComponentEnteredZoneTrigger();

                }
            }
        }

        public static IEnumerator SetComponentFieldValue(ComponentSelector componentSelector, string fieldName, Getter value)
        {
            List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
            for (int i = 0; i < components.Count; i++)
            {
                CGComponent component = components[i];
                component.SetFieldValue(fieldName, value.Get().ToString());
                yield return null;
            }
        }

        public static IEnumerator SetVariable(string variableName, Getter valueGetter)
        {
            string value = valueGetter.ToString();
            if (!instance.variables.ContainsKey(variableName))
                instance.variables.Add(variableName, value);
            else
                instance.variables[variableName] = value;
            instance.variables["variableName"] = variableName;
            instance.variables["variableValue"] = value;
            yield return instance.OnVariableChangedTrigger();
        }

        public static IEnumerator AddTagToComponent(ComponentSelector componentSelector, string tag)
        {
            List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
            for (int i = 0; i < components.Count; i++)
            {
                CGComponent component = components[i];
                component.AddTag(tag);
                component.SendMessage("OnTagAdded", tag);
                yield return null;
            }
        }

        public static IEnumerator RemoveTagFromComponent(ComponentSelector componentSelector, string tag)
        {
            List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
            for (int i = 0; i < components.Count; i++)
            {
                CGComponent component = components[i];
                component.RemoveTag(tag);
                component.SendMessage("OnTagRemoved", tag);
                yield return null;
            }
        }

        public static void ReceiveAction(string actionName)
        {
            instance.commands.Enqueue(new StringCommand(CommandType.UseAction, UseAction, actionName));
        }

        public static void ReceiveComponentUse(CGComponent component)
        {
            instance.commands.Enqueue(new SingleComponentCommand(UseComponent, component));
        }

        #endregion

        #region ======================================================================  P U B L I C  ================================================================================

        public static void StartMatch(CGComponent[] components, Zone[] zones = null)
        {
            StartMatch(null, null, components, zones);
        }

        public static void StartMatch(Game game, CGComponent[] components, Zone[] zones, int? matchNumber = null)
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

        public static void StartMatch(List<Rule> gameRules = null, List<string> phases = null, CGComponent[] components = null, Zone[] zones = null, int? matchNumber = null)
        {
            if (!instance)
                instance = FindObjectOfType<Match>();
            if (!instance)
                instance = new GameObject("Match").AddComponent<Match>();
            //Main data
            instance.gameRules = gameRules;
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

        public static bool HasVariable(string variableName)
        {
            return instance.variables.ContainsKey(variableName);
        }

        public static string GetVariable(string variableName)
        {
            return instance.variables[variableName];
        }

        public static CGComponent GetComponentByID(string id)
        {
            if (instance.componentByID.ContainsKey(id))
                return instance.componentByID[id];
            Debug.LogWarning("Couldn't find component id " + id);
            return null;
        }

        public static List<Zone> GetAllZones()
        {
            return instance.zones;
        }

        public static List<CGComponent> GetAllComponents()
        {
            return instance.components;
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
        OnComponentEnteredZone,
        OnComponentLeftZone,
        OnMessageSent,
        OnActionUsed,
        OnVariableChanged,
        OnRuleActivated
    }
}
