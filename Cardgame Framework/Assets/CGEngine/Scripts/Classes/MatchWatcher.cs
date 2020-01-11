using System.Collections;
using UnityEngine;

namespace CardGameFramework
{
	public abstract class MatchWatcher : MonoBehaviour
	{
		//private void Start()
		//{
		//	Register();
		//}

		//public void Register()
		//{
		//	if (Match.Current)
		//		Match.Current.AddWatcher(this);
		//}
		public abstract IEnumerator TreatTrigger(string triggerTag, params object[] args);

		protected object GetArgumentWithTag (string tag, object[] args)
		{
			if (args == null) return null;
			for (int i = 0; i < args.Length; i+=2)
			{
				if (args[i].ToString() == tag)
					if (args.Length > i + 1)
						return args[i + 1];
			}
			Debug.LogWarning("CGEngine: Couldn't find argument: " + tag);
			return null;
		}

		protected bool HasString (string tag, string[] arr)
		{
			if (arr == null) return false;
			for (int i = 0; i < arr.Length; i++)
			{
				if (arr[i] == tag)
					return true;
			}
			return false;
		}
	}
}