using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CardGameFramework
{
	public class StringUtility
	{
		static StringBuilder sb = new StringBuilder();

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

		public static string BuildMessage (string message, params object[] arguments)
		{
			sb.Clear();
			int start = 0;
			for (int i = 0; i < arguments.Length; i++)
			{
				int firstArgPos = message.IndexOf('@', start);
				if (firstArgPos >= 0)
				{
					sb.Append(message.Substring(start, firstArgPos - start));
					sb.Append(arguments[i]);
					if (i == arguments.Length - 1)
						sb.Append(message.Substring(firstArgPos + 1));
					else
					{
						start = firstArgPos + 1;
						if (start == message.Length)
							break;
					}
				}
				else
				{
					sb.Append(message.Substring(start));
				}
			}
			return sb.ToString();
		}

		public static string GetComparisonOperator (string value)
		{
			if (value.Contains("!="))
				return "!=";
			if (value.Contains("<="))
				return "<=";
			if (value.Contains(">="))
				return ">=";
			if (value.Contains("=>")) // x => selection  --  if selection contains x
				return "=>";
			if (value.Contains("="))
				return "=";
			if (value.Contains("<"))
				return "<";
			if (value.Contains(">"))
				return ">";
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