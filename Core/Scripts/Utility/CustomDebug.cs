using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameFramework
{
	internal class CustomDebug : MonoBehaviour
    {
        private static string[] identationTabs = new string[] { "", "    ", "        ", "            ", "                ", "                    ", "                            " };
        private const string logTag = "[CGBuilder]";

        private static string TreatedMessage(string message, int identation)
		{
            identation = Mathf.Min(identation, identationTabs.Length - 1);
            return $"{logTag}{identationTabs[identation]} {message}";
        }

        internal static void Log (string message, int identation = 0)
		{
            UnityEngine.Debug.Log(TreatedMessage(message, identation));
		}

        internal static void LogError (string message, int identation = 0)
        {
            UnityEngine.Debug.LogError(TreatedMessage(message, identation));
        }

        internal static void LogWarning (string message, int identation = 0)
        {
            UnityEngine.Debug.LogWarning(TreatedMessage(message, identation));
        }
    }
}
