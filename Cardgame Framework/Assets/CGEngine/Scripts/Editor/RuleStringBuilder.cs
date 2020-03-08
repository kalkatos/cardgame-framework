using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

namespace CardGameFramework
{
	#region Pieces ============================================================================================

	public interface IEditorPiece
	{
		void ShowInEditor ();
		string Codify ();
	}

	public class StringPiece : IEditorPiece
	{
		GUIContent _showValue;
		internal GUIContent showValue
		{ get { return _showValue; } set { _showValue = value; width = SetWidth(); } }
		internal float width { get; private set; }
		internal string codifyValue;
		internal StringPiece next { get; set; }

		internal StringPiece () : this("", "") { }

		internal StringPiece (string showString, string codifyString)
		{
			showValue = new GUIContent(showString);
			codifyValue = codifyString;
		}

		internal StringPiece (string singleString)
		{
			showValue = new GUIContent(singleString);
			codifyValue = singleString;
		}

		public virtual void Set (string value)
		{
			showValue = new GUIContent(value);
			codifyValue = value;
		}

		public virtual string CodifyAll ()
		{
			StringPiece piece = this;
			StringBuilder result = new StringBuilder();
			while (piece != null)
			{
				result.Append(piece.Codify());
				piece = piece.next;
			}
			return result.ToString();
		}

		public virtual void ShowInEditorAll ()
		{
			EditorGUILayout.BeginHorizontal();
			ShowInEditor();
			StringPiece piece = next;
			while (piece != null)
			{
				piece.ShowInEditor();
				piece = piece.next;
			}
			EditorGUILayout.EndHorizontal();
		}

		public virtual void ShowInEditor ()
		{
			EditorGUILayout.LabelField(showValue, GUILayout.Width(width));
		}

		public virtual string Codify ()
		{
			return codifyValue;
		}

		protected StringPiece GetEndNode (StringPiece piece)
		{
			while (piece.next != null)
				piece = piece.next;
			return piece;
		}

		internal StringPiece SetNext (params StringPiece[] pieces)
		{
			if (pieces.Length > 0)
			{
				StringPiece oldNext = next;
				next = pieces[0];
				for (int i = 0; i < pieces.Length; i++)
				{
					if (i == pieces.Length - 1)
						GetEndNode(pieces[i]).next = oldNext;
					else
						GetEndNode(pieces[i]).next = pieces[i + 1];
				}
			}
			return this;
		}

		public virtual StringPiece Clone ()
		{
			return new StringPiece(showValue != null ? showValue.text : "", codifyValue);
		}

		public virtual StringPiece CloneAll ()
		{
			StringPiece thisClone = Clone();
			thisClone.next = next != null ? next.CloneAll() : null;
			return thisClone;
		}

		public virtual void DisposeAllNext ()
		{
			if (next != null)
				next.DisposeAllNext();
			next = null;
		}

		protected virtual float SetWidth ()
		{
			return EditorStyles.label.CalcSize(showValue).x;
		}
	}

	public class StringPieceSequence
	{
		internal List<StringPiece> sequence;
		internal StringPiece model;

		internal StringPieceSequence (StringPiece model)
		{
			sequence = new List<StringPiece>();
			this.model = model;
		}

		internal StringPieceSequence (StringPiece model, params StringPiece[] pieces) : this(model)
		{
			sequence.AddRange(pieces);
		}

		public string CodifySequence (bool asCommand)
		{
			StringBuilder sb = new StringBuilder();
			bool lastWasEmpty = false;
			for (int i = 0; i < sequence.Count; i++)
			{
				string piece = sequence[i].CodifyAll();
				if (string.IsNullOrEmpty(piece))
				{
					lastWasEmpty = true;
					continue;
				}
				if (i > 0 && !lastWasEmpty && asCommand)
					sb.Append(";");
				sb.Append(sequence[i].CodifyAll());
				lastWasEmpty = false;
			}
			return sb.ToString();
		}

