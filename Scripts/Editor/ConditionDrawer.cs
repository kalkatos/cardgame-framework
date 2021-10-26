using UnityEditor;
using UnityEngine;

namespace CardgameCore
{
	public static class ConditionDrawer
	{
		public static void Draw (Rect rect, Rule rule)
		{
			EditorGUI.LabelField(rect, rule.conditionObject.ToString(false));
		}
	}

}
