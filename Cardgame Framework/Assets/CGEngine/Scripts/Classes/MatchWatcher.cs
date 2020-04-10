using System.Collections;
using UnityEngine;

namespace CardGameFramework
{
	public abstract class MatchWatcher : MonoBehaviour
	{
		public int priority;

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
				
	}
}