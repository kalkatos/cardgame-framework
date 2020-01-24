using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CardGameFramework
{



	public class NestedStrings : NestedBooleans
	{
		public string myString = "§";

		public NestedStrings () { }

		public NestedStrings (string buildingStr, bool isComparison = false)
		{
			Build(buildingStr, isComparison);
		}

		public void Build (string clause, bool isComparison = false)
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
						i = closingPar;
						strEnd = closingPar;
						break;
					case '&':
					case '|':
						if (i == 0 || i == clause.Length - 1) return; //clause is wrong (may not start or end with an operator)
						bool isAnd = c == '&';
						currentString.myString = clause.Substring(strStart, strEnd - strStart + 1);
						if (clause[i + 1] == '(')
						{
							closingPar = StringUtility.GetClosingParenthesisIndex(clause, i + 1);
							if (closingPar == -1) return; //clause is wrong (no ending parenthesis for this)
							if (!isComparison || StringUtility.GetComparisonOperator(clause.Substring(i + 1, closingPar - i - 1)) != "")
							{
								NestedStrings newSubStrings = new NestedStrings(clause.Substring(i + 2, closingPar - i - 2), isComparison);
								currentString.subAnd = isAnd ? newSubStrings : null;
								currentString.subOr = isAnd ? null : newSubStrings;
							}
							i = closingPar;
						}
						else
						{
							NestedStrings newStrings = new NestedStrings();
							currentString.and = isAnd ? newStrings : null;
							currentString.or = isAnd ? null : newStrings;
							currentString = newStrings;
							strStart = i + 1;
						}
						break;
					default:
						strEnd = i;
						if (i == clause.Length - 1)
							currentString.myString = clause.Substring(strStart, strEnd - strStart + 1);
						break;
				}
			}



			/*
			int strStart = 0;
			int strEnd = clause.Length;
			NestedStrings currentString = this;
			NestedStrings rootString = null;
			for (int i = 0; i < clause.Length; i++)
			{
				char c = clause[i];
				switch (c)
				{
					case '(':
						if (!isComparison)
						{
							strStart = i + 1;
							break;
						}
						int parEnd = StringUtility.GetClosingParenthesisIndex(clause, i);
						if (parEnd == -1) return; //clause is wrong (wrong number of parenthesis)
						if (StringUtility.GetComparisonOperator(clause.Substring(i, parEnd - i + 1)) != "")
							strStart = i + 1;
						break;
					case ')':
						if (i == 0) return;  //clause is wrong (may not start with closing parenthesis)
						currentString.and = null;
						currentString.or = null;
						currentString.subAnd = null;
						currentString.subOr = null;
						strEnd = clause[i - 1] == ')' ? strEnd : i;
						currentString.myString = clause.Substring(strStart, strEnd - strStart);
						break;
					case '&':
					case '|':
						if (i == 0 || i == clause.Length - 1) return; //clause is wrong (may not start or end with an operator)
						bool itsAnd = c == '&';
						bool closingParenthesis = clause[i - 1] == ')';

						if (closingParenthesis)
						{
							if (rootString != null)
								currentString = rootString;
						}
						else
						{
							strEnd = i;
							string sub = clause.Substring(strStart, strEnd - strStart);
							if (isComparison && StringUtility.GetComparisonOperator(sub) == "")
							{
								continue;
							}
							else
							{ 
								currentString.myString = sub;
							}
						}

						NestedStrings newString = new NestedStrings();
						if (clause[i + 1] == '(')
						{
							rootString = currentString;
							currentString.subAnd = itsAnd ? newString : null;
							currentString.subOr = itsAnd ? null : newString;
							currentString = newString;
						}
						else
						{
							currentString.and = itsAnd ? newString : null;
							currentString.or = itsAnd ? null : newString;
							currentString = newString;
						}

						strStart = i + 1;
						break;
					default:
						if (i == clause.Length - 1)
						{
							currentString.and = null;
							currentString.or = null;
							currentString.subAnd = null;
							currentString.subOr = null;
							strEnd = (i > 0 && clause[i - 1] != ')') ? clause.Length : strEnd;
							currentString.myString = clause.Substring(strStart, strEnd - strStart);
						}
						break;
				}
			}
			*/
		}

		public void PrepareEvaluation (string[] compareToStrings)
		{
			myBoolean = false;
			for (int i = 0; i < compareToStrings.Length; i++)
			{
				if (compareToStrings[i] == myString)
				{
					myBoolean = true;
					break;
				}
			}
			if (subAnd != null) ((NestedStrings)subAnd).PrepareEvaluation(compareToStrings);
			if (subOr != null) ((NestedStrings)subOr).PrepareEvaluation(compareToStrings);
			if (and != null) ((NestedStrings)and).PrepareEvaluation(compareToStrings);
			if (or != null) ((NestedStrings)or).PrepareEvaluation(compareToStrings);
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(myString);
			sb.Append(":");
			sb.Append(myBoolean);
			if (subAnd != null)
			{
				sb.Append(" & ( ");
				sb.Append(subAnd.ToString());
				sb.Append(" ) ");
			}
			else if (subOr != null)
			{
				sb.Append(" | ( ");
				sb.Append(subOr.ToString());
				sb.Append(" ) ");
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
		public NestedBooleans subAnd;
		public NestedBooleans subOr;
		public NestedBooleans and;
		public NestedBooleans or;
		public bool myBoolean;

		public virtual bool Evaluate ()
		{
			if (subAnd != null)
				myBoolean = myBoolean & subAnd.Evaluate();
			else if (subOr != null)
				myBoolean = myBoolean | subOr.Evaluate();

			if (and != null)
				return myBoolean & and.Evaluate();
			else if (or != null)
				return myBoolean | or.Evaluate();
			else
				return myBoolean;
		}
	}

}

