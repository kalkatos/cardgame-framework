  
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
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
        private List<CardgameObject> components = new List<CardgameObject>();
        private List<CardgameObject> zones = new List<CardgameObject>();
        private Dictionary<TriggerType, List<Rule>> gameRulesByTrigger = new Dictionary<TriggerType, List<Rule>>();
        private Dictionary<TriggerType, List<Rule>> compRulesByTrigger = new Dictionary<TriggerType, List<Rule>>();

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
            OnMatchStarted += OnMatchStartedTrigger;
            OnMatchEnded += OnMatchEndedTrigger;
            OnTurnStarted += OnTurnStartedTrigger;
            OnTurnEnded += OnTurnEndedTrigger;
            OnPhaseStarted += OnPhaseStartedTrigger;
            OnPhaseEnded += OnPhaseEndedTrigger;
            OnComponentUsed += OnComponentUsedTrigger;
            OnComponentEnteredZone += OnComponentEnteredZoneTrigger;
            OnComponentLeftZone += OnComponentLeftZoneTrigger;
            OnMessageSent += OnMessageSentTrigger;
            OnActionUsed += OnActionUsedTrigger;
            OnVariableChanged += OnVariableChangedTrigger;
        }

        // ======================================================================  P R I V A T E  ================================================================================

        private IEnumerator MatchLoop ()
        {
            yield return OnMatchStarted?.Invoke();
            while (!endMatch)
            {
                data.turnNumber++;
                yield return OnTurnStarted?.Invoke();
                for (int i = 0; i < phases.Count; i++)
                {
                    if (endMatch)
                        break;
                    data.currentPhase = phases[i];
                    yield return OnPhaseStarted?.Invoke();
                    if (subphases.Count > 0)
                    {
                        while (subphases.Count > 0)
                        {
                            for (int j = 0; j < subphases.Count; j++)
                            {
                                data.currentPhase = subphases[j];
                                yield return OnPhaseStarted?.Invoke();
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
                                yield return OnPhaseEnded?.Invoke();
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
                    yield return OnPhaseEnded?.Invoke();
                }
                if (endMatch) 
                    break;
                yield return OnTurnEnded?.Invoke();
            }
            yield return OnMatchEnded?.Invoke();
        }

        #region ======================================================================  T R I G G E R S  ================================================================================

        private IEnumerator Trigger (TriggerType type)
        {
            if (gameRulesByTrigger.ContainsKey(type))
            {
                List<Rule> rules = gameRulesByTrigger[type];
                for (int i = 0; i < rules.Count; i++)
                    if (rules[i].condition.Invoke(this))
						for (int j = 0; j < rules[i].trueCommands.Count; j++)
                            yield return ExecuteCommand(rules[i].trueCommands[j]);
                    else
                        for (int j = 0; j < rules[i].falseCommands.Count; j++)
                            yield return ExecuteCommand(rules[i].falseCommands[j]);
            }

            if (compRulesByTrigger.ContainsKey(type))
            {
                List<Rule> rules = compRulesByTrigger[type];
                for (int i = 0; i < rules.Count; i++)
                    if (rules[i].condition.Invoke(this))
                        for (int j = 0; j < rules[i].trueCommands.Count; j++)
                            yield return ExecuteCommand(rules[i].trueCommands[j]);
                    else
                        for (int j = 0; j < rules[i].falseCommands.Count; j++)
                            yield return ExecuteCommand(rules[i].falseCommands[j]);
            }
        }

        private IEnumerator OnMatchStartedTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnMatchStarted - matchNumber = " + data.matchNumber);
            yield return Trigger(TriggerType.OnMatchStarted); 
        }

        private IEnumerator OnMatchEndedTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnMatchEnded - matchNumber = " + data.matchNumber);
            yield return Trigger(TriggerType.OnMatchEnded); 
        }

        private IEnumerator OnTurnStartedTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnTurnStarted - turnNumber = " + data.turnNumber);
            yield return Trigger(TriggerType.OnTurnStarted); 
        }

        private IEnumerator OnTurnEndedTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnTurnEnded - turnNumber = " + data.turnNumber);
            yield return Trigger(TriggerType.OnTurnEnded); 
        }

        private IEnumerator OnPhaseStartedTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnPhaseStarted - phase = " + data.currentPhase);
            yield return Trigger(TriggerType.OnPhaseStarted); 
        }

        private IEnumerator OnPhaseEndedTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnPhaseEnded - phase = " + data.currentPhase);
            yield return Trigger(TriggerType.OnPhaseEnded); 
        }

        private IEnumerator OnComponentUsedTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnComponentUsed - component = " + data.usedComponent);
            yield return Trigger(TriggerType.OnComponentUsed); 
        }

        private IEnumerator OnComponentEnteredZoneTrigger () 
        {
            if (debugLog)
                Debug.Log($"Triggering: OnComponentEnteredZone - {data.movedComponent} - {data.newZone} - {data.oldZone}");
            yield return Trigger(TriggerType.OnComponentEnteredZone); 
        }

        private IEnumerator OnComponentLeftZoneTrigger () 
        {
            if (debugLog)
                Debug.Log($"Triggering: OnComponentLeftZone - {data.movedComponent} - {data.oldZone}");
            yield return Trigger(TriggerType.OnComponentLeftZone); 
        }

        private IEnumerator OnMessageSentTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnMessageSent - " + data.message);
            yield return Trigger(TriggerType.OnMessageSent); 
        }

        private IEnumerator OnActionUsedTrigger () 
        {
            if (debugLog)
                Debug.Log("Triggering: OnActionUsed - " + data.actionName);
            yield return Trigger(TriggerType.OnActionUsed); 
        }

        private IEnumerator OnVariableChangedTrigger () 
        {
            if (debugLog)
                Debug.Log($"Triggering: OnVariableChanged - variable: {data.variableName} - value: {data.variableValue}");
            yield return Trigger(TriggerType.OnVariableChanged); 
        }

		#endregion

		#region ======================================================================  C O M M A N D S  ================================================================================

		private IEnumerator ExecuteCommand (Command command)
		{
            List<CardgameObject> selectedZones = null;
            List<CardgameObject> selectedComponents = null;

            if (command != null)
            {
                switch (command.type)
                {
                    case CommandType.Empty:
                        if (debugLog)
                            Debug.Log("Executing Command: Empty");
                        yield return null;
                        break;
                    case CommandType.EndCurrentPhase:
                        if (debugLog)
                            Debug.Log("Executing Command: EndCurrentPhase");
                        yield return EndCurrentPhase();
                        break;
                    case CommandType.EndTheMatch:
                        if (debugLog)
                            Debug.Log("Executing Command: EndTheMatch");
                        yield return EndTheMatch();
                        break;
                    case CommandType.EndSubphaseLoop:
                        if (debugLog)
                            Debug.Log("Executing Command: EndSubphaseLoop");
                        yield return EndSubphaseLoop();
                        break;
                    case CommandType.UseAction:
                        if (debugLog)
                            Debug.Log($"Executing Command: UseAction (actionName = {command.string1})");
                        yield return UseAction(command.string1);
                        break;
                    case CommandType.SendMessage:
                        if (debugLog)
                            Debug.Log($"Executing Command: SendMessage (message = {command.string1})");
                        yield return SendMessage(command.string1);
                        break;
                    case CommandType.StartSubphaseLoop:
                        if (debugLog)
                            Debug.Log($"Executing Command: StartSubphaseLoop (loop = {command.string1})");
                        yield return StartSubphaseLoop(command.string1);
                        break;
                    case CommandType.Shuffle:
                        selectedZones = command.zoneSelector.Select(zones);
                        if (debugLog)
                        {
                            string log = "Executing Command: Shuffle (";
							for (int i = 0; i < selectedZones.Count; i++)
							{
                                if (i >= 3)
                                {
                                    log += $" ... and {selectedZones.Count - 3} other";
                                    break;
                                }
                                if (i > 0)
                                    log += " , ";
                                log += selectedZones[i];
							}
                            log += ")";
                            Debug.Log(log);
                        }
                        yield return Shuffle(selectedZones);
                        break;
                    case CommandType.UseComponent:
                        selectedComponents = command.componentSelector.Select(components);
                        if (debugLog)
                        {
                            string log = "Executing Command: UseComponent (";
                            for (int i = 0; i < selectedComponents.Count; i++)
                            {
                                if (i >= 3)
								{
                                    log += $" ... and {selectedComponents.Count - 3} other";
                                    break;
								}
                                if (i > 0)
                                    log += " , ";
                                log += selectedComponents[i];
                            }
                            log += ")";
                            Debug.Log(log);
                        }
                        yield return UseComponent(selectedComponents);
                        break;
                    case CommandType.MoveComponentToZone:
                        selectedComponents = command.componentSelector.Select(components);
                        selectedZones = command.zoneSelector.Select(zones);
                        if (selectedZones.Count >= 1)
                        {
                            if (debugLog)
                            {
                                string log = "Executing Command: MoveComponentToZone (";
                                for (int i = 0; i < selectedComponents.Count; i++)
                                {
                                    if (i >= 2)
                                    {
                                        log += $" ... and {selectedComponents.Count - 2} other";
                                        break;
                                    }
                                    if (i > 0)
                                        log += " , ";
                                    log += selectedComponents[i];
                                }
                                log += $" to {selectedZones[0]})";
                                Debug.Log(log);
                            }
                            yield return MoveComponentToZone(selectedComponents, (Zone)selectedZones[0]);
                        }
                        else
                            yield return null;
                        break;
                    case CommandType.SetComponentFieldValue:
                        selectedComponents = command.componentSelector.Select(components);
                        if (debugLog)
                        {
                            string log = "Executing Command: SetComponentFieldValue (";
                            for (int i = 0; i < selectedComponents.Count; i++)
                            {
                                if (i >= 2)
                                {
                                    log += $" ... and {selectedComponents.Count - 2} other";
                                    break;
                                }
                                if (i > 0)
                                    log += " , ";
                                log += selectedComponents[i];
                            }
                            log += $" field = {command.string1} value = {command.string2})";
                            Debug.Log(log);
                        }
                        yield return SetComponentFieldValue(selectedComponents, command.string1, command.string2);
                        break;
                    case CommandType.SetVariable:
                        if (debugLog)
                            Debug.Log($"Executing Command: SetVariable (variable = {command.string1} value = {command.string2})");
                        yield return SetVariable(command.string1, command.string2);
                        break;
                    case CommandType.AddTagToComponent:
                        selectedComponents = command.componentSelector.Select(components);
                        if (debugLog)
                        {
                            string log = "Executing Command: AddTagToComponent (";
                            for (int i = 0; i < selectedComponents.Count; i++)
                            {
                                if (i >= 2)
                                {
                                    log += $" ... and {selectedComponents.Count - 2} other";
                                    break;
                                }
                                if (i > 0)
                                    log += " , ";
                                log += selectedComponents[i];
                            }
                            log += $" tag = {command.string1})";
                            Debug.Log(log);
                        }
                        yield return AddTagToComponent(selectedComponents, command.string1);
                        break;
                    case CommandType.RemoveTagFromComponent:
                        selectedComponents = command.componentSelector.Select(components);
                        if (debugLog)
                        {
                            string log = "Executing Command: AddTagToComponent (";
                            for (int i = 0; i < selectedComponents.Count; i++)
                            {
                                if (i >= 2)
                                {
                                    log += $" ... and {selectedComponents.Count - 2} other";
                                    break;
                                }
                                if (i > 0)
                                    log += " , ";
                                log += selectedComponents[i];
                            }
                            log += $" tag = {command.string1})";
                            Debug.Log(log);
                        }
                        yield return RemoveTagFromComponent(selectedComponents, command.string1);
                        break;
                }
            }
		}

        private IEnumerator EndCurrentPhase ()
        {
            endPhase = true;
            yield return null;
        }

        private IEnumerator EndTheMatch ()
        {
            endMatch = true;
            yield return null;
        }

        private IEnumerator EndSubphaseLoop ()
        {
            subphases.Clear();
            yield return null;
        }

        private IEnumerator UseAction (string actionName) 
        {
            data.actionName = actionName;
            yield return OnActionUsed?.Invoke();
        }

        private new IEnumerator SendMessage (string message)
        {
            data.message = message;
            yield return OnMessageSent?.Invoke();
        }

        private IEnumerator StartSubphaseLoop (string phases)
        {
            instance.subphases.AddRange(phases.Split(','));
            yield return null;
        }

        private IEnumerator Shuffle (List<CardgameObject> zones)
		{
			for (int i = 0; i < zones.Count; i++)
                ((Zone)zones[i]).Shuffle();
            yield return null;
        }
         
        private IEnumerator UseComponent (List<CardgameObject> components)
		{
            for (int i = 0; i < components.Count; i++)
            {
                data.usedComponent = (Component)components[i];
                yield return OnComponentUsed?.Invoke();
            }
        }

        private IEnumerator MoveComponentToZone (List<CardgameObject> components, Zone zone)
		{
			for (int i = 0; i < components.Count; i++)
			{
                Component component = (Component)components[i];
                Zone oldZone = component.zone;
                oldZone.Pop(component);
                data.movedComponent = component;
                data.oldZone = oldZone;
                yield return OnComponentLeftZone?.Invoke();
                zone.Push(component);
                data.newZone = zone;
                yield return OnComponentEnteredZone?.Invoke();
			}
        }

        private IEnumerator SetComponentFieldValue (List<CardgameObject> components, string fieldName, string value)
		{
            for (int i = 0; i < components.Count; i++)
            {
                Component component = (Component)components[i];
                component.SetFieldValue(fieldName, value);
                yield return null;
            }
        }

        private IEnumerator SetVariable (string variableName, string value)
        {
            if (!variables.ContainsKey(variableName))
                variables.Add(variableName, value);
            else
                variables[variableName] = value;
            data.variableName = variableName;
            data.variableValue = value;
            yield return OnVariableChanged?.Invoke();
        }

        private IEnumerator AddTagToComponent (List<CardgameObject> components, string tag)
        {
            for (int i = 0; i < components.Count; i++)
            {
                Component component = (Component)components[i];
                component.AddTag(tag);
                yield return null;
            }
        }

        private IEnumerator RemoveTagFromComponent (List<CardgameObject> components, string tag)
        {
            for (int i = 0; i < components.Count; i++)
            {
                Component component = (Component)components[i];
                component.RemoveTag(tag);
                yield return null;
            }
        }

        #endregion

		#region ======================================================================  P U B L I C  ================================================================================

		public static void StartMatch (Game game, List<CardgameObject> components = null, List<CardgameObject> zones = null, int? matchNumber = null)
		{
            StartMatch(game.rules, game.phases, components, zones, matchNumber);
		}

        public static void StartMatch (List<Rule> gameRules, List<string> phases = null, List<CardgameObject> components = null, List<CardgameObject> zones = null, int? matchNumber = null)
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
            instance.components = components;
            instance.zones = zones;
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
            //Rules from cards
            if (components != null)
            {
                for (int i = 0; i < components.Count; i++)
                {
                    Component comp = (Component)components[i];
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
            //Match number
            if (matchNumber.HasValue)
                Data.matchNumber = matchNumber.Value;
            else
                Data.matchNumber = 1;
            Data.turnNumber = 0;
            //Start match
            instance.StartCoroutine(instance.MatchLoop());
        }

        public static void AddCommand (Command command)
		{
            instance.commands.Enqueue(command);
		}

		#endregion
	}

	[Serializable]
    public class Command
	{
        public CommandType type;
        public Selector componentSelector;
        public Selector zoneSelector;
        public string string1;
        public string string2;

		public Command (CommandType type)
		{
			this.type = type;
		}

		public Command (CommandType type, string string1) : this(type)
		{
			this.string1 = string1;
		}

		public Command (CommandType type, string string1, string string2) : this(type, string1)
		{
			this.string2 = string2;
		}

		public Command (CommandType type, Selector selector) : this(type)
		{
            if (type == CommandType.Shuffle)
                zoneSelector = selector;
            else
                componentSelector = selector;
		}

		public Command (CommandType type, Selector componentSelector, Selector zoneSelector) : this(type, componentSelector)
		{
			this.zoneSelector = zoneSelector;
		}

		public Command (CommandType type, Selector componentSelector, string string1) : this(type, componentSelector)
		{
			this.string1 = string1;
		}

		public Command (CommandType type, Selector componentSelector, string string1, string string2) : this(type, componentSelector, string1)
		{
			this.string2 = string2;
		}
	}

    public enum CommandType
	{
        Empty,
        EndCurrentPhase,
        EndTheMatch,
        EndSubphaseLoop,
        UseAction,
        SendMessage,
        StartSubphaseLoop,
        Shuffle,
        UseComponent,
        MoveComponentToZone,
        SetComponentFieldValue,
        SetVariable,
        AddTagToComponent,
        RemoveTagFromComponent
	}

    public enum TriggerType
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
