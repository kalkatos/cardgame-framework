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
		public string trueCommands;
		public string falseCommands;
		public NestedBooleans conditionObject = new NestedBooleans();
        public List<Command> trueCommandsList = new List<Command>();
        public List<Command> falseCommandsList = new List<Command>();

        public void Initialize ()
		{
			conditionObject = new NestedConditions(condition);
			trueCommandsList = Match.CreateCommands(trueCommands);
			falseCommandsList = Match.CreateCommands(falseCommands);
		}

		public override string ToString()
		{
			return $"{name} (id: {id})";
		}
	}
}