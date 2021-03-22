using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CardgameCore
{
    public class MessageActor : MonoBehaviour
    {
		[SerializeField] private string message;
		[SerializeField] private UnityEvent messageReceivedEvent;

		private void Awake()
		{
			Match.OnMessageSent += MessageReceived;
		}

		private void OnDestroy()
		{
			Match.OnMessageSent -= MessageReceived;
		}

		private IEnumerator MessageReceived ()
		{
			if (Match.GetVariable("message") == message)
				messageReceivedEvent.Invoke();
			yield break;
		}
	}
}