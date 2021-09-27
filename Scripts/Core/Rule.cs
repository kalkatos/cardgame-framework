using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
	public class Rule : ScriptableObject
	{
		[HideInInspector] public string id;
		[HideInInspector] public string origin;
		public string tags;
		public TriggerLabel trigger;
		public string condition;
		public string commands;
		public NestedBooleans conditionObject = new NestedBooleans();
		public List<Command> commandsList = new List<Command>();

		public void Initialize ()
		{
			conditionObject = new NestedConditions(condition);
			commandsList = Match.CreateCommands(commands);
		}

		public override string ToString ()
		{
			return $"{name} (id: {id})";
		}
	}
}