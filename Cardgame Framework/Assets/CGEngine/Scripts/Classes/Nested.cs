﻿using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardGameFramework
{
	
	public delegate bool ValueSetter ();

	public class NestedCardFieldConditions : NestedConditions
	{
		public NestedCardFieldConditions () : base() { }
		public NestedCardFieldConditions (string clause) : base(clause) { }

		protected override NestedStrings GetNew ()
		{
			return new NestedCardFieldConditions();
		}

		protected override NestedBooleans GetNew (string buildingStr, bool hasOperator = true)
		{
			return new NestedCardFieldConditions(buildingStr);
		}

		protected override void SetMyValue (object argument)
		{
			if (argument == null || argument.GetType() != typeof(Card)) return;
			Card card = (Card)argument;
			if (card.HasField(leftString))
			{
				if (card.GetFieldDataType(leftString) == CardFieldDataType.Number)
					left = new NumberGetter(card.GetNumFieldValue(leftString));
				else if (card.GetFieldDataType(leftString) == CardFieldDataType.Text)
					((StringGetter)left).value = card.GetTextFieldValue(leftString);
			}
			if (card.HasField(rightString))
			{
				if (card.GetFieldDataType(rightString) == CardFieldDataType.Number)
					right = new NumberGetter(card.GetNumFieldValue(rightString));
				else if (card.GetFieldDataType(rightString) == CardFieldDataType.Text)
					((StringGetter)right).value = card.GetTextFieldValue(rightString);
			}
			myBoolean = setterMethod.Invoke();
		}
	}

	public class NestedConditions : NestedStrings
	{
		protected Getter left;
		protected string leftString;
		protected Getter right;
		protected string rightString;
		protected ValueSetter setterMethod;

		protected NestedConditions (Getter left, Getter right) { }

		public NestedConditions () : base() { }

		public NestedConditions (string clause)
		{
			Build(clause, true);

			BuildCondition();
		}

		protected void BuildCondition ()
		{
			string op = StringUtility.GetOperator(myString, StringUtility.comparisonOperators);
			if (op == "") return;
			int indexOp = myString.IndexOf(op);
			leftString = myString.Substring(0, indexOp);
			rightString = myString.Substring(indexOp + op.Length);
			left = Getter.Build(leftString);
			right = Getter.Build(rightString);

			switch (op)
			{
				case "=":
					setterMethod = EqualsSetter;
					break;
				case "!=":
					setterMethod = DifferentThanSetter;
					break;
				case ">=":
					setterMethod = EqualsOrGreaterThanSetter;
					break;
				case "<=":
					setterMethod = EqualsOrLessThanSetter;
					break;
				case ">":
					setterMethod = GreaterThanSetter;
					break;
				case "<":
					setterMethod = LessThanSetter;
					break;
				case "=>":
					setterMethod = ContainsSetter;
					break;
				default:
					break;
			}

			if (sub != null) ((NestedConditions)sub).BuildCondition();
			if (and != null) ((NestedConditions)and).BuildCondition();
			if (or != null) ((NestedConditions)or).BuildCondition();
		}

		protected override NestedStrings GetNew ()
		{
			return new NestedConditions();
		}

		protected override NestedBooleans GetNew (string buildingStr, bool hasOperator = true)
		{
			return new NestedConditions(buildingStr);
		}

		protected override void SetMyValue (object argument = null)
		{
			myBoolean = setterMethod.Invoke();
		}

		protected bool EqualsSetter ()
		{
			return left == right;
		}

		protected bool DifferentThanSetter ()
		{
			return left != right;
		}

		protected bool LessThanSetter ()
		{
			object l = left.Get(), r = right.Get();
			if (l is float && r is float)
				return (float)left.Get() < (float)right.Get();
			return false;
		}

		protected bool GreaterThanSetter ()
		{
			object l = left.Get(), r = right.Get();
			if (l is float && r is float)
				return (float)left.Get() > (float)right.Get();
			return false;
		}

		protected bool EqualsOrLessThanSetter ()
		{
			object l = left.Get(), r = right.Get();
			if (l is float && r is float)
				return (float)left.Get() <= (float)right.Get();
			return false;
		}

		protected bool EqualsOrGreaterThanSetter ()
		{
			object l = left.Get(), r = right.Get();
			if (l is float && r is float)
				return (float)left.Get() >= (float)right.Get();
			return false;
		}
		
		protected bool ContainsSetter ()
		{
			Debug.Log("DEBUG  > > > > >  " + left.GetType() + " , " + right.GetType());
			object leftValue = left.Get(), rightValue = right.Get();
			if (rightValue is CardSelector)
			{
				CardSelector cardSelector = (CardSelector)rightValue;
				//if (Match.Current.HasVariable(leftString))
				//	leftValue = Match.Current.GetVariable(leftString);
				Debug.Log(leftValue);
				if (leftValue is Card)
				{
					return cardSelector.IsAMatch((Card)leftValue);
				}
				if (leftValue is CardSelector)
				{
					return Selector<Card>.Contains((CardSelector)left, cardSelector);
				}
			}
			else if (rightValue is Selector<Zone>)
			{
				ZoneSelector zoneSelector = (ZoneSelector)rightValue;
				if (leftValue is Zone)
				{
					return zoneSelector.IsAMatch((Zone)leftValue);
				}
				if (leftValue is ZoneSelector)
				{
					return Selector<Zone>.Contains((ZoneSelector)left, zoneSelector);
				}
			}
			Debug.LogWarning("DEBUG  !  !  !  !  Found no correspondence between "+ left + " and " + right);
			return false;
		}
	}


	public class NestedStrings : NestedBooleans
	{
		public string myString = "§";

		public NestedStrings () { }

		public NestedStrings (string buildingStr, bool hasOperator = false)
		{
			Build(buildingStr, hasOperator);
		}

		protected virtual NestedStrings GetNew ()
		{
			return new NestedStrings();
		}

		protected virtual NestedBooleans GetNew (string buildingStr, bool hasOperator = false)
		{
			return new NestedStrings(buildingStr);
		}

		protected virtual void Build (string clause, bool hasOperator = false)
		{
			clause = StringUtility.GetCleanStringForInstructions(clause);

			int strStart = 0;
			int strEnd = clause.Length;
			int closingPar = -1;
			NestedStrings currentString = this;
			for (int i = 0; i < clause.Length; i++)
			{
				char c = clause[i];
				switch (c)
				{
					case '(':
						closingPar = StringUtility.GetClosingParenthesisIndex(clause, i);
						if (closingPar == -1) return; //clause is wrong (no ending parenthesis for this)
						if (!hasOperator || StringUtility.GetAnyOperator(clause.Substring(i, closingPar - i)) != "")
						{
							currentString.sub = GetNew(clause.Substring(i + 1, closingPar - i - 1), hasOperator);
						}
						i = closingPar;
						strEnd = closingPar;
						if (i == clause.Length - 1 || (currentString.sub != null && (clause[i + 1] == '&' || clause[i + 1] == '|')))
							currentString.myString = clause.Substring(strStart, strEnd - strStart + 1);
						break;
					case '!':
						if (i == clause.Length - 1) return; //clause is wrong (may not end with ! operator)
						if (clause[i + 1] != '=')
						{
							currentString.not = true;
							strStart = i + 1;
						}
						break;
					case '&':
					case '|':
						if (i == 0 || i == clause.Length - 1) return; //clause is wrong (may not start or end with an operator)
						bool isAnd = c == '&';
						NestedStrings newStrings = GetNew();
						currentString.and = isAnd ? newStrings : null;
						currentString.or = isAnd ? null : newStrings;
						currentString = newStrings;
						strStart = i + 1;
						break;
					default:
						strEnd = i;
						if (i == clause.Length - 1 || clause[i + 1] == '&' || clause[i + 1] == '|')
							currentString.myString = clause.Substring(strStart, strEnd - strStart + 1);
						break;
				}
			}
		}

		protected virtual void SetMyValue (object argument = null)
		{
			if (argument == null) return;
			myBoolean = false;
			string[] compareToStrings = (string[])argument;
			for (int i = 0; i < compareToStrings.Length; i++)
			{
				if (compareToStrings[i] == myString)
				{
					myBoolean = true;
					break;
				}
			}
		}

		protected void PrepareEvaluation (object additionalObject)
		{
			if (sub != null)
			{
				((NestedStrings)sub).PrepareEvaluation(additionalObject);
			}
			else
			{
				SetMyValue(additionalObject);
			}
			if (and != null) ((NestedStrings)and).PrepareEvaluation(additionalObject);
			if (or != null) ((NestedStrings)or).PrepareEvaluation(additionalObject);
		}

		public override bool Evaluate (object additionalObject = null)
		{
			PrepareEvaluation(additionalObject);
			return base.Evaluate(additionalObject);
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder();
			if (not)
				sb.Append("!");
			if (sub != null)
			{
				sb.Append("( ");
				sb.Append(sub.ToString());
				sb.Append(" )");
			}
			else
			{
				sb.Append(myString);
				sb.Append(":");
				sb.Append(myBoolean);
			}

			if (and != null)
			{
				sb.Append(" & ");
				sb.Append(and.ToString());
			}
			else if (or != null)
			{
				sb.Append(" | ");
				sb.Append(or.ToString());
			}
			return sb.ToString();
		}
	}

	public class NestedBooleans
	{
		public NestedBooleans sub;
		public NestedBooleans and;
		public NestedBooleans or;
		bool _myBoolean;
		public bool myBoolean { get { return not ? !_myBoolean : _myBoolean; } set { _myBoolean = value; } }
		public bool not;

		public virtual bool Evaluate (object additionalObject = null)
		{
			if (sub != null)
				myBoolean = sub.Evaluate(additionalObject);

			if (and != null)
				return myBoolean & and.Evaluate(additionalObject);
			else if (or != null)
				return myBoolean | or.Evaluate(additionalObject);
			else
				return myBoolean;
		}
	}

}

