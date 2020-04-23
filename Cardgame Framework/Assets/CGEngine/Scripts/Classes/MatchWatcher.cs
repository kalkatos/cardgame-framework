using System.Collections;
using UnityEngine;

namespace CardGameFramework
{
	public class MatchWatcher : MonoBehaviour
	{
		public int priority;
		TriggerLabel _labels;
		public TriggerLabel labels
		{
			get
			{
				if (_labels == TriggerLabel.None)
				{
					if (GetType().GetMethod("OnZoneUsed").DeclaringType != typeof(MatchWatcher))
						_labels += (int)TriggerLabel.OnZoneUsed;
					if (GetType().GetMethod("OnCardUsed").DeclaringType != typeof(MatchWatcher))
						_labels += (int)TriggerLabel.OnCardUsed;
					if (GetType().GetMethod("OnCardEnteredZone").DeclaringType != typeof(MatchWatcher))
						_labels += (int)TriggerLabel.OnCardEnteredZone;
					if (GetType().GetMethod("OnCardLeftZone").DeclaringType != typeof(MatchWatcher))
						_labels += (int)TriggerLabel.OnCardLeftZone;
					if (GetType().GetMethod("OnMatchSetup").DeclaringType != typeof(MatchWatcher))
						_labels += (int)TriggerLabel.OnMatchSetup;
					if (GetType().GetMethod("OnMatchStarted").DeclaringType != typeof(MatchWatcher))
						_labels += (int)TriggerLabel.OnMatchStarted;
					if (GetType().GetMethod("OnMatchEnded").DeclaringType != typeof(MatchWatcher))
						_labels += (int)TriggerLabel.OnMatchEnded;
					if (GetType().GetMethod("OnTurnStarted").DeclaringType != typeof(MatchWatcher))
						_labels += (int)TriggerLabel.OnTurnStarted;
					if (GetType().GetMethod("OnTurnEnded").DeclaringType != typeof(MatchWatcher))
						_labels += (int)TriggerLabel.OnTurnEnded;
					if (GetType().GetMethod("OnPhaseStarted").DeclaringType != typeof(MatchWatcher))
						_labels += (int)TriggerLabel.OnPhaseStarted;
					if (GetType().GetMethod("OnPhaseEnded").DeclaringType != typeof(MatchWatcher))
						_labels += (int)TriggerLabel.OnPhaseEnded;
					if (GetType().GetMethod("OnMessageSent").DeclaringType != typeof(MatchWatcher))
						_labels += (int)TriggerLabel.OnMessageSent;
					if (GetType().GetMethod("OnVariableChanged").DeclaringType != typeof(MatchWatcher))
						_labels += (int)TriggerLabel.OnVariableChanged;
					if (GetType().GetMethod("OnActionUsed").DeclaringType != typeof(MatchWatcher))
						_labels += (int)TriggerLabel.OnActionUsed;

					Debug.Log($"     Object {name} has declarations for {_labels}");
				}
				return _labels;
			}

			protected set
			{
				_labels = value;
			}
		}

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