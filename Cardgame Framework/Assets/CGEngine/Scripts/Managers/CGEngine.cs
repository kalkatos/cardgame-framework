using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CardGameFramework
{
	public class CGEngine : MonoBehaviour
	{
		static CGEngine instance;
		public static CGEngine Instance
		{
			get
			{
				if (instance == null)
				{
					instance = FindObjectOfType<CGEngine>();
					if (!instance)
					{
						GameObject go = new GameObject("CGEngineManager");
						instance = go.AddComponent<CGEngine>();
					}
				}
				return instance;
			}
		}

		public CardGameData autoStartGame;

		int matchIdTracker;
		internal HashSet<string> systemVariables;
		public static string[] systemVariableNames =
		{
			"movedCard",
			"clickedCard",
			"usedCard",
			//zone
			"targetZone",
			"oldZone",
			//string
			"phase",
			"actionName",
			"message",
			"additionalInfo",
			"variable",
			//number
			"matchNumber",
			"turnNumber",
			"value",
			"min",
			"max"
		};

		private void Awake ()
		{
			if (instance == null)
			{
				instance = this;
			}
			else if (instance != this)
			{
				DestroyImmediate(gameObject);
				return;
			}

			DontDestroyOnLoad(gameObject);

			systemVariables = new HashSet<string>();
			for (int i = 0; i < systemVariableNames.Length; i++)
			{
				systemVariables.Add(systemVariableNames[i]);
			}

		}

		private void Start ()
		{
			if (autoStartGame != null && autoStartGame.rules != null && autoStartGame.rules.Count > 0)
			{
				Ruleset rules = autoStartGame.rules[0];
				StartMatch(autoStartGame, rules);
			}
		}

		public static bool IsSystemVariable (string variableName)
		{
			return Instance.systemVariables.Contains(variableName);
		}

		public static void StartMatch (CardGameData game, Ruleset rules)
		{
			Match match = new GameObject("CurrentMatch").AddComponent<Match>();
			match.id = "a" + (++Instance.matchIdTracker);
			match.matchNumber = Instance.matchIdTracker;
			Debug.Log(string.Format("[CGEngine] Match {0} created successfully.", match.id));
			match.Initialize(game, rules);
		}

		public static void CreateCards (GameObject template, List<CardData> cards, Vector3 position, Transform container = null)
		{
			Vector3 posInc = Vector3.up * 0.01f;
			if (cards != null)
			{
				for (int i = 0; i < cards.Count; i++)
				{
					Card newCard = Instantiate(template, position, Quaternion.identity, container).GetComponent<Card>();
					position += posInc;
					newCard.SetupData(cards[i]);
					newCard.gameObject.name = cards[i].cardDataID;
				}
			}
		}

		public void SendAction (string actionName)
		{
			if (Match.Current)
				Match.Current.UseAction(actionName);
		}

		/*
		public void SceneEnded ()
		{
			//FOR DEBUG
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}

		void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			CurrentScene = FindObjectOfType<BasicSceneManager>();
			if (CurrentScene == null)
			{
				Debug.LogError("CGEngine: Every scene must have a BasicSceneManager object.");
				return;
			}

			//FOR DEBUG
			User otherUser = new User(UserType.CPU);
			CurrentScene.Initialize(gameData.rules[0], localUser, otherUser);
		}

		[Header("Debug")]
		public bool debug;
		public string str;

		public void Test ()
		{
			Match.Current.Test(str);
		}
		*/
	}
}
