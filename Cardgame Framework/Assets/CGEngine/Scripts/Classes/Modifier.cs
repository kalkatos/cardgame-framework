using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGameFramework
{
	public class Modifier : MonoBehaviour
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
		string origin; //ID of origin card or player
		public string Origin
		{
			get
			{
				return origin;
			}
			set
			{
				origin = value;
				if (value != "")
				{
					condition = !string.IsNullOrEmpty(condition) ? condition.Replace("#this", "#" + value) : "";
					trigger = !string.IsNullOrEmpty(trigger) ? trigger.Replace("#this", "#" + value) : "";
					affected = !string.IsNullOrEmpty(affected) ? affected.Replace("#this", "#" + value) : "";
					trueEffect = !string.IsNullOrEmpty(trueEffect) ? trueEffect.Replace("#this", "#" + value) : "";
					falseEffect = !string.IsNullOrEmpty(falseEffect) ? falseEffect.Replace("#this", "#" + value) : "";
					target = !string.IsNullOrEmpty(target) ? target.Replace("#this", "#" + value) : "";
				}
				else if (data)
				{
					condition = data.condition;
					trigger = data.trigger;
					affected = data.affected;
					trueEffect = data.trueEffect;
					falseEffect = data.falseEffect;
				}
				else
					Debug.LogWarning("CGEngine: Modifier " + id + " changed its Origin field and ocurrencies of #this on other fields must be reviewed.");
			}
		}
		public string target; //ID of modified target
		public double numValue; //TODO MED implement changing of number
		List<Modifier> modifiers;
		public List<Modifier> Modifiers { get { if (modifiers == null) modifiers = new List<Modifier>(); return modifiers; } }

		public void Initialize(ModifierData data, string origin)
		{
			this.data = data;
			Origin = origin;
			tags = new List<string>();
			if (!string.IsNullOrEmpty(data.tags))
				tags.AddRange(data.tags.Split(','));
			condition = data.condition;
			trigger = data.trigger;
			affected = data.affected;
			trueEffect = data.trueEffect;
			falseEffect = data.falseEffect;
			numValue = data.startingNumValue;
		}

		public void Initialize (params string[] tags)
		{
			this.tags = new List<string>();
			this.tags.AddRange(tags);
		}
	}
}