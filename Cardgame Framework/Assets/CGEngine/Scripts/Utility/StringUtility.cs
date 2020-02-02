using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CardGameFramework
{
	public class StringUtility
	{
		static StringBuilder sb = new StringBuilder();
		public static string[] comparisonOperators = new string[] { "!=", ">=", "<=", "=>", "=", ">", "<" };
		public static string[] logicOperators = new string[] { "&", "|", "!" };
		public static string[] mathOperators = new string[] { "+", "-", "*", "/", "%", "^" };

		public static string[] ArgumentsBreakdown (string clause, bool ignoreCommas = false)
		{
			clause = GetCleanStringForInstructions(clause);
			List<string> result = new List<string>();
			string sub = "";
			int start = 0;
			int parCounter = 0;
			for (int i = 0; i < clause.Length; i++)
			{
				char c = clause[i];
				switch (c)
				{
					case '(':
						if (parCounter == 0)
						{
							sub = clause.Substring(start, i - start);
							if (!string.IsNullOrEmpty(sub))
								result.Add(sub);
							start = i + 1;
						}
						parCounter++;
						break;
					case ',':
						if (parCounter == 1 && !ignoreCommas)
						{
							sub = clause.Substring(start, i - start);
							if (!string.IsNullOrEmpty(sub))
								result.Add(sub);
							start = i + 1;
						}
						break;
					case ')':
						parCounter--;
						if (parCounter == 0)
						{
							sub = clause.Substring(start, i - start);
							if (!string.IsNullOrEmpty(sub))
								result.Add(sub);
							start = i + 1;
						}
						break;
					default:
						if (i == clause.Length - 1)
						{
							sub = clause.Substring(start, i - start + 1);
							if (!string.IsNullOrEmpty(sub))
								result.Add(sub);
						}
						continue;
				}
			}
			string[] resultArray = result.ToArray();
			return resultArray;
		}

		public static string GetCleanStringForInstructions (string s)
		{
			return s.Replace(" ", "").Replace(System.Environment.NewLine, "").Replace("\n", "").Replace("\n\r", "").Replace("\\n", "").Replace("\\n\\r", "");
		}

		public static string PrintStringArray (string[] str, bool inBrackets = true)
		{
			sb.Clear();
			for (int i = 0; i < str.Length; i++)
			{
				if (inBrackets) sb.Append(i + "{ ");
				sb.Append(str[i]);
				if (inBrackets) sb.Append(" }  ");
			}
			return sb.ToString();
		}

		//public static string BuildMessage (string message, params object[] arguments)
		//{
		//	sb.Clear();
		//	sb.Append("[CGEngine] ");
		//	sb.Append(string.Format(message, arguments));
		//	int start = 0;
		//	for (int i = 0; i <= arguments.Length; i++)
		//	{
		//		if (i == arguments.Length)
		//		{
		//			sb.Append(message.Substring(start));
		//			break;
		//		}

		//		message.;
		//		int firstArgPos = message.IndexOf('@', start);
		//		if (firstArgPos >= 0)
		//		{
		//			sb.Append(message.Substring(start, firstArgPos - start));
		//			sb.Append(arguments[i]);
		//			if (i == arguments.Length - 1)
		//				sb.Append(message.Substring(firstArgPos + 1));
		//			else
		//			{
		//				start = firstArgPos + 1;
		//				if (start == message.Length)
		//					break;
		//			}
		//		}
		//		else
		//		{
		//			sb.Append(message.Substring(start));
		//			for (int j = i; j < arguments.Length; j++)
		//			{
		//				sb.Append(" - ");
		//				sb.Append(arguments[j]);
		//			}
		//			break;
		//		}
		//	}
		//	return sb.ToString();
		//}

		//public static string GetComparisonOperator (string value)
		//{
		//	return GetOperator(value, comparisonOperators);
		//}

		//public static string GetLogicOperator (string value)
		//{
		//	return GetOperator(value, logicOperators);
		//}

		//public static string GetMathOperator (string value)
		//{
		//	return GetOperator(value, mathOperators);
		//}

		public static string GetAnyOperator (string value)
		{
			string op = GetOperator(value, comparisonOperators);
			return op == "" ? GetOperator(value, mathOperators) : op;
		}

		public static string GetOperator (string value, string[] operators)
		{
			for (int i = 0; i < operators.Length; i++)
			{
				if (value.Contains(operators[i]))
					return operators[i];
			}
			return "";
		}

		public static int GetClosingParenthesisIndex (string clause, int start)
		{
			int counter = 0;
			for (int i = start; i < clause.Length; i++)
			{
				if (clause[i] == '(')
					counter++;
				else if (clause[i] == ')')
				{
					counter--;
					if (counter == 0)
						return i;
				}
			}
			return -1;
		}
	}
}