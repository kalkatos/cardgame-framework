using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CardgameCore
{
    public class TagEventActor : MonoBehaviour
    {
        public string tagToAct;
        public UnityEvent onTagAddedEvent;
        public UnityEvent onTagRemovedEvent;

		private void OnValidate ()
		{
			for (int i = 0; i < onTagAddedEvent.GetPersistentEventCount(); i++)
				onTagAddedEvent.SetPersistentListenerState(i, UnityEventCallState.EditorAndRuntime);
			for (int i = 0; i < onTagRemovedEvent.GetPersistentEventCount(); i++)
				onTagRemovedEvent.SetPersistentListenerState(i, UnityEventCallState.EditorAndRuntime);
		}

		internal void OnTagAdded ()
		{
            onTagAddedEvent?.Invoke();
		}

        internal void OnTagRemoved ()
		{
            onTagRemovedEvent?.Invoke();
		}
    }
}