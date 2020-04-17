using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace CardGameFramework
{
	[CustomEditor(typeof(Zone)), CanEditMultipleObjects]
	public class ZoneInspector : Editor
	{
		Zone zone;
		
		ReorderableList specificPositionsList;

		private void OnEnable ()
		{
			zone = (Zone)target;

			specificPositionsList = new ReorderableList(serializedObject, serializedObject.FindProperty("specificPositions"), true, true, true, true);
			specificPositionsList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Positions"); };
			specificPositionsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				EditorGUI.PropertyField(
					new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
					specificPositionsList.serializedProperty.GetArrayElementAtIndex(index), GUIContent.none);
			};
			specificPositionsList.onAddCallback = (ReorderableList list) =>
			{
				GameObject go = new GameObject(zone.name + "-Position" + list.count.ToString().PadLeft(2, '0'));
				go.transform.position = zone.transform.position;
				go.transform.SetParent(zone.transform);
				var index = list.serializedProperty.arraySize;
				list.serializedProperty.arraySize++;
				list.index = index;
				var element = list.serializedProperty.GetArrayElementAtIndex(index);
				element.objectReferenceValue = go.transform;
			};
			specificPositionsList.onSelectCallback = (ReorderableList list) => {
				var prefab = list.serializedProperty.GetArrayElementAtIndex(list.index).FindPropertyRelative("Transform").objectReferenceValue as GameObject;
				if (prefab)
					EditorGUIUtility.PingObject(prefab.gameObject);
			};
		}

		public override void OnInspectorGUI ()
		{
			base.OnInspectorGUI();

			switch (zone.zoneConfig)
			{
				case ZoneConfiguration.Stack:
				case ZoneConfiguration.SideBySide:
					zone.distanceBetweenCards = EditorGUILayout.Vector3Field("Distance Between Cards", zone.distanceBetweenCards);
					zone.bounds = EditorGUILayout.Vector2Field("Bounds", zone.bounds);
					if (GUILayout.Button("Order Cards"))
					{
						Vector3 first = zone.transform.position;
						Vector3 distance = zone.distanceBetweenCards;
						if (zone.zoneConfig == ZoneConfiguration.SideBySide)
						{
							distance.x = Mathf.Clamp(zone.bounds.x / (zone.transform.childCount + 2), 0.001f, distance.x);
							first.x = first.x - (zone.transform.childCount - 1) * distance.x / 2;
						}
						float distanceMag = distance.magnitude;
						distance = zone.transform.TransformDirection(distance);
						for (int i = 0; i < zone.transform.childCount; i++)
						{
							Transform child = zone.transform.GetChild(i);
							if (child.GetComponent<Card>())
							{
								child.rotation = zone.transform.rotation;
								child.position = first + distance * i;
							}
						}
					}
					break;
				case ZoneConfiguration.Grid:
					zone.gridSize = EditorGUILayout.Vector2IntField("Grid Size", zone.gridSize);
					zone.gridSize.x = Mathf.Clamp(zone.gridSize.x, 1, int.MaxValue);
					zone.gridSize.y = Mathf.Clamp(zone.gridSize.y, 1, int.MaxValue);
					zone.cellSize = EditorGUILayout.Vector2Field("Cell Size", zone.cellSize);
					zone.bounds.x = zone.gridSize.x * zone.cellSize.x;
					zone.bounds.y = zone.gridSize.y * zone.cellSize.y;
					break;
				case ZoneConfiguration.SpecificPositions:
					zone.cellSize = EditorGUILayout.Vector2Field("Cell Size", zone.cellSize);
					serializedObject.Update();
					specificPositionsList.DoLayoutList();
					serializedObject.ApplyModifiedProperties();
					break;
				default:
					break;
			}
			
		}

		[MenuItem("GameObject/Card Game/Zone", false, 10)]
		static void CreateZone (MenuCommand menuCommand)
		{
			// Create a custom game object
			GameObject go = new GameObject("New Zone");
			go.AddComponent<Zone>();
			// Ensure it gets reparented if this was a context click (otherwise does nothing)
			GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
			// Register the creation in the undo system
			Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
			Selection.activeObject = go;
		}
	}
}