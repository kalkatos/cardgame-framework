using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
    public class Debug : MonoBehaviour
    {
        private const string logTag = "[CGBuilder]";

        private static string TreatedMessage(string message, int identation)
		{
            string identationStr = "";
            for (int i = 0; i < identation; i++)
                identationStr += "    ";
            return $"{logTag}{identationStr} {message}";
        }

        public static void Log (string message, int identation = 0)
		{
            UnityEngine.Debug.Log(TreatedMessage(message, identation));
		}

        public static void LogError (string message, int identation = 0)
        {
            UnityEngine.Debug.LogError(TreatedMessage(message, identation));
        }

        public static void LogWarning (string message, int identation = 0)
        {
            UnityEngine.Debug.LogWarning(TreatedMessage(message, identation));
        }
    }
}
