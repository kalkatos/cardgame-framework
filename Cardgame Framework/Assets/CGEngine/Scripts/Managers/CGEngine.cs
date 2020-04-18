using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	[DisallowMultipleComponent]
	public class CGEngine : MonoBehaviour
	{
		static CGEngine _instance;
		public static CGEngine instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindObjectOfType<CGEngine>();
					if (!_instance)
					{
						GameObject go = new GameObject("CGEngineManager");
						_instance = go.AddComponent<CGEngine>();
					}
				}
				return _instance;
			}
		}

		int matchIdTracker;
		internal HashSet<string> systemVariables;
		public static string[] systemVariableNames =
		{
			//card
			"movedCard",
			"clickedCard",
			"usedCard",
			//zone
			"targetZone",
			"oldZone",
			"usedZone",
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
			if (_instance == null)
			{
				_instance = this;
			}
			else if (_instance != this)
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
			return instance.systemVariables.Contains(variableName);
		}

		public static void StartMatch (CardGameData game, Ruleset rules)
		{
			Match match = new GameObject("CurrentMatch").AddComponent<Match>();
			match.ID = "a" + (++instance.matchIdTracker);
			match.matchNumber = instance.matchIdTracker;
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
