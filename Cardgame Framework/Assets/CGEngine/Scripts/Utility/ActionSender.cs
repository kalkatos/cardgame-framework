using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

namespace CardGameFramework
{
	[RequireComponent(typeof(Button))]
	public class ActionSender : MonoBehaviour
	{
		Button button;

		public string actionNameToSend;

		public void SendAction ()
		{
			if (Match.Current != null && !string.IsNullOrEmpty(actionNameToSend))
				Match.Current.UseAction(actionNameToSend);
		}

	}
}