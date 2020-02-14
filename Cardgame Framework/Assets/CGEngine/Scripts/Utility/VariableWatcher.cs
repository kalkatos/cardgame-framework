using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace CardGameFramework
{
	
	public class VariableWatcher : MatchWatcher
	{
		TextMeshProUGUI uiText;
		TextMeshPro sceneText;

		public string format;
		HashSet<string> variables;

		private void Awake ()
		{
			uiText = GetComponent<TextMeshProUGUI>();
			sceneText = GetComponent<TextMeshPro>();
			if (!uiText && !sceneText)
			{
				Debug.LogError($"The object {gameObject.name} with VariableWatcher component needs a TextMeshPro or a TextMeshProGUI component to work as intended.");
				return;
			}
			if (string.IsNullOrEmpty(format))
				return;
			
			variables = new HashSet<string>();
		}

		public override IEnumerator TreatTrigger (TriggerTag triggerTag, params object[] args)
		{
			if (triggerTag == TriggerTag.OnMatchSetup)
			{
				if (uiText)
					uiText.text = format;
				if (sceneText)
					sceneText.text = format;

				string[] formatSplit = format.Split('{', '}');
				for (int i = 0; i < formatSplit.Length; i++)
				{
					string varName = formatSplit[i];
					if (Match.Current.HasVariable(varName))
					{
						variables.Add(varName);
						string varValue = Match.Current.GetVariable(varName).ToString();
						if (uiText)
							uiText.text = uiText.text.Replace(varName, varValue);
						if (sceneText)
							sceneText.text = sceneText.text.Replace(varName, varValue);
					}
				}
			}

			if (triggerTag == TriggerTag.OnVariableChanged)
			{
				string variable = (string)GetArgumentWithTag("variable", args);

				if (variables.Contains(variable))
				{
					string resultString = format;
					foreach (string item in variables)
					{
						string varValue = Match.Current.GetVariable(item).ToString();
						resultString = resultString.Replace("{"+item+"}", varValue);
					}
					if (uiText)
						uiText.text = resultString;
					if (sceneText)
						sceneText.text = resultString;
				}
			}

			yield return null;
		}
	}
}