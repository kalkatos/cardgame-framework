
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Text;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace CardgameFramework
{
	[DefaultExecutionOrder(-20)]
	public class Match : MonoBehaviour
	{
		private static Match instance;

		public static bool IsRunning => instance != null;

		private static bool DebugLog => instance.debugLog;

		[SerializeField] private bool autoStartGame;
		[SerializeField] private Game game;
		[SerializeField] private bool debugLog;
		[SerializeField] private bool useCustomSeed;
		[SerializeField] private int customSeed;
		[SerializeField] private bool logSeeds;
		[SerializeField] private TextAsset seedLog;

		//Match control
		private int cardIDCounter = 1;
		private int zoneIDCounter = 1;
		private int ruleIDCounter = 1;
		private bool endPhase;
		private int matchNumber;
		private int turnNumber;
		private Coroutine matchLoopCoroutine;
		private List<string> phases = new List<string>();
		private List<string> subphases = new List<string>();
		private Queue<Command> commands = new Queue<Command>();
		private HashSet<Hash128> commandHashes = new HashSet<Hash128>();
		private Dictionary<string, string> variables = new Dictionary<string, string>();
		//Match information
		private List<Rule> rules = new List<Rule>();
		private List<Card> cards = new List<Card>();
		private List<Zone> zones = new List<Zone>();
		private Dictionary<string, Card> cardByID = new Dictionary<string, Card>();
		private Dictionary<string, Zone> zoneByID = new Dictionary<string, Zone>();
		private Dictionary<string, Rule> ruleByID = new Dictionary<string, Rule>();
		//Coroutines
		private Dictionary<Delegate, RuleCore> OnMatchStartedCoroutines = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnMatchEndedCoroutines = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnTurnStartedCoroutines = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnTurnEndedCoroutines = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnPhaseStartedCoroutines = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnPhaseEndedCoroutines = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnCardUsedCoroutines = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnZoneUsedCoroutines = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnCardEnteredZoneCoroutines = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnCardLeftZoneCoroutines = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnMessageSentCoroutines = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnActionUsedCoroutines = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnVariableChangedCoroutines = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnRuleActivatedCoroutines = new Dictionary<Delegate, RuleCore>();
		//Listeners
		private Dictionary<Delegate, RuleCore> OnMatchStartedListeners = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnMatchEndedListeners = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnTurnStartedListeners = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnTurnEndedListeners = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnPhaseStartedListeners = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnPhaseEndedListeners = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnCardUsedListeners = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnZoneUsedListeners = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnCardEnteredZoneListeners = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnCardLeftZoneListeners = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnMessageSentListeners = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnActionUsedListeners = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnVariableChangedListeners = new Dictionary<Delegate, RuleCore>();
		private Dictionary<Delegate, RuleCore> OnRuleActivatedListeners = new Dictionary<Delegate, RuleCore>();
		//Execution queues
		private Queue<RuleCore> OnMatchStartedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnMatchEndedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnTurnStartedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnTurnEndedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnPhaseStartedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnPhaseEndedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnCardUsedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnZoneUsedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnCardEnteredZoneActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnCardLeftZoneActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnMessageSentActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnActionUsedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnVariableChangedActiveRules = new Queue<RuleCore>();
		private Queue<RuleCore> OnRuleActivatedActiveRules = new Queue<RuleCore>();
		//Registers
		public static SingleIntRegister OnMatchStarted;
		public static SingleIntRegister OnMatchEnded;
		public static SingleIntRegister OnTurnStarted;
		public static SingleIntRegister OnTurnEnded;
		public static CardEnteredZoneRegister OnCardEnteredZone;
		public static VariableChangedRegister OnVariableChanged;
		//Command queues
		private Queue<StringCommand> availableUseActionCommands = new Queue<StringCommand>();
		private Queue<SingleCardCommand> availableUseCardCommands = new Queue<SingleCardCommand>();
		private Queue<SingleZoneCommand> availableUseZoneCommands = new Queue<SingleZoneCommand>();
		private Queue<SingleZoneCommand> availableOrganizeZoneCommands = new Queue<SingleZoneCommand>();

		private void Awake ()
		{
			if (instance == null)
				instance = this;
			else if (instance != this)
			{
				Destroy(this);
				return;
			}

			if (useCustomSeed)
				Random.InitState(customSeed);
#if UNITY_EDITOR
			else if (logSeeds)
			{
				DateTime now = DateTime.Now;
				int seed = (int)now.Ticks;
				Random.InitState(seed);
				if (seedLog != null)
				{
					string log = seedLog.text;
					string logValue = $"{now.ToString(System.Globalization.CultureInfo.InvariantCulture)} : {seed} {Environment.NewLine}";
					log += logValue;
					File.WriteAllText(AssetDatabase.GetAssetPath(seedLog), log);
					EditorUtility.SetDirty(seedLog);
					CustomDebug.Log(logValue);
				}
			}
#endif

			string[] matchVariables = StringUtility.MatchVariables;
			for (int i = 0; i < matchVariables.Length; i++)
				variables.Add(matchVariables[i], "");

			//Events
			OnMatchStarted = new SingleIntRegister(OnMatchStartedCoroutines, OnMatchStartedListeners, TriggerLabel.OnMatchStarted);
			OnMatchEnded = new SingleIntRegister(OnMatchEndedCoroutines, OnMatchEndedListeners, TriggerLabel.OnMatchEnded);
			OnTurnStarted = new SingleIntRegister(OnTurnStartedCoroutines, OnTurnStartedListeners, TriggerLabel.OnTurnStarted);
			OnTurnEnded = new SingleIntRegister(OnTurnEndedCoroutines, OnTurnEndedListeners, TriggerLabel.OnTurnEnded);
			OnCardEnteredZone = new CardEnteredZoneRegister(OnCardEnteredZoneCoroutines, OnCardEnteredZoneListeners);
			OnVariableChanged = new VariableChangedRegister(OnVariableChangedCoroutines, OnVariableChangedListeners);
		}

		private void Start ()
		{
			if (autoStartGame && game != null)
				StartMatch(game, FindObjectsOfType<Card>(), FindObjectsOfType<Zone>());
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
			foreach (var item in instance.OnMatchStartedListeners)
				if (item.Value.condition.Evaluate())
					((Action<int>)item.Key).Invoke(matchNumber);
			if (GatherMatchStartedTriggers())
				yield return OnMatchStartedTrigger();
			while (true)
			{
				variables["turnNumber"] = (++turnNumber).ToString();
				foreach (var item in instance.OnTurnStartedListeners)
					if (item.Value.condition.Evaluate())
						((Action<int>)item.Key).Invoke(turnNumber);
				if (GatherTurnStartedTriggers())
					yield return OnTurnStartedTrigger();
				for (int i = 0; i < phases.Count; i++)
				{
					variables["phase"] = phases[i];
					if (GatherPhaseStartedTriggers())
						yield return OnPhaseStartedTrigger(phases[i]);
					if (subphases.Count > 0)
					{
						while (subphases.Count > 0)
						{
							for (int j = 0; j < subphases.Count; j++)
							{
								variables["phase"] = subphases[j];
								if (GatherPhaseStartedTriggers())
									yield return OnPhaseStartedTrigger(subphases[j]);
								while (!endPhase)
								{
									if (commands.Count == 0)
										yield return null;
									else
										for (int k = 0; k < commands.Count; k++)
										{
											yield return ExecuteCommand(DequeueCommand(), 0);
											if (subphases.Count == 0)
												break;
										}
									if (subphases.Count == 0)
										break;
								}
								endPhase = false;
								if (GatherPhaseEndedTriggers())
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
									yield return ExecuteCommand(DequeueCommand(), 0);
								}
						}
						endPhase = false;
					}
					variables["phase"] = phases[i];
					if (GatherPhaseEndedTriggers())
						yield return OnPhaseEndedTrigger(phases[i]);
				}
				foreach (var item in instance.OnTurnEndedListeners)
					if (item.Value.condition.Evaluate())
						((Action<int>)item.Key).Invoke(turnNumber);
				if (GatherTurnEndedTriggers())
					yield return OnTurnEndedTrigger();
			}
		}

		#region =========================================================== T R I G G E R S  =======================================================================

		private bool GatherRuleActivatedTriggers ()
		{
			bool found = false;
			foreach (var item in OnRuleActivatedCoroutines)
			{
				bool evaluation = item.Value.EvaluateAndLogCondition();
				found |= evaluation;
				if (evaluation)
					OnRuleActivatedActiveRules.Enqueue(item.Value);
			}
			return found;
		}
		private IEnumerator OnRuleActivatedTrigger (Rule rule)
		{
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
			{
				bool debugShown = false;
				foreach (var item in OnRuleActivatedCoroutines)
				{
					if (!debugShown)
					{
						CustomDebug.Log($"Trigger: On Rule Activated - rule: {rule}", 1);
						debugShown = true;
					}
					bool evaluation = item.Value.condition.BoolValue;
					if (item.Value.parent != null)
						CustomDebug.Log($"Rule: {item.Value.name}", 2);
					else
						CustomDebug.Log($"Callback: {item.Value.name}", 2);
					CustomDebug.Log($"{evaluation} : {item.Value.condition}", 3);
				}
			}
#endif
			while (OnRuleActivatedActiveRules.Count > 0)
				yield return ((Func<Rule, IEnumerator>)OnRuleActivatedActiveRules.Dequeue().callback).Invoke(rule);
		}

		private bool GatherMatchStartedTriggers ()
		{
			bool found = false;
			foreach (var item in OnMatchStartedCoroutines)
			{
				bool evaluation = item.Value.EvaluateAndLogCondition();
				found |= evaluation;
				if (evaluation)
					OnMatchStartedActiveRules.Enqueue(item.Value);
			}
			return found;
		}
		private IEnumerator OnMatchStartedTrigger ()
		{
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
			{
				CustomDebug.Log($"Trigger: On Match Started - matchNumber: {matchNumber}");
				foreach (var item in OnMatchStartedCoroutines)
				{
					bool evaluation = item.Value.condition.BoolValue;
					if (item.Value.parent != null)
					{
						CustomDebug.Log($"Rule: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
						else
							CustomDebug.Log($"{evaluation}", 2);
					}
					else
					{
						CustomDebug.Log($"Callback: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
					}
				}
			}
#endif
			while (OnMatchStartedActiveRules.Count > 0)
			{
				RuleCore ruleCore = OnMatchStartedActiveRules.Dequeue();
				yield return ((Func<int, IEnumerator>)ruleCore.callback).Invoke(matchNumber);
				if (ruleCore.parent != null)
				{
					variables["rule"] = ruleCore.parent.id;
					variables["ruleName"] = ruleCore.parent.name;
					if (GatherRuleActivatedTriggers())
						yield return OnRuleActivatedTrigger(ruleCore.parent);
				}
			}
		}

		private bool GatherMatchEndedTriggers ()
		{
			bool found = false;
			foreach (var item in OnMatchEndedCoroutines)
			{
				bool evaluation = item.Value.EvaluateAndLogCondition();
				found |= evaluation;
				if (evaluation)
					OnMatchEndedActiveRules.Enqueue(item.Value);
			}
			return found;
		}
		private IEnumerator OnMatchEndedTrigger ()
		{
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
			{
				CustomDebug.Log($"Trigger: On Match Ended - matchNumber: {matchNumber}");
				foreach (var item in OnMatchEndedCoroutines)
				{
					bool evaluation = item.Value.condition.BoolValue;
					if (item.Value.parent != null)
					{
						CustomDebug.Log($"Rule: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
						else
							CustomDebug.Log($"{evaluation}", 2);
					}
					else
					{
						CustomDebug.Log($"Callback: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
					}
				}
			}
#endif
			while (OnMatchEndedActiveRules.Count > 0)
			{
				RuleCore ruleCore = OnMatchEndedActiveRules.Dequeue();
				yield return ((Func<int, IEnumerator>)ruleCore.callback).Invoke(matchNumber);
				if (ruleCore.parent != null)
				{
					variables["rule"] = ruleCore.parent.id;
					variables["ruleName"] = ruleCore.parent.name;
					if (GatherRuleActivatedTriggers())
						yield return OnRuleActivatedTrigger(ruleCore.parent);
				}
			}
		}

		private bool GatherTurnStartedTriggers ()
		{
			bool found = false;
			foreach (var item in OnTurnStartedCoroutines)
			{
				bool evaluation = item.Value.EvaluateAndLogCondition();
				found |= evaluation;
				if (evaluation)
					OnTurnStartedActiveRules.Enqueue(item.Value);
			}
			return found;
		}
		private IEnumerator OnTurnStartedTrigger ()
		{
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
			{
				CustomDebug.Log($"Trigger: On Turn Started - turnNumber: {turnNumber}");
				foreach (var item in OnTurnStartedCoroutines)
				{
					bool evaluation = item.Value.condition.BoolValue;
					if (item.Value.parent != null)
					{
						CustomDebug.Log($"Rule: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
						else
							CustomDebug.Log($"{evaluation}", 2);
					}
					else
					{
						CustomDebug.Log($"Callback: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
					}
				}
			}
#endif
			while (OnTurnStartedActiveRules.Count > 0)
			{
				RuleCore ruleCore = OnTurnStartedActiveRules.Dequeue();
				yield return ((Func<int, IEnumerator>)ruleCore.callback).Invoke(turnNumber);
				if (ruleCore.parent != null)
				{
					variables["rule"] = ruleCore.parent.id;
					variables["ruleName"] = ruleCore.parent.name;
					if (GatherRuleActivatedTriggers())
						yield return OnRuleActivatedTrigger(ruleCore.parent);
				}
			}
		}

		private bool GatherTurnEndedTriggers ()
		{
			bool found = false;
			foreach (var item in OnTurnEndedCoroutines)
			{
				bool evaluation = item.Value.EvaluateAndLogCondition();
				found |= evaluation;
				if (evaluation)
					OnTurnEndedActiveRules.Enqueue(item.Value);
			}
			return found;
		}
		private IEnumerator OnTurnEndedTrigger ()
		{
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
			{
				CustomDebug.Log($"Trigger: On Turn Ended - turnNumber: {turnNumber}");
				foreach (var item in OnTurnEndedCoroutines)
				{
					bool evaluation = item.Value.condition.BoolValue;
					if (item.Value.parent != null)
					{
						CustomDebug.Log($"Rule: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
						else
							CustomDebug.Log($"{evaluation}", 2);
					}
					else
					{
						CustomDebug.Log($"Callback: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
					}
				}
			}
#endif
			while (OnTurnEndedActiveRules.Count > 0)
			{
				RuleCore ruleCore = OnTurnEndedActiveRules.Dequeue();
				yield return ((Func<int, IEnumerator>)ruleCore.callback).Invoke(turnNumber);
				if (ruleCore.parent != null)
				{
					variables["rule"] = ruleCore.parent.id;
					variables["ruleName"] = ruleCore.parent.name;
					if (GatherRuleActivatedTriggers())
						yield return OnRuleActivatedTrigger(ruleCore.parent);
				}
			}
		}

		private bool GatherPhaseStartedTriggers ()
		{
			bool found = false;
			foreach (var item in OnPhaseStartedCoroutines)
			{
				bool evaluation = item.Value.EvaluateAndLogCondition();
				found |= evaluation;
				if (evaluation)
					OnPhaseStartedActiveRules.Enqueue(item.Value);
			}
			return found;
		}
		private IEnumerator OnPhaseStartedTrigger (string phase)
		{
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
			{
				CustomDebug.Log($"Trigger: On Phase Started - phase: {phase}");
				foreach (var item in OnPhaseStartedCoroutines)
				{
					bool evaluation = item.Value.condition.BoolValue;
					if (item.Value.parent != null)
					{
						CustomDebug.Log($"Rule: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
						else
							CustomDebug.Log($"{evaluation}", 2);
					}
					else
					{
						CustomDebug.Log($"Callback: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
					}
				}
			}
#endif
			while (OnPhaseStartedActiveRules.Count > 0)
			{
				RuleCore ruleCore = OnPhaseStartedActiveRules.Dequeue();
				yield return ((Func<string, IEnumerator>)ruleCore.callback).Invoke(phase);
				if (ruleCore.parent != null)
				{
					variables["rule"] = ruleCore.parent.id;
					variables["ruleName"] = ruleCore.parent.name;
					if (GatherRuleActivatedTriggers())
						yield return OnRuleActivatedTrigger(ruleCore.parent);
				}
			}
		}

		private bool GatherPhaseEndedTriggers ()
		{
			bool found = false;
			foreach (var item in OnPhaseEndedCoroutines)
			{
				bool evaluation = item.Value.EvaluateAndLogCondition();
				found |= evaluation;
				if (evaluation)
					OnPhaseEndedActiveRules.Enqueue(item.Value);
			}
			return found;
		}
		private IEnumerator OnPhaseEndedTrigger (string phase)
		{
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
			{
				CustomDebug.Log($"Trigger: On Phase Ended - phase: {phase}");
				foreach (var item in OnPhaseEndedCoroutines)
				{
					bool evaluation = item.Value.condition.BoolValue;
					if (item.Value.parent != null)
					{
						CustomDebug.Log($"Rule: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
						else
							CustomDebug.Log($"{evaluation}", 2);
					}
					else
					{
						CustomDebug.Log($"Callback: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
					}
				}
			}
#endif
			while (OnPhaseEndedActiveRules.Count > 0)
			{
				RuleCore ruleCore = OnPhaseEndedActiveRules.Dequeue();
				yield return ((Func<string, IEnumerator>)ruleCore.callback).Invoke(phase);
				if (ruleCore.parent != null)
				{
					variables["rule"] = ruleCore.parent.id;
					variables["ruleName"] = ruleCore.parent.name;
					if (GatherRuleActivatedTriggers())
						yield return OnRuleActivatedTrigger(ruleCore.parent);
				}
			}
		}

		private bool GatherCardUsedTriggers ()
		{
			bool found = false;
			foreach (var item in OnCardUsedCoroutines)
			{
				bool evaluation = item.Value.EvaluateAndLogCondition();
				found |= evaluation;
				if (evaluation)
					OnCardUsedActiveRules.Enqueue(item.Value);
			}
			return found;
		}
		private IEnumerator OnCardUsedTrigger (Card card, string additionalInfo)
		{
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
			{
				CustomDebug.Log($"Trigger: On Card Used - card: {card} - additionalInfo: {StringUtility.CheckEmpty(additionalInfo)}");
				foreach (var item in OnCardUsedCoroutines)
				{
					bool evaluation = item.Value.condition.BoolValue;
					if (item.Value.parent != null)
					{
						CustomDebug.Log($"Rule: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
						else
							CustomDebug.Log($"{evaluation}", 2);
					}
					else
					{
						CustomDebug.Log($"Callback: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
					}
				}
			}
#endif
			while (OnCardUsedActiveRules.Count > 0)
			{
				RuleCore ruleCore = OnCardUsedActiveRules.Dequeue();
				yield return ((Func<Card, string, IEnumerator>)ruleCore.callback).Invoke(card, additionalInfo);
				if (ruleCore.parent != null)
				{
					variables["rule"] = ruleCore.parent.id;
					variables["ruleName"] = ruleCore.parent.name;
					if (GatherRuleActivatedTriggers())
						yield return OnRuleActivatedTrigger(ruleCore.parent);
				}
			}
		}

		private bool GatherZoneUsedTriggers ()
		{
			bool found = false;
			foreach (var item in OnZoneUsedCoroutines)
			{
				bool evaluation = item.Value.EvaluateAndLogCondition();
				found |= evaluation;
				if (evaluation)
					OnZoneUsedActiveRules.Enqueue(item.Value);
			}
			return found;
		}
		private IEnumerator OnZoneUsedTrigger (Zone zone, string additionalInfo)
		{
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
			{
				CustomDebug.Log($"Trigger: On Zone Used - zone: {zone} - additionalInfo: {StringUtility.CheckEmpty(additionalInfo)}");
				foreach (var item in OnZoneUsedCoroutines)
				{
					bool evaluation = item.Value.condition.BoolValue;
					if (item.Value.parent != null)
					{
						CustomDebug.Log($"Rule: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
						else
							CustomDebug.Log($"{evaluation}", 2);
					}
					else
					{
						CustomDebug.Log($"Callback: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
					}
				}
			}
#endif
			while (OnZoneUsedActiveRules.Count > 0)
			{
				RuleCore ruleCore = OnZoneUsedActiveRules.Dequeue();
				yield return ((Func<Zone, string, IEnumerator>)ruleCore.callback).Invoke(zone, additionalInfo);
				if (ruleCore.parent != null)
				{
					variables["rule"] = ruleCore.parent.id;
					variables["ruleName"] = ruleCore.parent.name;
					if (GatherRuleActivatedTriggers())
						yield return OnRuleActivatedTrigger(ruleCore.parent);
				}
			}
		}

		private bool GatherCardEnteredZoneTriggers ()
		{
			bool found = false;
			foreach (var item in OnCardEnteredZoneCoroutines)
			{
				bool evaluation = item.Value.EvaluateAndLogCondition();
				found |= evaluation;
				if (evaluation)
					OnCardEnteredZoneActiveRules.Enqueue(item.Value);
			}
			return found;
		}
		private IEnumerator OnCardEnteredZoneTrigger (Card card, Zone newZone, Zone oldZone, string additionalInfo)
		{
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
			{
				CustomDebug.Log($"Trigger: On Card Entered Zone - card: {card.name} - newZone: {newZone.name} - oldZone: {oldZone.name} - additionalInfo: {StringUtility.CheckEmpty(additionalInfo)}");
				foreach (var item in OnCardEnteredZoneCoroutines)
				{
					bool evaluation = item.Value.condition.BoolValue;
					if (item.Value.parent != null)
					{
						CustomDebug.Log($"Rule: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
						else
							CustomDebug.Log($"{evaluation}", 2);
					}
					else
					{
						CustomDebug.Log($"Callback: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
					}
				}
			}
#endif
			while (OnCardEnteredZoneActiveRules.Count > 0)
			{
				RuleCore ruleCore = OnCardEnteredZoneActiveRules.Dequeue();
				yield return ((Func<Card, Zone, Zone, string, IEnumerator>)ruleCore.callback).Invoke(card, newZone, oldZone, additionalInfo);
				if (ruleCore.parent != null)
				{
					variables["rule"] = ruleCore.parent.id;
					variables["ruleName"] = ruleCore.parent.name;
					if (GatherRuleActivatedTriggers())
						yield return OnRuleActivatedTrigger(ruleCore.parent);
				}
			}
		}

		private bool GatherCardLeftZoneTriggers ()
		{
			bool found = false;
			foreach (var item in OnCardLeftZoneCoroutines)
			{
				bool evaluation = item.Value.EvaluateAndLogCondition();
				found |= evaluation;
				if (evaluation)
					OnCardLeftZoneActiveRules.Enqueue(item.Value);
			}
			return found;
		}
		private IEnumerator OnCardLeftZoneTrigger (Card card, Zone oldZone, string additionalInfo)
		{
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
			{
				CustomDebug.Log($"Trigger: On Card Left Zone - card: {card.name} - oldZone: {oldZone.name} - additionalInfo: {StringUtility.CheckEmpty(additionalInfo)}");
				foreach (var item in OnCardLeftZoneCoroutines)
				{
					bool evaluation = item.Value.condition.BoolValue;
					if (item.Value.parent != null)
					{
						CustomDebug.Log($"Rule: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
						else
							CustomDebug.Log($"{evaluation}", 2);
					}
					else
					{
						CustomDebug.Log($"Callback: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
					}
				}
			}
#endif
			while (OnCardLeftZoneActiveRules.Count > 0)
			{
				RuleCore ruleCore = OnCardLeftZoneActiveRules.Dequeue();
				yield return ((Func<Card, Zone, string, IEnumerator>)ruleCore.callback).Invoke(card, oldZone, additionalInfo);
				if (ruleCore.parent != null)
				{
					variables["rule"] = ruleCore.parent.id;
					variables["ruleName"] = ruleCore.parent.name;
					if (GatherRuleActivatedTriggers())
						yield return OnRuleActivatedTrigger(ruleCore.parent);
				}
			}
		}

		private bool GatherMessageSentTriggers ()
		{
			bool found = false;
			foreach (var item in OnMessageSentCoroutines)
			{
				bool evaluation = item.Value.EvaluateAndLogCondition();
				found |= evaluation;
				if (evaluation)
					OnMessageSentActiveRules.Enqueue(item.Value);
			}
			return found;
		}
		private IEnumerator OnMessageSentTrigger (string message, string additionalInfo)
		{
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
			{
				CustomDebug.Log($"Trigger: On Message Sent - message: {message} - additionalInfo: {StringUtility.CheckEmpty(additionalInfo)}");
				foreach (var item in OnMessageSentCoroutines)
				{
					bool evaluation = item.Value.condition.BoolValue;
					if (item.Value.parent != null)
					{
						CustomDebug.Log($"Rule: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
						else
							CustomDebug.Log($"{evaluation}", 2);
					}
					else
					{
						CustomDebug.Log($"Callback: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
					}
				}
			}
#endif
			while (OnMessageSentActiveRules.Count > 0)
			{
				RuleCore ruleCore = OnMessageSentActiveRules.Dequeue();
				yield return ((Func<string, string, IEnumerator>)ruleCore.callback).Invoke(message, additionalInfo);
				if (ruleCore.parent != null)
				{
					variables["rule"] = ruleCore.parent.id;
					variables["ruleName"] = ruleCore.parent.name;
					if (GatherRuleActivatedTriggers())
						yield return OnRuleActivatedTrigger(ruleCore.parent);
				}
			}
		}

		private bool GatherActionUsedTriggers ()
		{
			bool found = false;
			foreach (var item in OnActionUsedCoroutines)
			{
				bool evaluation = item.Value.EvaluateAndLogCondition();
				found |= evaluation;
				if (evaluation)
					OnActionUsedActiveRules.Enqueue(item.Value);
			}
			return found;
		}
		private IEnumerator OnActionUsedTrigger (string actionName, string additionalInfo)
		{
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
			{
				CustomDebug.Log($"Trigger: On Action Used - actionName: {actionName} - additionalInfo: {StringUtility.CheckEmpty(additionalInfo)}");
				foreach (var item in OnActionUsedCoroutines)
				{
					bool evaluation = item.Value.condition.BoolValue;
					if (item.Value.parent != null)
					{
						CustomDebug.Log($"Rule: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
						else
							CustomDebug.Log($"{evaluation}", 2);
					}
					else
					{
						CustomDebug.Log($"Callback: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
					}
				}
			}
#endif
			while (OnActionUsedActiveRules.Count > 0)
			{
				RuleCore ruleCore = OnActionUsedActiveRules.Dequeue();
				yield return ((Func<string, string, IEnumerator>)ruleCore.callback).Invoke(actionName, additionalInfo);
				if (ruleCore.parent != null)
				{
					variables["rule"] = ruleCore.parent.id;
					variables["ruleName"] = ruleCore.parent.name;
					if (GatherRuleActivatedTriggers())
						yield return OnRuleActivatedTrigger(ruleCore.parent);
				}
			}
		}

		private bool GatherVariableChangedTriggers ()
		{
			bool found = false;
			foreach (var item in OnVariableChangedCoroutines)
			{
				bool evaluation = item.Value.EvaluateAndLogCondition();
				found |= evaluation;
				if (evaluation)
					OnVariableChangedActiveRules.Enqueue(item.Value);
			}
			return found;
		}
		private IEnumerator OnVariableChangedTrigger (string variable, string newValue, string oldValue, string additionalInfo)
		{
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
			{
				CustomDebug.Log($"Trigger: On Variable Changed - variable: {variable} - newValue: {newValue} - oldValue: {oldValue} - additionalInfo: {StringUtility.CheckEmpty(additionalInfo)}");
				foreach (var item in OnVariableChangedCoroutines)
				{
					bool evaluation = item.Value.condition.BoolValue;
					if (item.Value.parent != null)
					{
						CustomDebug.Log($"Rule: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
						else
							CustomDebug.Log($"{evaluation}", 2);
					}
					else
					{
						CustomDebug.Log($"Callback: {item.Value.name}", 1);
						if (item.Value.condition.myString != StringUtility.Empty)
							CustomDebug.Log($"{evaluation} : {item.Value.condition}", 2);
					}
				}
			}
#endif
			while (OnVariableChangedActiveRules.Count > 0)
			{
				RuleCore ruleCore = OnVariableChangedActiveRules.Dequeue();
				yield return ((Func<string, string, string, string, IEnumerator>)ruleCore.callback).Invoke(variable, newValue, oldValue, additionalInfo);
				if (ruleCore.parent != null)
				{
					variables["rule"] = ruleCore.parent.id;
					variables["ruleName"] = ruleCore.parent.name;
					if (GatherRuleActivatedTriggers())
						yield return OnRuleActivatedTrigger(ruleCore.parent);
				}
			}
		}

		#endregion

		#region ==============================================================  C O M M A N D S  =========================================================================

		internal static IEnumerator ExecuteCommand (Command command, int identationLevel)
		{
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("Command: " + StringUtility.CommandNames[(int)command.type]);
				StringCommand stringCommand;
				ZoneCommand zoneCommand;
				switch (command.type)
				{
					case CommandType.UseAction:
					case CommandType.SendMessage:
						stringCommand = (StringCommand)command;
						sb.Append($" ({stringCommand.strParameter}) - AdditionalInfo: {StringUtility.CheckEmpty(stringCommand.additionalInfo)}");
						break;
					case CommandType.StartSubphaseLoop:
						stringCommand = (StringCommand)command;
						sb.Append($" ({stringCommand.strParameter})");
						break;
					case CommandType.UseCard:
						if (command is CardCommand)
						{
							CardCommand cardCommand = (CardCommand)command;
							sb.Append($" ({StringUtility.ListCardSelection(cardCommand.cardSelector, 3)}) - AdditionalInfo: {StringUtility.CheckEmpty(cardCommand.additionalInfo)}");
						}
						else if (command is SingleCardCommand)
						{
							SingleCardCommand singleCardCommand = (SingleCardCommand)command;
							sb.Append($" => {singleCardCommand.card}) - AdditionalInfo: {StringUtility.CheckEmpty(singleCardCommand.additionalInfo)}");
						}
						break;
					case CommandType.OrganizeZone:
					case CommandType.UseZone:
					case CommandType.Shuffle:
						if (command is ZoneCommand)
						{
							zoneCommand = (ZoneCommand)command;
							sb.Append($" ({StringUtility.ListZoneSelection(zoneCommand.zoneSelector, 2)}) - AdditionalInfo: {StringUtility.CheckEmpty(zoneCommand.additionalInfo)}");
						}
						else if (command is SingleZoneCommand)
						{
							SingleZoneCommand singleZoneCommand = (SingleZoneCommand)command;
							sb.Append($" ({singleZoneCommand.zone}) - AdditionalInfo: {StringUtility.CheckEmpty(singleZoneCommand.additionalInfo)}");
						}
						break;
					case CommandType.SetCardFieldValue:
						CardFieldCommand cardFieldCommand = (CardFieldCommand)command;
						sb.Append($" ({StringUtility.ListCardSelection(cardFieldCommand.cardSelector, 1)})");
						sb.Append($" - Field: {cardFieldCommand.fieldName} : {cardFieldCommand.valueGetter.Get()}");
						sb.Append($" - AdditionalInfo: {StringUtility.CheckEmpty(cardFieldCommand.additionalInfo)}");
						break;
					case CommandType.SetVariable:
						VariableCommand varCommand = (VariableCommand)command;
						string variableName = varCommand.variableName;
						string value = varCommand.value.Get().ToString();
						sb.Append($" {variableName} to value {value}");
						sb.Append($" - AdditionalInfo: {StringUtility.CheckEmpty(varCommand.additionalInfo)}");
						break;
					case CommandType.MoveCardToZone:
						CardZoneCommand cardZoneCommand = (CardZoneCommand)command;
						sb.Append(" - " + StringUtility.ListCardSelection(cardZoneCommand.cardSelector, 2));
						sb.Append(" to " + StringUtility.ListZoneSelection(cardZoneCommand.zoneSelector, 2));
						if (cardZoneCommand.additionalInfo != null)
							sb.Append(" - AdditionalInfo: " + StringUtility.CheckEmpty(cardZoneCommand.additionalInfo.ToString()));
						break;
					case CommandType.AddTagToCard:
					case CommandType.RemoveTagFromCard:
						ChangeCardTagCommand cardTagCommand = (ChangeCardTagCommand)command;
						sb.Append(" - " + StringUtility.ListCardSelection(cardTagCommand.cardSelector, 2) + $" Tag: {cardTagCommand.tag}");
						sb.Append($" - AdditionalInfo: {StringUtility.CheckEmpty(cardTagCommand.additionalInfo)}");
						break;
					default:
						break;
				}
				sb.Append($" - (Origin: {command.origin})");
				CustomDebug.Log(sb.ToString(), identationLevel);
			}
#endif
			yield return command.Execute();
			command.callback?.Invoke(command);
		}

		private bool EnqueueCommand (Command command)
		{
			if (commandHashes.Contains(command.hash))
				return false;
			commandHashes.Add(command.hash);
			commands.Enqueue(command);
			return true;
		}

		private Command DequeueCommand ()
		{
			Command command = commands.Dequeue();
			commandHashes.Remove(command.hash);
			return command;
		}

		private static void EnqueueCommands (List<Command> list, string origin)
		{
			for (int i = 0; i < list.Count; i++)
			{
				list[i].origin = origin;
				instance.EnqueueCommand(list[i]);
			}
		}

		internal static IEnumerator ExecuteInitializedCommands (List<Command> commands)
		{
			for (int i = 0; i < commands.Count; i++)
				yield return ExecuteCommand(commands[i], 3);
		}

		internal static IEnumerator EnqueueCommandsCoroutine (List<Command> commands)
		{
			for (int i = 0; i < commands.Count; i++)
				instance.EnqueueCommand(commands[i]);
			yield return null;
		}

		private static IEnumerator EndCurrentPhase ()
		{
			instance.endPhase = true;
			yield return null;
		}

		private static IEnumerator EndTheMatch ()
		{
			instance.StopMatchLoop();
			foreach (var item in instance.OnMatchEndedListeners)
				if (item.Value.condition.Evaluate())
					((Action<int>)item.Key).Invoke(instance.matchNumber);
			if (instance.GatherMatchEndedTriggers())
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
			if (instance.GatherActionUsedTriggers())
				yield return instance.OnActionUsedTrigger(actionName, additionalInfo);
		}

		private static IEnumerator SendMessage (string message, string additionalInfo)
		{
			instance.variables["message"] = message;
			instance.variables["additionalInfo"] = additionalInfo;
			if (instance.GatherMessageSentTriggers())
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

		private static IEnumerator UseCardPrivate (Card card, string additionalInfo)
		{
			instance.variables["usedCard"] = card.id;
			instance.variables["usedCardZone"] = card.Zone ? card.Zone.id : "";
			instance.variables["additionalInfo"] = additionalInfo;
			card.RaiseUsedEvent();
			if (instance.GatherCardUsedTriggers())
				yield return instance.OnCardUsedTrigger(card, additionalInfo);
		}

		private static IEnumerator UseCards (CardSelector cardSelector, string additionalInfo)
		{
			instance.variables["additionalInfo"] = additionalInfo;
			List<Card> cards = (List<Card>)cardSelector.Get();
			for (int i = 0; i < cards.Count; i++)
			{
				yield return UseCardPrivate(cards[i], additionalInfo);
			}
		}

		private static IEnumerator UseZones (ZoneSelector zoneSelector, string additionalInfo)
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
			if (instance.GatherZoneUsedTriggers())
				yield return instance.OnZoneUsedTrigger(zone, additionalInfo);
		}

		private static IEnumerator MoveCardToZone (Card card, Zone zone, MovementAdditionalInfo additionalInfo)
		{
			Zone oldZone = card.Zone;
			if (oldZone)
			{
				instance.variables["oldZone"] = oldZone.id;
				card.RaiseWillLeaveZoneEvent(oldZone);
				oldZone.Pop(card);
				card.RaiseZoneLeftEvent(oldZone);
			}
			else
				instance.variables["oldZone"] = string.Empty;
			instance.variables["movedCard"] = card.id;
			string addInfoStr = additionalInfo.ToString();
			instance.variables["additionalInfo"] = addInfoStr;
			if (instance.GatherCardLeftZoneTriggers())
				yield return instance.OnCardLeftZoneTrigger(card, oldZone, addInfoStr);
			instance.variables["newZone"] = zone.id;
			card.RaiseWillEnterZoneEvent(zone);
			zone.Push(card, additionalInfo);
			card.RaiseEnteredZoneEvent(zone);
			foreach (var item in instance.OnCardEnteredZoneListeners)
				if (item.Value.condition.Evaluate())
					((Action<Card, Zone, Zone, string>)item.Key).Invoke(card, zone, oldZone, addInfoStr);
			if (instance.GatherCardEnteredZoneTriggers())
				yield return instance.OnCardEnteredZoneTrigger(card, zone, oldZone, addInfoStr);
		}

		private static IEnumerator MoveCardToZone (CardSelector cardSelector, ZoneSelector zoneSelector, MovementAdditionalInfo additionalInfo)
		{
			List<Zone> zones = (List<Zone>)zoneSelector.Get();
			List<Zone> oldZones = new List<Zone>();
			for (int i = 0; i < zones.Count; i++)
			{
				Zone zoneToMove = zones[i];
				List<Card> cards = (List<Card>)cardSelector.Get();
				if (additionalInfo.keepOrder)
					for (int j = cards.Count - 1; j >= 0; j--)
					{
						if (cards[j].Zone && !oldZones.Contains(cards[j].Zone))
							oldZones.Add(cards[j].Zone);
						yield return MoveCardToZone(cards[j], zoneToMove, additionalInfo);
					}
				else
					for (int j = 0; j < cards.Count; j++)
					{
						if (cards[j].Zone && !oldZones.Contains(cards[j].Zone))
							oldZones.Add(cards[j].Zone);
						yield return MoveCardToZone(cards[j], zoneToMove, additionalInfo);
					}
				for (int j = 0; j < oldZones.Count; j++)
					oldZones[j].Organize();
				zones[i].Organize();
			}
		}

		private static IEnumerator SetCardFieldValue (CardSelector cardSelector, string fieldName, Getter value, string additionalInfo)
		{
			List<Card> cards = (List<Card>)cardSelector.Get();
			instance.variables["additionalInfo"] = additionalInfo;
			for (int i = 0; i < cards.Count; i++)
			{
				Card card = cards[i];
				string valueString = value.opChar != '\0' ? value.opChar + value.Get().ToString() : value.Get().ToString();
				card.SetFieldValue(fieldName, valueString, additionalInfo);
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
			foreach (var item in instance.OnVariableChangedListeners)
				if (item.Value.condition.Evaluate())
					((Action<string, string, string, string>)item.Key).Invoke(variableName, value, oldValue, additionalInfo);
			if (instance.GatherVariableChangedTriggers())
				yield return instance.OnVariableChangedTrigger(variableName, value, oldValue, additionalInfo);
		}

		private static IEnumerator AddTagToCard (CardSelector cardSelector, string tag, string additionalInfo)
		{
			List<Card> cards = (List<Card>)cardSelector.Get();
			instance.variables["additionalInfo"] = additionalInfo;
			for (int i = 0; i < cards.Count; i++)
			{
				Card card = cards[i];
				card.AddTag(tag);
			}
			yield return null;
		}

		private static IEnumerator RemoveTagFromCard (CardSelector cardSelector, string tag, string additionalInfo)
		{
			List<Card> cards = (List<Card>)cardSelector.Get();
			instance.variables["additionalInfo"] = additionalInfo;
			for (int i = 0; i < cards.Count; i++)
			{
				Card card = cards[i];
				card.RemoveTag(tag);
			}
			yield return null;
		}

		private static IEnumerator OrganizeZonePrivate (Zone zone, string addInfo = "")
		{
			zone.Organize(true);
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

		internal static List<Command> CreateCommands (string clause)
		{
			List<Command> commandSequence = new List<Command>();
			if (string.IsNullOrEmpty(clause))
				return commandSequence;
			string[] commandSequenceClause = clause.Split(';');
			for (int h = 0; h < commandSequenceClause.Length; h++)
			{
				Command newCommand = Command.Build(commandSequenceClause[h]);
				if (newCommand != null)
					commandSequence.Add(newCommand);
			}
			return commandSequence;
		}

		internal static void InitializeCommands (List<Command> commands)
		{
			for (int index = 0; index < commands.Count; index++)
			{
				Command command = commands[index];
				switch (command.type)
				{
					case CommandType.EndCurrentPhase:
						command.Initialize((Func<IEnumerator>)EndCurrentPhase);
						break;
					case CommandType.EndTheMatch:
						command.Initialize((Func<IEnumerator>)EndTheMatch);
						break;
					case CommandType.EndSubphaseLoop:
						command.Initialize((Func<IEnumerator>)EndSubphaseLoop);
						break;
					case CommandType.UseAction:
						command.Initialize((Func<string, string, IEnumerator>)UseActionPrivate);
						break;
					case CommandType.SendMessage:
						command.Initialize((Func<string, string, IEnumerator>)SendMessage);
						break;
					case CommandType.StartSubphaseLoop:
						command.Initialize((Func<string, string, IEnumerator>)StartSubphaseLoop);
						break;
					case CommandType.UseCard:
						command.Initialize((Func<CardSelector, string, IEnumerator>)UseCards, instance.cards);
						break;
					case CommandType.Shuffle:
						command.Initialize((Func<ZoneSelector, string, IEnumerator>)Shuffle, instance.zones);
						break;
					case CommandType.UseZone:
						command.Initialize((Func<ZoneSelector, string, IEnumerator>)UseZones, instance.zones);
						break;
					case CommandType.SetCardFieldValue:
						command.Initialize((Func<CardSelector, string, Getter, string, IEnumerator>)SetCardFieldValue, instance.cards);
						break;
					case CommandType.SetVariable:
						command.Initialize((Func<string, Getter, string, IEnumerator>)SetVariable);
						break;
					case CommandType.MoveCardToZone:
						command.Initialize((Func<CardSelector, ZoneSelector, MovementAdditionalInfo, IEnumerator>)MoveCardToZone, instance.cards, instance.zones);
						break;
					case CommandType.AddTagToCard:
						command.Initialize((Func<CardSelector, string, string, IEnumerator>)AddTagToCard, instance.cards);
						break;
					case CommandType.RemoveTagFromCard:
						command.Initialize((Func<CardSelector, string, string, IEnumerator>)RemoveTagFromCard, instance.cards);
						break;
				}
			}
		}

		private void EnqueueUseActionCommand (Command stringCommand)
		{
			availableUseActionCommands.Enqueue((StringCommand)stringCommand);
		}

		private void EnqueueUseCardCommand (Command stringCommand)
		{
			availableUseCardCommands.Enqueue((SingleCardCommand)stringCommand);
		}

		private void EnqueueOrganizeZoneCommand (Command stringCommand)
		{
			availableOrganizeZoneCommands.Enqueue((SingleZoneCommand)stringCommand);
		}

		private void EnqueueUseZoneCommand (Command stringCommand)
		{
			availableUseZoneCommands.Enqueue((SingleZoneCommand)stringCommand);
		}

		#endregion

		#region =============================================================  C A L L B A C K S  ========================================================================

		private static bool IsValidParameters (ref NestedBooleans condition, Delegate callback, string name)
		{
			if (condition == null)
				condition = new NestedBooleans(true);
			if (callback == null)
			{
				CustomDebug.LogError($"{name} error: Callback cannot be null.");
				return false;
			}
			return true;
		}

		internal static void AddMatchStartedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans(true);
			if (!instance.OnMatchStartedCoroutines.ContainsKey(ruleCore.callback))
				instance.OnMatchStartedCoroutines.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddMatchEndedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans(true);
			if (!instance.OnMatchEndedCoroutines.ContainsKey(ruleCore.callback))
				instance.OnMatchEndedCoroutines.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddTurnStartedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans(true);
			if (!instance.OnTurnStartedCoroutines.ContainsKey(ruleCore.callback))
				instance.OnTurnStartedCoroutines.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddTurnEndedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans(true);
			if (!instance.OnTurnEndedCoroutines.ContainsKey(ruleCore.callback))
				instance.OnTurnEndedCoroutines.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddPhaseStartedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans(true);
			if (!instance.OnPhaseStartedCoroutines.ContainsKey(ruleCore.callback))
				instance.OnPhaseStartedCoroutines.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddPhaseEndedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans(true);
			if (!instance.OnPhaseEndedCoroutines.ContainsKey(ruleCore.callback))
				instance.OnPhaseEndedCoroutines.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddCardUsedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans(true);
			if (!instance.OnCardUsedCoroutines.ContainsKey(ruleCore.callback))
				instance.OnCardUsedCoroutines.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddZoneUsedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans(true);
			if (!instance.OnZoneUsedCoroutines.ContainsKey(ruleCore.callback))
				instance.OnZoneUsedCoroutines.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddCardEnteredZoneCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans(true);
			if (!instance.OnCardEnteredZoneCoroutines.ContainsKey(ruleCore.callback))
				instance.OnCardEnteredZoneCoroutines.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddCardLeftZoneCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans(true);
			if (!instance.OnCardLeftZoneCoroutines.ContainsKey(ruleCore.callback))
				instance.OnCardLeftZoneCoroutines.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddMessageSentCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans(true);
			if (!instance.OnMessageSentCoroutines.ContainsKey(ruleCore.callback))
				instance.OnMessageSentCoroutines.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddActionUsedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans(true);
			if (!instance.OnActionUsedCoroutines.ContainsKey(ruleCore.callback))
				instance.OnActionUsedCoroutines.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddVariableChangedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans(true);
			if (!instance.OnVariableChangedCoroutines.ContainsKey(ruleCore.callback))
				instance.OnVariableChangedCoroutines.Add(ruleCore.callback, ruleCore);
		}
		internal static void AddRuleActivatedCallback (RuleCore ruleCore)
		{
			if (ruleCore.condition == null)
				ruleCore.condition = new NestedBooleans(true);
			if (!instance.OnRuleActivatedCoroutines.ContainsKey(ruleCore.callback))
				instance.OnRuleActivatedCoroutines.Add(ruleCore.callback, ruleCore);
		}

		public static void AddMatchStartedCallback (Func<int, IEnumerator> callback, string name = "Custom Match Started Callback") => AddMatchStartedCallback(null, callback, name);
		public static void AddMatchStartedCallback (NestedBooleans condition, Func<int, IEnumerator> callback, string name = "Custom Match Started Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnMatchStartedCoroutines.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnMatchStarted, condition, callback);
				ruleCore.name = name;
				instance.OnMatchStartedCoroutines.Add(callback, ruleCore);
			}
		}
		public static void RemoveMatchStartedCallback (Func<int, IEnumerator> callback)
		{
			if (instance.OnMatchStartedCoroutines.ContainsKey(callback))
				instance.OnMatchStartedCoroutines.Remove(callback);
		}

		public static void AddMatchEndedCallback (Func<int, IEnumerator> callback, string name = "Custom Match Ended Callback") => AddMatchEndedCallback(null, callback, name);
		public static void AddMatchEndedCallback (NestedBooleans condition, Func<int, IEnumerator> callback, string name = "Custom Match Ended Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnMatchEndedCoroutines.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnMatchEnded, condition, callback);
				ruleCore.name = name;
				instance.OnMatchEndedCoroutines.Add(callback, ruleCore);
			}
		}
		public static void RemoveMatchEndedCallback (Func<int, IEnumerator> callback)
		{
			if (instance.OnMatchEndedCoroutines.ContainsKey(callback))
				instance.OnMatchEndedCoroutines.Remove(callback);
		}

		public static void AddTurnStartedCallback (Func<int, IEnumerator> callback, string name = "Custom Turn Started Callback") => AddTurnStartedCallback(null, callback, name);
		public static void AddTurnStartedCallback (NestedBooleans condition, Func<int, IEnumerator> callback, string name = "Custom Turn Started Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnTurnStartedCoroutines.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnTurnStarted, condition, callback);
				ruleCore.name = name;
				instance.OnTurnStartedCoroutines.Add(callback, new RuleCore(TriggerLabel.OnTurnStarted, condition, callback));
			}
		}
		public static void RemoveTurnStartedCallback (Func<int, IEnumerator> callback)
		{
			if (instance.OnTurnStartedCoroutines.ContainsKey(callback))
				instance.OnTurnStartedCoroutines.Remove(callback);
		}

		public static void AddTurnEndedCallback (Func<int, IEnumerator> callback, string name = "Custom Turn Ended Callback") => AddTurnEndedCallback(null, callback, name);
		public static void AddTurnEndedCallback (NestedBooleans condition, Func<int, IEnumerator> callback, string name = "Custom Turn Ended Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnTurnEndedCoroutines.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnTurnEnded, condition, callback);
				ruleCore.name = name;
				instance.OnTurnEndedCoroutines.Add(callback, ruleCore);
			}
		}
		public static void RemoveTurnEndedCallback (Func<int, IEnumerator> callback)
		{
			if (instance.OnTurnEndedCoroutines.ContainsKey(callback))
				instance.OnTurnEndedCoroutines.Remove(callback);
		}

		public static void AddPhaseStartedCallback (Func<string, IEnumerator> callback, string name = "Custom Phase Started Callback") => AddPhaseStartedCallback(null, callback, name);
		public static void AddPhaseStartedCallback (NestedBooleans condition, Func<string, IEnumerator> callback, string name = "Custom Phase Started Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnPhaseStartedCoroutines.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnPhaseStarted, condition, callback);
				ruleCore.name = name;
				instance.OnPhaseStartedCoroutines.Add(callback, ruleCore);
			}
		}
		public static void RemovePhaseStartedCallback (Func<string, IEnumerator> callback)
		{
			if (instance.OnPhaseStartedCoroutines.ContainsKey(callback))
				instance.OnPhaseStartedCoroutines.Remove(callback);
		}

		public static void AddPhaseEndedCallback (Func<string, IEnumerator> callback, string name = "Custom Phase Ended Callback") => AddPhaseEndedCallback(null, callback, name);
		public static void AddPhaseEndedCallback (NestedBooleans condition, Func<string, IEnumerator> callback, string name = "Custom Phase Ended Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnPhaseEndedCoroutines.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnPhaseEnded, condition, callback);
				ruleCore.name = name;
				instance.OnPhaseEndedCoroutines.Add(callback, ruleCore);
			}
		}
		public static void RemovePhaseEndedCallback (Func<string, IEnumerator> callback)
		{
			if (instance.OnPhaseEndedCoroutines.ContainsKey(callback))
				instance.OnPhaseEndedCoroutines.Remove(callback);
		}

		public static void AddCardUsedCallback (Func<Card, string, IEnumerator> callback, string name = "Custom Card Used Callback") => AddCardUsedCallback(null, callback, name);
		public static void AddCardUsedCallback (NestedBooleans condition, Func<Card, string, IEnumerator> callback, string name = "Custom Card Used Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnCardUsedCoroutines.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnCardUsed, condition, callback);
				ruleCore.name = name;
				instance.OnCardUsedCoroutines.Add(callback, ruleCore);
			}
		}
		public static void RemoveCardUsedCallback (Func<Card, string, IEnumerator> callback)
		{
			if (instance.OnCardUsedCoroutines.ContainsKey(callback))
				instance.OnCardUsedCoroutines.Remove(callback);
		}

		public static void AddZoneUsedCallback (Func<Zone, string, IEnumerator> callback, string name = "Custom Zone Used Callback") => AddZoneUsedCallback(null, callback, name);
		public static void AddZoneUsedCallback (NestedBooleans condition, Func<Zone, string, IEnumerator> callback, string name = "Custom Zone Used Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnZoneUsedCoroutines.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnZoneUsed, condition, callback);
				ruleCore.name = name;
				instance.OnZoneUsedCoroutines.Add(callback, ruleCore);
			}
		}
		public static void RemoveZoneUsedCallback (Func<Zone, string, IEnumerator> callback)
		{
			if (instance.OnZoneUsedCoroutines.ContainsKey(callback))
				instance.OnZoneUsedCoroutines.Remove(callback);
		}

		public static void AddCardEnteredZoneCallback (Func<Card, Zone, Zone, string, IEnumerator> callback, string name = "Custom Card Entered Zone Callback") => AddCardEnteredZoneCallback(null, callback, name);
		public static void AddCardEnteredZoneCallback (NestedBooleans condition, Func<Card, Zone, Zone, string, IEnumerator> callback, string name = "Custom Card Entered Zone Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnCardEnteredZoneCoroutines.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnCardEnteredZone, condition, callback);
				ruleCore.name = name;
				instance.OnCardEnteredZoneCoroutines.Add(callback, ruleCore);
			}
		}
		public static void RemoveCardEnteredZoneCallback (Func<Card, Zone, Zone, string, IEnumerator> callback)
		{
			if (instance.OnCardEnteredZoneCoroutines.ContainsKey(callback))
				instance.OnCardEnteredZoneCoroutines.Remove(callback);
		}

		public static void AddCardLeftZoneCallback (Func<Card, Zone, string, IEnumerator> callback, string name = "Custom Card Left Zone Callback") => AddCardLeftZoneCallback(null, callback, name);
		public static void AddCardLeftZoneCallback (NestedBooleans condition, Func<Card, Zone, string, IEnumerator> callback, string name = "Custom Card Left Zone Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnCardLeftZoneCoroutines.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnCardLeftZone, condition, callback);
				ruleCore.name = name;
				instance.OnCardLeftZoneCoroutines.Add(callback, ruleCore);
			}
		}
		public static void RemoveCardLeftZoneCallback (Func<Card, Zone, string, IEnumerator> callback)
		{
			if (instance.OnCardLeftZoneCoroutines.ContainsKey(callback))
				instance.OnCardLeftZoneCoroutines.Remove(callback);
		}

		public static void AddMessageSentCallback (Func<string, string, IEnumerator> callback, string name = "Custom Message Sent Callback") => AddMessageSentCallback(null, callback, name);
		public static void AddMessageSentCallback (NestedBooleans condition, Func<string, string, IEnumerator> callback, string name = "Custom Message Sent Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnMessageSentCoroutines.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnMessageSent, condition, callback);
				ruleCore.name = name;
				instance.OnMessageSentCoroutines.Add(callback, ruleCore);
			}
		}
		public static void RemoveMessageSentCallback (Func<string, string, IEnumerator> callback)
		{
			if (instance.OnMessageSentCoroutines.ContainsKey(callback))
				instance.OnMessageSentCoroutines.Remove(callback);
		}

		public static void AddActionUsedCallback (Func<string, string, IEnumerator> callback, string name = "Custom Action Used Callback") => AddActionUsedCallback(null, callback, name);
		public static void AddActionUsedCallback (NestedBooleans condition, Func<string, string, IEnumerator> callback, string name = "Custom Action Used Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnActionUsedCoroutines.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnActionUsed, condition, callback);
				ruleCore.name = name;
				instance.OnActionUsedCoroutines.Add(callback, ruleCore);
			}
		}
		public static void RemoveActionUsedCallback (Func<string, string, IEnumerator> callback)
		{
			if (instance.OnActionUsedCoroutines.ContainsKey(callback))
				instance.OnActionUsedCoroutines.Remove(callback);
		}

		public static void AddVariableChangedCallback (Func<string, string, string, string, IEnumerator> callback, string name = "Custom Variable Changed Callback") => AddVariableChangedCallback(null, callback, name);
		public static void AddVariableChangedCallback (NestedBooleans condition, Func<string, string, string, string, IEnumerator> callback, string name = "Custom Variable Changed Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnVariableChangedCoroutines.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnVariableChanged, condition, callback);
				ruleCore.name = name;
				instance.OnVariableChangedCoroutines.Add(callback, ruleCore);
			}
		}
		public static void RemoveVariableChangedCallback (Func<string, string, string, string, IEnumerator> callback)
		{
			if (instance.OnVariableChangedCoroutines.ContainsKey(callback))
				instance.OnVariableChangedCoroutines.Remove(callback);
		}

		public static void AddRuleActivatedCallback (Func<Rule, IEnumerator> callback, string name = "Custom Rule Activated Callback") => AddRuleActivatedCallback(null, callback, name);
		public static void AddRuleActivatedCallback (NestedBooleans condition, Func<Rule, IEnumerator> callback, string name = "Custom Rule Activated Callback")
		{
			if (!IsValidParameters(ref condition, callback, name))
				return;
			if (!instance.OnRuleActivatedCoroutines.ContainsKey(callback))
			{
				RuleCore ruleCore = new RuleCore(TriggerLabel.OnRuleActivated, condition, callback);
				ruleCore.name = name;
				instance.OnRuleActivatedCoroutines.Add(callback, ruleCore);
			}
		}
		public static void RemoveRuleActivatedCallback (Func<Rule, IEnumerator> callback)
		{
			if (instance.OnRuleActivatedCoroutines.ContainsKey(callback))
				instance.OnRuleActivatedCoroutines.Remove(callback);
		}

		#endregion

		#region ===============================================================  P U B L I C  ==========================================================================

		public static void ExecuteCommands (CommandSequence commandSequence, string origin)
		{
			var list = commandSequence.List;
			InitializeCommands(list);
			EnqueueCommands(list, origin);
		}

		public static void UseAction (string actionName, string origin, string additionalInfo = "")
		{
			StringCommand command = null;
			if (instance.availableUseActionCommands.Count > 0)
				command = instance.availableUseActionCommands.Dequeue();
			else
			{
				command = new StringCommand(CommandType.UseAction, UseActionPrivate);
				command.callback = instance.EnqueueUseActionCommand;
			}
			command.Set(actionName, additionalInfo);
			command.origin = origin;
			if (!instance.EnqueueCommand(command))
				instance.availableUseActionCommands.Enqueue(command);
		}

		public static void UseCard (Card card, string origin, string additionalInfo = "")
		{
			SingleCardCommand command = null;
			if (instance.availableUseCardCommands.Count > 0)
				command = instance.availableUseCardCommands.Dequeue();
			else
			{
				command = new SingleCardCommand(UseCardPrivate);
				command.callback = instance.EnqueueUseCardCommand;
			}
			command.Set(card, additionalInfo);
			command.origin = origin;
			if (!instance.EnqueueCommand(command))
				instance.availableUseCardCommands.Enqueue(command);
		}

		public static void UseZone (Zone zone, string origin, string additionalInfo = "")
		{
			SingleZoneCommand command = null;
			if (instance.availableUseZoneCommands.Count > 0)
				command = instance.availableUseZoneCommands.Dequeue();
			else
			{
				command = new SingleZoneCommand(CommandType.UseZone, UseZonePrivate);
				command.callback = instance.EnqueueUseZoneCommand;
			}
			command.Set(zone, additionalInfo);
			command.origin = origin;
			if (!instance.EnqueueCommand(command))
				instance.availableUseZoneCommands.Enqueue(command);
		}

		public static void OrganizeZone (Zone zone, string origin, string additionalInfo = "")
		{
			SingleZoneCommand command = null;
			if (instance.availableOrganizeZoneCommands.Count > 0)
				command = instance.availableOrganizeZoneCommands.Dequeue();
			else
			{
				command = new SingleZoneCommand(CommandType.OrganizeZone, OrganizeZonePrivate);
				command.callback = instance.EnqueueOrganizeZoneCommand;
			}
			command.Set(zone, additionalInfo);
			command.origin = origin;
			if (!instance.EnqueueCommand(command))
				instance.availableOrganizeZoneCommands.Enqueue(command);
		}

		public static void StartMatch ()
		{
			if (instance.game != null)
				StartMatch(instance.game, FindObjectsOfType<Card>(), FindObjectsOfType<Zone>());
			else
				CustomDebug.LogError("Game reference is not set in match inspector.");
		}

		public static void StartMatch (Game game, Card[] cards, Zone[] zones, int? matchNumber = null)
		{
			instance.game = game;
			List<VariableValuePair> gameVars = game.variablesAndValues;
			for (int i = 0; i < gameVars.Count; i++)
			{
				if (instance.variables.ContainsKey(gameVars[i].variable))
				{
					CustomDebug.LogError("Match already has a variable named " + gameVars[i].variable);
					return;
				}
				instance.variables.Add(gameVars[i].variable, gameVars[i].value);
			}
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
				CustomDebug.Log($"Starting game {game.gameName}");
#endif
			StartMatch(game.rules, game.phases, cards, zones, matchNumber);
		}

		public static void StartMatch (List<Rule> gameRules = null, List<string> phases = null, Card[] cards = null, Zone[] zones = null, int? matchNumber = null)
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
					InitializeCommands(rule.commandsList);
					rule.id = "r" + instance.ruleIDCounter++.ToString().PadLeft(4, '0');
					instance.ruleByID.Add(rule.id, rule);
				}
			}
			//Cards
			if (cards != null)
			{
				instance.cards.AddRange(cards);
				for (int i = 0; i < cards.Length; i++)
				{
					Card card = cards[i];
					card.id = "c" + instance.cardIDCounter++.ToString().PadLeft(4, '0');
					//Cards by ID
					instance.cardByID.Add(card.id, card);
					//Rules from cards
					if (card.Rules != null)
						for (int j = 0; j < card.Rules.Count; j++)
						{
							Rule rule = card.Rules[j];
							rule.Initialize();
							InitializeCommands(rule.commandsList);
							rule.id = "r" + instance.ruleIDCounter++.ToString().PadLeft(4, '0');
							rule.origin = card.id;
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
			//Default commands
			for (int index = 0; index < 5; index++)
			{
				StringCommand useActionCommand = new StringCommand(CommandType.UseAction, UseActionPrivate);
				useActionCommand.callback = instance.EnqueueUseActionCommand;
				instance.availableUseActionCommands.Enqueue(useActionCommand);

				SingleCardCommand useCardCommand = new SingleCardCommand(UseCardPrivate);
				useCardCommand.callback = instance.EnqueueUseCardCommand;
				instance.availableUseCardCommands.Enqueue(useCardCommand);

				SingleZoneCommand organizeZoneCommand = new SingleZoneCommand(CommandType.OrganizeZone, OrganizeZonePrivate);
				organizeZoneCommand.callback = instance.EnqueueOrganizeZoneCommand;
				instance.availableOrganizeZoneCommands.Enqueue(organizeZoneCommand);

				SingleZoneCommand useZoneCommand = new SingleZoneCommand(CommandType.UseZone, UseZonePrivate);
				useZoneCommand.callback = instance.EnqueueUseZoneCommand;
				instance.availableUseZoneCommands.Enqueue(useZoneCommand);
			}
			//Match number
			if (matchNumber.HasValue)
				instance.matchNumber = matchNumber.Value;
			else
				instance.matchNumber = 1;
			instance.variables["matchNumber"] = matchNumber.ToString();
			instance.turnNumber = 0;
			//Start match loop
#if UNITY_EDITOR || CARDGAME_DEBUG
			if (DebugLog)
				CustomDebug.Log($"Starting match loop");
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

		public static Card GetCardByID (string id)
		{
			if (instance.cardByID.ContainsKey(id))
				return instance.cardByID[id];
			CustomDebug.LogWarning("Couldn't find card with id " + id);
			return null;
		}

		public static Zone GetZoneByID (string id)
		{
			if (instance.zoneByID.ContainsKey(id))
				return instance.zoneByID[id];
			CustomDebug.LogWarning("Couldn't find zone with id " + id);
			return null;
		}

		public static List<Zone> GetAllZones ()
		{
			if (IsRunning)
				return instance.zones;
			return new List<Zone>();
		}

		public static List<Card> GetAllCards ()
		{
			if (IsRunning)
				return instance.cards;
			return new List<Card>();
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
		OnCardUsed,
		OnZoneUsed,
		OnCardEnteredZone,
		OnCardLeftZone,
		OnMessageSent,
		OnActionUsed,
		OnVariableChanged,
		OnRuleActivated
	}

	public abstract class Register
	{
		internal Dictionary<Delegate, RuleCore> targetCoroutineDictionary;
		internal Dictionary<Delegate, RuleCore> targetListenerDictionary;
		internal TriggerLabel trigger;

		internal Register (Dictionary<Delegate, RuleCore> targetCoroutineDictionary, Dictionary<Delegate, RuleCore> targetListenerDictionary)
		{
			this.targetCoroutineDictionary = targetCoroutineDictionary;
			this.targetListenerDictionary = targetListenerDictionary;
		}

		internal void AddCoroutine (NestedBooleans condition, Delegate coroutine, string origin)
		{
			RuleCore ruleCore = new RuleCore(trigger, condition, coroutine);
			ruleCore.name = origin;
			targetCoroutineDictionary.Add(coroutine, ruleCore);
		}

		internal void AddListener (NestedBooleans condition, Delegate listener, string origin)
		{
			RuleCore ruleCore = new RuleCore(trigger, condition, listener);
			ruleCore.name = origin;
			targetListenerDictionary.Add(listener, ruleCore);
		}
	}

	public class SingleIntRegister : Register
	{
		internal SingleIntRegister (Dictionary<Delegate, RuleCore> targetCoroutineDictionary, Dictionary<Delegate, RuleCore> targetListenerDictionary, TriggerLabel trigger) 
			: base(targetCoroutineDictionary, targetListenerDictionary) { this.trigger = trigger; }

		public void AddCoroutine (NestedBooleans condition, Func<IEnumerator, int> coroutine, string origin = "")
		{
			if (string.IsNullOrEmpty(origin))
				origin = "Custom Coroutine " + StringUtility.TriggerNames[(int)trigger];
			base.AddCoroutine(condition, coroutine, origin);
		}
		public void AddCoroutine (Func<IEnumerator, int> coroutine, string origin = "") => AddCoroutine(new NestedBooleans(true), coroutine, origin);

		public void AddListener (NestedBooleans condition, Action<int> listener, string origin = "")
		{
			if (string.IsNullOrEmpty(origin))
				origin = "Custom Listener " + StringUtility.TriggerNames[(int)trigger];
			base.AddListener(condition, listener, origin);
		}
		public void AddListener (Action<int> listener, string origin = "") => AddListener(new NestedBooleans(true), listener, origin);

		public void RemoveCoroutine (Func<IEnumerator, int> coroutine) => targetCoroutineDictionary.Remove(coroutine);
		public void RemoveListener (Action<int> listener) => targetListenerDictionary.Remove(listener);
	}

	public class CardEnteredZoneRegister : Register
	{
		internal CardEnteredZoneRegister (Dictionary<Delegate, RuleCore> targetCoroutineDictionary, Dictionary<Delegate, RuleCore> targetListenerDictionary)
			: base(targetCoroutineDictionary, targetListenerDictionary) { trigger = TriggerLabel.OnCardEnteredZone; }

		public void AddCoroutine (NestedBooleans condition, Func<IEnumerator, Card, Zone, Zone, string> coroutine, string origin = "")
		{
			if (string.IsNullOrEmpty(origin))
				origin = "Custom Coroutine On Card Entered Zone";
			base.AddCoroutine(condition, coroutine, origin);
		}
		public void AddCoroutine (Func<IEnumerator, IEnumerator, Card, Zone, Zone, string> coroutine, string origin = "") => AddCoroutine(new NestedBooleans(true), coroutine, origin);

		public void AddListener (NestedBooleans condition, Action<Card, Zone, Zone, string> listener, string origin = "")
		{
			if (string.IsNullOrEmpty(origin))
				origin = "Custom Listener On Card Entered Zone";
			base.AddListener(condition, listener, origin);
		}
		public void AddListener (Action<Card, Zone, Zone, string> listener, string origin = "") => AddListener(new NestedBooleans(true), listener, origin);

		public void RemoveCoroutine (Func<IEnumerator, IEnumerator, Card, Zone, Zone, string> coroutine) => targetCoroutineDictionary.Remove(coroutine);
		public void RemoveListener (Action<Card, Zone, Zone, string> listener) => targetListenerDictionary.Remove(listener);
	}

	public class VariableChangedRegister : Register
	{
		internal VariableChangedRegister (Dictionary<Delegate, RuleCore> targetCoroutineDictionary, Dictionary<Delegate, RuleCore> targetListenerDictionary)
			: base(targetCoroutineDictionary, targetListenerDictionary) { trigger = TriggerLabel.OnVariableChanged; }

		public void AddCoroutine (NestedBooleans condition, Func<IEnumerator, string, string, string, string> coroutine, string origin = "")
		{
			if (string.IsNullOrEmpty(origin))
				origin = "Custom Coroutine On Variable Changed";
			base.AddCoroutine(condition, coroutine, origin);
		}
		public void AddCoroutine (Func<IEnumerator, string, string, string, string> coroutine, string origin = "") => AddCoroutine(new NestedBooleans(true), coroutine, origin);

		public void AddListener (NestedBooleans condition, Action<string, string, string, string> listener, string origin = "")
		{
			if (string.IsNullOrEmpty(origin))
				origin = "Custom Listener On Variable Changed";
			base.AddListener(condition, listener, origin);
		}
		public void AddListener (Action<string, string, string, string> listener, string origin = "") => AddListener(new NestedBooleans(true), listener, origin);

		public void RemoveCoroutine (Func<IEnumerator, string, string, string, string> coroutine) => targetCoroutineDictionary.Remove(coroutine);
		public void RemoveListener (Action<string, string, string, string> listener) => targetListenerDictionary.Remove(listener);
	}
}
