using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameFramework
{
    public class ActionSender : MonoBehaviour
    {
        public string actionName;

        public void SendAction (string origin)
        {
            Match.UseAction(actionName, origin);
        }
    }
}
