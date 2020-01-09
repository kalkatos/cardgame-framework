using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CGEngine
{
	/// <summary>
	/// Holds information about the current match and executes commands upon itself
	/// </summary>
	public class Match : MonoBehaviour
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
		Player[] players;
		Dictionary<string, Player> playersByID;
		PlayerRules[] playerRules;
		Zone[] zones;
		//List<Zone> neutralZones; //References to zones
		List<string> neutralResourcePools;
		List<Modifier> modifiers;
		Dictionary<string, double> customVariables;
		//DEBUG
		double playerHP;
		//TODO Clamp rules Dictionary<string, string> customVariablesClamp;
		//List<Card> cardSelector;
		//List<Player> playerSelector;
		//List<Zone> zoneSelector;
		Dictionary<string, object> context;

		bool isSimulation;
		int turnNumber;
		List<string> actionHistory;
		int activePlayer;
		bool gameEnded;
		bool endCurrentPhase;
		List<TurnPhase> currentTurnPhases;
		string externalSetEffect = null;
		Transform modifierContainer;
		Player winner = null;
		List<TurnPhase> subphases = null;
		bool endSubphaseLoop;
		double valueForNextEffect;

		//==================================================================================================================
		#region Initialization Methods ==================================================================================================
		//==================================================================================================================

		public void Initialize (Ruleset rules)
		{
			Current = this;
			this.rules = rules;
			//cardSelector = new List<Card>();
			//playerSelector = new List<Player>();
			//zoneSelector = new List<Zone>();
			context = new Dictionary<string, object>();
			customVariables = new Dictionary<string, double>();

			//Setup Players
			players = FindObjectsOfType<Player>();
			if (players == null)
			{
				Debug.LogError("CGEngine: Error: No players found in Match Scene.");
			}
			else
			{
				playerRules = new PlayerRules[players.Length];
				playersByID = new Dictionary<string, Player>();
				for (int i = 0; i < players.Length; i++)
				{
					players[i].id = "p" + (++playerIdTracker).ToString().PadLeft(2, '0');
					playersByID.Add(players[i].id, players[i]);
					playerRules[i] = players[i].playerRules;
				}
			}

			//Setup Cards
			cards = FindObjectsOfType<Card>();
			cardsByID = new Dictionary<string, Card>();
			for (int i = 0; i < cards.Length; i++)
			{
				cards[i].ID = "c" + (++cardIdTracker).ToString().PadLeft(4, '0');
				cardsByID.Add(cards[i].ID, cards[i]);
			}

			//Setup Zones
			zones = FindObjectsOfType<Zone>();
			if (zones == null)
			{
				Debug.LogError("CGEngine: Error: No zones found in Match Scene.");
			}
			else
			{
				for (int i = 0; i < zones.Length; i++)
				{
					zones[i].id = "z" + (++zoneIdTracker).ToString().PadLeft(2, '0');
					Card[] cardsAtZone = zones[i].GetComponentsInChildren<Card>();
					if (cardsAtZone != null)
					{
						for (int j = 0; j < cardsAtZone.Length; j++)
						{
							zones[i].PushCard(cardsAtZone[j]);
						}
					}
				}
			}

			if (!FindObjectOfType<CardMover>())
			{
				GameObject cardMover = new GameObject("CardMover");
				cardMover.AddComponent<CardMover>();
			}

			SetupModifiers();
			SetupWatchers();
			GetStartingPlayer();
			StartCoroutine(MatchLoop());
		}

		void SetupModifiers ()
		{
			modifiers = new List<Modifier>();
			if (rules.matchModifiers != null)
			{
				modifierContainer = new GameObject("ModifierContainer").transform;
				foreach (ModifierData data in rules.matchModifiers)
				{
					CreateModifier(data);
				}
			}

			//for (int i = 0; i < cards.Length; i++)
			//{
			//	if (cards[i].Modifiers != null)
			//		modifiers.AddRange(cards[i].Modifiers);

			//	if (cards[i].data && cards[i].data.cardModifiers != null && cards[i].data.cardModifiers.Length > 0)
			//	{
			//		foreach (ModifierData data in cards[i].data.cardModifiers)
			//		{
			//			cards[i].AddModifiers(CreateModifier(data));
			//		}
			//	}
			//}
		}

		void SetupWatchers ()
		{
			MatchWatcher[] watchers = FindObjectsOfType<MatchWatcher>();
			if (watchers != null)
				Watchers.AddRange(watchers);
		}

		void GetStartingPlayer ()
		{
			switch (rules.starter)
			{
				case Starter.Random:
					activePlayer = Random.Range(0, players.Length);
					break;
				case Starter.FirstInList:
					//TODO MIN
					break;
				case Starter.SpecificRole:
					//TODO MIN
					break;
				case Starter.SpecificTeam:
					//TODO MIN
					break;
			}
		}

		#endregion

		//==================================================================================================================
		#region Core Methods ==================================================================================================
		//==================================================================================================================

		IEnumerator MatchLoop()
		{
			yield return MatchSetup();
			yield return StartMatch();
			while (!gameEnded)
			{
				yield return StartTurn();
				currentTurnPhases = new List<TurnPhase>(playerRules[activePlayer].turnStructure);
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
				activePlayer = GetNextPlayer();
			}
			yield return EndMatch();
		}

		/*
		// Triggers ====================================
		OnActionUsed (playerId, actionName)
		OnCardDrawn (playerId, cardId)
		OnCardEnteredZone (cardId, zoneId)
		OnCardLeftZone(cardId,zoneId)
		OnCardFieldChanged (cardId, fieldTag, numValue, textValue)
		OnCardUsed (card)
		OnMatchEnded (winnerId, matchNumber)
		OnMatchSetup (matchNumber)
		OnMatchStarted (matchNumber)
		OnModifierChanged (modifierId)
		OnPhaseEnded (playerId, turnPhase)
		OnPhaseStarted (playerId, turnPhase)
		OnTurnEnded (playerId, turnNumber)
		OnTurnStarted (playerId, turnNumber)
		*/

		IEnumerator MatchSetup ()
		{
			Debug.Log("CGEngine: --- Match " + id + " Setup --- Trigger: OnMatchSetup");
			yield return NotifyWatchers("OnMatchSetup", "matchNumber", matchNumber);
			yield return NotifyModifiers("OnMatchSetup", "matchNumber", matchNumber);
		}

		IEnumerator StartMatch()
		{
			Debug.Log("CGEngine: --- Match " + id + " Started --- Trigger: OnMatchStarted");
			yield return NotifyWatchers("OnMatchStarted", "matchNumber", matchNumber);
			yield return NotifyModifiers("OnMatchStarted", "matchNumber", matchNumber);
		}

		IEnumerator StartTurn()
		{
			turnNumber++;
			Debug.Log("CGEngine: --- Starting turn " + turnNumber + ". Active player: " + players[activePlayer].name + " (" + players[activePlayer].id + ") - Trigger: OnTurnStarted");
			yield return NotifyWatchers("OnTurnStarted", "activePlayer", players[activePlayer]);
			yield return NotifyModifiers("OnTurnStarted", "activePlayer", players[activePlayer]);
		}
		
		public IEnumerator StartPhase(TurnPhase phase)
		{
			CurrentTurnPhase = phase.name;
			endCurrentPhase = false;
			endSubphaseLoop = false;
			externalSetEffect = null;
			Debug.Log("CGEngine:       Phase " + phase.name + " started.");
			yield return NotifyWatchers("OnPhaseStarted", "phaseName", phase.name, "activePlayer", players[activePlayer], "phaseObject", phase);
			yield return NotifyModifiers("OnPhaseStarted", "phaseName", phase.name, "activePlayer", players[activePlayer], "phaseObject", phase);
		}

		public void UseAction(string action)
		{
			TreatEffect("UseAction(" + action + ")");
		}

		IEnumerator UseActionRoutine(string action)
		{
			Debug.Log("CGEngine: ~~~~~~~ ACTION used: "+action);
			yield return NotifyWatchers("OnActionUsed", "actionName", action);
			yield return NotifyModifiers("OnActionUsed", "actionName", action);
		}

		public void UseCard(Card c)
		{
			TreatEffect("UseCard(card(#"+c.ID+"))");
		}

		IEnumerator UseCardRoutine (Card c)
		{
			Debug.Log("CGEngine: - - - - - - Card " + (c.data ? c.data.name : c.name) + " used.");
			context.Clear();
			context.Add("cardUsed", c);
			context.Add("cardController", c.controller);
			yield return NotifyWatchers("OnCardUsed", "card", c);
			yield return NotifyModifiers("OnCardUsed", "card", c);
		}

		public void ClickCard(Card c)
		{
			TreatEffect("ClickCard(card(#" + c.ID + "))");
		}

		IEnumerator ClickCardRoutine(Card c)
		{
			Debug.Log("CGEngine: ******* Card " + (c.data ? c.data.name : c.name) + " clicked.");
			context.Clear();
			context.Add("cardClicked", c);
			context.Add("cardController", c.controller);
			yield return NotifyWatchers("OnCardClicked", "card", c);
			yield return NotifyModifiers("OnCardClicked", "card", c);
		}

		IEnumerator EndPhase(TurnPhase phase)
		{
			yield return NotifyWatchers("OnPhaseEnded", "phaseName", phase.name, "activePlayer", players[activePlayer]);
			yield return NotifyModifiers("OnPhaseEnded", "phaseName", phase.name, "activePlayer", players[activePlayer]);
		}

		IEnumerator EndTurn()
		{
			yield return NotifyWatchers("OnTurnEnded", "activePlayer", players[activePlayer]);
			yield return NotifyModifiers("OnTurnEnded", "activePlayer", players[activePlayer]);
		}

		IEnumerator EndMatch()
		{
			yield return NotifyWatchers("OnMatchEnded", "matchNumber", matchNumber, "winner", winner);
			yield return NotifyModifiers("OnMatchEnded", "matchNumber", matchNumber, "winner", winner);
		}

		public void SetPossibleActions(Player player, string[] actions)
		{
			if (actions == null)
				return;

			player.actionChosen = "";
			List<string> playerActions = player.PossibleActions;
			playerActions.Clear();
			playerActions.Add("Pass");
			for (int i = 0; i < actions.Length; i++)
			{
				playerActions.Add(actions[i]);
			}
		}

		int GetNextPlayer ()
		{
			//TODO MIN Different turn passing dynamics
			int next = activePlayer + 1;
			if (next >= players.Length)
				next = 0;
			return next;
		}

		IEnumerator NotifyWatchers (string triggerTag, params object[] args)
		{
			foreach (MatchWatcher item in Watchers)
			{
				yield return item.TreatTrigger(triggerTag, args);
			}
		}

		IEnumerator NotifyModifiers (string triggerTag, params object[] args)
		{
			for (int i = 0; i < modifiers.Count; i++)
			{
				if (modifiers[i] == null)
					continue;
				
				bool trigg = CheckTrigger(modifiers[i].trigger, triggerTag, args);
				if (trigg)
				{
					Debug.Log("CGEngine: >>>>>>>> CHECKING TRIGGER: " + modifiers[i].trigger + " : " + trigg);
					if (CheckCondition(modifiers[i].condition))
					{
						yield return TreatEffectRoutine(modifiers[i].trueEffect);
					}
					else
					{
						yield return TreatEffectRoutine(modifiers[i].falseEffect);
					}
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

		public double GetVariable (string variableName)
		{
			if (customVariables.ContainsKey(variableName))
				return customVariables[variableName];
			Debug.LogWarning("CGEngine: Variable not found: " + variableName);
			return double.NaN;
		}

		IEnumerator TreatEffectRoutine (string effect)
		{
			if (string.IsNullOrEmpty(effect))
			{
				yield break;
			}

			string[] effLines = effect.Split(';');

			foreach (string effLine in effLines)
			{
				string[] effBreakdown = ArgumentsBreakdown(effLine);
				Debug.Log("CGEngine: Treating effect => " + DebugPrintStringArray(effBreakdown));

				//TODO MAX one for each command
				switch (effBreakdown[0])
				{
					case "ChangeMatchModifier":
						ChangeModifier(ArgumentsBreakdown(effBreakdown[1]), modifiers);
						break;
					case "ChangeCardModifier":
						List<Card> cardsToModify = SelectCards(ArgumentsBreakdown(effBreakdown[1]), cards);
						for (int i = 0; i < cardsToModify.Count; i++)
						{
							ChangeModifier(ArgumentsBreakdown(effBreakdown[2]), cardsToModify[i].Modifiers);
						}
						break;
					case "EndCurrentPhase":  //===============EndCurrentPhase======================
						endCurrentPhase = true;
						break;
					case "MoveCardToZone": //===============MoveCardToZone======================
						List<Card> cardsToMove = SelectCards(ArgumentsBreakdown(effBreakdown[1]), cards);
						Zone zoneToMoveTo = SelectZones(ArgumentsBreakdown(effBreakdown[2]), zones)[0];
						string[] moveTags = effBreakdown.Length > 3 ? new string[effBreakdown.Length - 3] : null;
						if (moveTags != null) for (int i = 0; i < moveTags.Length; i++) { moveTags[i] = effBreakdown[i + 3]; }
						yield return MoveCardToZone(cardsToMove, zoneToMoveTo, moveTags);
						break;
					case "Shuffle": //===========================Shuffle==========================
						Zone zoneToShuffle = SelectZones(ArgumentsBreakdown(effBreakdown[1]), zones)[0];
						zoneToShuffle.Shuffle();
						break;
					case "UseAction":
						yield return UseActionRoutine(effBreakdown[1]);
						break;
					case "EndTheGame":
					case "EndTheMatch":
						endCurrentPhase = true;
						endSubphaseLoop = true;
						gameEnded = true;
						break;
					//case "WinTheGame":
					//	winner = SelectPlayers(effBreakdown[1])[0];
					//	Debug.Log("CGEngine: ! ! ! ! ! ! " + winner.id + " WON THE GAME!!! ");
					//	gameEnded = true;
					//	break;
					case "SendMessage":
						MessageBus.Send(effBreakdown[1]);
						yield return NotifyWatchers("OnMessageSent", "message", effBreakdown[1]);
						yield return NotifyModifiers("OnMessageSent", "message", effBreakdown[1]);
						break;
					case "StartSubphaseLoop":
						subphases = CreateTurnPhasesFromStrings(ArgumentsBreakdown(effLine, true)[1]);
						break;
					case "EndSubphaseLoop":
						endCurrentPhase = true;
						endSubphaseLoop = true;
						break;
					case "GetValue":
					case "GetCardFieldValue":
						valueForNextEffect = GetValue(effBreakdown);
						if (double.IsNaN(valueForNextEffect))
							Debug.LogWarning("CGEngine: GetValue/GetCardFieldValue failure");
						break;
					case "SetValue":
					case "SetCardFieldValue":
						SetCardFieldValue(effBreakdown[1], effBreakdown[2], effBreakdown[3]);
						break;
					case "GetVariable":
						if (customVariables.ContainsKey(effBreakdown[1]))
							valueForNextEffect = customVariables[effBreakdown[1]];
						break;
					case "SetVariable":
						yield return SetVariable(effBreakdown[1], effBreakdown[2]);
						break;
					case "ChangeModifierValue": //ChangeModifierValue(mod(%HP),+1)
						yield return ChangeModifierValue(effBreakdown[1], effBreakdown[2]);
						break;
					case "GetModifierValue":
						List<Modifier> modForValue = SelectModifiers(ArgumentsBreakdown(effBreakdown[1]), modifiers);
						if (modForValue.Count > 0)
							valueForNextEffect = modForValue[0].numValue;
						break;
					case "UseCard":
						List<Card> cardsToUse = SelectCards(ArgumentsBreakdown(effBreakdown[1]), cards);
						for (int i = 0; i < cardsToUse.Count; i++)
						{
							yield return UseCardRoutine(cardsToUse[i]);
						}
						break;
					case "ClickCard":
						List<Card> cardsClicked = SelectCards(ArgumentsBreakdown(effBreakdown[1]), cards);
						for (int i = 0; i < cardsClicked.Count; i++)
						{
							yield return ClickCardRoutine(cardsClicked[i]);
						}
						break;
					case "ActivateCard":
						List<Card> cardsToActivate = SelectCards(ArgumentsBreakdown(effBreakdown[1]), cards);
						for (int i = 0; i < cardsToActivate.Count; i++)
						{
							if (cardsToActivate[i].Modifiers != null)
								modifiers.AddRange(cardsToActivate[i].Modifiers);

							if (cardsToActivate[i].data && cardsToActivate[i].data.cardModifiers != null && cardsToActivate[i].data.cardModifiers.Length > 0)
							{
								foreach (ModifierData data in cardsToActivate[i].data.cardModifiers)
								{
									cardsToActivate[i].AddModifiers(CreateModifier(data));
								}
							}
						}
						break;
					case "DeactivateCard":
						List<Card> cardsToDeactivate = SelectCards(ArgumentsBreakdown(effBreakdown[1]), cards);
						for (int i = 0; i < cardsToDeactivate.Count; i++)
						{
							
							if (cardsToDeactivate[i].data.cardModifiers != null)
							{
								
								for (int j = 0; j < cardsToDeactivate[i].data.cardModifiers.Length; j++)
								{
									int index = -1;
									for (int k = cardsToDeactivate[i].Modifiers.Count - 1; k >= 0; k--)
									{
										if (cardsToDeactivate[i].Modifiers[k].data == cardsToDeactivate[i].data.cardModifiers[j])
										{
											index = k;
											break;
										}
									}
									if (index >= 0)
									{
										Modifier mod = cardsToDeactivate[i].Modifiers[index];
										cardsToDeactivate[i].Modifiers.Remove(mod);
										modifiers.Remove(mod);
										Destroy(mod.gameObject);
									}
								}

								
							}
						}
						break;
					case "ExecuteModifierEffect":
						List<Modifier> mods = SelectModifiers(ArgumentsBreakdown(effBreakdown[1]), modifiers);
						if (mods.Count > 0)
							yield return TreatEffectRoutine(mods[0].trueEffect);
						break;
					default: //=================================================================
						Debug.LogWarning("CGEngine: Effect not found: " + effBreakdown[0]);
						break;
				}
			}

			valueForNextEffect = double.NaN;
		}

		void SetCardFieldValue (string cardSelectionClause, string fieldName, string value)
		{
			List<Card> list = SelectCards(ArgumentsBreakdown(cardSelectionClause), cards);
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].fields != null)
				{
					for (int j = 0; j < list[i].fields.Length; j++)
					{
						if (list[i].fields[j].dataType == CardFieldDataType.Number && list[i].fields[j].name == fieldName)
						{
							double val = list[i].fields[j].numValue;
							SetValue(value, ref val);
							list[i].ChangeCardField(fieldName, val);
						}
					}
				}
			}
		}

		void SetValue (string valueStr, ref double varToBeSet)
		{
			string firstChar = valueStr.Substring(0, 1);
			if ("+-/*".Contains(firstChar))
				valueStr = valueStr.Substring(1);

			double value = double.NaN;
			if (valueStr == "value")
			{
				value = valueForNextEffect;
			}
			if (!double.IsNaN(value) || double.TryParse(valueStr, out value))
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
				return;
			}
			Debug.LogWarning("CGEngine: SetValue failure!");
			varToBeSet = value;
		}

		IEnumerator SetVariable (string variableName, string valueStr)
		{
			if (!customVariables.ContainsKey(variableName))
			{
				customVariables.Add(variableName, 0);
				if ("+-/*".Contains(valueStr.Substring(0, 1)))
				{
					Debug.LogWarning("CGEngine: value is being set for the first time with an operator. It was set to 0 instead. Be sure to set a value with a number before using operators. Value: " + valueStr);
					yield break;
				}
			}

			double val = customVariables[variableName];
			SetValue(valueStr, ref val);
			customVariables[variableName] = val;

			//playerHP = customVariables[variableName];
			Debug.Log("CGEngine: Variable " + variableName + " is now " + customVariables[variableName]);
			yield return NotifyWatchers("OnVariableChanged", "variable", variableName, "value", customVariables[variableName]);
			yield return NotifyModifiers("OnVariableChanged", "variable", variableName, "value", customVariables[variableName]);
		}

		double GetValue (string[] condition)
		{
			if (condition.Length != 3)
			{
				Debug.LogWarning("CGEngine: Wrong number of parameters for getting values with: " + condition.ToString());
				return double.NaN;
			}

			//Card
			if (condition[1].StartsWith("card"))
			{ 
				List<Card> list = SelectCards(ArgumentsBreakdown(condition[1]), cards);
				if (list == null || list.Count == 0)
				{
					Debug.LogWarning("CGEngine: Couldn't find cards with value using condition: " + condition[1]);
					return double.NaN;
				}
				if (list.Count > 1)
				{
					Debug.LogWarning("CGEngine: There is an ambiguity with condition " + condition[1] + ". It should return only one element to be used as value.");
					return double.NaN;
				}

				//Field
				if (condition[2].StartsWith("/") || condition[2].StartsWith("f"))
				{
					string fieldName = condition[2].Substring(1);
					CardField[] cardFields = list[0].fields;
					for (int i = 0; i < cardFields.Length; i++)
					{
						if (cardFields[i].name == fieldName)
							return cardFields[i].numValue;
					}
				}
			}
			//TODO Other values
			Debug.LogWarning("CGEngine: Couldn't find any usable value with condition: " + condition[1] + "," + condition[2]);
			return double.NaN;
		}

		IEnumerator ChangeModifierValue (string clause, string value)
		{
			List<Modifier> mods = SelectModifiers(clause);

			if (mods.Count == 0)
			{
				mods.Add(CreateModifier(clause));
			}

			string action = value.Substring(0, 1);

			if ("+-/*".Contains(action))
				value = value.Substring(1);

			double numValue = double.NaN;

			if (value.Contains("value"))
			{
				numValue = valueForNextEffect;
			}

			if (!double.IsNaN(numValue) || double.TryParse(value, out numValue))
			{

				Debug.Log("DEBUG Num value in ChangeModifierValue is " + numValue);

				for (int i = 0; i < mods.Count; i++)
				{
					double oldValue = mods[i].numValue;

					if ("0123456789".Contains(action))
					{
						mods[i].numValue = numValue;
					}
					else if (action == "+")
					{
						mods[i].numValue += numValue;
					}
					else if (action == "-")
					{
						mods[i].numValue -= numValue;
					}
					else if (action == "*")
					{
						mods[i].numValue *= numValue;
					}
					else if (action == "/")
					{
						mods[i].numValue /= numValue;
					}

					yield return NotifyWatchers("OnModifierValueChanged", "modifier", mods[i], "value", numValue, "oldValue", oldValue);
					yield return NotifyModifiers("OnModifierValueChanged", "modifier", mods[i], "value", numValue, "oldValue", oldValue);
				}
			}
			else
				Debug.LogWarning("CGEngine: Couldn't process value " + value + " for changing in modifiers.");
		}

		void ChangeModifier (string[] definition, List<Modifier> list)
		{
			string[] modDefinitionForSearch = new string[] { definition[0], definition[1] };
			List<Modifier> mods = SelectModifiers(modDefinitionForSearch, list);
			string definitionValue = definition[2];
			string action = definition[2].Substring(0, 1);

			if (definitionValue.Contains("value"))
			{
				definitionValue = valueForNextEffect.ToString();
			}

			if (int.TryParse(definitionValue, out int value))
				Mathf.Abs(value);
			else
				Debug.LogWarning("CGEngine: Couldn't convert to int: " + definitionValue);

			if ("0123456789".Contains(action))
			{
				if (mods == null)
				{
					action = "+";
				}
				else if (value > mods.Count)
				{
					action = "+";
					value = value - mods.Count;
				}
				else if (mods.Count > value)
				{
					action = "-";
					value = mods.Count - value;
				}
			}

			if (action == "+")
			{
				for (int i = 0; i < value; i++)
				{
					Modifier newMod = CreateModifier(definition[1]);
					if (list != modifiers)
						list.Add(newMod);
				}
			}
			else if (action == "-")
			{
				if (mods != null)
				{
					
					for (int i = 0; i < value; i++)
					{
						Modifier m = mods[i];
						mods.Remove(m);
						list.Remove(m);
						if (list != modifiers) modifiers.Remove(m);
						Destroy(m.gameObject);
					}
				}
			}
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
							newMod = CreateModifierWithTags(subdef.Split('&', '|', ','));
						else
							newMod.tags.AddRange(subdef.Split('&', '|', ','));
						break;
					case "*":
					case "p":
						Player p = null;
						if (playersByID.ContainsKey(subdef))
							p = playersByID[subdef];
						else if (context.ContainsKey(subdef) && context[subdef].GetType() == typeof(Player))
						{
							p = (Player)context[subdef];
							subdef = p.id;
						}
						
						if (!newMod)
							newMod = CreateModifierWithTags();
						if (!p)
						{
							Debug.LogWarning("CGEngine: No player found with condition " + type + subdef + " for modifier " + newMod);
							break;
						}
						newMod.target = p.id;
						p.Modifiers.Add(newMod);
						break;
					default:
						Debug.LogWarning("CGEngine: Couldn't resolve Modifier creation with definitions " + type + subdef);
						break;
				}
			}

			if (newMod)
			{
				if (!modifiers.Contains(newMod)) modifiers.Add(newMod);
				Debug.Log("CGEngine: Created Modifier " + newMod.gameObject.name + " (" + newMod.id + ")");
				return newMod;
			}
			else
				Debug.Log("CGEngine: ******* Error or modifier definition not yet implemented.");
			return null;
		}

		void CreateModifier (Modifier reference)
		{
			if (reference.data != null)
			{
				CreateModifier(reference.data);
				return;
			}
			CreateModifierWithTags(reference.tags.ToArray());
			//TEST anything else to copy to the new modifier?
		}

		public Modifier CreateModifier (ModifierData data)
		{
			if (data == null)
				return null;

			Modifier newMod = new GameObject(data.name + "Modifier").AddComponent<Modifier>();
			newMod.transform.SetParent(modifierContainer);
			newMod.Initialize(data, id);
			newMod.id = "m" + (++modifierIdTracker).ToString().PadLeft(4, '0');
			Debug.Log("CGEngine: Created Modifier " + data.name + " (" + newMod.id + ")");
			modifiers.Add(newMod);
			return newMod;
		}

		Modifier CreateModifierWithTags (params string[] tags)
		{
			Modifier newMod = new GameObject().AddComponent<Modifier>();
			newMod.transform.SetParent(modifierContainer);
			newMod.Initialize(tags);
			newMod.id = "m" + (++modifierIdTracker).ToString().PadLeft(4, '0');
			string name = "Modifier (" + newMod.id + ")";
			if (tags != null)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("TagModifier (");
				for (int i = 0; i < tags.Length; i++)
				{
					sb.Append(tags[i]);
					if (i < tags.Length - 1)
						sb.Append("-");
				}
				sb.Append(")");
				name = sb.ToString();
			}
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

		IEnumerator MoveCardToZone(List<Card> c, Zone z, string[] commandTags = null)
		{
			if (c == null || c.Count == 0)
			{
				Debug.LogWarning("CGEngine: Moving card failed. No card found to be moved.");
				yield return null;
			}

			if (z == null)
			{
				Debug.LogWarning("CGEngine: Moving card failed. No zone was selected to move cards to.");
				yield return null;
			}

			for (int i = 0; i < c.Count; i++)
			{
				Zone oldZone = c[i].zone;
				if (c[i].zone != null)
				{
					c[i].zone.PopCard(c[i]);
					SetContext("card", c[i], "zone", oldZone, "cardController", c[i].controller, "zoneController", oldZone.controller);
					yield return NotifyWatchers("OnCardLeftZone", "card", c[i], "zone", oldZone, "commandTags", commandTags);
					yield return NotifyModifiers("OnCardLeftZone", "card", c[i], "zone", oldZone, "commandTags", commandTags);
				}
				RevealStatus revealStatus = RevealStatus.ZoneDefinition;
				if (commandTags != null)
				{
					for (int j = 0; j < commandTags.Length; j++)
					{
						if (commandTags[j] == "Hidden" || commandTags[j] == "FaceDown")
						{
							revealStatus = RevealStatus.Hidden;
							break;
						}
						else if (commandTags[j] == "Revealed" || commandTags[j] == "FaceUp")
						{
							revealStatus = RevealStatus.RevealedToEveryone;
							break;
						}
					}
				}
				z.PushCard(c[i], revealStatus);
				SetContext("card", c[i], "zone", z, "oldZone", oldZone, "cardController", c[i].controller, "zoneController", z.controller, "oldZoneController", oldZone != null ? oldZone.controller : null);
				yield return NotifyWatchers("OnCardEnteredZone", "card", c[i], "zone", z, "oldZone", oldZone, "commandTags", commandTags);
				yield return NotifyModifiers("OnCardEnteredZone", "card", c[i], "zone", z, "oldZone", oldZone, "commandTags", commandTags);
			}
			Debug.Log("CGEngine: " + c.Count + " card"+(c.Count > 1 ? "s" : "")+" moved.");
		}

		List<TurnPhase> CreateTurnPhasesFromStrings (string phaseNamesList)
		{
			Debug.Log("DEBUG creating list of phases from " + phaseNamesList);
			List<TurnPhase> phaseList = new List<TurnPhase>();
			string[] phases = phaseNamesList.Split(',');
			for (int i = 0; i < phases.Length; i++)
			{
				TurnPhase newPhase = new TurnPhase();
				newPhase.name = phases[i];
				phaseList.Add(newPhase);
			}
			return phaseList;
		}

		#endregion

		//==================================================================================================================
		#region Check Methods ==================================================================================================
		//==================================================================================================================

		public bool CheckCondition (string cond)
		{
			/*
			card
			phase
			history
			zone
			modifier
			variable
			*/

			if (string.IsNullOrEmpty(cond))
				return true;

			cond = cond.Replace(" ", "");
			string[] condParts = cond.Split(';');
			int partsFound = 0;

			foreach (string item in condParts) // each one of the conditions separated by ;
			{
				string op = GetOperator(item);
				if (op != "")
				{
					string comparer = item.Substring(item.IndexOf(op));
					string value = item.Replace(comparer, "");
					Debug.Log("DEBUG    >>>>>>   The two strings are: 1)" + value + " and 2)" + comparer);
					if (CheckValue(value, comparer))
						partsFound++;
				}
				else
				{
					string[] condBreakdown = ArgumentsBreakdown(item);

					switch (condBreakdown[0])
					{
						case "card":
							if (SelectCards(condBreakdown, cards).Count > 0)
								partsFound++;
							break;
						case "phase":
							if (condBreakdown[1] == CurrentTurnPhase)
								partsFound++;
							break;
						case "history":
							//TODO MIN
							break;
						case "zone":
							//TODO MED
							break;
						case "modifier":
						case "mod":
							if (SelectModifiers(condBreakdown, modifiers).Count > 0)
								partsFound++;
							break;
						default:
							Debug.LogError("CGEngine: Condition (" + cond + ") doesn't ask for a valid type (" + condBreakdown[0] + ")");
							return false;
					}
				}
			}
			bool result = partsFound == condParts.Length;
			Debug.Log("CGEngine: Condition " + cond + " found out to be " + result);
			return result;
		}

		bool CheckValue (string value, string comparerWithOperator)
		{
			string op = GetOperator(comparerWithOperator);
			if (op == "")
				op = "=";
			string comparer = comparerWithOperator.Replace(op, "");

			if (value == "UseCardInBrowser" || comparer == "UseCardInBrowser")
				Debug.Log("x");

			double v = 0;
			double c = 0;
			bool valueIsNumber = false;
			bool comparerIsNumber = false;

			if (value.StartsWith("$"))
			{
				v = GetValue(ArgumentsBreakdown(value));
				valueIsNumber = true;
			}
			else if (double.TryParse(value, out v))
				valueIsNumber = true;
			else if (customVariables.ContainsKey(value))
			{
				v = customVariables[value];
				valueIsNumber = true;
			}
			else
			{
				List<Modifier> modForValue = SelectModifiers(new string[] { "mod", "%" + value }, modifiers);
				if (modForValue.Count > 0)
				{
					v = modForValue[0].numValue;
					valueIsNumber = true;
				}
			}

			if (comparer.StartsWith("$"))
			{
				c = GetValue(ArgumentsBreakdown(comparer));
				comparerIsNumber = true;
			}
			else if (double.TryParse(comparer, out c))
				comparerIsNumber = true;
			else if (customVariables.ContainsKey(comparer))
			{
				c = customVariables[comparer];
				comparerIsNumber = true;
			}
			else
			{
				List<Modifier> modForValue = SelectModifiers(new string[] { "mod", "%" + value }, modifiers);
				if (modForValue.Count > 0)
				{
					c = modForValue[0].numValue;
					comparerIsNumber = true;
				}
			}	

			bool numbers = valueIsNumber && comparerIsNumber;

			switch (op)
			{
				case "!=":
					if (numbers) return v != c;
					return value != comparer;
				case "<=":
					if (numbers)
						return v <= c;
					else
						Debug.Log("CGEngine: Comparer argument failure.");
					break;
				case ">=":
					if (numbers)
						return v >= c;
					else
						Debug.Log("CGEngine: Comparer argument failure.");
					break;
				case "<":
					if (numbers)
						return v < c;
					else
						Debug.Log("CGEngine: Comparer argument failure.");
					break;
				case ">":
					if (numbers)
						return v > c;
					else
						Debug.Log("CGEngine: Comparer argument failure.");
					break;
				case "=":
					if (comparer.Contains("|"))
					{
						string[] subcomparers = comparer.Split('|');
						int counter = 0;
						foreach (string item in subcomparers)
						{
							if (CheckValue(value, item))
								counter++;
						}
						if (counter > 0)
							return true;
					}
					if (comparer.Contains("-"))
					{
						string[] subcomparers = comparer.Split('-');
						double d2 = 0;
						bool correct = double.TryParse(subcomparers[0], out double d1) && double.TryParse(subcomparers[1], out d2);
						if (subcomparers.Length > 2 || !correct)
						{
							Debug.LogWarning("CGEngine: Sintax error: " + comparer);
							return false;
						}
						return v >= d1 && v <= d2;
					}
					if (numbers)
						return v == c;
					return value == comparer;
				default:
					Debug.LogWarning("CGEngine: Unknown operator.");
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

		bool CheckContent(Zone zone, string poolSelection)
		{
			List<Zone> selection = SelectZones(poolSelection);
			if (selection != null && selection.Contains(zone))
				return true;
			return false;
		}

		bool CheckContent(Modifier modifier, string poolSelection)
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

		bool CheckTrigger(string trigger, string triggerTag, params object[] args)
		{
			if (string.IsNullOrEmpty(trigger))
				return false;

			trigger = trigger.Replace(" ", "");

			string[] subtriggers = trigger.Split(';');
			foreach (string subtrigger in subtriggers)
			{
				if (subtrigger.StartsWith(triggerTag))// So this is a trigger found
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
							if (!parameters.Contains("card"))
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
								if ((subtrigBreakdown[i].Contains("card") && CheckContent(c2, subtrigBreakdown[i])) |
									(subtrigBreakdown[i].Contains("zone") && CheckContent(z, subtrigBreakdown[i])))
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
								else
									return false;
							}
							return parts == 0;
						case "OnActionUsed":
						case "OnMatchSetup":
						case "OnMatchStarted":
						case "OnPhaseEnded":
						case "OnPhaseStarted":
						case "OnTurnEnded":
						case "OnTurnStarted":
							return CheckValue((string)args[1], subtrigBreakdown[1]);
						default:
							break;
					}
				}
			}

			return false;
		}

		public List<Card> SelectCards (string clause)
		{
			string[] clauseBreakdown = ArgumentsBreakdown(clause);
			return SelectCards(clauseBreakdown, cards);
		}

		List<Card> SelectCards(string[] clauseArray, Card[] fromPool)
		{
			if (fromPool == null)
			{
				Debug.LogWarning("CGEngine: Error: the pool of cards to be selected with condition (" + clauseArray + ") is null.");
				return null;
			}

			if (clauseArray == null || (clauseArray[0] != "card" && clauseArray[0] != "all"))
			{
				Debug.LogWarning("CGEngine: Syntax error on the selection of cards. The correct syntax is: 'card(argument1,argument2,...)'. Clause: "+
					clauseArray[0] + ", " + (clauseArray.Length > 1 ? clauseArray[1] : "") + ", " + (clauseArray.Length > 2 ? clauseArray[2] : ""));
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

			if (selection.Count == 0)
				Debug.LogWarning("CGEngine: No cards found with conditions " + DebugPrintStringArray(clauseArray));
			return selection;
		}

		List<Card> SelectCardsSingleCondition (string condition, Card[] fromPool)
		{
			string searchType = condition.Substring(0, 1);
			if (!"@z#i*po%m+c/fx".Contains(searchType))
			{
				Debug.LogWarning("CGEngine: Syntax error with condition " + condition + ". Search character " + searchType + " is not valid.");
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

			if (context.ContainsKey(identifier))
			{
				if (context[identifier].GetType() == typeof(Card))
					identifier = ((Card)context[identifier]).ID;
				else if (context[identifier].GetType() == typeof(Player))
					identifier = ((Player)context[identifier]).id;
				else if (context[identifier].GetType() == typeof(Modifier))
					identifier = ((Modifier)context[identifier]).id;
				else if (context[identifier].GetType() == typeof(Zone))
					identifier = ((Zone)context[identifier]).id;
			}

			//Player keywords
			if (searchType == "*" || searchType == "p" || searchType == "o")
			{
				if (identifier == "active")
					identifier = players[activePlayer].id;
				else if ("0123456789".Contains(identifier.Substring(0, 1)) && int.TryParse(identifier, out int playerIndex) && playerIndex < players.Length)
					identifier = players[playerIndex].id;
			}

			switch (searchType)
			{
				case "@": //zone
				case "z":
					for (int i = 0; i < fromPool.Length; i++)
					{
						if (fromPool[i].zone != null && ((equals && fromPool[i].zone.zoneType == identifier) || (!equals && fromPool[i].zone.zoneType != identifier)))
							selection.Add(fromPool[i]);
					}
					break;
				case "*": //controller player
				case "p":
					for (int i = 0; i < fromPool.Length; i++)
					{
						if (fromPool[i].controller != null && ((equals && fromPool[i].controller.id == identifier) || (!equals && fromPool[i].controller.id != identifier)))
							selection.Add(fromPool[i]);
					}
					break;
				case "o": //owner player
					for (int i = 0; i < fromPool.Length; i++)
					{
						if (fromPool[i].owner != null && ((equals && fromPool[i].owner.id == identifier) || (!equals && fromPool[i].owner.id != identifier)))
							selection.Add(fromPool[i]);
					}
					break;
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
				case "+": //card tag
				case "c":
					for (int i = 0; i < fromPool.Length; i++)
					{
						if ((equals && fromPool[i].data.tags.Contains(identifier)) || (!equals && !fromPool[i].data.tags.Contains(identifier)))
							selection.Add(fromPool[i]);
					}
					break;
				case "/": //card field
				case "f":
					//TEST
					string[] breakdownForFields = ArgumentsBreakdown(identifier);
					for (int i = 0; i < fromPool.Length; i++)
					{
						for (int j = 0; j < fromPool[i].data.fields.Length; j++)
						{
							if (fromPool[i].data.fields[j].name == breakdownForFields[0])
							{
								switch (fromPool[i].data.fields[j].dataType)
								{
									case CardFieldDataType.Text:
										if (CheckValue(fromPool[i].data.fields[j].stringValue, breakdownForFields[1]))
											selection.Add(fromPool[i]);
										break;
									case CardFieldDataType.Number:
										if (CheckValue(fromPool[i].data.fields[j].numValue.ToString(), breakdownForFields[1]))
											selection.Add(fromPool[i]);
										break;
								}
							}
						}
					}
					break;
				case "x": //quantity
					System.Array.Sort(fromPool, CompareCardsByIndex);
					if (!int.TryParse(identifier, out int qty))
					{
						Debug.LogError("CGEngine: The value following the x in (" + searchType + identifier + ") must be a number.");
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
			return SelectZones (clauseBreakdown, zones);
		}

		List<Zone> SelectZones (string[] clauseArray, Zone[] fromPool)
		{
			if (fromPool == null)
			{
				Debug.LogWarning("CGEngine: Error: the pool of zones to be selected with condition (" + clauseArray + ") is null.");
				return null;
			}

			if (clauseArray == null)
			{
				Debug.LogWarning("CGEngine: Error: the search zone is null.");
				return null;
			}

			if (clauseArray[0] != "zone" && clauseArray[0] != "all")
			{
				string start = clauseArray[0].Substring(0, 1);
				if ("@z".Contains(start))
					return SelectZonesSingleCondition(clauseArray[0], fromPool);
				else
					Debug.LogWarning("CGEngine: Syntax error: the zone definition need to be in the form: zone(argument1, argument2, ...) or @ZoneType");
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
				Debug.LogWarning("CGEngine: No zones found with conditions " + DebugPrintStringArray(clauseArray));
			return selection;
		}

		List<Zone> SelectZonesSingleCondition (string condition, Zone[] fromPool)
		{
			string searchType = condition.Substring(0, 1);
			if (!"@z#i*p".Contains(searchType))
			{
				Debug.LogError("CGEngine: Syntax error zone selecting with condition " + condition + ". Search character " + searchType + " is not valid.");
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

			if (context.ContainsKey(identifier))
			{
				if (context[identifier].GetType() == typeof(Card))
					identifier = ((Card)context[identifier]).ID;
				else if (context[identifier].GetType() == typeof(Player))
					identifier = ((Player)context[identifier]).id;
			}

			//Player keywords
			if (searchType == "*" || searchType == "p")
			{
				if (identifier == "active")
					identifier = players[activePlayer].id;
				else if ("0123456789".Contains(identifier.Substring(0, 1)) && int.TryParse(identifier, out int playerIndex) && playerIndex < players.Length)
				{
					identifier = players[playerIndex].id;
				}
			}

			switch (searchType)
			{
				case "@": //zone tag
				case "z":
					for (int i = 0; i < fromPool.Length; i++)
					{
						if ((equals && fromPool[i].zoneType == identifier) || (!equals && fromPool[i].zoneType != identifier))
							selection.Add(fromPool[i]);
					}
					break;
				case "*": //controller player
				case "p":
					for (int i = 0; i < fromPool.Length; i++)
					{
						if (fromPool[i].controller != null && ((equals && fromPool[i].controller.id == identifier) || (!equals && fromPool[i].controller.id != identifier)))
							selection.Add(fromPool[i]);
					}
					break;
				case "#": //zone id
				case "i":
					for (int i = 0; i < fromPool.Length; i++)
					{
						if ((equals && fromPool[i].id == identifier) || (!equals && fromPool[i].id != identifier))
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
				Debug.LogWarning("CGEngine: Pool of modifiers with condition (" + clauseArray + ") is null.");
				return null;
			}

			if (clauseArray == null)
			{
				Debug.LogWarning("CGEngine: Error: clause array of modifiers is null.");
				return null;
			}

			if (clauseArray[0] != "modifier" && clauseArray[0] != "mod" && clauseArray[0] != "all")
			{
				string start = clauseArray[0].Substring(0, 1);
				if ("%m".Contains(start))
					return SelectModifiersSingleCondition(clauseArray[0], fromPool);
				else
					Debug.LogWarning("CGEngine: Syntax error: the modifier definition need to be in the form: modifier(argument1, argument2, ...) or mod(argument1, argument2, ...) or %ModifierTag.");
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

			//if (selection == null || selection.Count == 0)
			//	Debug.LogWarning("CGEngine: No modifier found with conditions " + DebugPrintStringArray(clauseArray));
			return selection;
		}

		List<Modifier> SelectModifiersSingleCondition (string condition, List<Modifier> fromPool)
		{
			//TODO MAX
			string searchType = condition.Substring(0, 1);
			if (!"%m#i*pxc".Contains(searchType))
			{
				Debug.LogError("CGEngine: Syntax error zone selecting with condition " + condition + ". Search character " + searchType + " is not valid.");
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

			if (context.ContainsKey(identifier))
			{
				string temp = identifier;
				if (context[identifier].GetType() == typeof(Card))
					identifier = ((Card)context[identifier]).ID;
				else if (context[identifier].GetType() == typeof(Player))
					identifier = ((Player)context[identifier]).id;
				Debug.Log("DEBUG Context has key " + temp + " which is " + identifier);
			}

			//Player keywords
			if (searchType == "*" || searchType == "p")
			{
				if (identifier == "active")
					identifier = players[activePlayer].id;
				else if ("0123456789".Contains(identifier.Substring(0, 1)) && int.TryParse(identifier, out int playerIndex) && playerIndex < players.Length)
				{
					identifier = players[playerIndex].id;
				}
			}

			//Value substitution
			if (identifier.StartsWith("$"))
			{
				identifier = GetValue(ArgumentsBreakdown(identifier)).ToString();
			}

			switch (searchType)
			{
				//TODO MED Other targets: card and modifier
				case "c":
					Debug.Log("DEBUG Searching cards with identifier " + identifier);
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
				case "*": //player target
				case "p":
					for (int i = 0; i < fromPool.Count; i++)
					{
						if (!string.IsNullOrEmpty(fromPool[i].target) && ((equals && fromPool[i].target == identifier) || (!equals && fromPool[i].target != identifier)))
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
						Debug.LogError("CGEngine: The value following the x in (" + searchType + op + identifier + ") must be a number.");
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
					if (CheckValue(tempSelection.Count.ToString(), op + identifier))
						selection.AddRange(tempSelection);
					break;
			}
			
			return selection;
		}

		public List<Player> SelectPlayers (string clause)
		{
			string[] clauseBreakdown = ArgumentsBreakdown(clause);
			return SelectPlayers(clauseBreakdown, players);
		}

		List<Player> SelectPlayers (string[] clauseArray, Player[] fromPool)
		{
			if (fromPool == null)
			{
				Debug.LogWarning("CGEngine: Error: the pool of zones to be selected with condition (" + clauseArray + ") is null.");
				return null;
			}

			if (clauseArray == null)
			{
				Debug.LogWarning("CGEngine: Error: the search zone is null.");
				return null;
			}

			if (clauseArray[0] != "player" && clauseArray[0] != "all")
			{
				string start = clauseArray[0].Substring(0, 1);
				if ("*p".Contains(start))
					return SelectPlayersSingleCondition(clauseArray[0], fromPool);
				else
					Debug.LogWarning("CGEngine: Syntax error: the zone definition need to be in the form: zone(argument1, argument2, ...) or @ZoneType");
				return null;
			}

			List<Player> selection = new List<Player>();
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
					selection = SelectPlayersSingleCondition(clauseArray[i], selection.ToArray());
				}
			}

			if (selection == null || selection.Count == 0)
				Debug.LogWarning("CGEngine: No players found with conditions " + DebugPrintStringArray(clauseArray));
			return selection;
		}

		List<Player> SelectPlayersSingleCondition (string condition, Player[] fromPool)
		{
			string searchType = condition.Substring(0, 1);
			if (!"#i%m*ptr".Contains(searchType))
			{
				Debug.LogError("CGEngine: Syntax error zone selecting with condition " + condition + ". Search character " + searchType + " is not valid.");
				return null;
			}
			string identifier = condition.Substring(1);
			bool hasOr = identifier.Contains("|");
			bool hasAnd = identifier.Contains("&");

			List<Player> selection = new List<Player>();

			if (hasOr)
			{
				string[] orBreakdown = identifier.Split('|');
				selection = SelectPlayersSingleCondition(searchType + orBreakdown[0], fromPool);
				List<Player> tempPlayerSelector = null;
				for (int i = 1; i < orBreakdown.Length; i++)
				{
					tempPlayerSelector = SelectPlayersSingleCondition(searchType + orBreakdown[i], fromPool);
					for (int j = 0; j < tempPlayerSelector.Count; j++)
					{
						if (!selection.Contains(tempPlayerSelector[j]))
							selection.Add(tempPlayerSelector[j]);
					}
				}
				return selection;
			}

			if (hasAnd)
			{
				string[] andBreakdown = identifier.Split('&');
				selection = SelectPlayersSingleCondition(searchType + andBreakdown[0], fromPool);
				for (int i = 1; i < andBreakdown.Length; i++)
				{
					selection = SelectPlayersSingleCondition(searchType + andBreakdown[i], selection.ToArray());
				}
				return selection;
			}

			bool equals = true;
			if (identifier.Substring(0, 1) == "!")
			{
				equals = false;
				identifier = identifier.Substring(1);
			}

			if (context.ContainsKey(identifier))
			{
				if (context[identifier].GetType() == typeof(Player))
					identifier = ((Player)context[identifier]).id;
			}

			//Player keywords
			if (searchType == "*" || searchType == "p")
			{
				if (identifier == "active")
					identifier = players[activePlayer].id;
				else if ("0123456789".Contains(identifier.Substring(0, 1)) && int.TryParse(identifier, out int playerIndex) && playerIndex < players.Length)
				{
					identifier = players[playerIndex].id;
				}
			}

			switch (searchType)
			{
				case "*": //player id
				case "p":
				case "#":
				case "i":
					for (int i = 0; i < fromPool.Length; i++)
					{
						if ((equals && fromPool[i].id == identifier) || (!equals && fromPool[i].id != identifier))
							selection.Add(fromPool[i]);
					}
					break;
				case "t": // Team
					for (int i = 0; i < fromPool.Length; i++)
					{
						if ((equals && fromPool[i].playerRules.team == identifier) || (!equals && fromPool[i].playerRules.team != identifier))
							selection.Add(fromPool[i]);
					}
					break;
				case "r": // Role
					for (int i = 0; i < fromPool.Length; i++)
					{
						if ((equals && fromPool[i].playerRules.role == identifier) || (!equals && fromPool[i].playerRules.role != identifier))
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
			}
			return selection;
		}

		#endregion

		//==================================================================================================================
		#region Helper Methods ==================================================================================================
		//==================================================================================================================

		int CompareCardsByIndex(Card c1, Card c2)
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

		string GetOperator(string value)
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
			context.Clear();
			if (args == null)
				return;
			for (int i = 0; i < args.Length; i+=2)
			{
				string key = (string)args[i];
				context.Add(key, args[i + 1]);
			}
		}

		#endregion

		//==================================================================================================================
		#region DEBUG ========================================================================================================
		//==================================================================================================================

		public void Test(string str)
		{
			ArgumentsBreakdown(str);
		}

		string DebugPrintStringArray(string[] str)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < str.Length; i++)
			{
				sb.Append(i+"{ ");
				sb.Append(str[i]);
				sb.Append(" }  ");
			}
			return sb.ToString();
		}

		#endregion
	}
}