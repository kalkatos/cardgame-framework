using System;
using UnityEngine.EventSystems;

namespace CardgameCore
{
	public static class UsefulExtensions
	{
		public static T[] SubArray<T> (this T[] array, int offset)
		{
			int length = array.Length - offset;
			T[] result = new T[length];
			System.Array.Copy(array, offset, result, 0, length);
			return result;
		}

		public static bool Contains<T> (this T[] array, T value)
		{
			return System.Array.IndexOf(array, value) >= 0;
		}

		public static void AddTrigger (this EventTrigger trigger, EventTriggerType triggerType, Action<PointerEventData> function)
		{
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = triggerType;
			entry.callback.AddListener((data) => { function((PointerEventData)data); });
			trigger.triggers.Add(entry);
		}
	}
}