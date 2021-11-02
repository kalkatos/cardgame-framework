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
			Match.AddMessageSentCallback(MessageReceived);
		}

		private void OnDestroy()
		{
			Match.RemoveMessageSentCallback(MessageReceived);
		}

		private IEnumerator MessageReceived (string message, string additionalInfo)
		{
			if (message == this.message)
				messageReceivedEvent.Invoke();
			yield return null;
		}
	}
}