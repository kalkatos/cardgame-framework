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
			Match.OnVariableChanged += VariableChanged;
			Match.OnMatchStarted += MatchStarted;
		}

		private void OnDestroy()
		{
			Match.OnVariableChanged -= VariableChanged;
			Match.OnMatchStarted -= MatchStarted;
		}

		private IEnumerator VariableChanged ()
		{
			if (Match.GetVariable("variable") == variable)
			{
				string value = Match.GetVariable("newValue");
				if (textUI)
					textUI.text = value;
			}
			yield return null;
		}

		private IEnumerator MatchStarted ()
		{
			string value = Match.GetVariable(variable);
			if (textUI)
				textUI.text = value;
			yield return null;
		}
	}
}