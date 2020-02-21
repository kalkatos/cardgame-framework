using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CardGameFramework
{
	public class DefaultUIManager : MatchWatcher
	{
		public CardGameData autoStartGame;
		
		[Header("   Match Events  - - - ")]
		public TriggerTag[] triggerTags;
		public string[] conditions;
		public UnityEvent[] triggerEvents;
		NestedConditions[] nestedConditions;
		Dictionary<TriggerTag, List<int>> triggerReferences;
		[Header("   Message Receivers  - - - ")]
		public string[] messages;
		public UnityEvent[] messageEvents;
		[Header("   Variable Display Text  - - - ")]
		public string[] variableFormats;
		public TMP_Text[] uiText;
		HashSet<string>[] watchingVariables;
		[Header("   Message to SFX - - - ")]
		public AudioSource audioSource;
		public string[] messagesToSFX;
		public List<AudioClip[]> audioClips;
		

		private void Start ()
		{

			if (autoStartGame != null && autoStartGame.rules != null && autoStartGame.rules.Count > 0)
			{
				Ruleset rules = autoStartGame.rules[0];
				CGEngine.StartMatch(autoStartGame, rules);
			}

			triggerReferences = new Dictionary<TriggerTag, List<int>>();
			for (int i = 0; i < triggerTags.Length; i++)
			{
				if (!triggerReferences.ContainsKey(triggerTags[i]))
					triggerReferences.Add(triggerTags[i], new List<int>());
				triggerReferences[triggerTags[i]].Add(i);
			}


		}

		public void SendAction (string actionName)
		{
			if (Match.Current)
				Match.Current.UseAction(actionName);
			else
				Debug.LogWarning($"[CGEngine] Action {actionName} was used but there is no Match currently active");
		}

		void InvokeMatchTriggerEvents (TriggerTag tag)
		{
			if (triggerReferences.ContainsKey(tag))
			{
				List<int> triggersToResolve = triggerReferences[tag];
				for (int i = 0; i < triggersToResolve.Count; i++)
				{
					int indexToResolve = triggersToResolve[i];
					if (nestedConditions[indexToResolve].Evaluate())
					{
						triggerEvents[indexToResolve].Invoke();
					}
				}
			}
		}

		public override IEnumerator OnZoneUsed (Zone zone) { InvokeMatchTriggerEvents(TriggerTag.OnZoneUsed); yield return null; }
		public override IEnumerator OnCardUsed (Card card) { InvokeMatchTriggerEvents(TriggerTag.OnCardUsed); yield return null; }
		public override IEnumerator OnCardEnteredZone (Card card, Zone newZone, Zone oldZone, params string[] additionalParamenters) { InvokeMatchTriggerEvents(TriggerTag.OnCardEnteredZone); yield return null; }
		public override IEnumerator OnCardLeftZone (Card card, Zone oldZone) { InvokeMatchTriggerEvents(TriggerTag.OnCardLeftZone); yield return null; }
		public override IEnumerator OnMatchStarted (int matchNumber) { InvokeMatchTriggerEvents(TriggerTag.OnMatchStarted); yield return null; }
		public override IEnumerator OnMatchEnded (int matchNumber) { InvokeMatchTriggerEvents(TriggerTag.OnMatchEnded); yield return null; }
		public override IEnumerator OnTurnStarted (int turnNumber) { InvokeMatchTriggerEvents(TriggerTag.OnTurnStarted); yield return null; }
		public override IEnumerator OnTurnEnded (int turnNumber) { InvokeMatchTriggerEvents(TriggerTag.OnTurnEnded); yield return null; }
		public override IEnumerator OnPhaseStarted (string phase) { InvokeMatchTriggerEvents(TriggerTag.OnPhaseStarted); yield return null; }
		public override IEnumerator OnPhaseEnded (string phase) { InvokeMatchTriggerEvents(TriggerTag.OnPhaseEnded); yield return null; }
		public override IEnumerator OnActionUsed (string actionName) { InvokeMatchTriggerEvents(TriggerTag.OnActionUsed); yield return null; }

		public override IEnumerator OnMatchSetup (int matchNumber)
		{
			//Prepare conditions in Match Events Watcher
			nestedConditions = new NestedConditions[triggerTags.Length];
			for (int i = 0; i < triggerTags.Length; i++)
			{
				nestedConditions[i] = new NestedConditions(conditions[i]);
			}

			//Prepare variable watchers
			watchingVariables = new HashSet<string>[variableFormats.Length];
			for (int i = 0; i < variableFormats.Length; i++)
			{
				string[] formatSplit = variableFormats[i].Split('{', '}');
				watchingVariables[i] = new HashSet<string>();
				uiText[i].text = variableFormats[i];
				for (int j = 0; j < formatSplit.Length; j++)
				{
					string varName = formatSplit[j];
					if (Match.Current.HasVariable(varName))
					{
						string varValue = Match.Current.GetVariable(varName).ToString();
						varName = "{" + varName + "}";
						watchingVariables[i].Add(varName);
						uiText[i].text = uiText[i].text.Replace(varName, varValue);
					}
				}
			}

			InvokeMatchTriggerEvents(TriggerTag.OnMatchSetup);

			yield return null;
		}

		public override IEnumerator OnMessageSent (string message)
		{
			for (int i = 0; i < messages.Length; i++)
			{
				if (message == messages[i])
				{
					messageEvents[i].Invoke();
					break;
				}
			}

			for (int i = 0; i < messagesToSFX.Length; i++)
			{
				if (message == messagesToSFX[i])
				{
					int randomSFX = Random.Range(0, audioClips[i].Length);
					audioSource.clip = audioClips[i][randomSFX];
					audioSource.Play();
					break;
				}
			}

			InvokeMatchTriggerEvents(TriggerTag.OnMessageSent);

			yield return null;
		}

		public override IEnumerator OnVariableChanged (string variable, object value)
		{
			for (int i = 0; i < watchingVariables.Length; i++)
			{
				HashSet<string> variables = watchingVariables[i];
				if (variables.Contains("{" + variable + "}"))
				{
					uiText[i].text = variableFormats[i];
					foreach (string item in variables)
					{
						uiText[i].text = uiText[i].text.Replace(item, Match.Current.GetVariable(variable).ToString());
					}
				}
			}

			InvokeMatchTriggerEvents(TriggerTag.OnVariableChanged);

			yield return null;
		}

		
	}
}