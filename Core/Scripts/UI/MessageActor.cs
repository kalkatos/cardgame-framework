using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CardgameFramework
{
    public class MessageActor : MonoBehaviour
    {
		[SerializeField] private string message;
		[SerializeField] private UnityEvent messageReceivedEvent;

		private void Awake()
		{
			Match.AddMessageSentCallback(new NestedConditions($"message={message}"), MessageReceived, $"Message Actor ({name})");
		}

		private void OnDestroy()
		{
			Match.RemoveMessageSentCallback(MessageReceived);
		}

		private IEnumerator MessageReceived (string message, string additionalInfo)
		{
			messageReceivedEvent.Invoke();
			yield return null;
		}
	}
}