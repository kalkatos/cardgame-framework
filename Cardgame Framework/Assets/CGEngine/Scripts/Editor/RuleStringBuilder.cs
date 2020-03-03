using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

namespace CardGameFramework
{
	#region Pieces ============================================================================================

	internal interface IEditorPiece
	{
		void ShowInEditor ();
		string Codify ();
	}

	internal class StringPiece : IEditorPiece
	{
		GUIContent _showValue;
		internal GUIContent showValue
		{ get { return _showValue; } set { _showValue = value; width = EditorStyles.label.CalcSize(value).x; } }
		internal float width { get; private set; }
		internal string codifyValue;

		internal StringPiece (string showString, string codifyString)
		{
			showValue = new GUIContent(showString);
			codifyValue = codifyString;
		}

		public void ShowInEditor ()
		{
			EditorGUILayout.LabelField(showValue, GUILayout.Width(width));
		}

		public string Codify ()
		{
			return codifyValue;
		}
	}

	internal class StringPopupPiece : IEditorPiece
	{
		public GUIContent[] stringArray { get; private set; }
		public int previousIndex { get; private set; }
		int _index;
		public int index { get { return _index; } set { previousIndex = _index; _index = value; } }
		public float width { get; private set; }
		public string value { get { return stringArray[index].text; } }
		string tempTextBeingInserted;

		internal StringPopupPiece (string[] strings, int index, bool addOther = true)
		{
			stringArray = new GUIContent[strings.Length + (addOther ? 1 : 0)];
			this.index = index;
			width = 0;
			for (int i = 0; i < strings.Length; i++)
			{
				stringArray[i] = new GUIContent(strings[i]);
				float currentSize = EditorStyles.popup.CalcSize(stringArray[i]).x;
				if (currentSize > width)
					width = currentSize;
			}
			if (addOther)
			{
				GUIContent other = new GUIContent(StringPopupBuilder.newEntryString);
				float otherSize = EditorStyles.popup.CalcSize(other).x;
				if (otherSize > width)
					width = otherSize;
				stringArray[stringArray.Length - 1] = other;
			}
		}

		internal StringPopupPiece (GUIContent[] stringArray, int index)
		{
			this.stringArray = stringArray;
			this.index = index;
			width = 0;
			for (int i = 0; i < stringArray.Length; i++)
			{
				float size = EditorStyles.popup.CalcSize(stringArray[i]).x;
				if (size > width)
					width = size;
			}
		}

		internal int IndexOf (string value)
		{
			for (int i = 0; i < stringArray.Length; i++)
			{
				if (stringArray[i].text == value)
					return i;
			}
			return -1;
		}

		internal void Add (string value)
		{
			GUIContent[] newArray = new GUIContent[stringArray.Length + 1];
			for (int i = 0; i < stringArray.Length - 1; i++)
			{
				newArray[i] = stringArray[i];
			}
			newArray[stringArray.Length] = stringArray[stringArray.Length - 1];
			GUIContent newContent = new GUIContent(value);
			newArray[stringArray.Length - 1] = newContent;
			stringArray = newArray;
			float newSize = EditorStyles.popup.CalcSize(newContent).x;
			if (newSize > width)
				width = newSize;
		}

		public virtual void OnPopupChange () { }

		public void ShowInEditor ()
		{
			if (stringArray[index].text == StringPopupBuilder.newEntryString)
			{
				Event evt = Event.current;
				if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
				{
					index = previousIndex;
					return;
				}

				EditorGUI.BeginChangeCheck();
				tempTextBeingInserted = EditorGUILayout.DelayedTextField(tempTextBeingInserted, GUILayout.Width(100));
				if (EditorGUI.EndChangeCheck())
				{
					if (!string.IsNullOrEmpty(tempTextBeingInserted))
					{
						if (IndexOf(tempTextBeingInserted) == -1)
						{
							int indexAdded = stringArray.Length - 1;
							Add(tempTextBeingInserted);
							index = indexAdded;
							tempTextBeingInserted = "";
						}
					}
				}
			}
			else
			{
				EditorGUI.BeginChangeCheck();
				index = EditorGUILayout.Popup(index, stringArray, GUILayout.Width(width));
				if (EditorGUI.EndChangeCheck())
				{
					OnPopupChange();
				}
			}
		}

		public virtual string Codify ()
		{
			string result = stringArray[index].text;
			return result == StringPopupBuilder.blankEntryString ? "" : result;
		}
	}

	internal class CommandLabelPopup : StringPopupPiece
	{
		internal CommandLabelPopup () : base(StringPopupBuilder.commandLabel, 0) { }
		internal CommandLabelPopup (string value)
			: base(StringPopupBuilder.commandLabel, Mathf.Clamp(StringPopupBuilder.IndexOfCommand(value), 0, StringPopupBuilder.commandLabel.Length - 1))
		{ }

		public override string Codify ()
		{
			string result = base.Codify();
			return result.Replace(" ", "");
		}
	}

	#endregion

	internal class StringPopup : IEditorPiece
	{
		internal List<StringPopupPiece> pieces;

		internal StringPopup ()
		{
			pieces = new List<StringPopupPiece>();
		}

		public virtual void ShowInEditor ()
		{
			for (int i = 0; i < pieces.Count; i++)
				pieces[i].ShowInEditor();
		}

