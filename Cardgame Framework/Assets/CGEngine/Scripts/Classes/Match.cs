using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardGameFramework
{
	public delegate float ExtractNumber (string selection);

	/// <summary>
	/// Holds information about the current match and executes commands upon itself
	/// </summary>
	public class Match : MonoBehaviour
	{
		public static Match Current { get; private set; }

		int cardIdTracker;
		int playerIdTracker;
		int ruleIdTracker;
		int zoneIdTracker;

		public string ID { get; internal set; }
		public int matchNumber { get; internal set; }
		public int turnNumber { get; private set; }
		public string currentTurnPhase { get; private set; }
		public Dictionary<string, object> variables { get; private set; }

		CardGameData game;
		Ruleset ruleset;
		MatchWatcher[] watchers;
		Card[] cards;
		Zone[] zones;
		List<Rule> rules;
		List<Command> commandListToExecute;
		List<SingleCardCommand> useCardCommands;
		List<SingleZoneCommand> useZoneCommands;
		List<StringCommand> useActionCommands;
		List<Command> externalSetCommands;
		Dictionary<TriggerLabel, List<Rule>> triggerWatchers;
		bool isSimulation;
		bool gameEnded;
		bool endCurrentPhase;
		bool endSubphaseLoop;
		List<string> subphases = null;
		List<string> currentTurnPhases;
		Transform ruleContainer;

		#region Initialization Methods ===================================================================================================

		public void Initialize (CardGameData game, Ruleset ruleset)
		{
			Current = this;
			this.game = game;
			this.ruleset = ruleset;
			variables = new Dictionary<string, object>();
			rules = new List<Rule>();
			externalSetCommands = new List<Command>();
			commandListToExecute = new List<Command>();
			//InputManager.Register("ObjectClicked", Current);

			SetupSpecialCommands(3);
			SetupSystemVariables();
			SetupCustomVariables();
			SetupCards();
			SetupZones();
			SetupRules();
			SetupWatchers();
			StartCoroutine(MatchLoop());
		}

		void SetupSpecialCommands (int quantity)
		{
			useCardCommands = new List<SingleCardCommand>();
			for (int i = 0; i < quantity; i++)
				useCardCommands.Add(GetNewSpecialUseCardCommand());
			useZoneCommands = new List<SingleZoneCommand>();
			for (int i = 0; i < quantity; i++)
				useZoneCommands.Add(GetNewSpecialUseZoneCommand());
			useActionCommands = new List<StringCommand>();
			for (int i = 0; i < quantity; i++)
				useActionCommands.Add(GetNewActionStringCommand());
		}

		SingleCardCommand GetNewSpecialUseCardCommand ()
		{
			return new SingleCardCommand(SpecialUseCardCoroutine);
		}

		SingleZoneCommand GetNewSpecialUseZoneCommand ()
		{
			return new SingleZoneCommand(SpecialUseZoneCoroutine);
		}

		StringCommand GetNewActionStringCommand ()
		{
			return new StringCommand(CommandType.UseAction, UseActionCoroutine, "");
		}

		void SetupCustomVariables ()
		{
			if (game.gameVariableNames != null)
			{
				for (int i = 0; i < game.gameVariableNames.Count; i++)
				{
					if (!variables.ContainsKey(game.gameVariableNames[i]))
					{
						if (float.TryParse(game.gameVariableValues[i], out float val))
							variables.Add(game.gameVariableNames[i], val);
						else
							variables.Add(game.gameVariableNames[i], game.gameVariableValues[i]);
					}
				}
			}
			if (ruleset.rulesetVariableNames != null)
			{
				for (int i = 0; i < ruleset.rulesetVariableNames.Count; i++)
				{
					if (!variables.ContainsKey(ruleset.rulesetVariableNames[i]))
					{
						if (float.TryParse(ruleset.rulesetVariableValues[i], out float val))
							variables.Add(ruleset.rulesetVariableNames[i], val);
						else
							variables.Add(ruleset.rulesetVariableNames[i], ruleset.rulesetVariableValues[i]);
					}
					else
					{
						if (float.TryParse(ruleset.rulesetVariableValues[i], out float val))
							variables[ruleset.rulesetVariableNames[i]] = val;
						else
							variables[ruleset.rulesetVariableNames[i]] = ruleset.rulesetVariableValues[i];
					}
				}
			}
		}

		void SetupSystemVariables ()
		{
			//card
			variables.Add("movedCard", "");
			variables.Add("usedCard", "");
			//zone
			variables.Add("targetZone", "");
			variables.Add("oldZone", "");
			variables.Add("usedZone", "");
			//string
			variables.Add("phase", "");
			variables.Add("actionName", "");
			variables.Add("message", "");
			variables.Add("additionalInfo", "");
			variables.Add("variable", "");
			//number
			variables.Add("matchNumber", 0);
			variables.Add("turnNumber", 0);
			variables.Add("value", 0);
			variables.Add("min", float.MinValue);
			variables.Add("max", float.MaxValue);
		}

		void SetupCards ()
		{
			cards = FindObjectsOfType<Card>();
			for (int i = 0; i < cards.Length; i++)
			{
				cards[i].ID = "c" + (++cardIdTracker).ToString().PadLeft(4, '0');

				if (cards[i].Rules != null)
					rules.AddRange(cards[i].Rules);

				if (cards[i].data != null && cards[i].data.cardRules != null)
				{
					foreach (RuleData data in cards[i].data.cardRules)
					{
						cards[i].AddRule(CreateRule(data, cards[i].ID));
					}
				}
			}
		}

		void SetupZones ()
		{
			zones = FindObjectsOfType<Zone>();
			if (zones == null)
			{
				Debug.LogError("[CGEngine] Error: No zones found in scene.");
			}
			else
			{
				for (int i = 0; i < zones.Length; i++)
				{
					zones[i].ID = "z" + (++zoneIdTracker).ToString().PadLeft(2, '0');
				}
			}
		}

		void SetupRules ()
		{
			if (ruleset.matchRules != null)
			{
				//Create all rules
				for (int i = 0; i < ruleset.matchRules.Count; i++)
				{
					CreateRule(ruleset.matchRules[i], ID);
				}

				//Attach rules to labels				
				triggerWatchers = new Dictionary<TriggerLabel, List<Rule>>();
				TriggerLabel[] allLabels = (TriggerLabel[])Enum.GetValues(typeof(TriggerLabel));
				for (int i = 0; i < allLabels.Length; i++)
				{
					triggerWatchers.Add(allLabels[i], new List<Rule>());
					for (int j = 0; j < rules.Count; j++)
					{
						if ((rules[j].activeTriggers & (int)allLabels[i]) != 0)
						{
							triggerWatchers[allLabels[i]].Add(rules[j]);
						}
					}
				}
			}
		}

		void SetupWatchers ()
		{
			watchers = FindObjectsOfType<MatchWatcher>();
			if (watchers == null)
				watchers = new MatchWatcher[0];
		}

		void RegisterTrigger (Rule rule)
		{
			triggerWatchers = new Dictionary<TriggerLabel, List<Rule>>();
			TriggerLabel[] allLabels = (TriggerLabel[])Enum.GetValues(typeof(TriggerLabel));
			for (int i = 0; i < allLabels.Length; i++)
			{
				if ((rule.activeTriggers & (int)allLabels[i]) != 0)
				{
					if (!triggerWatchers.ContainsKey(allLabels[i]))
					{
						List<Rule> newList = new List<Rule>();
						newList.Add(rule);
						triggerWatchers.Add(allLabels[i], newList);
					}
					else
						triggerWatchers[allLabels[i]].Add(rule);
				}
			}
		}

		public Command CreateCommand (string clause)
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
					newCommand = new StringCommand(CommandType.UseAction, UseActionCoroutine, clauseBreak[1]);
					break;
				case "SendMessage":
					if (clauseBreak.Length != 2) break;
					newCommand = new StringCommand(CommandType.SendMessage, SendMessageToWatchers, clauseBreak[1]);
					break;
				case "StartSubphaseLoop":
					if (clauseBreak.Length != 2) break;
					newCommand = new StringCommand(CommandType.StartSubphaseLoop, StartSubphaseLoop, clauseBreak[1]);
					break;
				case "UseCard":
					if (clauseBreak.Length != 2) break;
					newCommand = new CardCommand(CommandType.UseCard, UseCardCoroutine, new CardSelector(clauseBreak[1], cards));
					break;
				case "Shuffle":
					if (clauseBreak.Length != 2) break;
					newCommand = new ZoneCommand(CommandType.Shuffle, ShuffleZones, new ZoneSelector(clauseBreak[1], zones));
					break;
				case "SetCardFieldValue":
					if (clauseBreak.Length != 4 && clauseBreak.Length != 6) break;
					newCommand = new CardFieldCommand(CommandType.SetCardFieldValue, SetCardFieldValue, new CardSelector(clauseBreak[1], cards), clauseBreak[2], Getter.Build(clauseBreak[3]), clauseBreak.Length > 4 ? Getter.Build(clauseBreak[4]) : null, clauseBreak.Length > 5 ? Getter.Build(clauseBreak[5]) : null);
					break;
				case "SetVariable":
					if (clauseBreak.Length != 3 && clauseBreak.Length != 5) break;
					char firstVarChar = clauseBreak[2][0];
					if (firstVarChar == '+' || firstVarChar == '*' || firstVarChar == '/' || firstVarChar == '%' || firstVarChar == '^')
					{
						clauseBreak[2] = clauseBreak[1] + clauseBreak[2];
					}
					newCommand = new VariableCommand(CommandType.SetVariable, SetVariable, clauseBreak[1], Getter.Build(clauseBreak[2]), clauseBreak.Length > 3 ? Getter.Build(clauseBreak[3]) : null, clauseBreak.Length > 4 ? Getter.Build(clauseBreak[4]) : null);
					break;
				case "MoveCardToZone":
					string[] additionalInfo = null;
					if (clauseBreak.Length > 3)
					{
						additionalInfo = new string[clauseBreak.Length - 3];
						for (int i = 3; i < clauseBreak.Length; i++)
							additionalInfo[i - 3] = clauseBreak[i];
					}
					newCommand = new CardZoneCommand(CommandType.MoveCardToZone, MoveCardToZone, new CardSelector(clauseBreak[1], cards), new ZoneSelector(clauseBreak[2], zones), additionalInfo);
					break;
				case "AddTagToCard":
					newCommand = new ChangeCardTagCommand(CommandType.AddTagToCard, ChangeCardTag, new CardSelector(clauseBreak[1], cards), clauseBreak[2], true);
					break;
				case "RemoveTagFromCard":
					newCommand = new ChangeCardTagCommand(CommandType.AddTagToCard, ChangeCardTag, new CardSelector(clauseBreak[1], cards), clauseBreak[2], false);
					break;

				default: //=================================================================
					Debug.LogWarning("[CGEngine] Effect not found: " + clauseBreak[0]);
					break;
			}

			if (newCommand == null)
				Debug.LogError("[CGEngine] Couldn't build a command with instruction: " + clause);
			return newCommand;
		}

		public Rule CreateRule (RuleData data, string originID)
		{
			if (data == null)
				return null;
			if (!ruleContainer)
				ruleContainer = new GameObject("RuleContainer").transform;

			Rule newRule = new GameObject(data.ruleID + "Rule").AddComponent<Rule>();
			newRule.transform.SetParent(ruleContainer);
			newRule.Initialize(data, originID);
			newRule.ID = "m" + (++ruleIdTracker).ToString().PadLeft(4, '0');
			Debug.Log(string.Format("[CGEngine] Created Rule {0} ({1})", data.ruleID, newRule.ID));
			rules.Add(newRule);
			return newRule;
		}

		#endregion

		#region Core Methods ======================================================================================================

		IEnumerator MatchLoop ()
		{
			yield return MatchSetup();
			yield return StartMatch();
			while (!gameEnded)
			{
				yield return StartTurn();
				currentTurnPhases = CreateTurnPhasesFromString(ruleset.turnStructure);
				for (int i = 0; i < currentTurnPhases.Count && !gameEnded; i++)
				{
					yield return StartPhase(currentTurnPhases[i]);
					while (!endCurrentPhase)
					{
						if (subphases != null && subphases.Count > 0)
						{
							while (!endSubphaseLoop)
							{
								for (int j = 0; j < subphases.Count && !endSubphaseLoop; j++)
								{
									endCurrentPhase = false;
									yield return StartPhase(subphases[j]);
									while (!endCurrentPhase && !endSubphaseLoop && !gameEnded)
									{
										if (externalSetCommands.Count > 0)
										{
											yield return ExecuteCommands(externalSetCommands.ToArray());
											externalSetCommands.Clear();
										}
										else
											yield return null;
									}
									yield return EndPhase(subphases[j]);
									if (endSubphaseLoop || gameEnded) break;
								}
							}
							subphases = null;
						}
						else
						{
							if (externalSetCommands.Count > 0)
							{
								yield return ExecuteCommands(externalSetCommands.ToArray());
								externalSetCommands.Clear();
							}
							else
								yield return null;
						}
					}
					yield return EndPhase(currentTurnPhases[i]);
				}
				yield return EndTurn();
			}
			yield return EndMatch();
		}

		IEnumerator NotifyRules(TriggerLabel label)
		{
			List<Rule> rules = triggerWatchers[label];
			for (int i = 0; i < rules.Count; i++)
			{
				Rule rule = rules[i];
				bool condition = rule.conditions.Evaluate();
				if (condition)
				{
					Debug.Log($"[CGEngine] TRIGGER: {label} from {rule.name}");
					for (int j = 0; j < rule.commands.Length; j++)
					{
						yield return rule.commands[j].Execute();
					}
				}
			}
		}

		IEnumerator MatchSetup ()
		{
			Debug.Log("[CGEngine] Match Setup: " + ID);
			SetContext("matchNumber", matchNumber);
			for (int i = 0; i < watchers.Length; i++)
				yield return watchers[i].OnMatchSetup(matchNumber);
			yield return NotifyRules(TriggerLabel.OnMatchSetup);
		}

		IEnumerator StartMatch ()
		{
			Debug.Log("[CGEngine] Match Started: " + ID);
			SetContext("matchNumber", matchNumber);
			for (int i = 0; i < watchers.Length; i++)
				yield return watchers[i].OnMatchStarted(matchNumber);
			yield return NotifyRules(TriggerLabel.OnMatchStarted);
		}

		IEnumerator StartTurn ()
		{
			turnNumber++;
			Debug.Log("[CGEngine] Turn Started: " + turnNumber);
			SetContext("turnNumber", turnNumber);
			for (int i = 0; i < watchers.Length; i++)
				yield return watchers[i].OnTurnStarted(turnNumber);
			yield return NotifyRules(TriggerLabel.OnTurnStarted);
		}

		IEnumerator StartPhase (string phase)
		{
			currentTurnPhase = phase;
			//TODO Should the cleanup be made here? Is it done somewhere else?
			endCurrentPhase = false;
			endSubphaseLoop = false;
			externalSetCommands.Clear();
			Debug.Log("[CGEngine] Phase Started: " + phase);
			SetContext("phase", phase);
			for (int i = 0; i < watchers.Length; i++)
				yield return watchers[i].OnPhaseStarted(phase);
			yield return NotifyRules(TriggerLabel.OnPhaseStarted);
		}

		IEnumerator UseActionCoroutine (string action)
		{
			Debug.Log("[CGEngine] ACTION used: " + action);
			SetContext("actionName", action);
			for (int i = 0; i < watchers.Length; i++)
				yield return watchers[i].OnActionUsed(action);
			yield return NotifyRules(TriggerLabel.OnActionUsed);
		}

		IEnumerator UseCardCoroutine (CardSelector selector)
		{
			Card[] cardsSelected = (Card[])selector.Get();
			for (int i = 0; i < cardsSelected.Length; i++)
			{
				Card card = cardsSelected[i];
				Debug.Log("[CGEngine] Card USED: " + (card.data != null ? card.data.cardDataID : card.name));
				SetContext("usedCard", card.ID);
				for (int j = 0; j < watchers.Length; j++)
					yield return watchers[j].OnCardUsed(card);
				yield return NotifyRules(TriggerLabel.OnCardUsed);
			}
		}

		IEnumerator SpecialUseCardCoroutine (Card card)
		{
			Debug.Log("[CGEngine] Card USED: " + (card.data != null ? card.data.cardDataID : card.name));
			SetContext("usedCard", card.ID);
			for (int i = 0; i < watchers.Length; i++)
				yield return watchers[i].OnCardUsed(card);
			yield return NotifyRules(TriggerLabel.OnCardUsed);
		}

		IEnumerator SpecialUseZoneCoroutine (Zone zone)
		{
			Debug.Log("[CGEngine] Zone USED: " + zone.name);
			SetContext("usedZone", zone.ID);
			for (int i = 0; i < watchers.Length; i++)
				yield return watchers[i].OnZoneUsed(zone);
			yield return NotifyRules(TriggerLabel.OnZoneUsed);
		}

		IEnumerator ShuffleZones (ZoneSelector zoneSelector)
		{
			Zone[] zonesToShuffle = (Zone[])zoneSelector.Get();
			for (int i = 0; i < zonesToShuffle.Length; i++)
			{
				zonesToShuffle[i].Shuffle();
				Debug.Log($"[CGEngine] Zone {zonesToShuffle[i].name} Shuffled");
				yield return null;
			}
			Array.Sort(cards, CardSelector.CompareCardsByIndexIncreasing);
		}

		IEnumerator MoveCardToZone (CardSelector cardSelector, ZoneSelector zoneSelector, string[] additionalInfo)
		{
			bool toBottom = false;
			Vector2Int? gridPos = null;
			Card[] selectedCards = (Card[])cardSelector.Get();
			Zone[] selectedZones = (Zone[])zoneSelector.Get();
			for (int h = 0; h < selectedZones.Length; h++)
			{
				Zone zoneToMove = selectedZones[h];
				for (int i = 0; i < selectedCards.Length; i++)
				{
					Card card = selectedCards[i];
					Zone oldZone = card.zone;
					if (oldZone != null)
					{
						oldZone.PopCard(card);
						SetContext("movedCard", card.ID, "oldZone", oldZone.ID);
						for (int j = 0; j < watchers.Length; j++)
							yield return watchers[j].OnCardLeftZone(card, oldZone);
						yield return NotifyRules(TriggerLabel.OnCardLeftZone);
					}
					RevealStatus revealStatus = RevealStatus.ZoneDefinition;
					if (additionalInfo != null)
					{
						for (int j = 0; j < additionalInfo.Length; j++)
						{
							if (additionalInfo[j] == "Hidden" || additionalInfo[j] == "FaceDown")
							{
								revealStatus = RevealStatus.Hidden;
								continue;
							}
							else if (additionalInfo[j] == "Revealed" || additionalInfo[j] == "FaceUp")
							{
								revealStatus = RevealStatus.RevealedToEveryone;
								continue;
							}
							else if (additionalInfo[j] == "Bottom")
							{
								toBottom = true;
							}
							else if (additionalInfo[j].StartsWith("("))
							{
								string[] gridPositions = StringUtility.ArgumentsBreakdown(additionalInfo[j]);
								if (zoneToMove.zoneConfig == ZoneConfiguration.Grid)
								{
									if (gridPositions.Length != 2) continue;
									Getter left = Getter.Build(gridPositions[0]);
									Getter right = Getter.Build(gridPositions[1]);
									if (left.Get() is float && right.Get() is float)
										gridPos = new Vector2Int((int)left.Get(), (int)right.Get());
									else
										Debug.LogWarning($"[CGEngine] Something is wrong in grid position with parameter {additionalInfo[j]}");
								}
								else if (zoneToMove.zoneConfig == ZoneConfiguration.SpecificPositions)
								{
									if (gridPositions.Length < 1) continue;
									Getter pos = Getter.Build(gridPositions[0]);
									if (pos.Get() is float)
										gridPos = new Vector2Int((int)pos.Get(), 1);
									else
										Debug.LogWarning($"[CGEngine] An invalid parameter was passed for zone {zoneToMove.name} with parameter {additionalInfo[j]}");
								}
							}
						}
					}
					if (gridPos == null)
						zoneToMove.PushCard(card, revealStatus, toBottom);
					else
						zoneToMove.PushCard(card, revealStatus, gridPos.Value);
					SetContext("movedCard", card.ID, "targetZone", zoneToMove.ID, "oldZone", oldZone.ID);
					for (int j = 0; j < watchers.Length; j++)
						yield return watchers[j].OnCardEnteredZone(card, zoneToMove, oldZone, additionalInfo);
					yield return NotifyRules(TriggerLabel.OnCardEnteredZone);
				}
				Debug.Log($"[CGEngine] {selectedCards.Length} card(s) moved to zone {zoneToMove.name}");
			}
			yield return null;
		}

		IEnumerator EndPhase (string phase)
		{
			Debug.Log("[CGEngine] Phase Ended: " + phase);
			SetContext("phase", phase);
			for (int i = 0; i < watchers.Length; i++)
				yield return watchers[i].OnPhaseEnded(phase);
			yield return NotifyRules(TriggerLabel.OnPhaseEnded);
		}

		IEnumerator EndTurn ()
		{
			Debug.Log("[CGEngine] Turn Ended: " + turnNumber);
			SetContext("turnNumber", turnNumber);
			for (int i = 0; i < watchers.Length; i++)
				yield return watchers[i].OnTurnEnded(turnNumber);
			yield return NotifyRules(TriggerLabel.OnTurnEnded);
		}

		IEnumerator EndMatch ()
		{
			Debug.Log("[CGEngine] Match Ended: " + matchNumber);
			SetContext("matchNumber", matchNumber);
			for (int i = 0; i < watchers.Length; i++)
				yield return watchers[i].OnMatchEnded(matchNumber);
			yield return NotifyRules(TriggerLabel.OnMatchEnded);
		}

		IEnumerator ExecuteCommands (Command[] commands)
		{
			if (commands == null)
			{
				yield break;
			}

			foreach (Command command in commands)
			{
				yield return command.Execute();
			}
		}

		IEnumerator StartSubphaseLoop (string subphasesDefinition)
		{
			subphases = CreateTurnPhasesFromString(subphasesDefinition);
			yield return null;
		}

		IEnumerator SendMessageToWatchers (string message)
		{
			Debug.Log("[CGEngine] Message Sent: " + message);
			SetContext("message", message);
			for (int i = 0; i < watchers.Length; i++)
				yield return watchers[i].OnMessageSent(message);
			yield return NotifyRules(TriggerLabel.OnMessageSent);
		}

		IEnumerator EndSubphaseLoop ()
		{
			endCurrentPhase = true;
			endSubphaseLoop = true;
			yield return null;
		}

		IEnumerator EndCurrentPhase ()
		{
			endCurrentPhase = true;
			yield return null;
		}

		IEnumerator EndTheMatch ()
		{
			endCurrentPhase = true;
			endSubphaseLoop = true;
			gameEnded = true;
			yield return null;
		}

		IEnumerator SetCardFieldValue (CardSelector selector, string fieldName, Getter value, Getter min, Getter max)
		{
			Card[] cardsSelected = (Card[])selector.Get();
			object valueGot = value.Get();
			for (int i = 0; i < cardsSelected.Length; i++)
			{
				Card card = cardsSelected[i];
				if (card.GetFieldDataType(fieldName) == CardFieldDataType.Number)
				{
					if (valueGot is float)
					{
						float val = (float)valueGot;
						float fieldValue = card.GetNumFieldValue(fieldName);
						if (value.opChar != '\0')
						{
							switch (value.opChar)
							{
								case '+':
									val = fieldValue + val;
									break;
								case '*':
									val = fieldValue * val;
									break;
								case '/':
									val = val == 0 ? 0 : fieldValue / val;
									break;
								case '%':
									val = fieldValue % val;
									break;
								case '^':
									val = Mathf.Pow(fieldValue, val);
									break;
							}
						}
						if (min != null && min.Get() is float && max != null && max.Get() is float)
						{
							float vMin = (float)min.Get(), vMax = (float)max.Get();
							if (val < vMin) val = vMin;
							else if (val > vMax) val = vMax;
						}
						card.SetCardFieldValue(fieldName, val);
						Debug.Log(string.Format("[CGEngine] Card field {0} in card {1} set to {2}", fieldName, card.data.cardDataID, val));
						continue;
					}
				}
				else if (card.GetFieldDataType(fieldName) == CardFieldDataType.Text)
				{
					if (valueGot is string)
					{
						card.SetCardFieldValue(fieldName, (string)valueGot);
						Debug.Log(string.Format("[CGEngine] Card field {0} in card {1} set to {2}", fieldName, card.data.cardDataID, valueGot));
						continue;
					}
				}
				Debug.LogWarning(string.Format("[CGEngine] Coudn't find field {0} in card {1} or the value being set ({2}) is not a compatible value type", fieldName, card.data.cardDataID, valueGot.ToString()));
			}

			yield return null;
		}

		IEnumerator SetVariable (string variableName, Getter value, Getter min = null, Getter max = null)
		{
			if (CGEngine.IsSystemVariable(variableName))
			{
				Debug.LogWarning(string.Format("[CGEngine] Variable {0} is a reserved variable and cannot be changed by the user", variableName));
				yield break;
			}

			if (variables.ContainsKey(variableName))
			{
				object valueGot = value.Get();
				if (valueGot is float)
				{
					float val = (float)valueGot;
					if (min != null && min.Get() is float && max != null && max.Get() is float)
					{
						float vMin = (float)min.Get(), vMax = (float)max.Get();
						if (val < vMin) val = vMin;
						else if (val > vMax) val = vMax;
					}
					if ((float)variables[variableName] != val)
					{
						variables[variableName] = val;
						Debug.Log(string.Format("[CGEngine] Variable {0} set to value {1}", variableName, val));
						SetContext("variable", variableName, "value", val);
						for (int i = 0; i < watchers.Length; i++)
							yield return watchers[i].OnVariableChanged(variableName, val);
						yield return NotifyRules(TriggerLabel.OnVariableChanged);
					}
					yield break;
				}

				if ((string)variables[variableName] != (string)valueGot)
				{
					variables[variableName] = valueGot;
					Debug.Log(string.Format("[CGEngine] Variable {0} set to value {1}", variableName, valueGot));
					SetContext("variable", variableName, "value", valueGot);
					for (int i = 0; i < watchers.Length; i++)
						yield return watchers[i].OnVariableChanged(variableName, valueGot);
					yield return NotifyRules(TriggerLabel.OnVariableChanged);
				}
			}
			Debug.LogWarning(string.Format("[CGEngine] Variable {0} not found. Make sure to declare beforehand in the ruleset all variables that will be used", variableName));
			yield return null;
		}

		IEnumerator ChangeCardTag (CardSelector selector, string tag, bool isAdd)
		{
			Card[] cardsSelected = (Card[])selector.Get();
			for (int i = 0; i < cardsSelected.Length; i++)
			{
				if (isAdd)
					cardsSelected[i].AddTag(tag);
				else
					cardsSelected[i].RemoveTag(tag);
			}
			yield return null;
		}

		void SetContext (params object[] args)
		{
			if (args == null)
				return;
			for (int i = 0; i < args.Length; i += 2)
			{
				string key = (string)args[i];
				object value = args[i + 1];
				if (!variables.ContainsKey(key))
					variables.Add(key, value);
				else
					variables[key] = value;
			}
		}
		
		List<string> CreateTurnPhasesFromString (string phaseNamesList)
		{
			phaseNamesList = StringUtility.GetCleanStringForInstructions(phaseNamesList);
			List<string> phaseList = new List<string>();
			phaseList.AddRange(phaseNamesList.Split(','));
			return phaseList;
		}

		#endregion

		#region External Interactors ====================================================================================================

		internal Card GetCardVariable (string contextId)
		{
			if (variables.ContainsKey(contextId))
				return (Card)variables[contextId];
			Debug.LogWarning("[CGEngine] Context doesn't have card identified with: " + contextId);
			return null;
		}

		public bool HasVariable (string variableName)
		{
			return variables.ContainsKey(variableName);
		}

		public object GetVariable (string variableName)
		{
			if (variables.ContainsKey(variableName))
				return variables[variableName];
			Debug.LogWarning("[CGEngine] Variable not found: " + variableName);
			return float.NaN;
		}

		public Zone[] GetAllZones ()
		{
			return zones;
		}

		public Card[] GetAllCards ()
		{
			return cards;
		}

		public void ExecuteCommandFromClause (string clause)
		{
			externalSetCommands.Add(CreateCommand(clause));
		}

		public void UseCard (Card c)
		{
			if (externalSetCommands.Contains(useCardCommands[0]))
				useCardCommands.Insert(0, GetNewSpecialUseCardCommand());
			SingleCardCommand command = useCardCommands[0];
			command.SetCard(c);
			useCardCommands.Remove(command);
			useCardCommands.Add(command);
			externalSetCommands.Add(command);
		}

		public void UseZone (Zone z)
		{
			if (externalSetCommands.Contains(useZoneCommands[0]))
				useZoneCommands.Insert(0, GetNewSpecialUseZoneCommand());
			SingleZoneCommand command = useZoneCommands[0];
			command.SetZone(z);
			useZoneCommands.Remove(command);
			useZoneCommands.Add(command);
			externalSetCommands.Add(command);
		}

		public void UseAction (string action)
		{
			if (externalSetCommands.Contains(useActionCommands[0]))
			{
				useActionCommands.Insert(0, GetNewActionStringCommand());
			}
			StringCommand command = useActionCommands[0];
			command.strParameter = action;
			useActionCommands.Remove(command);
			useActionCommands.Add(command);
			externalSetCommands.Add(command);
		}

		#endregion
	}
}