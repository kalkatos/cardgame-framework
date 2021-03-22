using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CardgameCore
{
    public class VariableWatcherText : MonoBehaviour
    {
        public string variable;
		public TextMeshProUGUI textUI;
		public TextMeshPro text3D;

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
				if (text3D)
					text3D.text = value;
				if (textUI)
					textUI.text = value;
			}
			yield break;
		}

		private IEnumerator MatchStarted ()
		{
			string value = Match.GetVariable(variable);
			if (text3D)
				text3D.text = value;
			if (textUI)
				textUI.text = value;
			yield break;
		}
	}
}