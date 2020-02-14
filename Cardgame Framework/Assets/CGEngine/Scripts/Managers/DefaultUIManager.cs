using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CardGameFramework
{
	public class DefaultUIManager : MatchWatcher, IInputEventReceiver
	{
		public CardGameData autoStartGame;
		[Header("   Input Events  - - - ")]
		public InputType[] inputTypes;
		public UnityEvent[] inputEvents;
		[Header("   Match Events  - - - ")]
		public TriggerTag[] triggerTags;
		public UnityEvent[] triggerEvents;

		InputObject target;

		private void Start ()
		{
			InputManager.Register(InputType.All, this);

			if (autoStartGame != null && autoStartGame.rules != null && autoStartGame.rules.Count > 0)
			{
				Ruleset rules = autoStartGame.rules[0];
				CGEngine.StartMatch(autoStartGame, rules);
			}
		}

		public void SendAction (string actionName)
		{
			if (Match.Current)
				Match.Current.UseAction(actionName);
		}

		public void UseObjectClicked ()
		{
			Card c = target.GetComponent<Card>();
			if (c)
				Match.Current.UseCard(c);
			else
			{
				Zone z = target.GetComponent<Zone>();
				if (z)
					Match.Current.UseZone(z);
			}
		}

		public void TreatEvent (InputType type, InputObject inputObject)
		{
			target = inputObject;
			for (int i = 0; i < inputTypes.Length; i++)
			{
				if (type == inputTypes[i])
					inputEvents[i].Invoke();
			}
		}

		public override IEnumerator TreatTrigger (TriggerTag triggerTag, params object[] args)
		{
			for (int i = 0; i < triggerTags.Length; i++)
			{
				if (triggerTag == triggerTags[i])
				{
					triggerEvents[i].Invoke();
				}
			}
			yield return null;
		}
	}
}