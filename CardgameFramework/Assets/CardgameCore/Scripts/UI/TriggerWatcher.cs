using UnityEngine;
using UnityEngine.Events;

namespace CardgameCore
{
	public class TriggerWatcher : MonoBehaviour
	{
		public TriggerLabel trigger;
		public UnityEvent<string> stringEvent;


	}
}