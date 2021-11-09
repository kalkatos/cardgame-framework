using UnityEditor;
using UnityEngine;

namespace CardgameFramework.Editor
{
	public static class ConditionDrawer
	{
		public static void Draw (Rect rect, Rule rule)
		{
			EditorGUI.LabelField(rect, rule.conditionObject.ToString(false));
		}
	}

}
