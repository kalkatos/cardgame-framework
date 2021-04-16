using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
	[Serializable]
    public class Rule
    {
        public string name;
		[HideInInspector] public string id;
        public string tags;
        public TriggerLabel type;
		public string condition;
		public string commands;
		public NestedBooleans conditionObject = new NestedBooleans();
        public List<Command> commandsList = new List<Command>();

        public void Initialize ()
		{
			conditionObject = new NestedConditions(condition);
			commandsList = Match.CreateCommands(commands);
		}

		public override string ToString()
		{
			return $"{name} (id: {id})";
		}
	}
}