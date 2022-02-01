using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CardgameFramework
{
    public class TagEventActor : MonoBehaviour
    {
        public string tagToAct;
        public UnityEvent<string> onTagAddedEvent;
        public UnityEvent<string> onTagRemovedEvent;

		private void OnValidate ()
		{
			for (int i = 0; i < onTagAddedEvent.GetPersistentEventCount(); i++)
				onTagAddedEvent.SetPersistentListenerState(i, UnityEventCallState.EditorAndRuntime);
			for (int i = 0; i < onTagRemovedEvent.GetPersistentEventCount(); i++)
				onTagRemovedEvent.SetPersistentListenerState(i, UnityEventCallState.EditorAndRuntime);
		}

		internal void OnTagAdded (string tag)
		{
            onTagAddedEvent?.Invoke(tag);
		}

        internal void OnTagRemoved (string tag)
		{
            onTagRemovedEvent?.Invoke(tag);
		}
    }
}