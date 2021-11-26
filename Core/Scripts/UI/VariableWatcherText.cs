using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CardgameFramework
{
	public class VariableWatcherText : MonoBehaviour
    {
        public string variable;
		public TMP_Text textUI;

		private void Awake ()
		{
			Match.OnMatchStarted.AddListener(MatchStarted, $"Variable Watcher ({name})");
			Match.OnVariableChanged.AddListener(new NestedConditions($"variable={variable}"), VariableChanged, $"Variable Watcher ({name})");
		}

		private void OnDestroy ()
		{
			Match.OnMatchStarted.RemoveListener(MatchStarted);
			Match.OnVariableChanged.RemoveListener(VariableChanged);
		}

		private void VariableChanged (string variable, string newValue, string oldValue, string additionalInfo)
		{
			if (textUI)
				textUI.text = newValue;
		}

		private void MatchStarted (int matchNumber)
		{
			if (textUI)
				textUI.text = Match.GetVariable(variable);
		}
	}
}