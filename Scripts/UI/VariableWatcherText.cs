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
			Match.AddMatchStartedCallback(MatchStarted, $"Variable Watcher ({name})");
			Match.AddVariableChangedCallback(VariableChanged, $"Variable Watcher ({name})");
		}

		private void OnDestroy()
		{
			Match.RemoveMatchStartedCallback(MatchStarted);
			Match.RemoveVariableChangedCallback(VariableChanged);
		}

		private IEnumerator VariableChanged (string variable, string newValue, string oldValue, string additionalInfo)
		{
			if (variable == this.variable)
			{
				if (textUI)
					textUI.text = newValue;
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