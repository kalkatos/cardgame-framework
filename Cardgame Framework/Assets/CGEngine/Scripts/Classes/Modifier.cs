using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	public class Modifier : MonoBehaviour
	{

		//Adds an effect to a card, player, zone or other modifier
		public string ID;   // starts with "m"
		public ModifierData data;
		public string tags { get; private set; }
		public int activeTriggers { get; private set; }
		public NestedBooleans conditions { get; private set; }
		public Command[] commands { get; private set; }
		public string origin { get; private set; }
		
		public void Initialize(ModifierData data, string origin)
		{
			this.data = data;
			this.origin = origin;
			tags = data.tags;
			//triggers
			SetActiveTriggers(data.trigger);
			//conditions
			string dataCondition = data.condition;
			dataCondition.Replace("#this", "#" + origin);
			if (string.IsNullOrEmpty(dataCondition))
				conditions = new NestedBooleans(true);
			else
				conditions = new NestedConditions(dataCondition);
			//commands
			string[] commandClauses = data.commands.Replace("#this", "#" + origin).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			commands = new Command[commandClauses.Length];
			for (int i = 0; i < commandClauses.Length; i++)
			{
				commands[i] = Match.Current.CreateCommand(commandClauses[i]);
			}
		}


		void SetActiveTriggers (string triggers)
		{
			string[] triggerTags = Enum.GetNames(typeof(TriggerTag));
			for (int i = 0; i < triggerTags.Length; i++)
			{
				if (triggers.Contains(triggerTags[i]))
				{
					activeTriggers += (int)Enum.Parse(typeof(TriggerTag), triggerTags[i]);
				}
			}
		}

	}
}