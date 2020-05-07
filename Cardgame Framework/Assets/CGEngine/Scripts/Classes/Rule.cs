using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	public class Rule : MonoBehaviour
	{
		public string ID;
		public RuleData data;
		public string tags { get; private set; }
		public int activeTriggers { get; private set; }
		public NestedBooleans conditions { get; private set; }
		public Command[] commands { get; private set; }
		public Command[] elseCommands { get; private set; }
		public string origin { get; private set; }
		
		public void Initialize(RuleData data, string origin)
		{
			this.data = data;
			this.origin = origin;
			tags = data.tags;
			//triggers
			SetActiveTriggers(data.trigger);
			//conditions
			string dataCondition = data.condition;
			dataCondition.Replace("i:this", "i:" + origin);
			if (string.IsNullOrEmpty(dataCondition))
				conditions = new NestedBooleans(true);
			else
				conditions = new NestedConditions(dataCondition);
			//commands
			string[] commandClauses = data.commands.Replace("i:$this", "i:" + origin).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			commands = new Command[commandClauses.Length];
			for (int i = 0; i < commandClauses.Length; i++)
			{
				commands[i] = Match.Current.CreateCommand(commandClauses[i]);
			}

			commandClauses = data.elseCommands.Replace("i:$this", "i:" + origin).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			elseCommands = new Command[commandClauses.Length];
			for (int i = 0; i < commandClauses.Length; i++)
			{
				elseCommands[i] = Match.Current.CreateCommand(commandClauses[i]);
			}
		}


		void SetActiveTriggers (string triggers)
		{
			triggers = StringUtility.GetCleanStringForInstructions(triggers);
			string[] triggerSplit = triggers.Split(',');
			for (int i = 0; i < triggerSplit.Length; i++)
			{
				if (Enum.TryParse(triggerSplit[i], out TriggerLabel label))
					activeTriggers += (int)label;
			}
		}

	}
}