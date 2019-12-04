using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CGEngine
{
	public enum CardAction
	{
		None,
		UseCard
	}

	public class MatchSceneManager : BasicSceneManager
	{
		// Initializes with information on some number of instances of User

		static MatchSceneManager instance;
		public static MatchSceneManager Instance
		{
			get
			{
				if (instance == null)
				{
					instance = FindObjectOfType<MatchSceneManager>();
					if (!instance)
					{
						GameObject go = new GameObject("MatchSceneManager");
						instance = go.AddComponent<MatchSceneManager>();
					}
				}
				return instance;
			}
			private set
			{
				instance = value;
			}
		}

		public static Vector3 mouseWorldPosition;

		CardGameData cardGame;
		Ruleset rules;
		User[] users;
		Player[] players;
		Match match;
		Camera mainCamera;
		Ray mouseRay;
		Plane xz = new Plane(Vector3.up, Vector3.zero);

		int matchIdTracker;

		private void Awake()
		{
			if (Instance != this)
				DestroyImmediate(gameObject);
		}

		private void Start()
		{
			mainCamera = Camera.main;
		}

		/// <summary>
		/// Initialized a Match Scene.
		/// </summary>
		/// <param name="args">A Match Scene needs a Ruleset and a number of Users greater than zero to be initialized.</param>
		public override void Initialize(params object[] args)
		{
			if (args == null || args.Length < 2)
			{
				Debug.LogError("CGEngine: MatchScene was Initialized with wrong arguments. It needs one Ruleset and at least one User to be initialized correctly.");
				return;
			}

			rules = (Ruleset)args[0];
			if (rules == null)
			{
				Debug.LogError("CGEngine: MatchScene was Initialized with wrong arguments. The first argument must be a Ruleset.");
				return;
			}

			users = new User[args.Length - 1];
			if (users.Length < 1)
			{
				Debug.LogError("CGEngine: MatchScene was Initialized with wrong arguments. It needs at least one User object to be initialized correctly.");
				return;
			}

			for (int i = 1; i < args.Length; i++)
			{
				if (args[i].GetType() != typeof(User))
				{
					Debug.LogError("CGEngine: MatchScene was Initialized with wrong arguments. Besides the Ruleset, arguments must be User objects.");
					return;
				}
				users[i - 1] = (User)args[i];
			}

			//CreateCards();
			CreateMatch();
		}

		void CreateCards()
		{
			Transform container = new GameObject("CardContainer").transform;
			Vector3 position = Vector3.zero;
			Vector3 posInc = Vector3.up * 0.02f;
			if (rules.neutralDecks != null)
			{
				foreach (Deck item in rules.neutralDecks)
				{
					if (item.cards != null)
					{
						for (int i = 0; i < item.cards.Count; i++)
						{
							Card newCard = Instantiate(CGEngineManager.Instance.cardTemplate, position, Quaternion.identity, container).GetComponent<Card>();
							position += posInc;
							newCard.SetupData(item.cards[i]);
						}
					}
				}
			}
			players = FindObjectsOfType<Player>();
			if (players != null)
			{
				foreach (Player item in players)
				{
					if (item.deck != null && item.deck.cards != null)
					{
						for (int i = 0; i < item.deck.cards.Count; i++)
						{
							Card newCard = Instantiate(CGEngineManager.Instance.cardTemplate, position, Quaternion.identity, container).GetComponent<Card>();
							position += posInc;
							newCard.SetupData(item.deck.cards[i]);
							newCard.owner = item;
						}
					}
				}
			}
		}

		void CreateMatch()
		{
			match = new GameObject("CurrentMatch").AddComponent<Match>();
			match.id = "a" + (++matchIdTracker);
			match.matchNumber = matchIdTracker;
			Debug.Log("Created match " + match.id);
			match.Initialize(rules);
		}

		public override IEnumerator TreatTrigger(string triggerTag, params object[] args)
		{
			if (triggerTag == "OnMatchEnded")
			{
				yield return new WaitForSeconds(5);
				CGEngineManager.Instance.SceneEnded();
			}
			yield return null;
		}

		public static Vector3 GetMouseWorldPosition (Plane plane)
		{
			float distance;
			plane.Raycast(Instance.mouseRay, out distance);
			return Instance.mouseRay.GetPoint(distance);
		}

		#region CardManagement

		public CardInteractionPack cardInteractionPack = new CardInteractionPack();
		public List<CardInteractionPack> cardInteractionListeners = new List<CardInteractionPack>();
		//public Card mouseDownCard;
		//public Card mouseUpCard;
		//public Card mouseOverCard;
		//public Card mouseEnterCard;
		//public Card mouseExitCard;
		//public Card mouseDragCard;
		//public Card mouseClickCard;

		public UnityEvent onMouseDownOnCard;
		public UnityEvent onMouseUpOnCard;
		public UnityEvent onMouseOverCard;
		public UnityEvent onMouseEnterCard;
		public UnityEvent onMouseExitCard;
		public UnityEvent onMouseDragCard;
		public UnityEvent onMouseClickCard;

		private void Update()
		{
			mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);
			float distanceForMouseRay;
			xz.Raycast(mouseRay, out distanceForMouseRay);
			mouseWorldPosition = mouseRay.GetPoint(distanceForMouseRay);
			//mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);


			if (cardInteractionPack.mouseDownCard) // DOWN
			{
				currentEventCard = cardInteractionPack.mouseDownCard;
				onMouseDownOnCard.Invoke();
				for (int i = 0; i < cardInteractionListeners.Count; i++)
				{
					cardInteractionListeners[i].mouseDownCard = cardInteractionPack.mouseDownCard;
				}
				cardInteractionPack.mouseDownCard = null;
			}
			if (cardInteractionPack.mouseUpCard) // UP
			{
				currentEventCard = cardInteractionPack.mouseUpCard;
				onMouseUpOnCard.Invoke();
				for (int i = 0; i < cardInteractionListeners.Count; i++)
				{
					cardInteractionListeners[i].mouseUpCard = cardInteractionPack.mouseUpCard;
				}
				cardInteractionPack.mouseUpCard = null;
			}
			if (cardInteractionPack.mouseOverCard) // OVER
			{
				currentEventCard = cardInteractionPack.mouseOverCard;
				onMouseOverCard.Invoke();
				for (int i = 0; i < cardInteractionListeners.Count; i++)
				{
					cardInteractionListeners[i].mouseOverCard = cardInteractionPack.mouseOverCard;
				}
				cardInteractionPack.mouseOverCard = null;
			}
			if (cardInteractionPack.mouseEnterCard) // ENTER
			{
				currentEventCard = cardInteractionPack.mouseEnterCard;
				onMouseEnterCard.Invoke();
				for (int i = 0; i < cardInteractionListeners.Count; i++)
				{
					cardInteractionListeners[i].mouseEnterCard = cardInteractionPack.mouseEnterCard;
				}
				cardInteractionPack.mouseEnterCard = null;
			}
			if (cardInteractionPack.mouseExitCard) // EXIT
			{
				currentEventCard = cardInteractionPack.mouseExitCard;
				onMouseExitCard.Invoke();
				for (int i = 0; i < cardInteractionListeners.Count; i++)
				{
					cardInteractionListeners[i].mouseExitCard = cardInteractionPack.mouseExitCard;
				}
				cardInteractionPack.mouseExitCard = null;
			}
			if (cardInteractionPack.mouseDragCard) // DRAG
			{
				currentEventCard = cardInteractionPack.mouseDragCard;
				onMouseDragCard.Invoke();
				for (int i = 0; i < cardInteractionListeners.Count; i++)
				{
					cardInteractionListeners[i].mouseDragCard = cardInteractionPack.mouseDragCard;
				}
				cardInteractionPack.mouseDragCard = null;
			}
			if (cardInteractionPack.mouseClickCard) // CLICK
			{
				currentEventCard = cardInteractionPack.mouseClickCard;
				onMouseClickCard.Invoke();
				for (int i = 0; i < cardInteractionListeners.Count; i++)
				{
					cardInteractionListeners[i].mouseClickCard = cardInteractionPack.mouseClickCard;
				}
				cardInteractionPack.mouseClickCard = null;
			}
		}

		Card currentEventCard;

		public void UseCard ()
		{
			Match.Current.UseCard(currentEventCard);
		}

		//public void UseClickedCard ()
		//{
		//	Match.Current.UseCard(cardInteractionPack.mouseClickCard);
		//}

		//public void SendConditionEffect (string condition, string trueEffect, string falseEffect = "")
		//{
		//	if (Match.Current.CheckCondition(condition))
		//	{
		//		Match.Current.TreatEffect(trueEffect);
		//	}
		//	else
		//	{
		//		Match.Current.TreatEffect(falseEffect);
		//	}
		//}

		//public void SendConditionEffect (string conditionAndEffect)
		//{
		//	string[] condBreakdown = conditionAndEffect.Split('\\');
		//	Debug.Log(condBreakdown.Length);
		//	SendConditionEffect(condBreakdown[0], condBreakdown[1], condBreakdown.Length > 2 ? condBreakdown[2] : "");
		//}

		public void RegisterCardInteractionListener (CardInteractionPack newListener)
		{
			cardInteractionListeners.Add(newListener);
		}

		#endregion
	}
}