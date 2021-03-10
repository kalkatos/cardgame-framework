using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
	[CreateAssetMenu(fileName = "New Component", menuName = "Cardgame/Component", order = 2)]
	public class ComponentData : ScriptableObject
	{
		public GameObject prefab;
		public new string name = "";
		public string tags = "";
		public List<ComponentField> fields = new List<ComponentField>();
		public List<Rule> rules = new List<Rule>();
	}
}
