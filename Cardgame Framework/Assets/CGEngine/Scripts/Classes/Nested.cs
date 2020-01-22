﻿using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CardGameFramework
{

	public class NestedStrings : NestedBooleans
	{
		public string myString = "§";

		public NestedStrings () { }

		public NestedStrings (string buildingStr)
		{
			Build(buildingStr);
		}

		public void Build (string clause)
		{
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
							currentString.myString = clause.Substring(strStart, strEnd - strStart);
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
			if (subAnd != null)
			{
				sb.Append("&(");
				sb.Append(subAnd.ToString());
				sb.Append(")");
			}
			else if (subOr != null)
			{
				sb.Append("|(");
				sb.Append(subOr.ToString());
				sb.Append(")");
			}

			if (and != null)
			{
				sb.Append("&");
				sb.Append(and.ToString());
			}
			else if (or != null)
			{
				sb.Append("|");
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