		public void ShowSequence ()
		{
			EditorGUILayout.BeginVertical();
			int toBeDeleted = -1;
			for (int i = 0; i < sequence.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				sequence[i].ShowInEditorAll();
				if (GUILayout.Button("–", GUILayout.Width(20)))
					toBeDeleted = i;
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();
			}
			if (toBeDeleted != -1)
				sequence.RemoveAt(toBeDeleted);
			if (GUILayout.Button("  +  ", GUILayout.Width(40)))
			{
				sequence.Add(model.CloneAll());
			}
			EditorGUILayout.EndVertical();
		}
	}

	public class StringPopupPiece : StringPiece
	{
		public GUIContent[] stringArray { get; private set; }
		public int previousIndex { get; private set; }
		int _index;
		public int index
		{
			get { return _index; }
			set
			{
				int newIndex = Mathf.Clamp(value, 0, stringArray.Length - 1);
				if (newIndex != _index)
				{
					previousIndex = _index;
					_index = newIndex;
					showValue = stringArray[newIndex];
				}
			}
		}
		public string popupValue { get { return stringArray[index].text; } }
		string tempTextBeingInserted;

		internal StringPopupPiece (GUIContent[] stringArray, int index)
		{
			this.stringArray = stringArray;
			this.index = index;
			showValue = stringArray[index];
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
			GUIContent newContent = new GUIContent(value);
			GUIContent[] newArray = new GUIContent[stringArray.Length + 1];
			int newlyAddedIndex = 0;
			if (stringArray[stringArray.Length - 1].text == StringPopupBuilder.newEntryString)
			{
				newArray[newArray.Length - 1] = stringArray[stringArray.Length - 1];
				newArray[newArray.Length - 2] = newContent;
				newlyAddedIndex = newArray.Length - 2;
			}
			else
			{
				newArray[newArray.Length - 1] = newContent;
				newlyAddedIndex = newArray.Length - 1;
			}

			for (int i = 0; i < newlyAddedIndex; i++)
			{
				newArray[i] = stringArray[i];
			}
			stringArray = newArray;
		}

		public override void ShowInEditor ()
		{
			if (popupValue == StringPopupBuilder.newEntryString)
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
				int oldIndex = index;
				index = EditorGUILayout.Popup(index, stringArray, GUILayout.Width(width));
				if (EditorGUI.EndChangeCheck())
				{
					if (oldIndex != index)
					{
						showValue = stringArray[index];
						OnPopupChanged();
					}
				}
			}
		}

		public override string Codify ()
		{
			return codifyValue = popupValue;
		}

		protected virtual void OnPopupChanged ()
		{
		}

		public override StringPiece Clone ()
		{
			return new StringPopupPiece(stringArray, 0);
		}

