using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CardGameFramework
{
	public class MessageReceiver : MatchWatcher
	{
		public string[] messagesToAttend;
		public UnityEvent[] eventsToPerform;

		public override IEnumerator TreatTrigger (TriggerTag triggerTag, params object[] args)
		{
			if (triggerTag == TriggerTag.OnMessageSent)
			{
				string message = (string)GetArgumentWithTag("message", args);
				for (int i = 0; i < messagesToAttend.Length; i++)
				{
					if (messagesToAttend[i] == message)
					{
						eventsToPerform[i].Invoke();
						yield break;
					}
				}
			}
		}
	}
}
