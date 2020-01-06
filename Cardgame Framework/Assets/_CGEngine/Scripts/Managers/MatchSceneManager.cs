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

	public class MatchSceneManager : BasicSceneManager, IMessageReceiver
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
			MessageBus.Register("All", this);

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
							Card newCard = Instantiate(CGEngine.Instance.cardTemplate, position, Quaternion.identity, container).GetComponent<Card>();
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
							Card newCard = Instantiate(CGEngine.Instance.cardTemplate, position, Quaternion.identity, container).GetComponent<Card>();
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
				CGEngine.Instance.SceneEnded();
			}
			yield return null;
		}

		public static Vector3 GetMouseWorldPosition (Plane plane)
		{
			float distance;
			plane.Raycast(Instance.mouseRay, out distance);
			return Instance.mouseRay.GetPoint(distance);
		}


		private void Update()
		{
			mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);
			float distanceForMouseRay;
			xz.Raycast(mouseRay, out distanceForMouseRay);
			mouseWorldPosition = mouseRay.GetPoint(distanceForMouseRay);
			//mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
		}

		public void TreatMessage(string type, InputObject inputObject)
		{
			switch (type)
			{
				case "ObjectClicked":
					Card c = inputObject.GetComponent<Card>();
					if (c) Match.Current.ClickCard(c);
					break;
			}
		}

	}
}