		protected override float SetWidth ()
		{
			return EditorStyles.popup.CalcSize(showValue).x;
		}
	}

	public class CommandLabelPopup : StringPopupPiece
	{
		internal CommandLabelPopup () : base(StringPopupBuilder.commandLabel, 0)
		{
			GetNextFromSignature();
		}
		internal CommandLabelPopup (int index) : base(StringPopupBuilder.commandLabel, index)
		{
			GetNextFromSignature();
		}
		internal CommandLabelPopup (string value)
			: base(StringPopupBuilder.commandLabel, Mathf.Clamp(StringPopupBuilder.IndexOfCommand(value), 0, StringPopupBuilder.commandLabel.Length - 1))
		{
			GetNextFromSignature();
		}

		protected override void OnPopupChanged ()
		{
			base.OnPopupChanged();
			GetNextFromSignature();
		}

		public override string Codify ()
		{
			return base.Codify().Replace(" ", "");
		}

		public override StringPiece Clone ()
		{
			return new CommandLabelPopup(index);
		}

		public void GetNextFromSignature ()
		{
			DisposeAllNext();
			next = StringPopupBuilder.commandSignatures[index].CloneAll();
		}
	}

	public class StringPieceList : StringPiece
	{
		protected List<StringPiece> pieces;

		internal StringPieceList (List<StringPiece> list) { pieces.AddRange(list); }
		internal StringPieceList (params StringPiece[] piecesToAdd) : base()
		{
			pieces = new List<StringPiece>();
			if (piecesToAdd.Length > 0)
			{
				pieces.AddRange(piecesToAdd);
			}
		}

		public override StringPiece Clone ()
		{
			List<StringPiece> cloneList = new List<StringPiece>();
			for (int i = 0; i < pieces.Count; i++)
			{
				cloneList.Add(pieces[i].Clone());
			}
			return new StringPieceList(cloneList);
		}

		public override void ShowInEditor ()
		{
			base.ShowInEditor();
			for (int i = 0; i < pieces.Count; i++)
			{
				pieces[i].ShowInEditor();
			}
		}

		public override string Codify ()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(base.Codify());
			for (int i = 0; i < pieces.Count; i++)
			{
				sb.Append(pieces[i].Codify());
			}
			return sb.ToString();
		}

		internal void Add (StringPiece stringPiece)
		{
			int indexToAdd = pieces.Count;
			if (pieces.Count > 0)
			{
				if (pieces[pieces.Count - 1].showValue.text == StringPopupBuilder.newEntryString)
					indexToAdd--;
			}
			pieces.Insert(indexToAdd, stringPiece);
		}
	}

	public class CardSelectionPieceList : StringPieceList
	{
		StringPiece lastPiece;
		StringPiece lastBracket;

		internal CardSelectionPieceList () : base(new CardSelectionPartPopup(), new StringPiece(")"))
		{
			showValue = new GUIContent("Card Selection (");
			codifyValue = "c(";
			lastPiece = pieces[0];
			lastBracket = pieces[1];
		}

		public override StringPiece Clone ()
		{
			return new CardSelectionPieceList();
		}

		public override void ShowInEditor ()
		{
			EditorGUILayout.LabelField(showValue, GUILayout.Width(width));
			if (lastPiece.showValue.text != StringPopupBuilder.blankEntryString && lastPiece.showValue.text != "allcards")
			{
				lastPiece = new CardSelectionPartPopup();
				int insertIndex = pieces.IndexOf(lastBracket);
				pieces.Insert(insertIndex, lastPiece);
				pieces.Insert(insertIndex, new StringPiece(","));
			}
			for (int i = 0; i < pieces.Count; i++)
			{
				StringPiece piece = pieces[i];
				if (piece != lastPiece && pieces[i].showValue.text == StringPopupBuilder.blankEntryString)
				{
					pieces.RemoveAt(i); //remove Piece
					pieces.RemoveAt(i); //remove ","
					i--;
					if (i < 0)
						continue;
				}
				pieces[i].ShowInEditor();
			}
		}

		public override string Codify ()
		{
			string code = base.Codify().Replace(",)", ")");
			if (code.Contains("allcards"))
				code = "allcards";
			return code;
		}
	}

	public class CardSelectionPartPopup : StringPopupPiece
	{
		StringPiece argument;

		public CardSelectionPartPopup () : base(StringPopupBuilder.cardSelectionParts, 0) { }

		public override StringPiece Clone ()
		{
			return new CardSelectionPartPopup();
		}

		protected override void OnPopupChanged ()
		{
			argument = null;
			if (popupValue != StringPopupBuilder.blankEntryString)
			{
				argument = StringPopupBuilder.cardSelectionPartsDefaults[index].CloneAll();
				//switch (popupValue)
				//{
				//	case "ID":
				//		break;
				//	case "Zone":
				//		argument = StringPopupBuilder.cardSelectionPartsDefaults[index].CloneAll();
				//		//argument = new StringPiece("=", "");
				//		//StringPiece popup = new StringPopupPiece(StringPopupBuilder.zoneTags, 0);
				//		//argument.SetNext(popup, new AndOrPopup(popup));
				//		break;
				//	default:
				//		return;
				//}
				codifyValue = StringPopupBuilder.cardSelectionPartsCodified[index];
			}
			else
			{
				argument = null;
			}
		}

		public override void ShowInEditor ()
		{
			base.ShowInEditor();
			if (argument != null)
			{
				argument.ShowInEditorAll();
			}
		}

		public override string Codify ()
		{
			codifyValue = StringPopupBuilder.cardSelectionPartsCodified[index];
			return codifyValue + (argument != null ? argument.CodifyAll() : "");
		}
	}

	public class AndOrPopup : StringPopupPiece
	{
		StringPiece previous;

		public AndOrPopup () : base(StringPopupBuilder.logicOperators, 0) { }

		public AndOrPopup (StringPiece previous) : base(StringPopupBuilder.logicOperators, 0) { this.previous = previous; }

		public override StringPiece Clone ()
		{
			return new AndOrPopup(previous);
		}

		public StringPiece SetPrevious (StringPiece previous)
		{
			this.previous = previous;
			previous.next = this;
			return previous;
		}

		protected override void OnPopupChanged ()
		{
			base.OnPopupChanged();
			if (next == null && popupValue != StringPopupBuilder.blankSpace)
			{
				StringPiece clone = previous.Clone();
				next = clone.SetNext(new AndOrPopup(clone));
			}
			else
			{
				next = null;
			}
		}

		public override string Codify ()
		{
			codifyValue = StringPopupBuilder.logicOperatorsCodified[index];
			return codifyValue;
		}
	}

	#endregion

	internal class StringPopupBuilder
	{
		internal const string newEntryString = "<other>";
		internal const string blankEntryString = "<none>";
		internal const string blankSpace = ".";
		internal static CardGameData _contextGame;
		internal static CardGameData contextGame
		{
			get { return _contextGame; }
			set
			{
				_contextGame = value;
				GetZoneTags();
			}
		}

		internal static GUIContent[] logicOperators = new GUIContent[]
		{
			new GUIContent(blankSpace),
			new GUIContent("AND"),
			new GUIContent("OR")
		};

		internal static string[] logicOperatorsCodified = new string[]
		{
			string.Empty,
			"&",
			"|"
		};

		internal static GUIContent[] cardSelectionParts = new GUIContent[]
		{
			new GUIContent(blankEntryString),
			new GUIContent("ID"),
			new GUIContent("Zone"),
			new GUIContent("Tag"),
			new GUIContent("Rule"),
			new GUIContent("Field"),
			new GUIContent("Top Qty"),
			new GUIContent("Bottom Qty"),
			new GUIContent("Slot"),
			new GUIContent("allcards")
		};

		internal static StringPiece[] cardSelectionPartsDefaults = new StringPiece[]
		{
			new StringPiece(), //Blank
			new StringPiece(), //ID
			new StringPiece("=", "").SetNext(new AndOrPopup().SetPrevious(new StringPopupPiece(zoneTags, 0))), //Zone
			new StringPiece(), //Tag
			new StringPiece(), //Rule
			new StringPiece(), //Field
			new StringPiece(), //Top Qty
			new StringPiece(), //Bottom Qty
			new StringPiece(), //Slot
			new StringPiece(), //allcards
		};

		internal static string[] cardSelectionPartsCodified = new string[] { "", "i:", "z:", "t:", "r:", "f:", "x:", "b:", "s:", "allcards" };

		internal static GUIContent[] _zoneTags;
		internal static GUIContent[] zoneTags
		{
			get
			{
				if (_zoneTags == null)
				{
					GetZoneTags();
				}
				return _zoneTags;
			}
		}

		//internal static GUIContent[] cardTags;
		//internal static GUIContent[] matchVariables;
		//internal static GUIContent[] cardIdReferences;
		//internal static GUIContent[] fields;

		internal static GUIContent[] commandLabel = new GUIContent[]
		{
			new GUIContent(blankEntryString),
			new GUIContent("Add Tag To Card", "Adds a tag to all cards selected."),
			new GUIContent("End Current Phase", "Ends current phase imediately."),
			new GUIContent("End Subphase Loop", "Ends a running subphase loop started with 'Start Subphase Loop'"),
			new GUIContent("End The Match", "Ends in sequence: current subphase loop, phase, turn and then the match."),
			new GUIContent("Move Card To Zone", "Places a selection of cards in a selection of zones. If more than one zone is selected, this will try to move cards to each of those zones. The cards will be moved 'in game' but a visualization of the movement must be implemented via a MatchWatcher or by using the CardMover utility."),
			new GUIContent("Remove Tag from Card", "Removes a tag from all cards selected. Cards that do not have that tag will be unaffected."),
			new GUIContent("Send Message", "Sends a message (ie a simple string) to all MatchWatchers. This is mostly used to trigger UI events."),
			new GUIContent("Set Card Field Value", "Changes the specified field of a selection of cards to the value passed. Use an operator (ie + * /) to make operations with the value already on the field."),
			new GUIContent("Set Variable", "Changes the value of a match variable to the value passed. Use an operator (ie + * /) to make operations with the value already on the variable."),
			new GUIContent("Shuffle", "Shuffles the components at the zones selected."),
			new GUIContent("Start Subphase Loop", "Starts a sequence of phases that will run indefinitely until 'End Subphase Loop' is called."),
			new GUIContent("Use Action", "Uses an action (ie a simple string) to start some behaviour on the match. This is mostly used to send events from the UI or the user to the match."),
			new GUIContent("Use Card", "Fires a special action with a card or selection of cards to be catched by the match."),
			new GUIContent("Use Zone", "Fires a special action with a zone or selection of zones to be catched by the match.")
		};

		internal static StringPiece[] commandSignatures = new StringPiece[]
		{
			new StringPiece(),
			new StringPiece(" ", "(").SetNext(new CardSelectionPieceList(), new StringPiece(" | ", ","), new StringPiece("Tag"), new StringPiece(" ", ")")),
			new StringPiece(),
			new StringPiece(),
			new StringPiece(),
					//_commandSignatures.Add("Move Card To Zone", "<Card Selection>,<Zone Selection>,?<Additional Param 1>,?<Additional Param 2>,?<Additional Param 3>,?<Additional Param 4>");
					//_commandSignatures.Add("Remove Tag from Card", "<Card Selection>,<Card Tag>");
					//_commandSignatures.Add("Send Message", "<Message>");
					//_commandSignatures.Add("Set Card Field Value", "<Card Field>,<Card Selection>");
					//_commandSignatures.Add("Set Variable", "<Variable Name>,<Value>,?<Min>,?<Max>");
					//_commandSignatures.Add("Shuffle", "<Zone Selection>");
					//_commandSignatures.Add("Start Subphase Loop", "<Subphases>");
					//_commandSignatures.Add("Use Action", "<Action Name>");
					//_commandSignatures.Add("Use Card", "<Card Selection>");
					//_commandSignatures.Add("Use Zone", "<Zone Selection>");
		};

		static void GetZoneTags ()
		{
			List<string> zoneList = StringUtility.ExtractZoneTags(contextGame);
			zoneList.Insert(0, blankEntryString);
			zoneList.Add(newEntryString);
			_zoneTags = new GUIContent[zoneList.Count];
			for (int i = 0; i < zoneList.Count; i++)
			{
				_zoneTags[i] = new GUIContent(zoneList[i]);
			}
		}

		internal static int IndexOfCommand (string name)
		{
			for (int i = 0; i < commandLabel.Length; i++)
			{
				if (commandLabel[i].text == name)
					return i;
			}
			return -1;
		}

		public static string DrawStringPieceSequence (StringPiece piece)
		{
			StringBuilder sb = new StringBuilder();
			while (piece != null)
			{
				sb.Append(piece.GetHashCode() + " , ");
				piece = piece.next;
			}
			return sb.ToString();
		}
	}
}