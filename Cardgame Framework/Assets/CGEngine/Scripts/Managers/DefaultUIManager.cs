using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CardGameFramework
{
	public class DefaultUIManager : MatchWatcher, IInputEventReceiver
	{
		public CardGameData autoStartGame;
		[Header("   Input Events  - - - ")]
		public InputType[] inputTypes;
		public UnityEvent[] inputEvents;
		[Header("   Match Events  - - - ")]
		public TriggerTag[] triggerTags;
		public string[] conditions;
		public UnityEvent[] triggerEvents;
		NestedConditions[] nestedConditions;
		[Header("   Message Receivers  - - - ")]
		public string[] messages;
		public UnityEvent[] messageEvents;
		[Header("   Variable Display Text  - - - ")]
		public string[] variableFormats;
		public TMP_Text[] uiText;
		HashSet<string>[] watchingVariables;

		InputObject target;
		InputObject draggingObject;

		private void Start ()
		{
			InputManager.Register(InputType.All, this);

			if (autoStartGame != null && autoStartGame.rules != null && autoStartGame.rules.Count > 0)
			{
				Ruleset rules = autoStartGame.rules[0];
				CGEngine.StartMatch(autoStartGame, rules);
			}
		}

		public void SendAction (string actionName)
		{
			if (Match.Current)
				Match.Current.UseAction(actionName);
		}

		public void SendUseObjectToMatch ()
		{
			Card c = target.GetComponent<Card>();
			if (c)
				Match.Current.UseCard(c);
			else
			{
				Zone z = target.GetComponent<Zone>();
				if (z)
					Match.Current.UseZone(z);
			}
		}

		public void TreatEvent (InputType type, InputObject inputObject)
		{
			target = inputObject;
			for (int i = 0; i < inputTypes.Length; i++)
			{
				if (type == inputTypes[i])
					inputEvents[i].Invoke();
			}
		}

		public override IEnumerator TreatTrigger (TriggerTag triggerTag, params object[] args)
		{
			if (triggerTag == TriggerTag.OnMatchSetup)
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
			}

			else if (triggerTag == TriggerTag.OnMessageSent)
			{
				string message = (string)GetArgumentWithTag("message", args);
				for (int i = 0; i < messages.Length; i++)
				{
					if (message == messages[i])
					{
						messageEvents[i].Invoke();
						break;
					}
				}
			}

			else if (triggerTag == TriggerTag.OnVariableChanged)
			{
				string variable = (string)GetArgumentWithTag("variable", args);
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
			}

			for (int i = 0; i < triggerTags.Length; i++)
			{
				if (triggerTag == triggerTags[i])
				{
					triggerEvents[i].Invoke();
					break;
				}
			}
			yield return null;
		}
	}
}