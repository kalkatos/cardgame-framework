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
		Ruleset rules;
		Card[] cards;
		Dictionary<string, Card> cardsByID;
		//List<PlayerRole> playerRules;
		Zone[] zones;
		//List<Zone> neutralZones; //References to zones
		List<string> neutralResourcePools;
		List<Modifier> modifiers;
		List<Command> commandListToExecute;
		Dictionary<string, object> variables;
		Dictionary<TriggerTag, List<Modifier>> triggerWatchers;

		bool isSimulation;
		int turnNumber;
		List<string> actionHistory;
		//int activePlayer;
		bool gameEnded;
		bool endCurrentPhase;
		List<string> currentTurnPhases;
		string externalSetEffect = null;
		Transform modifierContainer;
		List<string> subphases = null;
		bool endSubphaseLoop;
		//float valueForNextEffect;
		string logIdentation = "";

		//==================================================================================================================
		#region Initialization Methods ==================================================================================================
		//==================================================================================================================

		public void Initialize (Ruleset rules)
		{
			Current = this;
			this.rules = rules;
			variables = new Dictionary<string, object>();
			modifiers = new List<Modifier>();
			triggerWatchers = new Dictionary<TriggerTag, List<Modifier>>();
			commandListToExecute = new List<Command>();
			InputManager.Register("ObjectClicked", Current);

			SetupSystemVariables();
			SetupCards();
			SetupZones();
			SetupModifiers();
			SetupWatchers();
			StartCoroutine(MatchLoop());
		}

		void SetupSystemVariables ()
		{
			//card
			variables.Add("movedCard", null);
			variables.Add("clickedCard", null);
			variables.Add("usedCard", null);
			//zone
			variables.Add("targetZone", null);
			variables.Add("oldZone", null);
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

			//DEBUG
			variables.Add("Testing", 0);
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
						cards[i].AddModifiers(CreateModifier(data));
					}
				}
			}
		}

		void SetupZones ()
		{
			zones = FindObjectsOfType<Zone>();
			if (zones == null)
			{
				Debug.LogError(BuildMessage("Error: No zones found in Match Scene."));
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

			if (rules.matchModifiers != null)
			{
				modifierContainer = new GameObject("ModifierContainer").transform;
				foreach (ModifierData data in rules.matchModifiers)
				{
					Modifier mod = CreateModifier(data);
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
			if (string.IsNullOrEmpty(modifier.data.trigger))
				return;

			string[] triggers = modifier.data.trigger.Split(';');
			foreach (string item in triggers)
			{
				string[] triggerBreakdown = ArgumentsBreakdown(item, true);
				if (Enum.TryParse(triggerBreakdown[0], out TriggerTag tag))
				{
					if (!triggerWatchers.ContainsKey(tag))
					{
						List<Modifier> modList = new List<Modifier>();
						modList.Add(modifier);
						triggerWatchers.Add(tag, modList);
					}
					else
					{
						triggerWatchers[tag].Add(modifier);
					}
					modifier.activeTriggers = modifier.activeTriggers | (int)tag;
				}
				else
				{
					Debug.LogWarning(BuildMessage("Error trying to parse trigger: " + item + " in Modifier: " + modifier.data.modifierID));
				}

			}
		}
		//DEBUG should not be public
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
					Debug.LogWarning(BuildMessage("Effect not found: ", clauseBreak[0]));
					break;
			}
			Debug.Log("DEBUG  = = = " + newCommand.ToString());
			if (newCommand == null)
				Debug.LogError(BuildMessage("Couldn't build a command with instruction: ", clause));
			return newCommand;
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
				currentTurnPhases = CreateTurnPhasesFromString(rules.turnStructure);
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
										if (!string.IsNullOrEmpty(externalSetEffect))
										{
											yield return TreatEffectRoutine(externalSetEffect);
											externalSetEffect = "";
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
							if (!string.IsNullOrEmpty(externalSetEffect))
							{
								yield return TreatEffectRoutine(externalSetEffect);
								externalSetEffect = "";
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
			Debug.Log(BuildMessage("Match Setup: ", id));
			yield return NotifyWatchers(TriggerTag.OnMatchSetup, "matchNumber", matchNumber);
			yield return NotifyModifiers(TriggerTag.OnMatchSetup, "matchNumber", matchNumber);
		}

		IEnumerator StartMatch ()
		{
			Debug.Log(BuildMessage("Match Started: ", id));
			yield return NotifyWatchers(TriggerTag.OnMatchStarted, "matchNumber", matchNumber);
			yield return NotifyModifiers(TriggerTag.OnMatchStarted, "matchNumber", matchNumber);
		}

		IEnumerator StartTurn ()
		{
			turnNumber++;
			Debug.Log(BuildMessage("Turn Started: ", turnNumber.ToString()));
			yield return NotifyWatchers(TriggerTag.OnTurnStarted, "turnNumber", turnNumber);
			yield return NotifyModifiers(TriggerTag.OnTurnStarted, "turnNumber", turnNumber);
		}

		public IEnumerator StartPhase (string phase)
		{
			CurrentTurnPhase = phase;
			endCurrentPhase = false;
			endSubphaseLoop = false;
			externalSetEffect = null;
			Debug.Log(BuildMessage("Phase Started: ", phase));
			yield return NotifyWatchers(TriggerTag.OnPhaseStarted, "phase", phase);
			yield return NotifyModifiers(TriggerTag.OnPhaseStarted, "phase", phase);
		}

		public void UseAction (string action)
		{
			TreatEffect("UseAction(" + action + ")");
		}

		IEnumerator UseActionCoroutine (string action)
		{
			Debug.Log(BuildMessage("ACTION used: ", action));
			yield return NotifyWatchers(TriggerTag.OnActionUsed, "actionName", action);
			yield return NotifyModifiers(TriggerTag.OnActionUsed, "actionName", action);
		}

		public void UseCard (Card c)
		{
			TreatEffect("UseCard(card(#" + c.ID + "))");
		}

		IEnumerator UseCardCoroutine (CardSelector selector)
		{
			Card[] cardsSelected = (Card[])selector.Get();
			for (int i = 0; i < cardsSelected.Length; i++)
			{
				Debug.Log(BuildMessage("Card USED: ", cardsSelected[i].data != null ? cardsSelected[i].data.cardDataID : cardsSelected[i].name));
				SetContext("usedCard", cardsSelected[i]);
				yield return NotifyWatchers(TriggerTag.OnCardUsed, "usedCard", cardsSelected[i]);
				yield return NotifyModifiers(TriggerTag.OnCardUsed, "usedCard", cardsSelected[i]);
			}
		}

		IEnumerator UseCardRoutineOld (Card c)
		{
			Debug.Log(BuildMessage("Card USED: ", c.data != null ? c.data.cardDataID : c.name));
			SetContext("usedCard", c);
			yield return NotifyWatchers(TriggerTag.OnCardUsed, "usedCard", c);
			yield return NotifyModifiers(TriggerTag.OnCardUsed, "usedCard", c);
		}

		public void ClickCard (Card c)
		{
			TreatEffect("ClickCard(card(#" + c.ID + "))");
		}

		IEnumerator ClickCardCoroutine (CardSelector selector)
		{
			Card[] cardsSelected = (Card[])selector.Get();
			for (int i = 0; i < cardsSelected.Length; i++)
			{
				Debug.Log(BuildMessage("Card CLICKED: ", cardsSelected[i].data != null ? cardsSelected[i].data.cardDataID : cardsSelected[i].name));
				SetContext("clickedCard", cardsSelected[i]);
				yield return NotifyWatchers(TriggerTag.OnCardClicked, "clickedCard", cardsSelected[i]);
				yield return NotifyModifiers(TriggerTag.OnCardClicked, "clickedCard", cardsSelected[i]);
			}
		}

		IEnumerator ClickCardRoutineOld (Card c)
		{
			Debug.Log(BuildMessage("Card CLICKED: ", c.data != null ? c.data.cardDataID : c.name));
			SetContext("clickedCard", c);
			yield return NotifyWatchers(TriggerTag.OnCardClicked, "clickedCard", c);
			yield return NotifyModifiers(TriggerTag.OnCardClicked, "clickedCard", c);
		}

		IEnumerator ShuffleZones (ZoneSelector zoneSelector)
		{
			Zone[] zonesToShuffle = (Zone[])zoneSelector.Get();
			for (int i = 0; i < zonesToShuffle.Length; i++)
			{
				zonesToShuffle[i].Shuffle();
				Debug.Log(StringUtility.BuildMessage("Zone @ Shuffled", zonesToShuffle[i].zoneTags));
				yield return null;
			}
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
						SetContext("movedCard", card, "oldZone", oldZone);
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
									Debug.LogWarning(StringUtility.BuildMessage("Something is wrong in grid position with parameter " + additionalInfo[j]));
								}
							}
						}
					}
					if (gridPos == null)
						zoneToMove.PushCard(card, revealStatus, toBottom);
					else
						zoneToMove.PushCard(card, revealStatus, gridPos.Value);
					SetContext("movedCard", card, "targetZone", zoneToMove, "oldZone", oldZone);
					yield return NotifyWatchers(TriggerTag.OnCardEnteredZone, "movedCard", card, "targetZone", zoneToMove, "oldZone", oldZone, "additionalInfo", additionalInfo);
					yield return NotifyModifiers(TriggerTag.OnCardEnteredZone, "movedCard", card, "targetZone", zoneToMove, "oldZone", oldZone, "additionalInfo", additionalInfo);
				}
				Debug.Log(StringUtility.BuildMessage("@ card(s) moved to zone @", selectedCards.Length, zoneToMove.zoneTags));
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

				bool trigg = CheckTriggerWithArguments(triggerWatchers[tag][i].trigger, tag, args);
				if (trigg)
				{
					Debug.Log(BuildMessage("TRIGGER: ", triggerWatchers[tag][i].trigger, "  on ", triggerWatchers[tag][i].name));
					logIdentation = "    ";
					if (CheckCondition(triggerWatchers[tag][i].condition))
					{
						yield return TreatEffectRoutine(triggerWatchers[tag][i].trueEffect);
					}
					else
					{
						yield return TreatEffectRoutine(triggerWatchers[tag][i].falseEffect);
					}
					logIdentation = "";
				}
			}
		}

		public void TreatEffect (string effect)
		{
			if (string.IsNullOrEmpty(externalSetEffect))
				externalSetEffect = effect;
			else
				externalSetEffect = externalSetEffect + ";" + effect;
		}



		IEnumerator TreatEffectRoutine (string effect)
		{
			if (string.IsNullOrEmpty(effect))
			{
				yield break;
			}

			effect = GetCleanStringForInstructions(effect);

			string[] effLines = effect.Split(';');

			foreach (string effLine in effLines)
			{
				string[] effBreakdown = ArgumentsBreakdown(effLine);
				Debug.Log(BuildMessage("Treating effect => ", PrintStringArray(effBreakdown)));

				//TODO MAX one for each command
				switch (effBreakdown[0])
				{
					case "EndCurrentPhase":
						yield return EndCurrentPhase();
						break;
					case "EndTheMatch":
						yield return EndTheMatch();
						break;
					case "EndSubphaseLoop":
						yield return EndSubphaseLoop();
						break;
					case "UseAction":
						yield return UseActionCoroutine(effBreakdown[1]);
						break;
					case "SendMessage":
						yield return SendMessageToWatchers(effBreakdown[1]);
						break;
					case "StartSubphaseLoop":
						yield return StartSubphaseLoop(ArgumentsBreakdown(effLine, true)[1]);
						break;
					case "MoveCardToZone":
						List<Card> cardsToMove = SelectCards(ArgumentsBreakdown(effBreakdown[1]), cards);
						Zone zoneToMoveTo = SelectZones(ArgumentsBreakdown(effBreakdown[2]), zones)[0];
						string[] moveTags = effBreakdown.Length > 3 ? new string[effBreakdown.Length - 3] : null;
						if (moveTags != null) for (int i = 0; i < moveTags.Length; i++) { moveTags[i] = effBreakdown[i + 3]; }
						yield return MoveCardToZoneOld(cardsToMove, zoneToMoveTo, moveTags);
						break;
					case "Shuffle":
						Zone zoneToShuffle = SelectZones(ArgumentsBreakdown(effBreakdown[1]), zones)[0];
						zoneToShuffle.Shuffle();
						Debug.Log(BuildMessage("Zone ", zoneToShuffle.zoneTags, " shuffled."));
						break;
					case "SetCardFieldValue":
						SetCardFieldValueOld(effBreakdown[1], effBreakdown[2], effBreakdown[3]);
						break;
					case "SetVariable":
						if (effBreakdown.Length == 5)
							yield return SetVariableOld(effBreakdown[1], effBreakdown[2], effBreakdown[3], effBreakdown[4]);
						else if (effBreakdown.Length == 3)
							yield return SetVariableOld(effBreakdown[1], effBreakdown[2]);
						else
							Debug.LogWarning(BuildMessage("Wrong number of arguments for ", effLine, ". The correct is \"SetVariable(variableName, value)\" or \"SetVariable(variableName, value, min, max)\""));
						break;
					case "UseCard":
						List<Card> cardsToUse = SelectCards(ArgumentsBreakdown(effBreakdown[1]), cards);
						for (int i = 0; i < cardsToUse.Count; i++)
						{
							yield return UseCardRoutineOld(cardsToUse[i]);
						}
						break;
					case "ClickCard":
						List<Card> cardsClicked = SelectCards(ArgumentsBreakdown(effBreakdown[1]), cards);
						for (int i = 0; i < cardsClicked.Count; i++)
						{
							yield return ClickCardRoutineOld(cardsClicked[i]);
						}
						break;

					default: //=================================================================
						Debug.LogWarning(BuildMessage("Effect not found: ", effBreakdown[0]));
						break;
				}
			}

		}

		IEnumerator StartSubphaseLoop (string subphasesDefinition)
		{
			subphases = CreateTurnPhasesFromString(subphasesDefinition);
			yield return null;
		}

		IEnumerator SendMessageToWatchers (string message)
		{
			Debug.Log(BuildMessage("Message Sent:  ", message));
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
						if (min != null && min.Get() is float && max != null && max.Get() is float)
						{
							float vMin = (float)min.Get(), vMax = (float)max.Get();
							if (val < vMin) val = vMin;
							else if (val > vMax) val = vMax;
						}
						card.SetCardFieldValue(fieldName, val);
						Debug.Log(StringUtility.BuildMessage("Card field @ in card @ set to @", fieldName, card.data.cardDataID, val));
						continue;
					}
				}
				else if (card.GetFieldDataType(fieldName) == CardFieldDataType.Text)
				{
					if (valueGot is string)
					{
						card.SetCardFieldValue(fieldName, (string)valueGot);
						Debug.Log(StringUtility.BuildMessage("Card field @ in card @ set to @", fieldName, card.data.cardDataID, valueGot));
						continue;
					}
				}
				Debug.LogWarning(StringUtility.BuildMessage("Coudn't find field @ in card @ or the value being set (@) is not a compatible value type", fieldName, card.data.cardDataID, valueGot.ToString()));
			}

			yield return null;
		}

		IEnumerator SetVariable (string variableName, Getter value, Getter min = null, Getter max = null)
		{
			if (CGEngine.IsSystemVariable(variableName))
			{
				Debug.LogWarning(StringUtility.BuildMessage("Variable @ is a reserved variable and cannot be changed by the user", variableName));
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
					variables[variableName] = val;
					Debug.Log(StringUtility.BuildMessage("Variable @ set to value @", variableName, val));
					yield break;
				}
				variables[variableName] = value.Get();
				Debug.Log(StringUtility.BuildMessage("Variable @ set to value @", variableName, value));
			}
			Debug.LogWarning(StringUtility.BuildMessage("Variable @ not found. Make sure to declare beforehand in the ruleset all variables that will be used", variableName));
			yield return null;
		}

		void SetCardFieldValueOld (string cardSelectionClause, string fieldName, string value)
		{
			List<Card> list = SelectCards(ArgumentsBreakdown(cardSelectionClause), cards);
			for (int i = 0; i < list.Count; i++)
			{
				if (!list[i].HasField(fieldName))
				{
					Debug.LogWarning(BuildMessage("There is no field with name ", fieldName));
					return;
				}
				CardFieldDataType type = list[i].GetFieldDataType(fieldName);

				switch (type)
				{
					case CardFieldDataType.Text:
						list[i].SetCardFieldValue(fieldName, value);
						break;
					case CardFieldDataType.Number:
						float val = list[i].GetNumFieldValue(fieldName);
						SetValue(value, ref val);
						list[i].SetCardFieldValue(fieldName, val);
						break;
					case CardFieldDataType.Image:
						Debug.LogWarning(BuildMessage("Sorry, setting image in card field is not implemented yet"));
						break;
				}
			}
		}

		bool SetValue (string valueStr, ref float varToBeSet)
		{
			string firstChar = valueStr.Substring(0, 1);
			bool isOperator = "+-/*".Contains(firstChar);
			if (isOperator) valueStr = valueStr.Substring(1);

			float value = ExtractNumber(valueStr);
			if (!float.IsNaN(value))
			{
				if (firstChar == "+")
					varToBeSet += value;
				else if (firstChar == "-")
					varToBeSet -= value;
				else if (firstChar == "*")
					varToBeSet *= value;
				else if (firstChar == "/")
					varToBeSet /= value;
				else
					varToBeSet = value;
				return true;
			}
			Debug.LogWarning(BuildMessage("SetValue failure: ", valueStr));
			return false;
		}

		IEnumerator SetVariableOld (string variableName, string valueStr, string min = "min", string max = "max")
		{
			float val = 0;
			if (!variables.ContainsKey(variableName))
			{
				variables.Add(variableName, 0);
				if ("-/*".Contains(valueStr.Substring(0, 1)))
				{
					Debug.LogWarning(BuildMessage("variable \"", variableName, "\" is being set for the first time with an operator - * or /. It was set to 0 instead. Be sure to set a variable with a number before using operators. Value: ", valueStr));
					yield break;
				}
			}
			else
				val = (float)variables[variableName];

			float minVal = min == "min" ? float.MinValue : float.Parse(min);
			float maxVal = max == "max" ? float.MaxValue : float.Parse(max);
			bool valueSet = SetValue(valueStr, ref val);
			if (valueSet)
			{
				if (val > maxVal) val = maxVal;
				else if (val < minVal) val = minVal;
				variables[variableName] = val;
			}
			else
			{
				Debug.LogWarning(BuildMessage("Error setting variable \"", variableName, "\" with value from ", valueStr));
				yield break;
			}

			Debug.Log(BuildMessage("Setting variable \"", variableName, "\" = ", variables[variableName].ToString()));
			yield return NotifyWatchers(TriggerTag.OnVariableChanged, "variable", variableName, "value", variables[variableName]);
			yield return NotifyModifiers(TriggerTag.OnVariableChanged, "variable", variableName, "value", variables[variableName]);
		}

		float GetValueFromCardFieldOrExpression (string[] conditionBreakdown)
		{

			if (conditionBreakdown[1].Contains("-") || conditionBreakdown[1].Contains("+") || conditionBreakdown[1].Contains("*") || conditionBreakdown[1].Contains("/"))
			{
				string sentence = conditionBreakdown[1];
				int expressionIndex = sentence.IndexOf("$");
				int end = 0;
				while (expressionIndex != -1)
				{
					end = GetEndOfFirstParenthesis(sentence, expressionIndex);
					if (end != -1)
					{
						string expression = sentence.Substring(expressionIndex, end - expressionIndex + 1);
						float value = ExtractNumber(expression);
						if (value != float.NaN)
						{
							sentence = sentence.Replace(expression, value.ToString());
						}
						else
						{
							Debug.LogError(BuildMessage("There is a problem with expression ", expression, " inside of ", conditionBreakdown[1], "."));
							return float.NaN;
						}
					}
					else
					{
						Debug.LogError(BuildMessage("There is a problem with expression ", conditionBreakdown[1], ". Maybe missing a parenthesis?"));
						return float.NaN;
					}
					expressionIndex = sentence.IndexOf("$");
				}
				UnityEditor.ExpressionEvaluator.Evaluate(sentence, out float result);
				if (result == 0)
					Debug.LogWarning(BuildMessage("There can be a problem with expression ", conditionBreakdown[1], "."));

				if (conditionBreakdown.Length > 3) //Clamp Rules min , max
				{
					if (conditionBreakdown[2] != "min")
					{

					}
				}

				return result;
			}

			//Card
			else if (conditionBreakdown[1].StartsWith("card"))
			{
				List<Card> cardList = SelectCards(ArgumentsBreakdown(conditionBreakdown[1]), cards);
				if (cardList == null || cardList.Count == 0)
				{
					Debug.LogWarning(BuildMessage("Couldn't find cards with value using condition: ", conditionBreakdown[1]));
					return float.NaN;
				}

				if (cardList.Count > 1)
				{
					Debug.LogWarning(BuildMessage("There is an ambiguity with condition ", conditionBreakdown[1], ". It should return only one element to search for values."));
				}

				if (conditionBreakdown.Length < 2)
				{
					Debug.LogWarning(BuildMessage("Wrong number of arguments for condition ", conditionBreakdown[1], ". It should contain the name of the card field to extract value from."));
					return float.NaN;
				}

				//Field
				string fieldName = conditionBreakdown[2];
				if (conditionBreakdown[2].StartsWith("/") || conditionBreakdown[2].StartsWith("f"))
					fieldName = conditionBreakdown[2].Substring(1);

				return cardList[0].GetNumFieldValue(fieldName);
				//CardField[] cardFields = cardList[0].fields;
				//for (int i = 0; i < cardFields.Length; i++)
				//{
				//	if (cardFields[i].fieldName == fieldName)
				//		return cardFields[i].numValue;
				//}
			}

			Debug.LogWarning(BuildMessage("Couldn't find any usable value with condition: ", PrintStringArray(conditionBreakdown)));
			return float.NaN;
		}



		public Modifier CreateModifier (string definitions)
		{
			string[] definitionsBreakdown = ArgumentsBreakdown(definitions);
			Modifier newMod = null;
			for (int i = 0; i < definitionsBreakdown.Length; i++)
			{
				if (definitionsBreakdown[i] == "mod" || definitionsBreakdown[i] == "modifier")
					continue;

				string type = definitionsBreakdown[i].Substring(0, 1);
				string subdef = definitionsBreakdown[i].Substring(1);

				switch (type)
				{
					case "%":
					case "m":
						if (!newMod)
							newMod = CreateModifierWithTags(subdef);
						else
							newMod.tags = subdef;
						break;
					default:
						Debug.LogWarning(BuildMessage("Couldn't resolve Modifier creation with definitions ", type, subdef));
						break;
				}
			}

			if (newMod)
			{
				if (!modifiers.Contains(newMod)) modifiers.Add(newMod);
				Debug.Log(BuildMessage("Created Modifier ", newMod.gameObject.name, " (", newMod.id, ")"));
				return newMod;
			}
			else
				Debug.LogWarning(BuildMessage("Error or modifier definition not yet implemented."));
			return null;
		}

		void CreateModifier (Modifier reference)
		{
			if (reference.data != null)
			{
				CreateModifier(reference.data);
				return;
			}
			CreateModifierWithTags(reference.tags);
		}

		public Modifier CreateModifier (ModifierData data)
		{
			if (data == null)
				return null;

			Modifier newMod = new GameObject(data.modifierID + "Modifier").AddComponent<Modifier>();
			newMod.transform.SetParent(modifierContainer);
			newMod.Initialize(data, id);
			newMod.id = "m" + (++modifierIdTracker).ToString().PadLeft(4, '0');
			Debug.Log(BuildMessage("Created Modifier ", data.modifierID, " (", newMod.id, ")"));
			modifiers.Add(newMod);
			return newMod;
		}

		Modifier CreateModifierWithTags (string tags)
		{
			Modifier newMod = new GameObject().AddComponent<Modifier>();
			newMod.transform.SetParent(modifierContainer);
			newMod.Initialize(tags);
			newMod.id = "m" + (++modifierIdTracker).ToString().PadLeft(4, '0');
			string name = "Modifier (" + newMod.id + ")";
			newMod.tags = tags.Replace(" ", "");
			newMod.gameObject.name = name;
			modifiers.Add(newMod);
			return newMod;
		}

		#endregion

		//==================================================================================================================
		#region Command Methods ==================================================================================================
		//==================================================================================================================

		/*
		// Commands ============================================================
		AskForTarget(Player, string targetDefinition) : bool   //true if target could be found
		AskForCost(Player, string cost) : bool  //true if successfully paid the cost
		ChangeCardField(Card,CardFieldName,int,string)
		ChangeCardRevealStatus(revealStatus)
		ChangeController(Card,Player)
		ChangeModifier(Card, ModifierType, int)
		ChangeModifier(Player, ModifierType, int)
		ChangeModifier(Match, ModifierType, int)
		ChangePlayerRules
		ChangeResource
		ChangeScore
		CheckModifier(ModifierType, Card[]) : int
		CreateCard(string cardDataID, Player controller, Zone atZone)
		DestroyPermanent
		EnableCards(cardCondition)
		EndCurrentPhase()
		FindSequences(Card[], CardFieldName)
				//GetTarget(string) : object
				//GetTargetCard(string cardDefinition) : Card
				//GetCards(Card[], Faction, CardType) : Card[]
				//GetLastCards(Card[], int) : Card[]
		FindInCardUsedHistoryThisTurn(string definition) : bool
		FindInCardUsedHistory(string definition) : bool
		Invoke(MethodName)
		LoseTheGame(Player)
		MoveCardToZone
		MoveCardInGrid
		RevealZoneCards(Zone,int)
		Shuffle(Zone)
		SendSignal(tag) // Send a simple signal to all modifiers and watchers which could interact with it
		StoreNumber(int)
		StoreCard(Card)
		UseAction(action)
		UseCard(Card,Modifier)
		WinTheGame(Player)

		//---custom commands (just a set of default commands)
		DealDamage(int)
		//In magic: creature(Thoughness(-X)) or player(score - X)

		//---one method for each ActionName---
		DrawCard(Player)TreatAction(Action)

		//---define for each ResourceType
		PayResource(ResourceType, int)
		CanPayResource(ResourceType, int) : bool
		*/

		//TODO MAX All Commands



		IEnumerator MoveCardToZoneOld (List<Card> c, Zone z, string[] additionalInfo = null)
		{
			if (c == null || c.Count == 0)
			{
				Debug.Log(BuildMessage("No cards found to be moved."));
				yield return null;
			}

			if (z == null)
			{
				Debug.LogWarning(BuildMessage("Moving card failed. No zone was selected to move cards to."));
				yield return null;
			}

			bool toBottom = false;
			Vector2Int? gridPos = null;
			for (int i = 0; i < c.Count; i++)
			{
				Zone oldZone = c[i].zone;
				if (oldZone != null)
				{
					oldZone.PopCard(c[i]);
					SetContext("movedCard", c[i], "targetZone", oldZone);
					yield return NotifyWatchers(TriggerTag.OnCardLeftZone, "movedCard", c[i], "targetZone", oldZone, "additionalInfo", additionalInfo);
					yield return NotifyModifiers(TriggerTag.OnCardLeftZone, "movedCard", c[i], "targetZone", oldZone, "additionalInfo", additionalInfo);
				}
				RevealStatus revealStatus = RevealStatus.ZoneDefinition;
				if (additionalInfo != null)
				{
					for (int j = 0; j < additionalInfo.Length; j++)
					{
						if (additionalInfo[j] == "Hidden" || additionalInfo[j] == "FaceDown")
						{
							revealStatus = RevealStatus.Hidden;
							break;
						}
						else if (additionalInfo[j] == "Revealed" || additionalInfo[j] == "FaceUp")
						{
							revealStatus = RevealStatus.RevealedToEveryone;
							break;
						}
						else if (additionalInfo[j] == "Bottom")
						{
							toBottom = true;
						}
						else if (additionalInfo[j].StartsWith("(") && z.zoneConfig == ZoneConfiguration.Grid) //A grid position
						{
							//We need to find a number value to the left and to the right of the comma as in (X,Y)
							float left = float.NaN, right = float.NaN;
							int commaIndex = additionalInfo[j].IndexOf(",");
							if (commaIndex == -1)
								Debug.LogWarning(BuildMessage("Couldn't find a value for grid position with ", additionalInfo[j]));
							else
							{
								string gridPosString = additionalInfo[j].Replace("(", "").Replace(")", "");
								commaIndex--; //commaIndex is now 1 less because of replaces
											  //But what if we have a clause to one of the sides? E.g.  ($(card(@Play),Power),$(card(@Discard),Power))
											  //We need to make sure that the comma we find is the right one and that we can extract the correct value from both sides.
								while (commaIndex != -1 && commaIndex < gridPosString.Length && (float.IsNaN(left) || float.IsNaN(right)))
								{
									//This goes by trial and error through the string until we can extract correct values or we reach the end of the string and no value can be found
									left = ExtractNumber(gridPosString.Substring(0, commaIndex));
									commaIndex++;
									if (commaIndex < gridPosString.Length)
										right = ExtractNumber(gridPosString.Substring(commaIndex));
									commaIndex = gridPosString.IndexOf(",", commaIndex);
								}

								if (!float.IsNaN(left) && !float.IsNaN(right))
								{
									gridPos = new Vector2Int((int)left, (int)right);
								}
								else
								{
									Debug.LogWarning(BuildMessage("DEBUG Something is wrong in grid position! ", gridPosString, " , ", left.ToString(), " , ", right.ToString(), " , ", commaIndex.ToString()));
								}
							}
						}
					}
				}
				if (gridPos == null)
					z.PushCard(c[i], revealStatus, toBottom);
				else
					z.PushCard(c[i], revealStatus, gridPos.Value);
				SetContext("movedCard", c[i], "targetZone", z, "oldZone", oldZone);
				yield return NotifyWatchers(TriggerTag.OnCardEnteredZone, "movedCard", c[i], "targetZone", z, "oldZone", oldZone, "additionalInfo", additionalInfo);
				yield return NotifyModifiers(TriggerTag.OnCardEnteredZone, "movedCard", c[i], "targetZone", z, "oldZone", oldZone, "additionalInfo", additionalInfo);
			}
			Debug.Log(BuildMessage("", c.Count.ToString(), " card", (c.Count > 1 ? "s" : ""), " moved."));
		}

		List<string> CreateTurnPhasesFromString (string phaseNamesList)
		{
			phaseNamesList = GetCleanStringForInstructions(phaseNamesList);
			List<string> phaseList = new List<string>();
			phaseList.AddRange(phaseNamesList.Split(','));
			return phaseList;
		}

		#endregion

		//==================================================================================================================
		#region Check & Get Methods ================================================================================================
		//==================================================================================================================

		internal Card GetCardVariable (string contextId)
		{
			if (variables.ContainsKey(contextId))
				return (Card)variables[contextId];
			Debug.LogWarning(StringUtility.BuildMessage("Context doesn't have card identified with: @", contextId));
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
			Debug.LogWarning(StringUtility.BuildMessage("Variable not found: @", variableName));
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

		public bool CheckCondition (string cond)
		{
			/*
			card
			phase
			zone
			modifier
			variable
			*/
			if (string.IsNullOrEmpty(cond))
				return true;

			cond = GetCleanStringForInstructions(cond);

			string[] condParts = cond.Split(';');
			//int partsFound = 0;

			bool result = true;

			foreach (string item in condParts) // each one of the conditions separated by ;
			{
				string op = GetOperator(item);
				if (op != "")
				{
					if (CompareWithOperator(item, op))
						continue;
					else
					{
						result = false;
						break;
					}
				}
				else
				{
					//string[] condBreakdown = ArgumentsBreakdown(item);

					//if (condBreakdown[0] == "card")
					//{
					//	if (SelectCards(condBreakdown, cards).Count > 0)
					//		continue;
					//	else
					//	{
					//		result = false;
					//		break;
					//	}
					//}
					//else if (condBreakdown[0] == "phase")
					//{
					//	if (condBreakdown[1] == CurrentTurnPhase)
					//		continue;
					//	else
					//	{
					//		result = false;
					//		break;
					//	}
					//}
					//else if (condBreakdown[0].StartsWith("mod"))
					//{
					//	if (SelectModifiers(condBreakdown, modifiers).Count > 0)
					//		continue;
					//	else
					//	{
					//		result = false;
					//		break;
					//	}
					//}
					//else
					//{
					Debug.LogWarning(BuildMessage("Condition (", cond, ") doesn't ask for a valid type (", item, ")"));
					return false;
					//}
				}
			}
			Debug.Log(BuildMessage("Condition ", cond, " found out to be ", result.ToString()));
			return result;
		}

		bool CompareWithOperator (string clause, string op)
		{

			int index = clause.IndexOf(op);
			int compIndex = index + op.Length;
			return Compare(clause.Substring(0, index), clause.Substring(compIndex, clause.Length - compIndex), op);
		}

		bool Compare (string clause)
		{
			string op = GetOperator(clause);
			if (op == "")
			{
				Debug.LogWarning(BuildMessage("Couldn't find an operator in clause ", clause, " when trying to Check Value"));
				return false;
			}
			return CompareWithOperator(clause, op);
		}

		bool Compare (string value, string comparer)
		{
			return Compare(value, comparer, "=");
		}

		float ExtractNumber (string s)
		{
			float value = float.NaN;
			if (s.StartsWith("$"))
			{
				value = GetValueFromCardFieldOrExpression(ArgumentsBreakdown(s));
			}
			else if (float.TryParse(s, out value))
			{
			}
			else if (s.StartsWith("card"))
			{
				List<Card> cardList = SelectCards(s);
				if (cardList.Count > 0)
				{
					value = cardList.Count;
				}
			}
			else if (variables.ContainsKey(s))
			{
				value = (float)variables[s];
			}
			//else if (variables.ContainsKey(s))
			//{
			//	value = (float)variables[s];
			//}
			else if (s.StartsWith("mod"))
			{
				List<Modifier> modList = SelectModifiers(s);
				if (modList.Count > 0)
				{
					value = modList.Count;
				}
			}
			else
			{
				value = float.NaN;
			}
			return value;
		}

		bool Compare (string value, string comparer, string op)
		{
			if (value == "phase")
				value = CurrentTurnPhase;
			if (comparer == "phase")
				comparer = CurrentTurnPhase;

			float v = ExtractNumber(value);
			float c = ExtractNumber(comparer);

			bool numbers = !float.IsNaN(v) && !float.IsNaN(c);

			switch (op)
			{
				case "!=":
					if (numbers) return v != c;
					return value != comparer;
				case "<=":
					if (numbers)
						return v <= c;
					else
						Debug.Log(BuildMessage("Comparer argument failure."));
					break;
				case ">=":
					if (numbers)
						return v >= c;
					else
						Debug.Log(BuildMessage("Comparer argument failure."));
					break;
				case "<":
					if (numbers)
						return v < c;
					else
						Debug.Log(BuildMessage("Comparer argument failure."));
					break;
				case ">":
					if (numbers)
						return v > c;
					else
						Debug.Log(BuildMessage("Comparer argument failure."));
					break;
				case "=":
					if (comparer.Contains("|"))
					{
						string[] subcomparers = comparer.Split('|');
						int counter = 0;
						foreach (string item in subcomparers)
						{
							if (Compare(value, item))
								counter++;
						}
						if (counter > 0)
							return true;
					}
					if (comparer.Contains("-"))
					{
						string[] subcomparers = comparer.Split('-');
						float d2 = 0;
						bool correct = float.TryParse(subcomparers[0], out float d1) && float.TryParse(subcomparers[1], out d2);
						if (subcomparers.Length > 2 || !correct)
						{
							Debug.LogWarning(BuildMessage("Sintax error: ", comparer));
							return false;
						}
						return v >= d1 && v <= d2;
					}
					if (numbers)
						return v == c;
					return value == comparer;
				default:
					Debug.LogWarning(BuildMessage("Unknown operator."));
					break;
			}
			return false;
		}

		bool CheckContent (Card card, string poolSelection)
		{
			List<Card> selection = SelectCards(poolSelection);
			if (selection != null && selection.Contains(card))
				return true;
			return false;
		}

		bool CheckContent (Zone zone, string poolSelection)
		{
			List<Zone> selection = SelectZones(poolSelection);
			if (selection != null && selection.Contains(zone))
				return true;
			return false;
		}

		bool CheckContent (Modifier modifier, string poolSelection)
		{
			//TODO MED
			return false;
		}

		/*
		// Triggers ====================================
			OnActionUsed (actionName)
			OnCardEnteredZone (card, zone, oldZone)
			OnCardLeftZone (card, zone)
		OnCardFieldChanged (card, field, numValue, textValue)
			OnCardUsed (card)
		OnMatchEnded (winner, matchNumber)
			OnMatchSetup (matchNumber)
			OnMatchStarted (matchNumber)
		OnModifierChanged (modifier)
			OnPhaseEnded (turnPhase)
			OnPhaseStarted (turnPhase)
		OnSignalReceived
			OnTurnEnded (turnNumber)
			OnTurnStarted (turnNumber)

		TODO MIN Create Lists of Modifiers for each trigger, register modifiers to them, and only call the ones that make reference to that specific trigger
		*/

		bool CheckTriggerWithArguments (string modTrigger, TriggerTag tag, params object[] args)
		{
			if (string.IsNullOrEmpty(modTrigger))
				return false;

			modTrigger = GetCleanStringForInstructions(modTrigger);

			string[] subtriggers = modTrigger.Split(';');

			foreach (string subtrigger in subtriggers)
			{
				if (subtrigger.StartsWith(tag.ToString()))// So this is a trigger found
				{
					if (args == null || args.Length == 0) //The trigger point doesn't come with arguments
						return true;

					string[] subtrigBreakdown = ArgumentsBreakdown(subtrigger, true);

					if (subtrigBreakdown.Length == 1) //The trigger does not care about arguments
						return true;

					switch (subtrigBreakdown[0])
					{
						case "OnCardClicked":
						case "OnCardUsed":
							Card c = (Card)args[1];
							string parameters = subtrigBreakdown[1];
							if (!parameters.Contains("clickedCard") && !parameters.Contains("usedCard"))
								parameters = "card(" + parameters + ")";
							List<Card> selection = SelectCards(parameters);
							if (selection != null && selection.Contains(c))
								return true;
							break;
						case "OnCardEnteredZone":
						case "OnCardLeftZone":
							Card c2 = (Card)args[1];
							Zone z = (Zone)args[3];
							//Zone oldZone = args.Length > 4 ? (Zone)args[5] : null;
							subtrigBreakdown = ArgumentsBreakdown(subtrigger);
							int parts = subtrigBreakdown.Length - 1;
							for (int i = 1; i < subtrigBreakdown.Length; i++)
							{
								if ((subtrigBreakdown[i].StartsWith("movedCard") && CheckContent(c2, subtrigBreakdown[i])) |
									(subtrigBreakdown[i].StartsWith("zone") && CheckContent(z, subtrigBreakdown[i])))
									parts--;
								//else if (subtrigBreakdown[i].Contains("oldZone"))
								//{
								//	string oldZoneSelection = subtrigBreakdown[i].Replace("oldZone", "zone");
								//	if (CheckContent(oldZone, oldZoneSelection))
								//		parts--;
								//}
								else if (subtrigBreakdown[i].Substring(0, 1) == "@")
								{
									string zoneSelection = "zone(" + subtrigBreakdown[i] + ")";
									if (CheckContent(z, zoneSelection))
										parts--;
								}
								//else
								//	return false;
							}
							return parts == 0;
						case "OnActionUsed":
						case "OnMatchSetup":
						case "OnPhaseEnded":
						case "OnPhaseStarted":
						case "OnTurnEnded":
						case "OnTurnStarted":
						case "OnMatchEnded":
						case "OnMatchStarted":
						case "OnMessageSent":
							if (Compare((string)args[1], subtrigBreakdown[1]))
								return true;
							break;
						case "OnModifierValueChanged":
							Modifier mod = (Modifier)args[1];
							subtrigBreakdown = ArgumentsBreakdown(subtrigger);
							List<Modifier> modSelection = SelectModifiers(subtrigBreakdown[1]);
							if (modSelection.Contains(mod))
							{
								float newValue = (float)args[3];
								float oldValue = (float)args[5];
								subtrigBreakdown[2] = subtrigBreakdown[2].Replace("newValue", newValue.ToString()).Replace("oldValue", oldValue.ToString());
								if (Compare(subtrigBreakdown[2]))
									return true;
							}
							break;
						default:
							Debug.LogWarning(BuildMessage("Trigger call not found: ", subtrigBreakdown[0]));
							break;
					}
				}
			}

			return false;
		}

		public List<Card> SelectCards (string clause)
		{
			return SelectCards(ArgumentsBreakdown(clause), cards);
		}

		List<Card> SelectCards (string[] clauseArray, Card[] fromPool)
		{
			if (fromPool == null)
			{
				Debug.LogWarning(BuildMessage("Error: the pool of cards to be selected with condition (", PrintStringArray(clauseArray), ") is null."));
				return null;
			}

			if (clauseArray == null || (clauseArray[0] != "card" && clauseArray[0] != "all"))
			{
				Debug.LogWarning(BuildMessage("Syntax error on the selection of cards. The correct syntax is: 'card(argument1,argument2,...)'. Clause: ", PrintStringArray(clauseArray)));
				return null;
			}

			List<Card> selection = new List<Card>();
			bool selectionEnded = false;

			selection.AddRange(fromPool);

			if (clauseArray.Length == 1)
			{
				selectionEnded = true;
			}

			if (!selectionEnded)
			{
				for (int i = 1; i < clauseArray.Length; i++)
				{
					selection = SelectCardsSingleCondition(clauseArray[i], selection.ToArray());
				}
			}

			if (selection == null || selection.Count == 0)
				Debug.Log(BuildMessage("No cards found with conditions ", PrintStringArray(clauseArray)));
			return selection;
		}

		List<Card> SelectCardsSingleCondition (string condition, Card[] fromPool)
		{
			string searchType = condition.Substring(0, 1);
			if (!"@z#i%m~tfx".Contains(searchType))
			{
				Debug.LogWarning(BuildMessage("Syntax error with condition ", condition, ". Search character ", searchType, " is not valid."));
				return null;
			}
			string identifier = condition.Substring(1);
			char[] identifierChar = identifier.ToCharArray();
			//bool hasParenthesis = false;
			bool hasOr = false;
			bool hasAnd = false;
			int insideParenthesis = 0;

			for (int i = 0; i < identifierChar.Length; i++)
			{
				switch (identifierChar[i])
				{
					case '(':
						insideParenthesis++;
						//hasParenthesis = true;
						break;
					case ')':
						insideParenthesis--;
						break;
					case '|':
						if (insideParenthesis == 0) hasOr = true;
						break;
					case '&':
						if (insideParenthesis == 0) hasAnd = true;
						break;
				}
			}
			List<Card> selection = new List<Card>();

			if (hasOr)
			{
				string[] orBreakdown = identifier.Split('|');
				selection = SelectCardsSingleCondition(searchType + orBreakdown[0], fromPool);
				List<Card> tempCardSelector = null;
				for (int i = 1; i < orBreakdown.Length; i++)
				{
					tempCardSelector = SelectCardsSingleCondition(searchType + orBreakdown[i], fromPool);
					for (int j = 0; j < tempCardSelector.Count; j++)
					{
						if (!selection.Contains(tempCardSelector[j]))
							selection.Add(tempCardSelector[j]);
					}
				}
				return selection;
			}

			if (hasAnd)
			{
				string[] andBreakdown = identifier.Split('&');
				selection = SelectCardsSingleCondition(searchType + andBreakdown[0], fromPool);
				for (int i = 1; i < andBreakdown.Length; i++)
				{
					selection = SelectCardsSingleCondition(searchType + andBreakdown[i], selection.ToArray());
				}
				return selection;
			}

			bool equals = true;
			if (identifier.Substring(0, 1) == "!")
			{
				equals = false;
				identifier = identifier.Substring(1);
			}

			if (variables.ContainsKey(identifier))
			{
				if (variables[identifier].GetType() == typeof(Card))
					identifier = ((Card)variables[identifier]).ID;
				//else if (variables[identifier].GetType() == typeof(Player))
				//	identifier = ((Player)variables[identifier]).id;
				else if (variables[identifier].GetType() == typeof(Modifier))
					identifier = ((Modifier)variables[identifier]).id;
				else if (variables[identifier].GetType() == typeof(Zone))
					identifier = ((Zone)variables[identifier]).ID;
			}

			//Player keywords
			//if (searchType == "*" || searchType == "p" || searchType == "o")
			//{
			//	if (identifier == "active")
			//		identifier = players[activePlayer].id;
			//	else if ("0123456789".Contains(identifier.Substring(0, 1)) && int.TryParse(identifier, out int playerIndex) && playerIndex < players.Length)
			//		identifier = players[playerIndex].id;
			//}

			switch (searchType)
			{
				case "@": //zone
				case "z":
					for (int i = 0; i < fromPool.Length; i++)
					{
						if (fromPool[i].zone != null && ((equals && fromPool[i].zone.zoneTags == identifier) || (!equals && fromPool[i].zone.zoneTags != identifier)))
							selection.Add(fromPool[i]);
					}
					break;
				//case "*": //controller player
				//case "p":
				//	for (int i = 0; i < fromPool.Length; i++)
				//	{
				//		if (fromPool[i].controller != null && ((equals && fromPool[i].controller.id == identifier) || (!equals && fromPool[i].controller.id != identifier)))
				//			selection.Add(fromPool[i]);
				//	}
				//	break;
				//case "o": //owner player
				//	for (int i = 0; i < fromPool.Length; i++)
				//	{
				//		if (fromPool[i].owner != null && ((equals && fromPool[i].owner.id == identifier) || (!equals && fromPool[i].owner.id != identifier)))
				//			selection.Add(fromPool[i]);
				//	}
				//	break;
				case "#": //card id
				case "i":
					for (int i = 0; i < fromPool.Length; i++)
					{
						if ((equals && fromPool[i].ID == identifier) || (!equals && fromPool[i].ID != identifier))
							selection.Add(fromPool[i]);
					}
					break;
				case "%": //condition name or tag
				case "m":
					string[] breakdownForMod = ArgumentsBreakdown(identifier);
					for (int i = 0; i < fromPool.Length; i++)
					{
						for (int j = 0; j < fromPool[i].Modifiers.Count; j++)
						{
							if ((equals && fromPool[i].Modifiers[j].tags.Contains(breakdownForMod[0])) || (!equals && !fromPool[i].Modifiers[j].tags.Contains(breakdownForMod[0])))
							{
								selection.Add(fromPool[i]);
								break;
							}
						}
					}
					break;
				case "~": //card tag
				case "t":
					for (int i = 0; i < fromPool.Length; i++)
					{
						if ((equals && fromPool[i].data.tags.Contains(identifier)) || (!equals && !fromPool[i].data.tags.Contains(identifier)))
							selection.Add(fromPool[i]);
					}
					break;
				case "f": //card field
						  //TEST
					string[] breakdownForFields = ArgumentsBreakdown(identifier);
					for (int i = 0; i < fromPool.Length; i++)
					{
						for (int j = 0; j < fromPool[i].data.fields.Count; j++)
						{
							if (fromPool[i].data.fields[j].fieldName == breakdownForFields[0])
							{
								switch (fromPool[i].data.fields[j].dataType)
								{
									case CardFieldDataType.Text:
										if (Compare(fromPool[i].data.fields[j].stringValue, breakdownForFields[1]))
											selection.Add(fromPool[i]);
										break;
									case CardFieldDataType.Number:
										if (Compare(fromPool[i].data.fields[j].numValue.ToString(), breakdownForFields[1]))
											selection.Add(fromPool[i]);
										break;
								}
							}
						}
					}
					break;
				case "x": //quantity
					System.Array.Sort(fromPool, CompareCardsByIndexForSorting);
					if (!int.TryParse(identifier, out int qty))
					{
						Debug.LogError(BuildMessage("The value following the x in (", searchType, identifier, ") must be a number."));
						return null;
					}
					for (int i = 1; i <= qty; i++)
					{
						if (fromPool.Length - i >= 0)
							selection.Add(fromPool[fromPool.Length - i]);
					}
					//for (int i = 0; i < qty; i++)
					//{
					//	if (i < fromPool.Length)
					//		selection.Add(fromPool[i]);
					//}
					break;
			}
			return selection;
		}

		public List<Zone> SelectZones (string clause)
		{
			string[] clauseBreakdown = ArgumentsBreakdown(clause);
			return SelectZones(clauseBreakdown, zones);
		}

		List<Zone> SelectZones (string[] clauseArray, Zone[] fromPool)
		{
			if (fromPool == null)
			{
				Debug.LogWarning(BuildMessage("Error: the pool of zones to be selected with condition (", PrintStringArray(clauseArray), ") is null."));
				return null;
			}

			if (clauseArray == null)
			{
				Debug.LogWarning(BuildMessage("Error: the search zone is null."));
				return null;
			}

			if (clauseArray[0] != "zone" && clauseArray[0] != "all")
			{
				string start = clauseArray[0].Substring(0, 1);
				if ("@z".Contains(start))
					return SelectZonesSingleCondition(clauseArray[0], fromPool);
				else
					Debug.LogWarning(BuildMessage("Syntax error: the zone definition need to be in the form: zone(argument1, argument2, ...) or @ZoneType"));
				return null;
			}

			List<Zone> selection = new List<Zone>();
			bool selectionEnded = false;

			selection.AddRange(fromPool);

			if (clauseArray.Length == 1)
			{
				selectionEnded = true;
			}

			if (!selectionEnded)
			{
				for (int i = 1; i < clauseArray.Length; i++)
				{
					selection = SelectZonesSingleCondition(clauseArray[i], selection.ToArray());
				}
			}

			if (selection == null || selection.Count == 0)
				Debug.LogWarning(BuildMessage("No zones found with conditions ", PrintStringArray(clauseArray)));
			return selection;
		}

		List<Zone> SelectZonesSingleCondition (string condition, Zone[] fromPool)
		{
			string searchType = condition.Substring(0, 1);
			if (!"@z#i*p".Contains(searchType))
			{
				Debug.LogError(BuildMessage("Syntax error zone selecting with condition ", condition, ". Search character ", searchType, " is not valid."));
				return null;
			}
			string identifier = condition.Substring(1);
			bool hasOr = identifier.Contains("|");
			bool hasAnd = identifier.Contains("&");

			List<Zone> selection = new List<Zone>();

			if (hasOr)
			{
				string[] orBreakdown = identifier.Split('|');
				selection = SelectZonesSingleCondition(searchType + orBreakdown[0], fromPool);
				List<Zone> tempZoneSelector = null;
				for (int i = 1; i < orBreakdown.Length; i++)
				{
					tempZoneSelector = SelectZonesSingleCondition(searchType + orBreakdown[i], fromPool);
					for (int j = 0; j < tempZoneSelector.Count; j++)
					{
						if (!selection.Contains(tempZoneSelector[j]))
							selection.Add(tempZoneSelector[j]);
					}
				}
				return selection;
			}

			if (hasAnd)
			{
				string[] andBreakdown = identifier.Split('&');
				selection = SelectZonesSingleCondition(searchType + andBreakdown[0], fromPool);
				for (int i = 1; i < andBreakdown.Length; i++)
				{
					selection = SelectZonesSingleCondition(searchType + andBreakdown[i], selection.ToArray());
				}
				return selection;
			}

			bool equals = true;
			if (identifier.Substring(0, 1) == "!")
			{
				equals = false;
				identifier = identifier.Substring(1);
			}

			if (variables.ContainsKey(identifier))
			{
				if (variables[identifier].GetType() == typeof(Card))
					identifier = ((Card)variables[identifier]).ID;
				//else if (variables[identifier].GetType() == typeof(Player))
				//	identifier = ((Player)variables[identifier]).id;
			}

			//Player keywords
			//if (searchType == "*" || searchType == "p")
			//{
			//	if (identifier == "active")
			//		identifier = players[activePlayer].id;
			//	else if ("0123456789".Contains(identifier.Substring(0, 1)) && int.TryParse(identifier, out int playerIndex) && playerIndex < players.Length)
			//	{
			//		identifier = players[playerIndex].id;
			//	}
			//}

			switch (searchType)
			{
				case "@": //zone tag
				case "z":
					for (int i = 0; i < fromPool.Length; i++)
					{
						if ((equals && fromPool[i].zoneTags == identifier) || (!equals && fromPool[i].zoneTags != identifier))
							selection.Add(fromPool[i]);
					}
					break;
				//case "*": //controller player
				//case "p":
				//	for (int i = 0; i < fromPool.Length; i++)
				//	{
				//		if (fromPool[i].controller != null && ((equals && fromPool[i].controller.id == identifier) || (!equals && fromPool[i].controller.id != identifier)))
				//			selection.Add(fromPool[i]);
				//	}
				//	break;
				case "#": //zone id
				case "i":
					for (int i = 0; i < fromPool.Length; i++)
					{
						if ((equals && fromPool[i].ID == identifier) || (!equals && fromPool[i].ID != identifier))
							selection.Add(fromPool[i]);
					}
					break;
			}
			return selection;
		}

		public List<Modifier> SelectModifiers (string clause)
		{
			string[] clauseBreakdown = ArgumentsBreakdown(clause);
			return SelectModifiers(clauseBreakdown, modifiers);
		}

		List<Modifier> SelectModifiers (string[] clauseArray, List<Modifier> fromPool)
		{
			if (fromPool == null)
			{
				Debug.LogWarning(BuildMessage("Pool of modifiers with condition (", PrintStringArray(clauseArray), ") is null."));
				return null;
			}

			if (clauseArray == null)
			{
				Debug.LogWarning(BuildMessage("Error: clause array of modifiers is null."));
				return null;
			}

			if (clauseArray[0] != "modifier" && clauseArray[0] != "mod" && clauseArray[0] != "all")
			{
				string start = clauseArray[0].Substring(0, 1);
				if ("%m".Contains(start))
					return SelectModifiersSingleCondition(clauseArray[0], fromPool);
				else
					Debug.LogWarning(BuildMessage("Syntax error: the modifier definition need to be in the form: modifier(argument1, argument2, ...) or mod(argument1, argument2, ...) or %ModifierTag."));
				return null;
			}

			List<Modifier> selection = new List<Modifier>();
			bool selectionEnded = false;

			selection.AddRange(fromPool);

			if (clauseArray.Length == 1)
			{
				selectionEnded = true;
			}

			if (!selectionEnded)
			{
				for (int i = 1; i < clauseArray.Length; i++)
				{
					selection = SelectModifiersSingleCondition(clauseArray[i], selection);
				}
			}

			return selection;
		}

		List<Modifier> SelectModifiersSingleCondition (string condition, List<Modifier> fromPool)
		{
			string searchType = condition.Substring(0, 1);
			if (!"%m#i*pxc".Contains(searchType))
			{
				Debug.LogError(BuildMessage("Syntax error zone selecting with condition ", condition, ". Search character ", searchType, " is not valid."));
				return null;
			}
			string identifier = condition.Substring(1);
			bool hasOr = identifier.Contains("|");
			bool hasAnd = identifier.Contains("&");

			List<Modifier> selection = new List<Modifier>();

			if (hasOr)
			{
				string[] orBreakdown = identifier.Split('|');
				selection = SelectModifiersSingleCondition(searchType + orBreakdown[0], fromPool);
				List<Modifier> tempModifierSelector = null;
				for (int i = 1; i < orBreakdown.Length; i++)
				{
					tempModifierSelector = SelectModifiersSingleCondition(searchType + orBreakdown[i], fromPool);
					for (int j = 0; j < tempModifierSelector.Count; j++)
					{
						if (!selection.Contains(tempModifierSelector[j]))
							selection.Add(tempModifierSelector[j]);
					}
				}
				return selection;
			}

			if (hasAnd)
			{
				string[] andBreakdown = identifier.Split('&');
				selection = SelectModifiersSingleCondition(searchType + andBreakdown[0], fromPool);
				for (int i = 1; i < andBreakdown.Length; i++)
				{
					selection = SelectModifiersSingleCondition(searchType + andBreakdown[i], selection);
				}
				return selection;
			}

			//NOT
			bool equals = true;
			if (identifier.Substring(0, 1) == "!")
			{
				equals = false;
				identifier = identifier.Substring(1);
			}

			if (variables.ContainsKey(identifier))
			{
				string temp = identifier;
				if (variables[identifier].GetType() == typeof(Card))
					identifier = ((Card)variables[identifier]).ID;
			}

			//Value substitution
			if (identifier.StartsWith("$"))
			{
				identifier = GetValueFromCardFieldOrExpression(ArgumentsBreakdown(identifier)).ToString();
			}

			switch (searchType)
			{
				//TODO MED Other targets: card and modifier
				case "c":
					List<Card> cardList = SelectCards("card(" + identifier + ")");
					for (int i = 0; i < cardList.Count; i++)
					{
						selection.AddRange(cardList[i].Modifiers);
					}
					break;

				case "%":
				case "m":
					for (int i = 0; i < fromPool.Count; i++)
					{
						if ((equals && fromPool[i].tags.Contains(identifier)) || (!equals && !fromPool[i].tags.Contains(identifier)))
							selection.Add(fromPool[i]);
					}
					break;
				case "#": //modifier id
				case "i":
					for (int i = 0; i < fromPool.Count; i++)
					{
						if ((equals && fromPool[i].id == identifier) || (!equals && fromPool[i].id != identifier))
							selection.Add(fromPool[i]);
					}
					break;
				case "x": //quantity
					string op = GetOperator(identifier);
					if (op != "")
						identifier = identifier.Replace(op, "");
					if (!int.TryParse(identifier, out int qty))
					{
						Debug.LogError(BuildMessage("The value following the x in (", searchType, op, identifier, ") must be a number."));
						return null;
					}
					List<Modifier> tempSelection = new List<Modifier>();
					for (int i = 1; i <= qty; i++)
					{
						if (fromPool.Count - i >= 0)
							tempSelection.Add(fromPool[fromPool.Count - i]);
						else
							break;
					}
					if (Compare(tempSelection.Count.ToString(), identifier, op))
						selection.AddRange(tempSelection);
					break;
			}

			return selection;
		}

		protected float foo = 0;

		#endregion

		//==================================================================================================================
		#region Helper Methods ==================================================================================================
		//==================================================================================================================



		int CompareCardsByIndexForSorting (Card c1, Card c2)
		{
			if (c1.zone != null && c2.zone != null)
			{
				if (c1.zone.Content.IndexOf(c1) < c2.zone.Content.IndexOf(c2))
					return -1;
				if (c1.zone.Content.IndexOf(c1) > c2.zone.Content.IndexOf(c2))
					return 1;
			}
			return 0;
		}

		string GetOperator (string value)
		{
			if (value.Contains("!="))
				return "!=";
			if (value.Contains("<="))
				return "<=";
			if (value.Contains(">="))
				return ">=";
			if (value.Contains("="))
				return "=";
			if (value.Contains("<"))
				return "<";
			if (value.Contains(">"))
				return ">";
			return "";
		}



		string[] ArgumentsBreakdown (string clause, bool onlyParenthesis = false)
		{
			clause = clause.Replace(" ", "");
			char[] clauseChar = clause.ToCharArray();
			List<string> result = new List<string>();
			string sub = "";
			int lastSubStartIndex = 0;
			int parCounter = 0;
			for (int i = 0; i < clauseChar.Length; i++)
			{
				switch (clauseChar[i])
				{
					case '(':
						if (parCounter == 0)
						{
							sub = clause.Substring(lastSubStartIndex, i - lastSubStartIndex);
							result.Add(sub);
							lastSubStartIndex = i + 1;
						}
						parCounter++;
						break;
					case ',':
						if (parCounter == 1 && !onlyParenthesis)
						{
							sub = clause.Substring(lastSubStartIndex, i - lastSubStartIndex);
							result.Add(sub);
							lastSubStartIndex = i + 1;
						}
						break;
					case ')':
						parCounter--;
						if (parCounter == 0)
						{
							sub = clause.Substring(lastSubStartIndex, i - lastSubStartIndex);
							result.Add(sub);
							lastSubStartIndex = i + 1;
						}
						break;
					default:
						if (i == clauseChar.Length - 1)
						{
							sub = clause.Substring(lastSubStartIndex, i - lastSubStartIndex + 1);
							result.Add(sub);
						}
						continue;
				}
			}
			string[] resultArray = result.ToArray();
			return resultArray;
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

		int GetEndOfFirstParenthesis (string clause, int start)
		{
			int counter = 0;
			for (int i = start; i < clause.Length; i++)
			{
				if (clause[i] == '(')
					counter++;
				else if (clause[i] == ')')
				{
					counter--;
					if (counter == 0)
						return i;
				}
			}
			return -1;
		}

		string GetCleanStringForInstructions (string s)
		{
			return s.Replace(" ", "").Replace(System.Environment.NewLine, "").Replace("\n", "").Replace("\n\r", "").Replace("\\n", "").Replace("\\n\\r", "");
		}

		string PrintStringArray (string[] str, bool inBrackets = true)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < str.Length; i++)
			{
				if (inBrackets) sb.Append(i + "{ ");
				sb.Append(str[i]);
				if (inBrackets) sb.Append(" }  ");
			}
			return sb.ToString();
		}

		StringBuilder logMessageCreator = new StringBuilder();

		string BuildMessage (params string[] msgParts)
		{
			logMessageCreator.Clear();
			logMessageCreator.Append("CGEngine: ");
			logMessageCreator.Append(logIdentation);
			foreach (string item in msgParts)
			{
				logMessageCreator.Append(item);
			}
			return logMessageCreator.ToString();
		}

		public void TreatEvent (string type, InputObject inputObject)
		{
			Card c = inputObject.GetComponent<Card>();
			if (c) ClickCard(c);
		}

		#endregion
	}

}