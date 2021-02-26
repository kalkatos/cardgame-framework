using System;
using System.Collections.Generic;

namespace CardgameCore
{
	[Serializable]
    public class Rule
    {
        public string name;
        public string tags;
        public TriggerLabel type;
		public string condition;
		public string trueCommands;
		public string falseCommands;
		public NestedBooleans conditionObject = new NestedBooleans();
        public List<Command> trueCommandsList = new List<Command>();
        public List<Command> falseCommandsList = new List<Command>();

	}
}