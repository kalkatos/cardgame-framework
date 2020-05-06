using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CardGameFramework
{
    public class MessageWatcher : MatchWatcher
    {
        [SerializeField] List<string> messages;
        [SerializeField] List<UnityEvent> events;

        public override IEnumerator OnMessageSent(string message)
        {
            if (messages.Contains(message))
            {
                events[messages.IndexOf(message)].Invoke();
            }
            yield return null;
        }
    }
}