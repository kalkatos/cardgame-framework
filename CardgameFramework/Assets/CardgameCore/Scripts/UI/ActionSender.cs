using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
    public class ActionSender : MonoBehaviour
    {
        public string actionName;

        public void SendAction ()
		{
            Match.UseAction(actionName);
		}
    }
}
