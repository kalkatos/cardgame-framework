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
	public class Match : MonoBehaviour, IInputEventReceiver
	{
		public static Match Current { get; private set; }

		int cardIdTracker;
		int playerIdTracker;
		int modifierIdTracker;
		int zoneIdTracker;

		public string id;  //starts with "a"

		public int matchNumber;
		public string CurrentTurnPhase { get; private set; }

		List<MatchWatcher> watchers;
		List<MatchWatcher> Watchers
		{ get { if (watchers == null) watchers = new List<MatchWatcher>(); return watchers; } }
		CardGameData game;
		Ruleset ruleset;
		Card[] cards;
		Dictionary<string, Card> cardsByID;
		Zone[] zones;
		List<string> neutralResourcePools;
		List<Modifier> modifiers;
		List<Command> commandListToExecute;
		public Dictionary<string, object> variables { get; private set; }
		Dictionary<TriggerTag, List<Modifier>> triggerWatchers;
		SpecialClickCardCommand clickCardCommand;
		StringCommand useActionCommand;
		bool isSimulation;
		int turnNumber;
		List<string> actionHistory;
		bool gameEnded;
		bool endCurrentPhase;
		List<string> currentTurnPhases;
		List<Command> externalSetCommands;
		Transform modifierContainer;
		List<string> subphases = null;
		bool endSubphaseLoop;

		//==================================================================================================================
		#region Initialization Methods ==================================================================================================
		//==================================================================================================================

		public void Initialize (CardGameData game, Ruleset ruleset)
		{
			Current = this;
			this.game = game;
			this.ruleset = ruleset;
			variables = new Dictionary<string, object>();
			modifiers = new List<Modifier>();
			triggerWatchers = new Dictionary<TriggerTag, List<Modifier>>();
			externalSetCommands = new List<Command>();
			commandListToExecute = new List<Command>();
			clickCardCommand = new SpecialClickCardCommand(SpecialClickCardCoroutine);
			useActionCommand = new StringCommand(CommandType.UseAction, UseActionCoroutine, "");
			InputManager.Register("ObjectClicked", Current);

			SetupSystemVariables();
			SetupCustomVariables();
			SetupCards();
			SetupZones();
			SetupModifiers();
			SetupWatchers();
			StartCoroutine(MatchLoop());
		}

		void SetupCustomVariables ()
		{
			if (game.customVariableNames != null)
			{
				for (int i = 0; i < game.customVariableNames.Count; i++)
				{
					if (!variables.ContainsKey(game.customVariableNames[i]))
					{
						if (float.TryParse(game.customVariableValues[i], out float val))
							variables.Add(game.customVariableNames[i], val);
						else
							variables.Add(game.customVariableNames[i], game.customVariableValues[i]);
					}
				}
			}
			if (ruleset.customVariableNames != null)
			{
				for (int i = 0; i < ruleset.customVariableNames.Count; i++)
				{
					if (!variables.ContainsKey(ruleset.customVariableNames[i]))
					{
						if (float.TryParse(ruleset.customVariableValues[i], out float val))
							variables.Add(ruleset.customVariableNames[i], val);
						else
							variables.Add(ruleset.customVariableNames[i], ruleset.customVariableValues[i]);
					}
					else
					{
						if (float.TryParse(ruleset.customVariableValues[i], out float val))
							variables[ruleset.customVariableNames[i]] = val;
						else
							variables[ruleset.customVariableNames[i]] = ruleset.customVariableValues[i];
					}
				}
			}
		}

		void SetupSystemVariables ()
		{
			//card
			variables.Add("movedCard", "");
			variables.Add("clickedCard", "");
			variables.Add("usedCard", "");
			//zone
			variables.Add("targetZone", "");
			variables.Add("oldZone", "");
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
			cardsByID = new Dictionary<string, Card>();
			for (int i = 0; i < cards.Length; i++)
			{
				cards[i].ID = "c" + (++cardIdTracker).ToString().PadLeft(4, '0');
				cardsByID.Add(cards[i].ID, cards[i]);

				if (cards[i].Modifiers != null)
					modifiers.AddRange(cards[i].Modifiers);

				if (cards[i].data != null && cards[i].data.cardModifiers != null)
				{
					foreach (ModifierData data in cards[i].data.cardModifiers)
					{
						cards[i].AddModifier(CreateModifier(data, cards[i].ID));
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

		void SetupModifiers ()
		{

			if (ruleset.matchModifiers != null)
			{
				modifierContainer = new GameObject("ModifierContainer").transform;
				foreach (ModifierData data in ruleset.matchModifiers)
				{
					Modifier mod = CreateModifier(data, id);
					RegisterTrigger(mod);
				}
			}
		}

		void SetupWatchers ()
		{
			MatchWatcher[] watchers = FindObjectsOfType<MatchWatcher>();
			if (watchers != null)
				Watchers.AddRange(watchers);
		}

		void RegisterTrigger (Modifier modifier)
		{
			TriggerTag[] allTags = (TriggerTag[])Enum.GetValues(typeof(TriggerTag));
			for (int i = 0; i < allTags.Length; i++)
			{
				if ((modifier.activeTriggers & (int)allTags[i]) != 0)
				{
					if (!triggerWatchers.ContainsKey(allTags[i]))
					{
						List<Modifier> newList = new List<Modifier>();
						newList.Add(modifier);
						triggerWatchers.Add(allTags[i], newList);
					}
					else
						triggerWatchers[allTags[i]].Add(modifier);
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
				case "ClickCard":
					if (clauseBreak.Length != 2) break;
					newCommand = new CardCommand(CommandType.ClickCard, ClickCardCoroutine, new CardSelector(clauseBreak[1], cards));
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

				default: //=================================================================
					Debug.LogWarning("[CGEngine] Effect not found: " + clauseBreak[0]);
					break;
			}
			
			if (newCommand == null)
				Debug.LogError("[CGEngine] Couldn't build a command with instruction: " + clause);
			return newCommand;
		}

		public Modifier CreateModifier (ModifierData data, string originID)
		{
			if (data == null)
				return null;

			Modifier newMod = new GameObject(data.modifierID + "Modifier").AddComponent<Modifier>();
			newMod.transform.SetParent(modifierContainer);
			newMod.Initialize(data, originID);
			newMod.ID = "m" + (++modifierIdTracker).ToString().PadLeft(4, '0');
			Debug.Log(string.Format("[CGEngine] Created Modifier {0} ({1})", data.modifierID, newMod.ID));
			modifiers.Add(newMod);
			return newMod;
		}

		#endregion

		//==================================================================================================================
		#region Core Methods ==================================================================================================
		//==================================================================================================================

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

		IEnumerator MatchSetup ()
		{
			Debug.Log("[CGEngine] Match Setup: " + id);
			SetContext("matchNumber", matchNumber);
			yield return NotifyWatchers(TriggerTag.OnMatchSetup, "matchNumber", matchNumber);
			yield return NotifyModifiers(TriggerTag.OnMatchSetup, "matchNumber", matchNumber);
		}

		IEnumerator StartMatch ()
		{
			Debug.Log("[CGEngine] Match Started: " + id);
			SetContext("matchNumber", matchNumber);
			yield return NotifyWatchers(TriggerTag.OnMatchStarted, "matchNumber", matchNumber);
			yield return NotifyModifiers(TriggerTag.OnMatchStarted, "matchNumber", matchNumber);
		}

		IEnumerator StartTurn ()
		{
			turnNumber++;
			Debug.Log("[CGEngine] Turn Started: " + turnNumber.ToString());
			SetContext("turnNumber", turnNumber);
			yield return NotifyWatchers(TriggerTag.OnTurnStarted, "turnNumber", turnNumber);
			yield return NotifyModifiers(TriggerTag.OnTurnStarted, "turnNumber", turnNumber);
		}

		public IEnumerator StartPhase (string phase)
		{
			CurrentTurnPhase = phase;
			endCurrentPhase = false;
			endSubphaseLoop = false;
			externalSetCommands.Clear();
			Debug.Log("[CGEngine] Phase Started: " + phase);
			SetContext("phase", phase);
			yield return NotifyWatchers(TriggerTag.OnPhaseStarted, "phase", phase);
			yield return NotifyModifiers(TriggerTag.OnPhaseStarted, "phase", phase);
		}

		public void UseAction (string action)
		{
			useActionCommand.strParameter = action;
			externalSetCommands.Add(useActionCommand);
		}

		IEnumerator UseActionCoroutine (string action)
		{
			Debug.Log("[CGEngine] ACTION used: " + action);
			SetContext("actionName", action);
			yield return NotifyWatchers(TriggerTag.OnActionUsed, "actionName", action);
			yield return NotifyModifiers(TriggerTag.OnActionUsed, "actionName", action);
		}

		//public void UseCard (Card c)
		//{
		//	TreatEffect("UseCard(card(#" + c.ID + "))");
		//}

		IEnumerator UseCardCoroutine (CardSelector selector)
		{
			Card[] cardsSelected = (Card[])selector.Get();
			for (int i = 0; i < cardsSelected.Length; i++)
			{
				Debug.Log("[CGEngine] Card USED: " + cardsSelected[i].data != null ? cardsSelected[i].data.cardDataID : cardsSelected[i].name);
				SetContext("usedCard", cardsSelected[i].ID);
				yield return NotifyWatchers(TriggerTag.OnCardUsed, "usedCard", cardsSelected[i]);
				yield return NotifyModifiers(TriggerTag.OnCardUsed, "usedCard", cardsSelected[i]);
			}
		}

		IEnumerator UseCardRoutineOld (Card c)
		{
			Debug.Log("[CGEngine] Card USED: " + c.data != null ? c.data.cardDataID : c.name);
			SetContext("usedCard", c.ID);
			yield return NotifyWatchers(TriggerTag.OnCardUsed, "usedCard", c);
			yield return NotifyModifiers(TriggerTag.OnCardUsed, "usedCard", c);
		}

		public void ClickCard (Card c)
		{
			clickCardCommand.SetCard(c);
			externalSetCommands.Add(clickCardCommand);
		}

		IEnumerator ClickCardCoroutine (CardSelector selector)
		{
			Card[] cardsSelected = (Card[])selector.Get();
			for (int i = 0; i < cardsSelected.Length; i++)
			{
				Debug.Log("[CGEngine] Card CLICKED: " + cardsSelected[i].data != null ? cardsSelected[i].data.cardDataID : cardsSelected[i].name);
				SetContext("clickedCard", cardsSelected[i].ID);
				yield return NotifyWatchers(TriggerTag.OnCardClicked, "clickedCard", cardsSelected[i]);
				yield return NotifyModifiers(TriggerTag.OnCardClicked, "clickedCard", cardsSelected[i]);
			}
		}

		IEnumerator SpecialClickCardCoroutine (Card c)
		{
			Debug.Log("[CGEngine] Card CLICKED: " + (c.data != null ? c.data.cardDataID : c.name));
			SetContext("clickedCard", c.ID);
			yield return NotifyWatchers(TriggerTag.OnCardClicked, "clickedCard", c);
			yield return NotifyModifiers(TriggerTag.OnCardClicked, "clickedCard", c);
		}

		IEnumerator ShuffleZones (ZoneSelector zoneSelector)
		{
			Zone[] zonesToShuffle = (Zone[])zoneSelector.Get();
			for (int i = 0; i < zonesToShuffle.Length; i++)
			{
				zonesToShuffle[i].Shuffle();
				Debug.Log(string.Format("[CGEngine] Zone {0} Shuffled", zonesToShuffle[i].zoneTags));
				yield return null;
			}
			Array.Sort(cards, CardSelector.CompareCardsByIndexForSorting);
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
						yield return NotifyWatchers(TriggerTag.OnCardLeftZone, "movedCard", card, "oldZone", oldZone, "additionalInfo", additionalInfo);
						yield return NotifyModifiers(TriggerTag.OnCardLeftZone, "movedCard", card, "oldZone", oldZone, "additionalInfo", additionalInfo);
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
							else if (additionalInfo[j].StartsWith("(") && zoneToMove.zoneConfig == ZoneConfiguration.Grid) //A grid position
							{
								string[] gridPositions = StringUtility.ArgumentsBreakdown(additionalInfo[j]);
								if (gridPositions.Length != 2) continue;
								Getter left = Getter.Build(gridPositions[0]);
								Getter right = Getter.Build(gridPositions[1]);
								if (left.Get() is float && right.Get() is float)
								{
									gridPos = new Vector2Int((int)left.Get(), (int)right.Get());
								}
								else
								{
									Debug.LogWarning("[CGEngine] Something is wrong in grid position with parameter " + additionalInfo[j]);
								}
							}
						}
					}
					if (gridPos == null)
						zoneToMove.PushCard(card, revealStatus, toBottom);
					else
						zoneToMove.PushCard(card, revealStatus, gridPos.Value);
					SetContext("movedCard", card.ID, "targetZone", zoneToMove.ID, "oldZone", oldZone.ID);
					yield return NotifyWatchers(TriggerTag.OnCardEnteredZone, "movedCard", card, "targetZone", zoneToMove, "oldZone", oldZone, "additionalInfo", additionalInfo);
					yield return NotifyModifiers(TriggerTag.OnCardEnteredZone, "movedCard", card, "targetZone", zoneToMove, "oldZone", oldZone, "additionalInfo", additionalInfo);
				}
				Debug.Log(string.Format("[CGEngine] {0} card(s) moved to zone {1}", selectedCards.Length, zoneToMove.zoneTags));
			}
			yield return null;
		}

		IEnumerator EndPhase (string phase)
		{
			yield return NotifyWatchers(TriggerTag.OnPhaseEnded, "phase", phase);
			yield return NotifyModifiers(TriggerTag.OnPhaseEnded, "phase", phase);
		}

		IEnumerator EndTurn ()
		{
			yield return NotifyWatchers(TriggerTag.OnTurnEnded, "turnNumber", turnNumber);
			yield return NotifyModifiers(TriggerTag.OnTurnEnded, "turnNumber", turnNumber);
		}

		IEnumerator EndMatch ()
		{
			yield return NotifyWatchers(TriggerTag.OnMatchEnded, "matchNumber", matchNumber);
			yield return NotifyModifiers(TriggerTag.OnMatchEnded, "matchNumber", matchNumber);
		}

		IEnumerator NotifyWatchers (TriggerTag triggerTag, params object[] args)
		{
			foreach (MatchWatcher item in Watchers)
			{
				yield return item.TreatTrigger(triggerTag, args);
			}
		}

		IEnumerator NotifyModifiers (TriggerTag tag, params object[] args)
		{
			if (!triggerWatchers.ContainsKey(tag))
				yield break;

			for (int i = 0; i < triggerWatchers[tag].Count; i++)
			{
				if (triggerWatchers[tag][i] == null)
					continue;

				Modifier mod = triggerWatchers[tag][i];
				//Debug.Log(string.Format("DEBUG = Checking condition in {0}   ||   {1}   ||   {2}", mod.name, mod.conditions, StringUtility.GetCleanStringForInstructions(mod.data.condition)));
				bool condition = mod.conditions.Evaluate();
				if (condition)
				{
					Debug.Log(string.Format("[CGEngine] TRIGGER: {0} from {1}", tag.ToString(), mod.name));
					for (int j = 0; j < mod.commands.Length; j++)
					{
						yield return mod.commands[j].Execute();
					}
				}
			}
		}

		public void ExecuteCommandFromClause (string clause)
		{
			externalSetCommands.Add(CreateCommand(clause));
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
			yield return NotifyWatchers(TriggerTag.OnMessageSent, "message", message);
			yield return NotifyModifiers(TriggerTag.OnMessageSent, "message", message);
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
						yield return NotifyWatchers(TriggerTag.OnVariableChanged, "variable", variableName, "value", val);
						yield return NotifyModifiers(TriggerTag.OnVariableChanged, "variable", variableName, "value", val);
					}
					yield break;
				}

				if ((string)variables[variableName] != (string)valueGot)
				{
					variables[variableName] = valueGot;
					Debug.Log(string.Format("[CGEngine] Variable {0} set to value {1}", variableName, valueGot));
					SetContext("variable", variableName, "value", valueGot);
					yield return NotifyWatchers(TriggerTag.OnVariableChanged, "variable", variableName, "value", valueGot);
					yield return NotifyModifiers(TriggerTag.OnVariableChanged, "variable", variableName, "value", valueGot);
				}
			}
			Debug.LogWarning(string.Format("[CGEngine] Variable {0} not found. Make sure to declare beforehand in the ruleset all variables that will be used", variableName));
			yield return null;
		}
		
		#endregion
				
		List<string> CreateTurnPhasesFromString (string phaseNamesList)
		{
			phaseNamesList = StringUtility.GetCleanStringForInstructions(phaseNamesList);
			List<string> phaseList = new List<string>();
			phaseList.AddRange(phaseNamesList.Split(','));
			return phaseList;
		}
		
		internal Card GetCardVariable (string contextId)
		{
			if (variables.ContainsKey(contextId))
				return (Card)variables[contextId];
			Debug.LogWarning("[CGEngine] Context doesn't have card identified with: "+ contextId);
			return null;
		}

		//public bool HasCardField (string fieldName)
		//{
		//	return cardFieldDefinitions.ContainsKey(fieldName);
		//}

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
		
		public void TreatEvent (string type, InputObject inputObject)
		{
			Card c = inputObject.GetComponent<Card>();
			if (c)
			{
				ClickCard(c);
			}
		}
	}
}