		public virtual string Codify ()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < pieces.Count; i++)
				sb.Append(pieces[i].Codify());
			return sb.ToString();
		}
	}

	

	internal class CommandStringPopup : StringPopup
	{
		internal CommandStringPopup () : base()
		{
			pieces.Add(new CommandLabelPopup());
		}

		internal virtual void OnPopupChange ()
		{
			StringPopupPiece first = pieces[0];
			pieces.Clear();
			string newCommand = first.value;
			string parameters = StringPopupBuilder.commandSignatures[newCommand];
		}
	}

	internal class StringPopupBuilder
	{
		internal const string newEntryString = "<other>";
		internal const string blankEntryString = "<none>";

		internal static GUIContent[] commandLabel = new GUIContent[]
		{
			new GUIContent(blankEntryString),
			new GUIContent("Add Tag To Card"),
			new GUIContent("End Current Phase"),
			new GUIContent("End Subphase Loop"),
			new GUIContent("End The Match"),
			new GUIContent("Move Card To Zone"),
			new GUIContent("Remove Tag from Card"),
			new GUIContent("Send Message"),
			new GUIContent("Set Card Field Value"),
			new GUIContent("Set Variable"),
			new GUIContent("Shuffle"),
			new GUIContent("Start Subphase Loop"),
			new GUIContent("Use Action"),
			new GUIContent("Use Card"),
			new GUIContent("Use Zone")
		};

		internal static GUIContent[] zoneTags;
		internal static GUIContent[] cardTags;
		internal static GUIContent[] Tags;

		static Dictionary<string, string> _commandSignatures = null;
		internal static Dictionary<string, string> commandSignatures
		{
			get
			{
				if (_commandSignatures == null)
				{
					_commandSignatures = new Dictionary<string, string>();
					_commandSignatures.Add("Add Tag To Card", "<Card Selection>,<Card Tag>");
					_commandSignatures.Add("End Current Phase", "");
					_commandSignatures.Add("End Subphase Loop", "");
					_commandSignatures.Add("End The Match", "");
					_commandSignatures.Add("Move Card To Zone", "<Card Selection>,<Zone Selection>,?<Additional Param 1>,?<Additional Param 2>,?<Additional Param 3>,?<Additional Param 4>");
					_commandSignatures.Add("Remove Tag from Card", "<Card Selection>,<Card Tag>");
					_commandSignatures.Add("Send Message", "<Message>");
					_commandSignatures.Add("Set Card Field Value", "<Card Field>,<Card Selection>");
					_commandSignatures.Add("Set Variable", "<Variable Name>,<Value>,?<Min>,?<Max>");
					_commandSignatures.Add("Shuffle", "<Zone Selection>");
					_commandSignatures.Add("Start Subphase Loop", "<Subphases>");
					_commandSignatures.Add("Use Action", "<Action Name>");
					_commandSignatures.Add("Use Card", "<Card Selection>");
					_commandSignatures.Add("Use Zone", "<Zone Selection>");
				}
				return _commandSignatures;
			}
		}

		static string textBeingInserted = "";

		internal static int IndexOfCommand (string name)
		{
			for (int i = 0; i < commandLabel.Length; i++)
			{
				if (commandLabel[i].text == name)
					return i;
			}
			return -1;
		}

		//internal static StringPopupSequence BuildNewStringSequence ()
		//{
		//	StringPopupSequence sequence = new StringPopupSequence();
		//	return sequence;
		//}

		//internal static void ShowStringPopupSequence (StringPopupSequence sequence)
		//{
		//	EditorGUILayout.BeginVertical();
		//	for (int i = 0; i < sequence.list.Count; i++)
		//	{
		//		ShowPopupClause(sequence.list[i]);
		//	}
		//	EditorGUILayout.EndVertical();
		//}

		//static void ShowPopupClause (StringPopup commandList)
		//{
		//	EditorGUILayout.BeginHorizontal();
		//	foreach (StringPopupPiece item in commandList.pieces)
		//	{
		//		ShowPopupPiece(item);
		//	}
		//	EditorGUILayout.EndHorizontal();
		//}

		//static void ShowPopupPiece (StringPopupPiece piece)
		//{
		//	if (piece.stringArray[piece.index].text == newEntryString)
		//	{
		//		Event evt = Event.current;
		//		if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
		//		{
		//			piece.index = piece.previousIndex;
		//			return;
		//		}

		//		EditorGUI.BeginChangeCheck();
		//		textBeingInserted = EditorGUILayout.DelayedTextField(textBeingInserted, GUILayout.Width(100));
		//		if (EditorGUI.EndChangeCheck())
		//		{
		//			if (!string.IsNullOrEmpty(textBeingInserted))
		//			{
		//				if (piece.IndexOf(textBeingInserted) == -1)
		//				{
		//					int indexAdded = piece.stringArray.Length - 1;
		//					piece.Add(textBeingInserted);
		//					piece.index = indexAdded;
		//				}
		//			}
		//		}
		//	}
		//	else
		//	{
		//		EditorGUI.BeginChangeCheck();
		//		piece.index = EditorGUILayout.Popup(piece.index, piece.stringArray, GUILayout.Width(piece.width));
		//		if (EditorGUI.EndChangeCheck())
		//		{

		//		}
		//	}
		//}
	}

	internal class StringPopupSequence : IEditorPiece
	{
		internal List<StringPopup> list;

		internal StringPopupSequence () { list = new List<StringPopup>(); }

		public void ShowInEditor ()
		{
			EditorGUILayout.BeginVertical();
			for (int i = 0; i < list.Count; i++)
				list[i].ShowInEditor();
			EditorGUILayout.EndVertical();
		}

		public string Codify ()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < list.Count; i++)
			{
				sb.Append(list[i].Codify());
				if (i < list.Count - 1)
					sb.Append(";");
			}
			return sb.ToString();
		}
	}
}