using System.Collections.Generic;
using UnityEngine;

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

		

		public static bool IsSystemVariable (string variableName)
		{
			return Instance.systemVariables.Contains(variableName);
		}

		public static void StartMatch (CardGameData game, Ruleset rules)
		{
			Match match = new GameObject("CurrentMatch").AddComponent<Match>();
			match.ID = "a" + (++Instance.matchIdTracker);
			match.matchNumber = Instance.matchIdTracker;
			Debug.Log(string.Format("[CGEngine] Match {0} created successfully.", match.ID));
			match.Initialize(game, rules);
		}

		public static void CreateCards (GameObject template, List<CardData> cards, Vector3 position, Transform container = null)
		{
			Vector3 posInc = Vector3.up * 0.005f;
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

		
	}
}
