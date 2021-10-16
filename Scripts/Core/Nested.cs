using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardgameCore
{
	
	public delegate bool ValueSetter ();

	[Serializable]
	public class NestedComponentIndexConditions : NestedConditions
	{
		public NestedComponentIndexConditions () : base() { }
		public NestedComponentIndexConditions (string clause) : base(clause) { }

		protected override NestedStrings GetNew ()
		{
			return new NestedComponentIndexConditions();
		}

		protected override NestedBooleans GetNew (string buildingStr, bool hasOperator = true)
		{
			return new NestedComponentIndexConditions(buildingStr);
		}

		protected override void SetMyValue (object argument)
		{
			if (argument == null || argument.GetType() != typeof(CGComponent)) return;
			CGComponent component = (CGComponent)argument;

			if (component.Zone)
				left = new NumberGetter(component.Zone.GetIndexOf(component));
			else
				left = new NumberGetter(-1);

			myBoolean = setterMethod.Invoke();
		}
	}

	// ======================================================================

	[Serializable]
	public class NestedComponentFieldConditions : NestedConditions
	{
		public NestedComponentFieldConditions () : base() { }
		public NestedComponentFieldConditions (string clause) : base(clause) { }

		protected override NestedStrings GetNew ()
		{
			return new NestedComponentFieldConditions();
		}

		protected override NestedBooleans GetNew (string buildingStr, bool hasOperator = true)
		{
			return new NestedComponentFieldConditions(buildingStr);
		}

		protected override void SetMyValue (object argument)
		{
			if (argument == null || argument.GetType() != typeof(CGComponent)) return;
			CGComponent component = (CGComponent)argument;
			if (component.HasField(leftString))
			{
				if (component.GetFieldDataType(leftString) == FieldType.Number)
					left = new NumberGetter(component.GetNumFieldValue(leftString));
				else if (component.GetFieldDataType(leftString) == FieldType.Text)
					((StringGetter)left).value = component.GetTextFieldValue(leftString);
			}
			else
				left = Getter.Build(leftString);
			if (component.HasField(rightString))
			{
				if (component.GetFieldDataType(rightString) == FieldType.Number)
					right = new NumberGetter(component.GetNumFieldValue(rightString));
				else if (component.GetFieldDataType(rightString) == FieldType.Text)
					((StringGetter)right).value = component.GetTextFieldValue(rightString);
			}
			else
				right = Getter.Build(rightString);
			myBoolean = setterMethod.Invoke();
		}
	}

	// ======================================================================

	[Serializable]
	public class NestedConditions : NestedStrings
	{
		protected Getter left;
		protected string leftString;
		protected Getter right;
		protected string rightString;
		protected ValueSetter setterMethod;

		public NestedConditions () : base()
		{
		}

		public NestedConditions (string clause) : base()
		{
			buildingStr = clause;
			Build(buildingStr, true);
			BuildCondition();
		}

		protected void BuildCondition ()
		{
			string op = "";
			int indexOp = -1;
			if (myString.Contains("c(") && (myString.Contains("f:") || myString.Contains("n:")))
			{
				int start = 0;
				while (true)
				{
					op = StringUtility.GetOperator(myString.Substring(start), StringUtility.ComparisonOperators);
					indexOp = myString.IndexOf(op, start);
					start = myString.IndexOf("c(", start);
					if (start < 0 || indexOp < start)
						break;
					else
					{
						int end = StringUtility.GetClosingParenthesisIndex(myString, start + 2);
						start = end + 1;
					} 
				}
			}
			else
			{
				op = StringUtility.GetOperator(myString, StringUtility.ComparisonOperators);
				indexOp = myString.IndexOf(op);
			}
			if (op == "") return;
			leftString = myString.Substring(0, indexOp);
			rightString = myString.Substring(indexOp + op.Length);

			left = Getter.Build(leftString);
			if (leftString == "variable")
				right = new StringGetter(rightString);
			else
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
			if (setterMethod != null)
				myBoolean = setterMethod.Invoke();
		}

		protected bool EqualsSetter ()
		{
			if (right is ComponentSelector)
			{
				ComponentSelector cardSelector = (ComponentSelector)right;
				if (left is MatchVariableGetter)
				{
					string leftValue = (string)left.Get();
					return ComponentSelector.Contains(leftValue, cardSelector);
				}
				else if (left is ComponentSelector)
				{
					return ComponentSelector.Contains((ComponentSelector)left, cardSelector);
				}
			}
			else if (right is ZoneSelector)
			{
				ZoneSelector zoneSelector = (ZoneSelector)right;
				if (left is MatchVariableGetter)
				{
					string leftValue = (string)left.Get();
					return ZoneSelector.Contains(leftValue, zoneSelector);
				}
				else if (left is ZoneSelector)
				{
					return ZoneSelector.Contains((ZoneSelector)left, zoneSelector);
				}
			}
			else if (right is RuleSelector)
			{
				RuleSelector ruleSelector = (RuleSelector)right;
				if (left is MatchVariableGetter)
				{
					string leftValue = (string)left.Get();
					return RuleSelector.Contains(leftValue, ruleSelector);
				}
				else if (left is RuleSelector)
				{
					return RuleSelector.Contains((RuleSelector)left, ruleSelector);
				}
			}
			object l = left.Get(), r = right.Get();
			if (float.TryParse(l.ToString(), out float leftFloat) && float.TryParse(r.ToString(), out float rightFloat))
				return leftFloat == rightFloat;
			return left == right;
		}

		protected bool DifferentThanSetter ()
		{
			//return !EqualsSetter();
			if (right is ComponentSelector)
			{
				ComponentSelector cardSelector = (ComponentSelector)right;
				if (left is MatchVariableGetter)
				{
					string leftValue = (string)left.Get();
					return !ComponentSelector.Contains(leftValue, cardSelector);
				}
				else if (left is ComponentSelector)
				{
					return !ComponentSelector.Contains((ComponentSelector)left, cardSelector);
				}
			}
			else if (right is ZoneSelector)
			{
				ZoneSelector zoneSelector = (ZoneSelector)right;
				if (left is MatchVariableGetter)
				{
					string leftValue = (string)left.Get();
					return !ZoneSelector.Contains(leftValue, zoneSelector);
				}
				else if (left is ZoneSelector)
				{
					return !ZoneSelector.Contains((ZoneSelector)left, zoneSelector);
				}
			}
			else if (right is RuleSelector)
			{
				RuleSelector ruleSelector = (RuleSelector)right;
				if (left is MatchVariableGetter)
				{
					string leftValue = (string)left.Get();
					return !RuleSelector.Contains(leftValue, ruleSelector);
				}
				else if (left is RuleSelector)
				{
					return !RuleSelector.Contains((RuleSelector)left, ruleSelector);
				}
			}
			object l = left.Get(), r = right.Get();
			if (float.TryParse(l.ToString(), out float leftFloat) && float.TryParse(r.ToString(), out float rightFloat))
				return leftFloat != rightFloat;
			return left != right;
		}

		protected bool LessThanSetter ()
		{
			object l = left.Get(), r = right.Get();
			if (float.TryParse(l.ToString(), out float leftFloat) && float.TryParse(r.ToString(), out float rightFloat))
				return leftFloat < rightFloat;
			return false;
		}

		protected bool GreaterThanSetter ()
		{
			object l = left.Get(), r = right.Get();
			if (float.TryParse(l.ToString(), out float leftFloat) && float.TryParse(r.ToString(), out float rightFloat))
				return leftFloat > rightFloat;
			return false;
		}

		protected bool EqualsOrLessThanSetter ()
		{
			object l = left.Get(), r = right.Get();
			if (float.TryParse(l.ToString(), out float leftFloat) && float.TryParse(r.ToString(), out float rightFloat))
				return leftFloat <= rightFloat;
			return false;
		}

		protected bool EqualsOrGreaterThanSetter ()
		{
			object l = left.Get(), r = right.Get();
			if (float.TryParse(l.ToString(), out float leftFloat) && float.TryParse(r.ToString(), out float rightFloat))
				return leftFloat >= rightFloat;
			return false;
		}
	}

	// ======================================================================

	[Serializable]
	public class NestedStrings : NestedBooleans
	{
		public string buildingStr = "";
		[HideInInspector] public string myString = "§";
		[HideInInspector] public Getter varGetter;

		public NestedStrings () { }

		public NestedStrings (string buildingStr, bool hasOperator = false)
		{
			this.buildingStr = buildingStr;
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
			if (string.IsNullOrEmpty(clause))
			{
				myBoolean = true;
				return;
			}

			clause = StringUtility.GetCleanStringForInstructions(clause);

			int strStart = 0;
			int strEnd = clause.Length;
			NestedStrings currentString = this;
			for (int i = 0; i < clause.Length; i++)
			{
				char c = clause[i];
				switch (c)
				{
					case '(':
						int closingPar = StringUtility.GetClosingParenthesisIndex(clause, i);
						if (closingPar == -1) return; //clause is wrong (no ending parenthesis for this)
						string subClause = clause.Substring(i, closingPar - i);
						string op = StringUtility.GetAnyOperator(subClause);
						if (!hasOperator || op != "")
						{
							//if (!(i > 0 && clause[i - 1] == 'c')) //Card selection with field condition
							//if (!subClause.Contains("f:") || StringUtility.GetStringParenthesisLevel(subClause, "f:") > 2) //Card selection with field condition
							if (!subClause.Contains("f:") || StringUtility.GetStringParenthesisLevel(subClause, op) != StringUtility.GetStringParenthesisLevel(subClause, "f:")) //Card selection with field condition or zone selection by card
								currentString.sub = GetNew(clause.Substring(i + 1, closingPar - i - 1), hasOperator);
						}
						i = closingPar;
						strEnd = closingPar;
						if (i == clause.Length - 1 || clause[i + 1] == '&' || clause[i + 1] == '|')
						{
							currentString.myString = clause.Substring(strStart, strEnd - strStart + 1);
							if (Match.HasVariable(currentString.myString))
								currentString.varGetter = Getter.Build(currentString.myString);
						}
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
						{
							currentString.myString = clause.Substring(strStart, strEnd - strStart + 1);
							if (Match.HasVariable(currentString.myString))
								currentString.varGetter = Getter.Build(currentString.myString);
						}
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
				if (varGetter != null)
					myString = (string)varGetter.Get();
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
				if ((!myBoolean && or == null) || (myBoolean && and == null)) return;
			}
			if (and != null) ((NestedStrings)and).PrepareEvaluation(additionalObject);
			if (or != null) ((NestedStrings)or).PrepareEvaluation(additionalObject);
		}

		public override bool Evaluate (object additionalObject = null)
		{
			PrepareEvaluation(additionalObject);
			return base.Evaluate(additionalObject);
		}
		
	}

	// ======================================================================

	[Serializable]
	public class NestedBooleans
	{
		[HideInInspector] public NestedBooleans sub;
		[HideInInspector] public NestedBooleans and;
		[HideInInspector] public NestedBooleans or;
		private bool _myBoolean;
		public bool myBoolean { get { return not ? !_myBoolean : _myBoolean; } set { _myBoolean = value; } }
		[HideInInspector] public bool not;

		public NestedBooleans () { }
		public NestedBooleans (bool value) { myBoolean = value; }

		public virtual bool Evaluate (object additionalObject = null)
		{
			if (sub != null)
				myBoolean = sub.Evaluate(additionalObject);

			bool result = false;

			if (and != null && myBoolean)
				result = myBoolean & and.Evaluate(additionalObject);
			else if (or != null && !myBoolean)
				result = myBoolean | or.Evaluate(additionalObject);
			else
				result = myBoolean;
			//if (Match.DebugLog)
			//	Debug.Log($"Evaluating condition {ToString()}");
			return result;
		}

		public override string ToString ()
		{
			return ToString(true);
		}

		public string ToString (bool evaluate = true, char separationChar = ' ')
		{
			StringBuilder sb = new StringBuilder();
			if (not)
				sb.Append("!");
			if (sub != null)
			{
				sb.Append("( ");
				sb.Append(sub.ToString(evaluate, separationChar));
				sb.Append(" )");
			}
			else if (this is NestedStrings)
			{
				sb.Append(((NestedStrings)this).myString);
				if (evaluate)
				{
					sb.Append(":");
					sb.Append(myBoolean);
				}
			}

			if (and != null)
			{
				sb.Append(separationChar);
				sb.Append("&");
				sb.Append(separationChar);
				sb.Append(and.ToString(evaluate, separationChar));
			}
			else if (or != null)
			{
				sb.Append(separationChar);
				sb.Append("|");
				sb.Append(separationChar);
				sb.Append(or.ToString(evaluate, separationChar));
			}
			return sb.ToString();
		}
	}
}

