using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
	[CreateAssetMenu(fileName = "New Game", menuName = "Cardgame/Game", order = 1)]
	public class Game : ScriptableObject
    {
		public string gameName = "";
		public List<string> phases = new List<string>();
		public List<string> variables = new List<string>();
		public List<string> values = new List<string>();
		public List<Rule> rules = new List<Rule>();
	}
}