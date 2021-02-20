using System;
using System.Collections.Generic;

namespace CardgameCore
{
	[Serializable]
    public class Rule
    {
        public string name;
        public string tags;
        public TriggerType type;
		public Func<Match, bool> condition = (x) => { return true; };
        public List<Command> trueCommands = new List<Command>();
        public List<Command> falseCommands = new List<Command>();

		public Rule (string name, TriggerType type, Func<Match, bool> condition, Command[] trueCommands, Command[] falseCommands = null)
		{
			this.name = name;
			this.type = type;
			if (condition != null)
				this.condition = condition;
			if (trueCommands != null)
				this.trueCommands.AddRange(trueCommands);
			if (falseCommands != null)
				this.falseCommands.AddRange(falseCommands);
		}
	}
}