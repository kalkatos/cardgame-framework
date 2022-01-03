using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameFramework
{
	[CreateAssetMenu(fileName = "New Game", menuName = "Cardgame/Game", order = 1)]
	public class Game : ScriptableObject
    {
		public List<string> phases = new List<string>();
		public List<VariableValuePair> variablesAndValues = new List<VariableValuePair>();
		[HideInInspector] public List<Rule> rules = new List<Rule>();
	}

	[System.Serializable]
	public class VariableValuePair
	{
		public string variable;
		public string value;
	}
}