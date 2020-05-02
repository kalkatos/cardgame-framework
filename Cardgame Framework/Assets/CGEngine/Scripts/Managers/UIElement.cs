using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CardGameFramework
{
	public class UIElement : MatchWatcher
	{
		public List<TriggeredConditionedEvent> triggerEvents;
		public List<MessageForUIEvent> messageEvents;
		public List<VariableDisplayText> variableDisplayTexts;
		public AudioSource audioSource;
		public List<MessageForRandomSFX> messageToSFX;

		public void ChangeScene (string nextSceneName)
		{
			SceneManager.LoadScene(nextSceneName);
		}

		public void SendAction (string actionName)
		{
			if (Match.Current)
				Match.Current.UseAction(actionName);
			else
				Debug.LogWarning($"[CGEngine] Action {actionName} was used but there is no Match currently active");
		}

		public void WaitForSeconds (float seconds)
		{
			Match.Current.WaitForSeconds(seconds);
		}

		void InvokeMatchTriggerEvents (TriggerLabel label)
		{
			for (int i = 0; i < triggerEvents.Count; i++)
			{
				TriggeredConditionedEvent triggerForUIEvent = triggerEvents[i];
				if (triggerForUIEvent.triggerLabel.HasFlag(label) && triggerForUIEvent.nestedCondition.Evaluate())
				{
					triggerForUIEvent.conditionEvent.Invoke();
				}
			}
		}

		public override IEnumerator OnZoneUsed (Zone zone) { InvokeMatchTriggerEvents(TriggerLabel.OnZoneUsed); yield return null; }
		public override IEnumerator OnCardUsed (Card card) { InvokeMatchTriggerEvents(TriggerLabel.OnCardUsed); yield return null; }
		public override IEnumerator OnCardEnteredZone (Card card, Zone newZone, Zone oldZone, params string[] additionalParamenters) { InvokeMatchTriggerEvents(TriggerLabel.OnCardEnteredZone); yield return null; }
		public override IEnumerator OnCardLeftZone (Card card, Zone oldZone) { InvokeMatchTriggerEvents(TriggerLabel.OnCardLeftZone); yield return null; }
		public override IEnumerator OnMatchStarted (int matchNumber) { InvokeMatchTriggerEvents(TriggerLabel.OnMatchStarted); yield return null; }
		public override IEnumerator OnMatchEnded (int matchNumber) { InvokeMatchTriggerEvents(TriggerLabel.OnMatchEnded); yield return null; }
		public override IEnumerator OnTurnStarted (int turnNumber) { InvokeMatchTriggerEvents(TriggerLabel.OnTurnStarted); yield return null; }
		public override IEnumerator OnTurnEnded (int turnNumber) { InvokeMatchTriggerEvents(TriggerLabel.OnTurnEnded); yield return null; }
		public override IEnumerator OnPhaseStarted (string phase) { InvokeMatchTriggerEvents(TriggerLabel.OnPhaseStarted); yield return null; }
		public override IEnumerator OnPhaseEnded (string phase) { InvokeMatchTriggerEvents(TriggerLabel.OnPhaseEnded); yield return null; }
		public override IEnumerator OnActionUsed (string actionName) { InvokeMatchTriggerEvents(TriggerLabel.OnActionUsed); yield return null; }

		public override IEnumerator OnMatchSetup (int matchNumber)
		{
			//Prepare conditions in Match Events Watcher
			for (int i = 0; i < triggerEvents.Count; i++)
			{
				triggerEvents[i].Initialize();
			}

			//Prepare variable watchers
			for (int i = 0; i < variableDisplayTexts.Count; i++)
			{
				VariableDisplayText displayText = variableDisplayTexts[i];
				string[] formatSplit = displayText.displayFormat.Split('{', '}');
				displayText.variablesBeingWatched = new HashSet<string>();
				displayText.uiText.text = displayText.displayFormat;
				for (int j = 0; j < formatSplit.Length; j++)
				{
					string varName = formatSplit[j];
					if (Match.Current.HasVariable(varName))
					{
						string varValue = Match.Current.GetVariable(varName).ToString();
						displayText.variablesBeingWatched.Add(varName);
						displayText.uiText.text = displayText.uiText.text.Replace("{" + varName + "}", varValue);
					}
				}
			}

			InvokeMatchTriggerEvents(TriggerLabel.OnMatchSetup);

			yield return null;
		}

		public override IEnumerator OnMessageSent (string message)
		{
			for (int i = 0; i < messageEvents.Count; i++)
			{
				if (message == messageEvents[i].message)
				{
					messageEvents[i].eventToExecute.Invoke();
					break;
				}
			}

			if (audioSource)
			{
				for (int i = 0; i < messageToSFX.Count; i++)
				{
					MessageForRandomSFX clips = messageToSFX[i];
					if (message == clips.message)
					{
						int randomSFX = Random.Range(0, clips.sfx.Count);
						audioSource.clip = clips.sfx[randomSFX];
						audioSource.Play();
						break;
					}
				}
			}

			InvokeMatchTriggerEvents(TriggerLabel.OnMessageSent);

			yield return null;
		}

		public override IEnumerator OnVariableChanged (string variable, object value)
		{
			for (int i = 0; i < variableDisplayTexts.Count; i++)
			{
				VariableDisplayText displayText = variableDisplayTexts[i];
				if (displayText.variablesBeingWatched.Contains(variable))
				{
					string result = displayText.displayFormat;
					foreach (string item in displayText.variablesBeingWatched)
					{
						result = result.Replace("{" + item + "}", Match.Current.GetVariable(item).ToString());
					}
					displayText.uiText.text = result;
					
				}
			}

			InvokeMatchTriggerEvents(TriggerLabel.OnVariableChanged);

			yield return null;
		}
	}

	//[System.Serializable]
	//public class MessageForUIEvent
	//{
	//	public string message;
	//	public UnityEvent eventToExecute;
	//}

	//[System.Serializable]
	//public class ConditionedEvent
	//{
	//	public string condition;
	//	public UnityEvent conditionEvent;
	//	public NestedConditions nestedCondition;

	//	public void Initialize ()
	//	{
	//		nestedCondition = new NestedConditions(condition);
	//	}
	//}

	//[System.Serializable]
	//public class TriggeredConditionedEvent : ConditionedEvent
	//{
	//	public TriggerLabel triggerLabel;
	//}

	//[System.Serializable]
	//public class VariableDisplayText
	//{
	//	public string displayFormat;
	//	public TMP_Text uiText;
	//	public HashSet<string> variablesBeingWatched;
	//}

	//[System.Serializable]
	//public class MessageForRandomSFX
	//{
	//	public string message;
	//	public List<AudioClip> sfx;
	//}
}