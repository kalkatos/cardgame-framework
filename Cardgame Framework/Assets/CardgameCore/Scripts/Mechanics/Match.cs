  
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
    [DefaultExecutionOrder(-20)]
    public class Match : MonoBehaviour
    {
        [Serializable]
        public class MatchData
		{
            public int matchNumber;
            public int turnNumber;
            public string currentPhase;
            public string actionName;
            public string message;
            public string variableName;
            public string variableValue;
            public Component usedComponent;
            public Component movedComponent;
            public Zone newZone;
            public Zone oldZone;
        }

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

        public static MatchData Data { get { return instance.data; } }

        [SerializeField] private bool debugLog;

        //Match data
        private MatchData data = new MatchData();

        //Match control
        private bool endMatch;
        private bool endPhase;
        private List<string> phases = new List<string>();
        private List<string> subphases = new List<string>();
        private Queue<Command> commands = new Queue<Command>();
        private Dictionary<string, string> variables = new Dictionary<string, string>();
        //Match information
        private List<Rule> gameRules = new List<Rule>();
        private List<Component> components = new List<Component>();
        private List<Zone> zones = new List<Zone>();
        private Dictionary<TriggerLabel, List<Rule>> gameRulesByTrigger = new Dictionary<TriggerLabel, List<Rule>>();
        private Dictionary<TriggerLabel, List<Rule>> compRulesByTrigger = new Dictionary<TriggerLabel, List<Rule>>();
        private Dictionary<string, Component> componentByID = new Dictionary<string, Component>();

        private void Awake ()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
            {
                Destroy(this);
                return;
            }

            data = new MatchData();
        }

        // ======================================================================  P R I V A T E  ================================================================================

        private IEnumerator MatchLoop ()
        {
            yield return OnMatchStartedTrigger();
            while (!endMatch)
            {
                data.turnNumber++;
                yield return OnTurnStartedTrigger();
                for (int i = 0; i < phases.Count; i++)
                {
                    if (endMatch)
                        break;
                    data.currentPhase = phases[i];
                    yield return OnPhaseStartedTrigger();
                    if (subphases.Count > 0)
                    {
                        while (subphases.Count > 0)
                        {
                            for (int j = 0; j < subphases.Count; j++)
                            {
                                data.currentPhase = subphases[j];
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
                    data.currentPhase = phases[i];
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
                    if (rules[i].conditionObject.Evaluate())
						for (int j = 0; j < rules[i].trueCommandsList.Count; j++)
                            yield return ExecuteCommand(rules[i].trueCommandsList[j]);
                    else
                        for (int j = 0; j < rules[i].falseCommandsList.Count; j++)
                            yield return ExecuteCommand(rules[i].falseCommandsList[j]);
            }
            if (compRulesByTrigger.ContainsKey(type))
            {
                List<Rule> rules = compRulesByTrigger[type];
                for (int i = 0; i < rules.Count; i++)
                    if (rules[i].conditionObject.Evaluate())
                        for (int j = 0; j < rules[i].trueCommandsList.Count; j++)
                            yield return ExecuteCommand(rules[i].trueCommandsList[j]);
                    else
                        for (int j = 0; j < rules[i].falseCommandsList.Count; j++)
                            yield return ExecuteCommand(rules[i].falseCommandsList[j]);
            }
        }

        private IEnumerator Invoke (Func<IEnumerator> trigger)
		{
            if (trigger != null)
		    	foreach (var func in trigger.GetInvocationList())
                    yield return func.DynamicInvoke();
		}

        private IEnumerator OnMatchStartedTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnMatchStarted - matchNumber = " + data.matchNumber);
            yield return TriggerRules(TriggerLabel.OnMatchStarted);
            yield return Invoke(OnMatchStarted);
        }

        private IEnumerator OnMatchEndedTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnMatchEnded - matchNumber = " + data.matchNumber);
            yield return TriggerRules(TriggerLabel.OnMatchEnded);
            yield return Invoke(OnMatchEnded);
        }

        private IEnumerator OnTurnStartedTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnTurnStarted - turnNumber = " + data.turnNumber);
            yield return TriggerRules(TriggerLabel.OnTurnStarted);
            yield return Invoke(OnTurnStarted);
        }

        private IEnumerator OnTurnEndedTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnTurnEnded - turnNumber = " + data.turnNumber);
            yield return TriggerRules(TriggerLabel.OnTurnEnded);
            yield return Invoke(OnTurnEnded);
        }

        private IEnumerator OnPhaseStartedTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnPhaseStarted - phase = " + data.currentPhase);
            yield return TriggerRules(TriggerLabel.OnPhaseStarted);
            yield return Invoke(OnPhaseStarted);
        }

        private IEnumerator OnPhaseEndedTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnPhaseEnded - phase = " + data.currentPhase);
            yield return TriggerRules(TriggerLabel.OnPhaseEnded);
            yield return Invoke(OnPhaseEnded);
        }

        private IEnumerator OnComponentUsedTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnComponentUsed - component = " + data.usedComponent);
            yield return TriggerRules(TriggerLabel.OnComponentUsed);
            yield return Invoke(OnComponentUsed);
        }

        private IEnumerator OnComponentEnteredZoneTrigger () 
        {
            if (debugLog)
                Debug.Log($"Triggering: OnComponentEnteredZone - {data.movedComponent} - {data.newZone} - {data.oldZone}");
            yield return TriggerRules(TriggerLabel.OnComponentEnteredZone);
            yield return Invoke(OnComponentEnteredZone);
        }

        private IEnumerator OnComponentLeftZoneTrigger () 
        {
            if (debugLog)
                Debug.Log($"Triggering: OnComponentLeftZone - {data.movedComponent} - {data.oldZone}");
            yield return TriggerRules(TriggerLabel.OnComponentLeftZone);
            yield return Invoke(OnComponentLeftZone);
        }

        private IEnumerator OnMessageSentTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnMessageSent - " + data.message);
            yield return TriggerRules(TriggerLabel.OnMessageSent);
            yield return Invoke(OnMessageSent);
        }

        private IEnumerator OnActionUsedTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnActionUsed - " + data.actionName);
            yield return TriggerRules(TriggerLabel.OnActionUsed);
            yield return Invoke(OnActionUsed);
        }

        private IEnumerator OnVariableChangedTrigger () 
        {
            if (debugLog)
                Debug.Log($"Triggering: OnVariableChanged - variable: {data.variableName} - value: {data.variableValue}");
            yield return TriggerRules(TriggerLabel.OnVariableChanged);
            yield return Invoke(OnVariableChanged);
        }

        #endregion

        #region ======================================================================  C O M M A N D S  ================================================================================

        public Command CreateCommand(string clause)
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
                    newCommand = new CardCommand(CommandType.UseCard, UseComponent, new ComponentSelector(clauseBreak[1], components));
                    break;
                case "Shuffle":
                    if (clauseBreak.Length != 2) break;
                    newCommand = new ZoneCommand(CommandType.Shuffle, Shuffle, new ZoneSelector(clauseBreak[1], zones));
                    break;
                case "SetCardFieldValue":
                    if (clauseBreak.Length != 4 && clauseBreak.Length != 6) break;
                    newCommand = new CardFieldCommand(CommandType.SetCardFieldValue, SetComponentFieldValue, new ComponentSelector(clauseBreak[1], components), clauseBreak[2], Getter.Build(clauseBreak[3]));
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
                case "MoveCardToZone":
                    string[] additionalInfo = null;
                    if (clauseBreak.Length > 3)
                    {
                        additionalInfo = new string[clauseBreak.Length - 3];
                        for (int i = 3; i < clauseBreak.Length; i++)
                            additionalInfo[i - 3] = clauseBreak[i];
                    }
                    newCommand = new CardZoneCommand(CommandType.MoveCardToZone, MoveComponentToZone, new ComponentSelector(clauseBreak[1], components), new ZoneSelector(clauseBreak[2], zones), additionalInfo);
                    break;
                case "AddTagToCard":
                    newCommand = new ChangeCardTagCommand(CommandType.AddTagToCard, AddTagToComponent, new ComponentSelector(clauseBreak[1], components), clauseBreak[2]);
                    break;
                case "RemoveTagFromCard":
                    newCommand = new ChangeCardTagCommand(CommandType.AddTagToCard, RemoveTagFromComponent, new ComponentSelector(clauseBreak[1], components), clauseBreak[2]);
                    break;

                default: //=================================================================
                    Debug.LogWarning("[CGEngine] Effect not found: " + clauseBreak[0]);
                    break;
            }

            if (newCommand == null)
                Debug.LogError("[CGEngine] Couldn't build a command with instruction: " + clause);
            return newCommand;
        }

        private IEnumerator ExecuteCommand (Command command)
		{
            if (instance.debugLog)
                Debug.Log($"[CGEngine] Executing command: {command.type}");
            yield return command.Execute();

            /*
       //     List<Zone> selectedZones = null;
       //     List<Component> selectedComponents = null;
       //     if (command != null)
       //     {
       //         switch (command.type)
       //         {
       //             //case CommandType.Empty:
       //             //    if (debugLog)
       //             //        Debug.Log("Executing Command: Empty");
       //             //    yield return null;
       //             //    break;
       //             case CommandType.EndCurrentPhase:
       //                 if (debugLog)
       //                     Debug.Log("Executing Command: EndCurrentPhase");
       //                 yield return EndCurrentPhase();
       //                 break;
       //             case CommandType.EndTheMatch:
       //                 if (debugLog)
       //                     Debug.Log("Executing Command: EndTheMatch");
       //                 yield return EndTheMatch();
       //                 break;
       //             case CommandType.EndSubphaseLoop:
       //                 if (debugLog)
       //                     Debug.Log("Executing Command: EndSubphaseLoop");
       //                 yield return EndSubphaseLoop();
       //                 break;
       //             case CommandType.UseAction:
       //                 if (debugLog)
       //                     Debug.Log($"Executing Command: UseAction (actionName = {command.string1})");
       //                 yield return UseAction(command.string1);
       //                 break;
       //             case CommandType.SendMessage:
       //                 if (debugLog)
       //                     Debug.Log($"Executing Command: SendMessage (message = {command.string1})");
       //                 yield return SendMessage(command.string1);
       //                 break;
       //             case CommandType.StartSubphaseLoop:
       //                 if (debugLog)
       //                     Debug.Log($"Executing Command: StartSubphaseLoop (loop = {command.string1})");
       //                 yield return StartSubphaseLoop(command.string1);
       //                 break;
       //             case CommandType.Shuffle:
       //                 selectedZones = command.zoneSelector.Select(zones);
       //                 if (debugLog)
       //                 {
       //                     string log = "Executing Command: Shuffle (";
	   //					//for (int i = 0; i < selectedZones.Count; i++)
	   //					//{
       //                         if (i >= 3)
       //                         {
       //                             log += $" ... and {selectedZones.Count - 3} other";
       //                             break;
       //                         }
       //                         if (i > 0)
       //                             log += " , ";
       //                         log += selectedZones[i];
	   //					//}
       //                     log += ")";
       //                     Debug.Log(log);
       //                 }
       //                 yield return Shuffle(selectedZones);
       //                 break;
       //             case CommandType.UseComponent:
       //                 selectedComponents = command.componentSelector.Select(components);
       //                 if (debugLog)
       //                 {
       //                     string log = "Executing Command: UseComponent (";
       //                     for (int i = 0; i < selectedComponents.Count; i++)
       //                     {
       //                         if (i >= 3)
	   //   				//	{
       //                             log += $" ... and {selectedComponents.Count - 3} other";
       //                             break;
	   //					//	}
       //                         if (i > 0)
       //                             log += " , ";
       //                         log += selectedComponents[i];
       //                     }
       //                     log += ")";
       //                     Debug.Log(log);
       //                 }
       //                 yield return UseComponent(selectedComponents);
       //                 break;
       //             case CommandType.MoveComponentToZone:
       //                 selectedComponents = command.componentSelector.Select(components);
       //                 selectedZones = command.zoneSelector.Select(zones);
       //                 if (selectedZones.Count >= 1)
       //                 {
       //                     if (debugLog)
       //                     {
       //                         string log = "Executing Command: MoveComponentToZone (";
       //                         for (int i = 0; i < selectedComponents.Count; i++)
       //                         {
       //                             if (i >= 2)
       //                             {
       //                                 log += $" ... and {selectedComponents.Count - 2} other";
       //                                 break;
       //                             }
       //                             if (i > 0)
       //                                 log += " , ";
       //                             log += selectedComponents[i];
       //                         }
       //                         log += $" to {selectedZones[0]})";
       //                         Debug.Log(log);
       //                     }
       //                     yield return MoveComponentToZone(selectedComponents, (Zone)selectedZones[0]);
       //                 }
       //                 else
       //                     yield return null;
       //                 break;
       //             case CommandType.SetComponentFieldValue:
       //                 selectedComponents = command.componentSelector.Select(components);
       //                 if (debugLog)
       //                 {
       //                     string log = "Executing Command: SetComponentFieldValue (";
       //                     for (int i = 0; i < selectedComponents.Count; i++)
       //                     {
       //                         if (i >= 2)
       //                         {
       //                             log += $" ... and {selectedComponents.Count - 2} other";
       //                             break;
       //                         }
       //                         if (i > 0)
       //                             log += " , ";
       //                         log += selectedComponents[i];
       //                     }
       //                     log += $" field = {command.string1} value = {command.string2})";
       //                     Debug.Log(log);
       //                 }
       //                 yield return SetComponentFieldValue(selectedComponents, command.string1, command.string2);
       //                 break;
       //             case CommandType.SetVariable:
       //                 if (debugLog)
       //                     Debug.Log($"Executing Command: SetVariable (variable = {command.string1} value = {command.string2})");
       //                 yield return SetVariable(command.string1, command.string2);
       //                 break;
       //             case CommandType.AddTagToComponent:
       //                 selectedComponents = command.componentSelector.Select(components);
       //                 if (debugLog)
       //                 {
       //                     string log = "Executing Command: AddTagToComponent (";
       //                     for (int i = 0; i < selectedComponents.Count; i++)
       //                     {
       //                         if (i >= 2)
       //                         {
       //                             log += $" ... and {selectedComponents.Count - 2} other";
       //                             break;
       //                         }
       //                         if (i > 0)
       //                             log += " , ";
       //                         log += selectedComponents[i];
       //                     }
       //                     log += $" tag = {command.string1})";
       //                     Debug.Log(log);
       //                 }
       //                 yield return AddTagToComponent(selectedComponents, command.string1);
       //                 break;
       //             case CommandType.RemoveTagFromComponent:
       //                 selectedComponents = command.componentSelector.Select(components);
       //                 if (debugLog)
       //                 {
       //                     string log = "Executing Command: AddTagToComponent (";
       //                     for (int i = 0; i < selectedComponents.Count; i++)
       //                     {
       //                         if (i >= 2)
       //                         {
       //                             log += $" ... and {selectedComponents.Count - 2} other";
       //                             break;
       //                         }
       //                         if (i > 0)
       //                             log += " , ";
       //                         log += selectedComponents[i];
       //                     }
       //                     log += $" tag = {command.string1})";
       //                     Debug.Log(log);
       //                 }
       //                 yield return RemoveTagFromComponent(selectedComponents, command.string1);
       //                 break;
       //         }
       //     }
            */
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

        public static IEnumerator UseAction (string actionName) 
        {
            instance.data.actionName = actionName;
            yield return instance.OnActionUsedTrigger();
        }

        public static new IEnumerator SendMessage (string message)
        {
            instance.data.message = message;
            yield return instance.OnMessageSentTrigger();
        }

        public static IEnumerator StartSubphaseLoop (string phases)
        {
            instance.subphases.AddRange(phases.Split(','));
            yield return null;
        }

        public static IEnumerator Shuffle (ZoneSelector zoneSelector)
		{
            List<Zone> zones = (List<Zone>)zoneSelector.Get();
			for (int i = 0; i < zones.Count; i++)
                zones[i].Shuffle();
            yield return null;
        }
         
        public static IEnumerator UseComponent (ComponentSelector componentSelector)
		{
            List<Component> components = (List<Component>)componentSelector.Get();
            for (int i = 0; i < components.Count; i++)
            {
                instance.data.usedComponent = components[i];
                yield return instance.OnComponentUsedTrigger();
            }
        }

        public static IEnumerator MoveComponentToZone (ComponentSelector componentSelector, ZoneSelector zoneSelector, string[] additionalInfo)
		{
            bool toBottom = false;
            List<Component> components = (List<Component>)componentSelector.Get();
            List<Zone> zones = (List<Zone>)zoneSelector.Get();
            for (int h = 0; h < zones.Count; h++)
            {
                Zone zoneToMove = zones[h];

                for (int i = 0; i < components.Count; i++)
                {
                    Component component = components[i];
                    Zone oldZone = component.zone;
                    instance.data.movedComponent = component;
                    if (oldZone != null)
                    {
                        oldZone.Pop(component);
                        instance.data.oldZone = oldZone;
                        yield return instance.OnComponentLeftZoneTrigger();
                    }
                    RevealStatus revealStatus = RevealStatus.ZoneDefinition;
                    if (additionalInfo != null)
                    {
                        for (int j = 0; j < additionalInfo.Length; j++)
                        {
                            if (additionalInfo[j] == "Hidden")
                            {
                                revealStatus = RevealStatus.Hidden;
                                continue;
                            }
                            else if (additionalInfo[j] == "Revealed")
                            {
                                revealStatus = RevealStatus.RevealedToEveryone;
                                continue;
                            }
                            else if (additionalInfo[j] == "Bottom")
                            {
                                toBottom = true;
                            }
							//else if (additionalInfo[j].StartsWith("("))
							//{
							//	string[] gridPositions = StringUtility.ArgumentsBreakdown(additionalInfo[j]);
							//	if (zoneToMove.zoneConfig == ZoneConfiguration.Grid)
							//	{
							//		if (gridPositions.Length != 2) continue;
							//		Getter left = Getter.Build(gridPositions[0]);
							//		Getter right = Getter.Build(gridPositions[1]);
							//		if (left.Get() is float && right.Get() is float)
							//			gridPos = new Vector2Int((int)left.Get(), (int)right.Get());
							//		else
							//			Debug.LogWarning($"[CGEngine] Something is wrong in grid position with parameter {additionalInfo[j]}");
							//	}
							//	else if (zoneToMove.zoneConfig == ZoneConfiguration.SpecificPositions)
							//	{
							//		if (gridPositions.Length < 1) continue;
							//		Getter pos = Getter.Build(gridPositions[0]);
							//		if (pos.Get() is float)
							//			gridPos = new Vector2Int((int)pos.Get(), 1);
							//		else
							//			Debug.LogWarning($"[CGEngine] An invalid parameter was passed for zone {zoneToMove.name} with parameter {additionalInfo[j]}");
							//	}
							//}
						}
                    }
                    zoneToMove.Push(component, revealStatus, toBottom);
                    instance.data.newZone = zoneToMove;
                    yield return instance.OnComponentEnteredZoneTrigger();

                }
            }
        }

        public static IEnumerator SetComponentFieldValue (ComponentSelector componentSelector, string fieldName, Getter value)
		{
            List<Component> components = (List<Component>)componentSelector.Get();
            for (int i = 0; i < components.Count; i++)
            {
                Component component = components[i];
                component.SetFieldValue(fieldName, (string)value.Get());
                yield return null;
            }
        }

        public static IEnumerator SetVariable (string variableName, Getter valueGetter)
        {
            string value = valueGetter.ToString();
            if (!instance.variables.ContainsKey(variableName))
                instance.variables.Add(variableName, value);
            else
                instance.variables[variableName] = value;
            instance.data.variableName = variableName;
            instance.data.variableValue = value;
            yield return instance.OnVariableChangedTrigger();
        }

        public static IEnumerator AddTagToComponent (ComponentSelector componentSelector, string tag)
        {
            List<Component> components = (List<Component>)componentSelector.Get();
            for (int i = 0; i < components.Count; i++)
            {
                Component component = components[i];
                component.AddTag(tag);
                yield return null;
            }
        }

        public static IEnumerator RemoveTagFromComponent (ComponentSelector componentSelector, string tag)
        {
            List<Component> components = (List<Component>)componentSelector.Get();
            for (int i = 0; i < components.Count; i++)
            {
                Component component = components[i];
                component.RemoveTag(tag);
                yield return null;
            }
        }

        #endregion

		#region ======================================================================  P U B L I C  ================================================================================

		public static void StartMatch (Game game, Component[] components = null, Zone[] zones = null, int? matchNumber = null)
		{
            StartMatch(game.rules, game.phases, components, zones, matchNumber);
		}

        public static void StartMatch (List<Rule> gameRules = null, List<string> phases = null, Component[] components = null, Zone[] zones = null, int? matchNumber = null)
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
                }
            }
            //Components
            if (components != null)
            {
                instance.components.AddRange(components);
                for (int i = 0; i < components.Length; i++)
                {
                    Component comp = (Component)components[i];
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
                        }
                }
            }
            //Zones
            if (zones != null)
                instance.zones.AddRange(zones);
            //Match number
            if (matchNumber.HasValue)
                Data.matchNumber = matchNumber.Value;
            else
                Data.matchNumber = 1;
            Data.turnNumber = 0;
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

        public static Component GetComponentByID (string id)
		{
            if (instance.componentByID.ContainsKey(id))
                return instance.componentByID[id];
            Debug.LogWarning("Couldn't find component id " + id);
            return null;
		}

        public static List<Zone> GetAllZones ()
		{
            return instance.zones;
		}

        public static List<Component> GetAllComponents()
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
        OnVariableChanged
    }
}
