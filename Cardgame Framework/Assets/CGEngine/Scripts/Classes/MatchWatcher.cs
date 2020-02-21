using System.Collections;
using UnityEngine;

namespace CardGameFramework
{
	public abstract class MatchWatcher : MonoBehaviour
	{
		//private void Start()
		//{
		//	Register();
		//}

		//public void Register()
		//{
		//	if (Match.Current)
		//		Match.Current.AddWatcher(this);
		//}
		//public abstract IEnumerator TreatMatchTrigger(TriggerTag triggerTag, params object[] args);

		/*
		OnZoneUsed
		OnCardUsed
		OnCardEnteredZone
		OnCardLeftZone
		OnMatchSetup
		OnMatchStarted
		OnMatchEnded
		OnTurnStarted
		OnTurnEnded
		OnPhaseStarted
		OnPhaseEnded
		OnMessageSent
		OnVariableChanged
		OnActionUsed
		*/

		public virtual IEnumerator OnZoneUsed (Zone zone) { yield return null; }
		public virtual IEnumerator OnCardUsed (Card card) { yield return null; }
		public virtual IEnumerator OnCardEnteredZone (Card card, Zone newZone, Zone oldZone, params string[] additionalParamenters) { yield return null; }
		public virtual IEnumerator OnCardLeftZone (Card card, Zone oldZone) { yield return null; }
		public virtual IEnumerator OnMatchSetup (int matchNumber) { yield return null; }
		public virtual IEnumerator OnMatchStarted (int matchNumber) { yield return null; }
		public virtual IEnumerator OnMatchEnded (int matchNumber) { yield return null; }
		public virtual IEnumerator OnTurnStarted (int turnNumber) { yield return null; }
		public virtual IEnumerator OnTurnEnded (int turnNumber) { yield return null; }
		public virtual IEnumerator OnPhaseStarted (string phase) { yield return null; }
		public virtual IEnumerator OnPhaseEnded (string phase) { yield return null; }
		public virtual IEnumerator OnMessageSent (string message) { yield return null; }
		public virtual IEnumerator OnVariableChanged (string variable, object value) { yield return null; }
		public virtual IEnumerator OnActionUsed (string actionName) { yield return null; }

		//protected object GetArgumentWithTag (string tag, object[] args)
		//{
		//	if (args == null) return null;
		//	for (int i = 0; i < args.Length; i+=2)
		//	{
		//		if (args[i].ToString() == tag)
		//			if (args.Length > i + 1)
		//				return args[i + 1];
		//	}
		//	Debug.LogWarning("CGEngine: Couldn't find argument: " + tag);
		//	return null;
		//}

		//protected bool HasString (string tag, string[] arr)
		//{
		//	if (arr == null) return false;
		//	for (int i = 0; i < arr.Length; i++)
		//	{
		//		if (arr[i] == tag)
		//			return true;
		//	}
		//	return false;
		//}
	}
}