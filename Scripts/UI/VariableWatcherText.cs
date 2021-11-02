using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CardgameCore
{
	public class VariableWatcherText : MonoBehaviour
    {
        public string variable;
		public TMP_Text textUI;

		private void Awake()
		{
			Match.AddVariableChangedCallback(VariableChanged, $"Variable Watcher ({name})");
			Match.AddMatchStartedCallback(MatchStarted, $"Variable Watcher ({name})");
		}

		private void OnDestroy()
		{
			Match.RemoveVariableChangedCallback(VariableChanged);
			Match.RemoveMatchStartedCallback(MatchStarted);
		}

		private IEnumerator VariableChanged (string variable, string newValue, string oldValue, string additionalInfo)
		{
			if (variable == this.variable)
			{
				string value = Match.GetVariable("newValue");
				if (textUI)
					textUI.text = value;
			}
			yield return null;
		}

		private IEnumerator MatchStarted (int matchNumber)
		{
			string value = Match.GetVariable(variable);
			if (textUI)
				textUI.text = value;
			yield return null;
		}
	}
}