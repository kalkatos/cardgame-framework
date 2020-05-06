using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CardGameFramework
{
	public class ActionSender : MonoBehaviour
	{
		public void SendAction(string actionName)
		{
			Match.Current.UseAction(actionName);
		}
	}
}