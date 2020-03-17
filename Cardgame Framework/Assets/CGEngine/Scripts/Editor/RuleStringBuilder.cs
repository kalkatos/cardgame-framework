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
		CardTags = 9,
		MatchMessages = 10,
		MatchUIAction = 11,
		CardVariables = 12,
		ZoneVariables = 13,
		ZoneSelectionParts = 14,
		ZoneSelectionPartsDefaults = 15,
		ZoneSelectionPartsCodified = 16,
		MoveCardRevealedOptions = 17,
		MoveCardPositionOptions = 18,
		MoveCardRevealedOptionsCodified = 19,
		MoveCardPositionOptionsCodified = 20,
		CommandLabelsCodified = 21,
		CardFields = 22,
		MatchVariables = 23,
		CardRules = 24,
		ComparisonOperators = 25,
		ComparisonOperatorsCodified = 26,
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
				if (GUILayout.Button("X", GUILayout.Width(20)))
					toBeDeleted = i;
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();
			}
			if (toBeDeleted != -1)
				sequence.RemoveAt(toBeDeleted);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("  +  ", GUILayout.Width(45)))
			{
				sequence.Add(model.CloneAll());
			}
			if (GUILayout.Button("Clear", GUILayout.Width(45)))
			{
				for (int i = 0; i < sequence.Count; i++)
				{
					sequence[i].DisposeAllNext();
				}
				sequence.Clear();
			}
			GUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
		}
	}

	public class StringPopupPiece : StringPiece
	{
		InfoList infoList;
		public GUIContent[] stringArray { get { return (GUIContent[])StringPopupBuilder.instance.lists[infoList]; } }
		InfoList codifyList;
		public string[] codifyArray { get { return (string[])StringPopupBuilder.instance.lists[codifyList]; } }
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
			this.index = index;
			showValue = stringArray[index];
			codifyList = InfoList.Blank;
		}
		internal StringPopupPiece (InfoList infoList, InfoList codifyList, int index) : this(infoList, index)
		{
			this.codifyList = codifyList;
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
			if (codifyList != InfoList.Blank)
				return codifyValue = codifyArray[index];
			return codifyValue = popupValue;
		}
		protected virtual void OnPopupChanged ()
		{
		}
		public override StringPiece Clone ()
		{
			return new StringPopupPiece(infoList, codifyList, 0);
		}
		protected override float SetWidth ()
		{
			return EditorStyles.popup.CalcSize(showValue).x;
		}
	}

	public class CommandLabelPopup : StringPopupPiece
	{
		internal CommandLabelPopup () : base(InfoList.CommandLabels, InfoList.CommandLabelsCodified, 0)
		{
			GetNextFromSignature();
		}
		internal CommandLabelPopup (int index) : base(InfoList.CommandLabels, InfoList.CommandLabelsCodified, index)
		{
			GetNextFromSignature();
		}
		internal CommandLabelPopup (string value)
			: base(InfoList.CommandLabels, InfoList.CommandLabelsCodified, Mathf.Clamp(StringPopupBuilder.instance.IndexOfCommand(value), 0, StringPopupBuilder.commandLabels.Length - 1))
		{
			GetNextFromSignature();
		}
		protected override void OnPopupChanged ()
		{
			base.OnPopupChanged();
			GetNextFromSignature();
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
		internal CardSelectionPieceList (string showString, string codeString) : base(new CardSelectionPartPopup(), new StringPiece("", ")"))
		{
			showValue = new GUIContent(showString);
			codifyValue = codeString;
			lastPiece = pieces[0];
			lastBracket = pieces[1];
		}
		public override StringPiece Clone ()
		{
			return new CardSelectionPieceList(showValue.text, codifyValue);
		}
		public override void ShowInEditor ()
		{
			EditorGUILayout.LabelField(showValue, GUILayout.Width(width));
			if (lastPiece.showValue.text != StringPopupBuilder.blankEntryString && lastPiece.showValue.text != "allcards")
			{
				lastPiece = new CardSelectionPartPopup();
				int insertIndex = pieces.IndexOf(lastBracket);
				pieces.Insert(insertIndex, lastPiece);
				pieces.Insert(insertIndex, new StringPiece("&", ","));
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
		public CardSelectionPartPopup () : base(InfoList.CardSelectionParts, InfoList.CardSelectionPartsCodified, 0) { }
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
			codifyValue = base.Codify();
			return codifyValue + (argument != null ? argument.CodifyAll() : "");
		}
	}

	public class ZoneSelectionPieceList : StringPieceList
	{
		StringPiece lastPiece;
		StringPiece lastBracket;
		internal ZoneSelectionPieceList () : base(new ZoneSelectionPartPopup(), new StringPiece("", ")"))
		{
			showValue = new GUIContent("[ Get Zones ");
			codifyValue = "z(";
			lastPiece = pieces[0];
			lastBracket = pieces[1];
		}
		public override StringPiece Clone ()
		{
			return new ZoneSelectionPieceList();
		}
		public override void ShowInEditor ()
		{
			EditorGUILayout.LabelField(showValue, GUILayout.Width(width));
			if (lastPiece.showValue.text != StringPopupBuilder.blankEntryString && lastPiece.showValue.text != "allzones")
			{
				lastPiece = new ZoneSelectionPartPopup();
				int insertIndex = pieces.IndexOf(lastBracket);
				pieces.Insert(insertIndex, lastPiece);
				pieces.Insert(insertIndex, new StringPiece("&", ","));
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
			if (code.Contains("allzones"))
				code = "allzones";
			return code;
		}
	}

	public class ZoneSelectionPartPopup : StringPopupPiece
	{
		StringPiece argument;
		public ZoneSelectionPartPopup () : base(InfoList.ZoneSelectionParts, InfoList.ZoneSelectionPartsCodified, 0) { }
		public override StringPiece Clone ()
		{
			return new ZoneSelectionPartPopup();
		}
		protected override void OnPopupChanged ()
		{
			argument = null;
			if (popupValue != StringPopupBuilder.blankEntryString)
			{
				argument = StringPopupBuilder.zoneSelectionPartsDefaults[index].CloneAll();
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
			codifyValue = base.Codify();
			return codifyValue + (argument != null ? argument.CodifyAll() : "");
		}
	}
	
	public class AndOrPopup : StringPopupPiece
	{
		StringPiece previous;
		public AndOrPopup () : base(InfoList.LogicOperators, InfoList.LogicOperatorsCodified, 0) { }
		public AndOrPopup (StringPiece previous) : base(InfoList.LogicOperators, InfoList.LogicOperatorsCodified, 0) { this.previous = previous; }
		public override StringPiece Clone ()
		{
			return new AndOrPopup(previous.Clone());
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
	}

	public class SubphaseLoopPiece : StringPiece
	{
		List<string> subphases;
		public SubphaseLoopPiece (string label = "") : base ()
		{
			showValue.text = label;
			subphases = new List<string>();
			subphases.Add("");
		}
		public override StringPiece Clone ()
		{
			return new SubphaseLoopPiece(showValue.text);
		}
		public override void ShowInEditor ()
		{
			EditorGUILayout.BeginHorizontal();
			base.ShowInEditor();
			int delete = -1;
			for (int i = 0; i < subphases.Count; i++)
			{
				subphases[i] = EditorGUILayout.TextField(subphases[i], GUILayout.Width(80));
				if (GUILayout.Button("X", GUILayout.Width(18)))
				{
					delete = i;
				}
			}
			if (delete > -1)
				subphases.RemoveAt(delete);
			if (GUILayout.Button("+", GUILayout.Width(18)))
			{
				subphases.Add("");
			}
			EditorGUILayout.EndHorizontal();
		}
		public override string Codify ()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < subphases.Count; i++)
			{
				if (string.IsNullOrEmpty(subphases[i]))
					continue;
				sb.Append(subphases[i]);
				if (i < subphases.Count - 1)
					sb.Append(",");
			}
			return codifyValue = sb.ToString();
		}
	}
	public class ConditionPopup : StringPopupPiece
	{
		StringPiece leftCompare;
		StringPiece rightCompare;
		public ConditionPopup (StringPiece leftCompare, StringPiece rightCompare) : base(InfoList.ComparisonOperators, InfoList.ComparisonOperatorsCodified, 0)
		{
			this.leftCompare = leftCompare;
			this.rightCompare = rightCompare;
		}
		public override StringPiece Clone ()
		{
			return new ConditionPopup(leftCompare.Clone(), rightCompare.Clone());
		}
		public StringPiece SetPieces (StringPiece leftCompare, StringPiece rightCompare)
		{
			this.leftCompare = leftCompare;
			this.rightCompare = rightCompare;
			return this;
		}
		//protected override void OnPopupChanged ()
		//{
		//	base.OnPopupChanged();
		//	if (next == null && popupValue != StringPopupBuilder.blankSpace)
		//	{
		//		StringPiece clone = previous.Clone();
		//		next = clone.SetNext(new AndOrPopup(clone));
		//	}
		//	else
		//	{
		//		next = null;
		//	}
		//}
		public override void ShowInEditor ()
		{
			leftCompare.ShowInEditor();
			base.ShowInEditor();
			rightCompare.ShowInEditor();
		}
		public override string Codify ()
		{
			return leftCompare.Codify() + base.Codify() + rightCompare.Codify();
		}
	}
	public class EnterValuePiece : StringPiece
	{
		public override StringPiece Clone ()
		{
			return new EnterValuePiece();
		}
		public override void ShowInEditor ()
		{
			showValue.text = EditorGUILayout.TextField(showValue.text, EditorStyles.label, GUILayout.Width(80));
			codifyValue = showValue.text;
		}
	}
	public class NumberOfCardsPiece : StringPiece
	{

	}
	#endregion
	/*
			//card
			variables.Add("movedCard", "");
			variables.Add("usedCard", "");
			//zone
			variables.Add("targetZone", "");
			variables.Add("oldZone", "");
			variables.Add("usedZone", "");
			//string
			variables.Add("phase", "");
			variables.Add("actionName", "");
			variables.Add("message", "");
			variables.Add("additionalInfo", "");
			variables.Add("variable", "");
			//number
			variables.Add("matchNumber", 0);
			variables.Add("turnNumber", 0);
			variables.Add("value", 0);
			variables.Add("min", float.MinValue);
			variables.Add("max", float.MaxValue);
	*/
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
		internal const string chooseCommandString = "< choose >";
		internal const string blankEntryString = "_";
		internal const string blankSpace = "+";
		void Initialize ()
		{
			GUIContent[] blankList = new GUIContent[] { new GUIContent(blankEntryString), new GUIContent(newEntryString) };
			GUIContent[] logicOperators = new GUIContent[] { new GUIContent(blankSpace), new GUIContent("AND"), new GUIContent("OR") };
			string[] logicOperatorsCodified = new string[] { string.Empty, "&", "|" };
			GUIContent[] comparisonOperators = new GUIContent[]
			{
				new GUIContent("="),
				new GUIContent("!="),
				new GUIContent(">="),
				new GUIContent("<="),
				new GUIContent("IS IN"),
				new GUIContent(">"),
				new GUIContent("<"),
			};
			string[] comparisonOperatorsCodified = new string[] { "=", "!=", ">=", "<=", "=>", ">", "<" };
			GUIContent[] cardSelectionParts = new GUIContent[] {
				new GUIContent(blankEntryString),
				new GUIContent("From Variable"),
				new GUIContent("In Zone"),
				new GUIContent("With Tag"),
				new GUIContent("With Rule"), 
				new GUIContent("With Field"),
				new GUIContent("Quantity from Top"),
				new GUIContent("Quantity from Bottom"),
				new GUIContent("In Grid Slot"),
				new GUIContent("All Cards")
			};
			StringPiece[] cardSelectionPartsDefaults = new StringPiece[] {
				new StringPiece(), // 0 - Blank
				new StringPiece(), // 1 - ID / Variable
				new StringPiece(), // 2 - Zone
				new StringPiece(), // 3 - Tag
				new StringPiece(), // 4 - Rule
				new StringPiece(), // 5 - Field
				new StringPiece(), // 6 - Top Qty
				new StringPiece(), // 7 - Bottom Qty
				new StringPiece(), // 8 - Slot
				new StringPiece(), // 9 - allcards
			};
			string[] cardSelectionPartsCodified = new string[] { "", "i:", "z:", "t:", "r:", "f:", "x:", "b:", "s:", "allcards" };
			GUIContent[] zoneSelectionParts = new GUIContent[] {
				new GUIContent(blankEntryString),
				new GUIContent("From Variable"),
				new GUIContent("With Tag"),
				new GUIContent("All Zones")
			};
			StringPiece[] zoneSelectionPartsDefaults = new StringPiece[] {
				new StringPiece(), // 0 - Blank
				new StringPiece(), // 1 - ID / Variable
				new StringPiece(), // 2 - Tag
				new StringPiece(), // 3 - allzones
			};
			string[] zoneSelectionPartsCodified = new string[] { "", "i:", "t:", "allzones" };
			GUIContent[] zoneTags = blankList;
			GUIContent[] cardTags = blankList;
			GUIContent[] matchMessages = blankList;
			GUIContent[] matchUIActions = blankList;
			GUIContent[] cardFields = blankList;
			GUIContent[] cardRules = new GUIContent[] { new GUIContent(blankEntryString) };
			GUIContent[] matchVariables = new GUIContent[] { new GUIContent(blankEntryString) };
			GUIContent[] cardVariables = new GUIContent[]
			{
				new GUIContent("movedCard"),
				new GUIContent("usedCard")
			};
			GUIContent[] zoneVariables = new GUIContent[]
			{
				new GUIContent("targetZone"),
				new GUIContent("oldZone"),
				new GUIContent("usedZone")
			};
			GUIContent[] commandLabels = new GUIContent[]
			{
				new GUIContent(chooseCommandString),
				new GUIContent("Add Tag to Card", "Adds a tag to all cards selected."),
				new GUIContent("End Current Phase", "Ends current phase imediately."),
				new GUIContent("End Subphase Loop", "Ends a running subphase loop started with 'Start Subphase Loop'"),
				new GUIContent("End the Match", "Ends in sequence: current subphase loop, phase, turn and then the match."),
				new GUIContent("Move Card to Zone", "Places a selection of cards in a selection of zones. If more than one zone is selected, this will try to move cards to each of those zones. The cards will be moved 'in game' but a visualization of the movement must be implemented via a MatchWatcher or by using the CardMover utility."),
				new GUIContent("Remove Tag from Card", "Removes a tag from all cards selected. Cards that do not have that tag will be unaffected."),
				new GUIContent("Send Message", "Sends a message (ie a simple string) to all MatchWatchers. This is mostly used to trigger UI events."),
				new GUIContent("Set Card Field Value", "Changes the specified field of a selection of cards to the value passed. Use an operator (eg + * /) to make operations with the value already on the field."),
				new GUIContent("Set Variable", "Changes the value of a match variable to the value passed. Use an operator (eg + * /) to make operations with the value already on the variable."),
				new GUIContent("Shuffle", "Shuffles the components at the zones selected."),
				new GUIContent("Start Subphase Loop", "Starts a sequence of phases that will run indefinitely until 'End Subphase Loop' is called."),
				new GUIContent("Use Action", "Uses an action (ie a simple string) to start some behaviour on the match. This is mostly used to send events from the UI or the user to the match."),
				new GUIContent("Use Card", "Fires a special action with a card or selection of cards to be catched by the match."),
				new GUIContent("Use Zone", "Fires a special action with a zone or selection of zones to be catched by the match.")
			};
			string[] commandLabelsCodified = new string[]
			{
				"",
				"AddTagToCard",
				"EndCurrentPhase",
				"EndSubphaseLoop",
				"EndTheMatch",
				"MoveCardToZone",
				"RemoveTagFromCard",
				"SendMessage",
				"SetCardFieldValue",
				"SetVariable",
				"Shuffle",
				"StartSubphaseLoop",
				"UseAction",
				"UseCard",
				"UseZone"
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
				new StringPiece()  // 14 - Use Zone
			};
			GUIContent[] moveCardRevealedOptions = new GUIContent[]
			{
				new GUIContent(blankSpace),
				new GUIContent("Revealed", "Set as Revealed regardless of zone definition."),
				new GUIContent("Hidden", "Set as Hidden regardless of zone definition."),
			};
			string[] moveCardRevealedOptionsCodified = new string[] { "", ",Revealed", ",Hidden" };
			GUIContent[] moveCardPositionOptions = new GUIContent[]
			{
				new GUIContent("To the Top", "Moves the card to the top of the zone (if stack) or to the rightmost position (if side-by-side)."),
				new GUIContent("To the Bottom", "Moves the card to the bottom of the zone (if stack) or to the leftmost position (if side-by-side)."),
			};
			string[] moveCardPositionOptionsCodified = new string[] { "", ",Bottom" };

			lists.Add(InfoList.Blank, blankList);
			lists.Add(InfoList.LogicOperators, logicOperators);
			lists.Add(InfoList.LogicOperatorsCodified, logicOperatorsCodified);
			lists.Add(InfoList.ComparisonOperators, comparisonOperators);
			lists.Add(InfoList.ComparisonOperatorsCodified, comparisonOperatorsCodified);
			lists.Add(InfoList.CardSelectionParts, cardSelectionParts);
			lists.Add(InfoList.CardSelectionPartsDefaults, cardSelectionPartsDefaults);
			lists.Add(InfoList.CardSelectionPartsCodified, cardSelectionPartsCodified);
			lists.Add(InfoList.CommandLabels, commandLabels);
			lists.Add(InfoList.CommandLabelsCodified, commandLabelsCodified);
			lists.Add(InfoList.CommandDefaults, commandDefaults);
			lists.Add(InfoList.ZoneTags, zoneTags);
			lists.Add(InfoList.CardTags, cardTags);
			lists.Add(InfoList.CardFields, cardFields);
			lists.Add(InfoList.CardRules, cardRules);
			lists.Add(InfoList.MatchMessages, matchMessages);
			lists.Add(InfoList.MatchUIAction, matchUIActions);
			lists.Add(InfoList.MatchVariables, matchVariables);
			lists.Add(InfoList.CardVariables, cardVariables);
			lists.Add(InfoList.ZoneVariables, zoneVariables);
			lists.Add(InfoList.ZoneSelectionParts, zoneSelectionParts);
			lists.Add(InfoList.ZoneSelectionPartsDefaults, zoneSelectionPartsDefaults);
			lists.Add(InfoList.ZoneSelectionPartsCodified, zoneSelectionPartsCodified);
			lists.Add(InfoList.MoveCardRevealedOptions, moveCardRevealedOptions);
			lists.Add(InfoList.MoveCardRevealedOptionsCodified, moveCardRevealedOptionsCodified);
			lists.Add(InfoList.MoveCardPositionOptions, moveCardPositionOptions);
			lists.Add(InfoList.MoveCardPositionOptionsCodified, moveCardPositionOptionsCodified);

			cardSelectionPartsDefaults[1] = new StringPiece(":", "").SetNext(new StringPopupPiece(InfoList.CardVariables, 0)); //Variable / ID
			cardSelectionPartsDefaults[2] = new StringPiece(":", "").SetNext(new AndOrPopup().SetPrevious(new StringPopupPiece(InfoList.ZoneTags, 0))); //Zone
			cardSelectionPartsDefaults[3] = new StringPiece(":", "").SetNext(new AndOrPopup().SetPrevious(new StringPopupPiece(InfoList.CardTags, 0))); //Tag
			cardSelectionPartsDefaults[4] = new StringPiece(":", "").SetNext(new AndOrPopup().SetPrevious(new StringPopupPiece(InfoList.CardRules, 0))); //Rule
			cardSelectionPartsDefaults[5] = new StringPiece(":", "").SetNext(new AndOrPopup().SetPrevious(new ConditionPopup(new StringPopupPiece(InfoList.CardFields, 0), new EnterValuePiece(/*TODO value helpers*/)))); //Field
			cardSelectionPartsDefaults[6] = new StringPiece(":", "").SetNext(new EnterValuePiece(/*TODO value helpers*/)); //Top Qty
			cardSelectionPartsDefaults[7] = new StringPiece(":", "").SetNext(new EnterValuePiece(/*TODO value helpers*/)); //Bottom Qty
			cardSelectionPartsDefaults[8] = new StringPiece(":", "").SetNext(new EnterValuePiece(/*TODO value helpers*/)); //Slot

			zoneSelectionPartsDefaults[1] = new StringPiece("=", "").SetNext(new StringPopupPiece(InfoList.ZoneVariables, 0)); //Variable / ID
			zoneSelectionPartsDefaults[2] = new StringPiece("=", "").SetNext(new AndOrPopup().SetPrevious(new StringPopupPiece(InfoList.ZoneTags, 0))); //Zone Tag

			commandDefaults[1] = new StringPiece(" ", "(").SetNext(new CardSelectionPieceList("[ Card(s) ", "c("), new StringPiece("]  [", ","), new StringPiece("Set Tag ", ""), new StringPopupPiece(InfoList.CardTags, 0), new StringPiece("]", ")"));
			commandDefaults[5] = new StringPiece("", "(").SetNext(new CardSelectionPieceList("[ Card(s) ", "c("), new StringPiece("] move to", ","), new ZoneSelectionPieceList(), new StringPiece("]", ""), new StringPopupPiece(InfoList.MoveCardPositionOptions, InfoList.MoveCardPositionOptionsCodified, 0), new StringPopupPiece(InfoList.MoveCardRevealedOptions, InfoList.MoveCardRevealedOptionsCodified, 0), new StringPiece("", ")")); //TODO Grid position
			commandDefaults[6] = new StringPiece(" ", "(").SetNext(new CardSelectionPieceList("[ Card(s) ", "c("), new StringPiece("]  [", ","), new StringPiece("Remove Tag =", ""), new StringPopupPiece(InfoList.CardTags, 0), new StringPiece("]", ")"));
			commandDefaults[7] = new StringPiece("", "(").SetNext(new StringPopupPiece(InfoList.MatchMessages, 0), new StringPiece("", ")"));
			commandDefaults[8] = new StringPiece("", "(").SetNext(new CardSelectionPieceList("[ Card(s) ", ","), new StringPiece("]  [ Field", ","), new StringPopupPiece(InfoList.CardFields, 0), new StringPiece("]  [ Value", ","), new EnterValuePiece(/*TODO value helpers*/), new StringPiece("]", ")"));
			commandDefaults[9] = new StringPiece("[ Variable ", "(").SetNext(new StringPopupPiece(InfoList.MatchVariables, 0), new StringPiece("]  [ Value", ","), new EnterValuePiece(/*TODO value helpers*/), new StringPiece("]", ")"));
			commandDefaults[10] = new StringPiece("", "(").SetNext(new ZoneSelectionPieceList(), new StringPiece("]", ")"));
			commandDefaults[11] = new StringPiece("", "(").SetNext(new SubphaseLoopPiece(), new StringPiece("   ", ")"));
			commandDefaults[12] = new StringPiece("", "(").SetNext(new StringPopupPiece(InfoList.MatchUIAction, 0), new StringPiece("", ")")); 
			commandDefaults[13] = new StringPiece("", "(").SetNext(new CardSelectionPieceList("[ Card(s) ", "c("), new StringPiece("]", ")"));
			commandDefaults[14] = new StringPiece("", "(").SetNext(new ZoneSelectionPieceList(), new StringPiece("]", ")"));
		}
		CardGameData _contextGame;
		internal CardGameData contextGame
		{
			get { return _contextGame; }
			set
			{
				_contextGame = value;
				if (value == null)
					return;
				lists[InfoList.ZoneTags] = GetZoneTags(value);
				GUIContent[] tempCardTags, tempCardFields, tempCardRules;
				GetCardInfoLists(value, out tempCardTags, out tempCardFields, out tempCardRules);
				lists[InfoList.CardTags] = tempCardTags;
				lists[InfoList.CardFields] = tempCardFields;
				lists[InfoList.CardRules] = tempCardRules;
				lists[InfoList.MatchVariables] = GetGameVariableNames(value);
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
		internal static GUIContent[] commandLabels { get { return (GUIContent[])instance.lists[InfoList.CommandLabels]; } }
		internal static string[] commandLabelsCodified { get { return (string[])instance.lists[InfoList.CommandLabelsCodified]; } }
		internal static StringPiece[] commandSignatures { get { return (StringPiece[])instance.lists[InfoList.CommandDefaults]; } }
		internal static GUIContent[] zoneSelectionParts { get { return (GUIContent[])instance.lists[InfoList.ZoneSelectionParts]; } }
		internal static StringPiece[] zoneSelectionPartsDefaults { get { return (StringPiece[])instance.lists[InfoList.ZoneSelectionPartsDefaults]; } }
		internal static string[] zoneSelectionPartsCodified { get { return (string[])instance.lists[InfoList.ZoneSelectionPartsCodified]; } }
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
		void GetCardInfoLists (CardGameData game, out GUIContent[] tagsContents, out GUIContent[] fieldContents, out GUIContent[] rulesContents)
		{
			List<string> cardTagList, cardFieldList, cardRulesList;
			StringUtility.ExtractCardInfoLists(game, out cardTagList, out cardFieldList, out cardRulesList);
			cardTagList.Insert(0, blankEntryString);
			cardTagList.Add(newEntryString);
			if (cardFieldList.Count == 0)
				cardFieldList.Add(blankEntryString);
			if (cardRulesList.Count == 0)
				cardRulesList.Add(blankEntryString);
			tagsContents = new GUIContent[cardTagList.Count];
			for (int i = 0; i < cardTagList.Count; i++)
			{
				tagsContents[i] = new GUIContent(cardTagList[i]);
			}
			fieldContents = new GUIContent[cardFieldList.Count];
			for (int i = 0; i < cardFieldList.Count; i++)
			{
				fieldContents[i] = new GUIContent(cardFieldList[i]);
			}
			rulesContents = new GUIContent[cardRulesList.Count];
			for (int i = 0; i < cardRulesList.Count; i++)
			{
				rulesContents[i] = new GUIContent(cardRulesList[i]);
			}
		}
		GUIContent[] GetGameVariableNames (CardGameData game)
		{
			List<GUIContent> result = new List<GUIContent>();
			result.Add(new GUIContent(blankEntryString));
			if (game.gameVariableNames != null)
			{
				for (int i = 0; i < game.gameVariableNames.Count; i++)
				{
					result.Add(new GUIContent(game.gameVariableNames[i]));
				}
			}
			if (game.rulesets != null)
			{
				for (int i = 0; i < game.rulesets.Count; i++)
				{
					if (game.rulesets[i].rulesetVariableNames != null)
					{
						for (int j = 0; j < game.rulesets[i].rulesetVariableNames.Count; j++)
						{
							result.Add(new GUIContent(game.rulesets[i].rulesetVariableNames[j]));
						}
					}
				}
			}
			return result.ToArray();
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
