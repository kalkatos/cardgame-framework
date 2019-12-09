using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
{
	public class Modifier : InputObject
	{
		/*
		//Adds an effect to a card, player, zone or other modifier

		id : string   // starts with "m"
		data : ModifierData
		isActive : bool
		origin : string  //id of origin card or player
		target : string //id of modified target
		*/

		public string id;   // starts with "m"
		public ModifierData data;
		public List<string> tags;
		//public bool isActive;
		public string condition;
		public string trigger;
		public string affected;
		public string trueEffect;
		public string falseEffect;
		public string origin;  //ID of origin card or player
		public string target; //ID of modified target
		public double numValue; //TODO MED implement changing of number
		List<Modifier> modifiers;
		public List<Modifier> Modifiers { get { if (modifiers == null) modifiers = new List<Modifier>(); return modifiers; } }

		public void Initialize(ModifierData data, string origin)
		{
			this.data = data;
			this.origin = origin;
			tags = new List<string>();
			if (!string.IsNullOrEmpty(data.tags))
				tags.AddRange(data.tags.Split(','));
			condition = data.condition;
			trigger = data.trigger;
			affected = data.affected;
			trueEffect = data.trueEffect;
			falseEffect = data.falseEffect;
		}

		public void Initialize (params string[] tags)
		{
			this.tags = new List<string>();
			this.tags.AddRange(tags);
		}
	}
}