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

		int matchIdTracker;
		internal HashSet<string> systemVariables;

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
			//card
			systemVariables.Add("movedCard");
			systemVariables.Add("clickedCard");
			systemVariables.Add("usedCard");
			//zone
			systemVariables.Add("targetZone");
			systemVariables.Add("oldZone");
			//string
			systemVariables.Add("phase");
			systemVariables.Add("actionName");
			systemVariables.Add("message");
			systemVariables.Add("additionalInfo");
			systemVariables.Add("variable");
			//number
			systemVariables.Add("matchNumber");
			systemVariables.Add("turnNumber");
			systemVariables.Add("value");
			systemVariables.Add("min");
			systemVariables.Add("max");
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
			Debug.Log(StringUtility.BuildMessage("Match @ created successfully.", match.id));
			match.Initialize(game, rules);
		}

		public static void CreateCards(GameObject template, List<CardData> cards, Vector3 position, Transform container = null)
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
