using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
	public class Rule : ScriptableObject
	{
		[HideInInspector] public Game game;
		[HideInInspector] public string id;
		[HideInInspector] public string origin;
		public string tags;
		public TriggerLabel trigger;
		public string condition;
		public string commands;
		public NestedBooleans conditionObject;
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

		public void Copy (Rule other)
		{
			name = other.name;
			game = other.game;
			id = other.id;
			origin = other.origin;
			tags = other.tags;
			trigger = other.trigger;
			condition = other.condition;
			commands = other.commands;
		}
	}
}