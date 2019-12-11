using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

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
		//List<Card> cardSelector;
		//List<Player> playerSelector;
		//List<Zone> zoneSelector;
		Dictionary<string, object> context;

		bool isSimulation;
		int turnNumber;
		List<string> actionHistory;
		int activePlayer;
		string currentTurnPhase = "";
		bool gameEnded;
		bool endCurrentPhase;
		List<TurnPhase> currentTurnPhases;
		IEnumerator playerActionRoutine = null;
		Transform modifierContainer;
		Player winner = null;

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
				cards[i].id = "c" + (++cardIdTracker).ToString().PadLeft(4, '0');
				cardsByID.Add(cards[i].id, cards[i]);
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
			for (int i = 0; i < cards.Length; i++)
			{
				for (int j = 0; j < cards[i].Modifiers.Count; j++)
				{
					modifiers.Add(cards[i].Modifiers[j]);
				}
			}
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
				for (int i = 0; i < currentTurnPhases.Count; i++)
				{
					currentTurnPhase = currentTurnPhases[i].name;
					endCurrentPhase = false;
					playerActionRoutine = null;
					yield return StartPhase(currentTurnPhases[i]);
					while (!endCurrentPhase)
						yield return playerActionRoutine;
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
		/*
		public new string name;
		public string[] allowedActions;
		public int[] allowedActionsCount;
		public bool giveEachPlayer;
		public bool giveEachPlayerRealtime;
		public int maxActions;
		*/
		public IEnumerator StartPhase(TurnPhase phase)
		{
			Debug.Log("CGEngine:       Phase " + phase.name + " started.");
			yield return NotifyWatchers("OnPhaseStarted", "phaseName", phase.name, "activePlayer", players[activePlayer], "phaseObject", phase);
			yield return NotifyModifiers("OnPhaseStarted", "phaseName", phase.name, "activePlayer", players[activePlayer], "phaseObject", phase);
		}

		public void UseAction(string action)
		{
			playerActionRoutine = UseActionRoutine(action);
		}

		public IEnumerator UseActionRoutine (string action)
		{
			yield return NotifyWatchers("OnActionUsed", "actionName", action);
			yield return NotifyModifiers("OnActionUsed", "actionName", action);
		}

		public void UseCard(Card c)
		{
			playerActionRoutine = UseCardRoutine(c);
		}

		IEnumerator UseCardRoutine (Card c)
		{
			context.Clear();
			context.Add("cardUsed", c);
			context.Add("cardController", c.controller);
			yield return NotifyWatchers("OnCardUsed", "card", c);
			yield return NotifyModifiers("OnCardUsed", "card", c);
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
					Debug.Log(">>>>>>>> CHECKING TRIGGER: " + modifiers[i].trigger + " : " + trigg);
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
			playerActionRoutine = TreatEffectRoutine(effect);
		}

		IEnumerator TreatEffectRoutine (string effect)
		{
			if (string.IsNullOrEmpty(effect))
				yield break;

			string[] effLines = effect.Split(';');
			foreach (string effLine in effLines)
			{
				//int firstParIndex = effLine.IndexOf('(');
				//int lastParIndex = effLine.LastIndexOf(')');
				//if (firstParIndex == -1 || lastParIndex == -1)
				//{
				//	Debug.LogError("CGEngine: Syntax error on effect: (" + effLine + "). It must contain parenthesis to be recognized as a command.");
				//	yield break;
				//}
				//string command = effLine.Substring(0, firstParIndex);
				//string args = effLine.Substring(firstParIndex + 1, lastParIndex - firstParIndex);
				//string[] argsBreakdown = args.Split(',');
				string[] effBreakdown = ArgumentsBreakdown(effLine);
				Debug.Log("Treating effect => " + DebugPrintStringArray(effBreakdown));

				//TODO MAX one for each command
				switch (effBreakdown[0])
				{
					case "ChangeModifier":
						List<Modifier> mods = SelectModifiers(effBreakdown[1]);
						string action = effBreakdown[2].Substring(0, 1);
						if (int.TryParse(effBreakdown[2], out int value))
							Mathf.Abs(value);
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
								CreateModifier(effBreakdown[1]);
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
									modifiers.Remove(m);
									Destroy(m.gameObject);
								}
							}
						}
						else
						{
							Debug.Log(" *********  Change Modifier specification not yet implemented.");
							//TODO MED trigger, condition, tag, affected, effect, etc...
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
					case "WinTheGame":
						winner = SelectPlayers(effBreakdown[1])[0];
						Debug.Log("! ! ! ! ! ! " + winner.id + " WON THE GAME!!! ");
						gameEnded = true;
						break;
					default: //=================================================================
						Debug.LogWarning("CGEngine: Effect not found: " + effBreakdown[0]);
						break;
				}
			}
		}

		void CreateModifier (string definitions)
		{
			string[] definitionsBreakdown = ArgumentsBreakdown(definitions);
			Modifier newMod = null;
			for (int i = 1; i < definitionsBreakdown.Length; i++)
			{
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
						
						break;
				}
			}

			if (newMod)
			{
				if (!modifiers.Contains(newMod)) modifiers.Add(newMod);
				Debug.Log("Created Modifier " + newMod.gameObject.name + " (" + newMod.id + ")");
			}
			else
				Debug.Log(" ******* Error or modifier definition not yet implemented.");
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

		void CreateModifier (ModifierData data)
		{
			if (data == null)
				return;

			Modifier newMod = new GameObject(data.name + "Modifier").AddComponent<Modifier>();
			newMod.transform.SetParent(modifierContainer);
			newMod.Initialize(data, id);
			newMod.id = "m" + (++modifierIdTracker).ToString().PadLeft(4, '0');
			Debug.Log("Created Modifier " + data.name + " (" + newMod.id + ")");
			modifiers.Add(newMod);
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
			Debug.Log(c.Count + " cards moved.");
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
			*/

			if (string.IsNullOrEmpty(cond))
				return true;

			cond = cond.Replace(" ", "");
			string[] condParts = cond.Split(';');
			int partsFound = 0;

			foreach (string item in condParts) // each one of the conditions separated by ;
			{
				string[] condBreakdown = ArgumentsBreakdown(item);

				switch (condBreakdown[0])
				{
					case "card":
						if (SelectCards(condBreakdown, cards).Count > 0)
							partsFound++;
						break;
					case "phase":
						if (condBreakdown[1] == currentTurnPhase)
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
			bool result = partsFound == condParts.Length;
			Debug.Log("Condition " + cond + " found out to be " + result);
			return result;
		}

		bool CheckValue (string value, string comparerWithOperator)
		{
			string op = GetOperator(comparerWithOperator);
			if (op == "")
				op = "=";
			string comparer = comparerWithOperator.Replace(op, "");

			double v = 0;
			double c = 0;
			bool numbers = double.TryParse(value, out v) && double.TryParse(comparer, out c);

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
					if (numbers) return v == c;
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
				if (subtrigger.Contains(triggerTag))// So this is a trigger found
				{
					if (args == null || args.Length == 0) //The trigger point doesn't come with arguments
						return true;

					string[] subtrigBreakdown = ArgumentsBreakdown(subtrigger, true);

					if (subtrigBreakdown.Length == 1) //The trigger does not care about arguments
						return true;

					switch (subtrigBreakdown[0])
					{
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
							Zone oldZone = args.Length > 4 ? (Zone)args[5] : null;
							subtrigBreakdown = ArgumentsBreakdown(subtrigger);
							int parts = subtrigBreakdown.Length - 1;
							for (int i = 1; i < subtrigBreakdown.Length; i++)
							{
								if ((subtrigBreakdown[i].Contains("card(") && CheckContent(c2, subtrigBreakdown[i])) |
									(subtrigBreakdown[i].Contains("zone(") && CheckContent(z, subtrigBreakdown[i])))
									parts--;
								else if (subtrigBreakdown[i].Contains("oldZone("))
								{
									string oldZoneSelection = subtrigBreakdown[i].Replace("oldZone", "zone");
									if (CheckContent(oldZone, oldZoneSelection))
										parts--;
								}
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
				Debug.LogWarning("CGEngine: Syntax error on the selection of cards. The correct syntax is: 'card(argument1,argument2,...)'");
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
					identifier = ((Card)context[identifier]).id;
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
						if ((equals && fromPool[i].id == identifier) || (!equals && fromPool[i].id != identifier))
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
					identifier = ((Card)context[identifier]).id;
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
			if (!"%m#i*px".Contains(searchType))
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
					identifier = ((Card)context[identifier]).id;
				else if (context[identifier].GetType() == typeof(Player))
					identifier = ((Player)context[identifier]).id;
				Debug.Log("Context has key " + temp + " which is " + identifier);
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
				//TODO MED Other targets: card and modifier
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

		#region DEBUG

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