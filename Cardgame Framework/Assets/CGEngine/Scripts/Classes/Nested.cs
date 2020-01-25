using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CardGameFramework
{
	//variable>number
	//number<=numberGet
	//string=string
	//contextObj=>selection
	//

	public class NestedConditions : NestedStrings
	{
		Getter left;
		Getter right;
	}

	public class NestedStrings : NestedBooleans
	{
		public string myString = "§";

		public NestedStrings () { }

		public NestedStrings (string buildingStr, bool hasOperator = false)
		{
			Build(buildingStr, hasOperator);
		}

		public virtual void Build (string clause, bool hasOperator = false)
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
							currentString.sub = new NestedStrings(clause.Substring(i + 1, closingPar - i - 1), hasOperator);
						}
						i = closingPar;
						strEnd = closingPar;
						if (currentString.sub != null && (i == clause.Length - 1 || clause[i + 1] == '&' || clause[i + 1] == '|'))
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
						NestedStrings newStrings = new NestedStrings();
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

		protected virtual void PrepareEvaluation (string[] compareToStrings)
		{
			if (sub != null)
			{
				((NestedStrings)sub).PrepareEvaluation(compareToStrings);
			}
			else
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
			}
			if (and != null) ((NestedStrings)and).PrepareEvaluation(compareToStrings);
			if (or != null) ((NestedStrings)or).PrepareEvaluation(compareToStrings);
		}

		public override bool Evaluate (object addObj)
		{
			PrepareEvaluation((string[])addObj);
			return base.Evaluate(addObj);
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

		public virtual bool Evaluate (object addObj = null)
		{
			if (sub != null)
				myBoolean = sub.Evaluate(addObj);

			if (and != null)
				return myBoolean & and.Evaluate(addObj);
			else if (or != null)
				return myBoolean | or.Evaluate(addObj);
			else
				return myBoolean;
		}
	}

}

