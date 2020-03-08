using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace CardGameFramework
{
	#region Pieces ============================================================================================

	public interface IEditorPiece
	{
		void ShowInEditor ();
		string Codify ();
		StringPiece Clone ();
	}

	public enum InfoList
	{
		Blank = 0,
		LogicOperators = 1,
		LogicOperatorsCodified = 2,
		CardSelectionParts = 3,
		CardSelectionPartsDefaults = 4,
		CardSelectionPartsCodified = 5,
		CommandLabels = 6,
		CommandDefaults = 7,
		ZoneTags = 8,
		CardTags = 9
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
		InfoList infoList;
		public GUIContent[] stringArray { get { return (GUIContent[])StringPopupBuilder.instance.lists[infoList]; } }
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

		internal StringPopupPiece (InfoList infoList, int index)
		{
			this.infoList = infoList;
			//stringArray = (GUIContent[])StringPopupBuilder.instance.lists[infoList];
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
			StringPopupBuilder.instance.lists[infoList] = newArray;
		}

		public override void ShowInEditor ()
		{
			if (popupValue == StringPopupBuilder.newEntryString)
			{
				Event evt = Event.current;
				if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape)
				{
					index = previousIndex;
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
			return new StringPopupPiece(infoList, 0);
		}

		protected override float SetWidth ()
		{
			return EditorStyles.popup.CalcSize(showValue).x;
		}
	}

	public class CommandLabelPopup : StringPopupPiece
	{
		internal CommandLabelPopup () : base(InfoList.CommandLabels, 0)
		{
			GetNextFromSignature();
		}
		internal CommandLabelPopup (int index) : base(InfoList.CommandLabels, index)
		{
			GetNextFromSignature();
		}
		internal CommandLabelPopup (string value)
			: base(InfoList.CommandLabels, Mathf.Clamp(StringPopupBuilder.instance.IndexOfCommand(value), 0, StringPopupBuilder.commandLabels.Length - 1))
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

		internal CardSelectionPieceList () : base(new CardSelectionPartPopup(), new StringPiece("", ")"))
		{
			showValue = new GUIContent("[ Card Selection ");
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
				pieces.Insert(insertIndex, new StringPiece("|", ","));
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

		public CardSelectionPartPopup () : base(InfoList.CardSelectionParts, 0) { }

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

		public AndOrPopup () : base(InfoList.LogicOperators, 0) { }

		public AndOrPopup (StringPiece previous) : base(InfoList.LogicOperators, 0) { this.previous = previous; }

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
		static StringPopupBuilder _instance;
		internal static StringPopupBuilder instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new StringPopupBuilder();
					_instance.Initialize();
				}
				return _instance;
			}
		}

		internal const string newEntryString = "< new >";
		internal const string blankEntryString = "( none )";
		internal const string blankSpace = "+";

		void Initialize ()
		{
			GUIContent[] blankList = new GUIContent[] { new GUIContent(blankEntryString), new GUIContent(newEntryString) };
			GUIContent[] logicOperators = new GUIContent[] { new GUIContent(blankSpace), new GUIContent("AND"), new GUIContent("OR") };
			string[] logicOperatorsCodified = new string[] { string.Empty, "&", "|" };
			GUIContent[] cardSelectionParts = new GUIContent[] {
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
			StringPiece[] cardSelectionPartsDefaults = new StringPiece[] {
				new StringPiece(), //Blank
				new StringPiece(), //ID
				new StringPiece(), //Zone
				new StringPiece(), //Tag
				new StringPiece(), //Rule
				new StringPiece(), //Field
				new StringPiece(), //Top Qty
				new StringPiece(), //Bottom Qty
				new StringPiece(), //Slot
				new StringPiece(), //allcards
			};
			string[] cardSelectionPartsCodified = new string[] { "", "i:", "z:", "t:", "r:", "f:", "x:", "b:", "s:", "allcards" };
			GUIContent[] zoneTags = blankList;
			GUIContent[] cardTags = blankList;
			//GUIContent[] matchVariables;
			//GUIContent[] cardIdReferences;
			//GUIContent[] fields;
			GUIContent[] commandLabels = new GUIContent[]
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
			StringPiece[] commandDefaults = new StringPiece[]
			{
				new StringPiece(), //  0 - Blank
				new StringPiece(), //  1 - Add Tag To Card
				new StringPiece(), //  2 - End Current Phase
				new StringPiece(), //  3 - End Subphase Loop
				new StringPiece(), //  4 - End The Match
				new StringPiece(), //  5 - Move Card To Zone
				new StringPiece(), //  6 - Remove Tag From Card
				new StringPiece(), //  7 - Send Message
				new StringPiece(), //  8 - Set Card Field Value
				new StringPiece(), //  9 - Set Variable
				new StringPiece(), // 10 - Shuffle
				new StringPiece(), // 11 - Start Subphase Loop
				new StringPiece(), // 12 - Use Action
				new StringPiece(), // 13 - Use Card
				new StringPiece() // 14 - Use Zone
			};

			lists.Add(InfoList.Blank, blankList);
			lists.Add(InfoList.LogicOperators, logicOperators);
			lists.Add(InfoList.LogicOperatorsCodified, logicOperatorsCodified);
			lists.Add(InfoList.CardSelectionParts, cardSelectionParts);
			lists.Add(InfoList.CardSelectionPartsDefaults, cardSelectionPartsDefaults);
			lists.Add(InfoList.CardSelectionPartsCodified, cardSelectionPartsCodified);
			lists.Add(InfoList.CommandLabels, commandLabels);
			lists.Add(InfoList.CommandDefaults, commandDefaults);
			lists.Add(InfoList.ZoneTags, zoneTags);
			lists.Add(InfoList.CardTags, cardTags);
			
			cardSelectionPartsDefaults[2] = new StringPiece("=", "").SetNext(new AndOrPopup().SetPrevious(new StringPopupPiece(InfoList.ZoneTags, 0))); //Zone
			cardSelectionPartsDefaults[3] = new StringPiece("=", "").SetNext(new AndOrPopup().SetPrevious(new StringPopupPiece(InfoList.CardTags, 0))); //Tag

			commandDefaults[1] = new StringPiece(" ", "(").SetNext(new CardSelectionPieceList(), new StringPiece("]  [", ","), new StringPiece("Tag =", ""), new StringPopupPiece(InfoList.CardTags, 0), new StringPiece("]", ")"));
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
		}

		CardGameData _contextGame;
		internal CardGameData contextGame
		{
			get { return _contextGame; }
			set
			{
				_contextGame = value;
				lists[InfoList.ZoneTags] = GetZoneTags(value);
				lists[InfoList.CardTags] = GetCardTags(value);
			}
		}

		internal Hashtable lists = new Hashtable();

		internal static GUIContent[] blankList { get { return (GUIContent[])instance.lists[InfoList.Blank]; } }
		internal static GUIContent[] logicOperators { get { return (GUIContent[])instance.lists[InfoList.LogicOperators]; } }
		internal static string[] logicOperatorsCodified { get { return (string[])instance.lists[InfoList.LogicOperatorsCodified]; } }
		internal static GUIContent[] cardSelectionParts { get { return (GUIContent[])instance.lists[InfoList.CardSelectionParts]; } }
		internal static StringPiece[] cardSelectionPartsDefaults { get { return (StringPiece[])instance.lists[InfoList.CardSelectionPartsDefaults]; } }
		internal static string[] cardSelectionPartsCodified { get { return (string[])instance.lists[InfoList.CardSelectionPartsCodified]; } }
		internal static GUIContent[] zoneTags { get { return (GUIContent[])instance.lists[InfoList.ZoneTags]; } }
		internal static GUIContent[] cardTags { get { return (GUIContent[])instance.lists[InfoList.CardTags]; } }
		//internal static GUIContent[] matchVariables { get { return (GUIContent[])instance.lists[InfoList.MatchVariables]; } }
		//internal static GUIContent[] cardIdReferences { get { return (GUIContent[])instance.lists[InfoList.CardIDReferences]; } }
		//internal static GUIContent[] fields { get { return (GUIContent[])instance.lists[InfoList.Fields]; } }
		internal static GUIContent[] commandLabels { get { return (GUIContent[])instance.lists[InfoList.CommandLabels]; } }
		internal static StringPiece[] commandSignatures { get { return (StringPiece[])instance.lists[InfoList.CommandDefaults]; } }

		GUIContent[] GetZoneTags (CardGameData game)
		{
			List<string> zoneList = StringUtility.ExtractZoneTags(game);
			zoneList.Insert(0, blankEntryString);
			zoneList.Add(newEntryString);
			GUIContent[] zoneTags = zoneTags = new GUIContent[zoneList.Count];
			for (int i = 0; i < zoneList.Count; i++)
			{
				zoneTags[i] = new GUIContent(zoneList[i]);
			}
			return zoneTags;
		}

		GUIContent[] GetCardTags (CardGameData game)
		{
			List<string> cardList = StringUtility.ExtractCardTags(game);
			cardList.Insert(0, blankEntryString);
			cardList.Add(newEntryString);
			GUIContent[] cardTags = new GUIContent[cardList.Count];
			for (int i = 0; i < cardList.Count; i++)
			{
				cardTags[i] = new GUIContent(cardList[i]);
			}
			return cardTags;
		}

		internal int IndexOfCommand (string name)
		{
			for (int i = 0; i < commandLabels.Length; i++)
			{
				if (commandLabels[i].text == name)
					return i;
			}
			return -1;
		}

		public string DrawStringPieceSequence (StringPiece piece)
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