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
			return s.Replace(" ", "").Replace(System.Environment.NewLine, "").Replace("\n", "").Replace("\n\r", "").Replace("\\n", "").Replace("\\n\\r", "").Replace("\t", "");
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

		public static string PrintStringList (List<string> str, bool inBrackets = true)
		{
			sb.Clear();
			for (int i = 0; i < str.Count; i++)
			{
				if (inBrackets) sb.Append(i + "{ ");
				sb.Append(str[i]);
				if (inBrackets) sb.Append(" }  ");
			}
			return sb.ToString();
		}

		public static string Concatenate (string[] strArray, string interStr)
		{
			sb.Clear();
			for (int i = 0; i < strArray.Length; i++)
			{
				if (string.IsNullOrEmpty(strArray[i]))
					continue;
				if (i > 0)
					sb.Append(interStr);
				sb.Append(strArray[i]);
			}
			return sb.ToString();
		}

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

		public static string[] GetSplitStringArray(string str)
		{
			str = str.Replace("&", "§&§");
			str = str.Replace("|", "§|§");
			str = str.Replace("(", "§(§");
			str = str.Replace(")", "§)§");
			str = str.Replace(",", "§,§");
			str = str.Replace(";", "§;§");
			str = str.Replace("+", "§+§");
			str = str.Replace("-", "§-§");
			str = str.Replace("*", "§*§");
			str = str.Replace("/", "§/§");
			str = str.Replace("%", "§%§");
			str = str.Replace("^", "§^§");
			str = str.Replace("=", "§=§");
			str = str.Replace("!§=§", "§!=§");
			str = str.Replace(">", "§>§");
			str = str.Replace("<", "§<§");
			str = str.Replace("§<§§=§", "§<=§");
			str = str.Replace("§>§§=§", "§>=§");
			str = str.Replace("§=§§>§", "§=>§");
			str = str.Replace("§§", "§");
			return str.Split(new char[] { '§' }, System.StringSplitOptions.RemoveEmptyEntries);
		}

		public static List<string> ExtractZoneTags (CardGameData gameData)
		{
			List<string> zoneTags = new List<string>();
			//ADD zone tags from the open scene
			Zone[] zones = UnityEngine.Object.FindObjectsOfType<Zone>();
			if (zones != null)
			{
				for (int i = 0; i < zones.Length; i++)
				{
					AddUnique(zoneTags, zones[i].zoneTags.Split(','));
				}
			}
			if (gameData != null)
			{
				//ADD zone tags from the game itself
			}
			return zoneTags;
		}

		static void AddUnique (List<string> list, string[] names)
		{
			for (int i = 0; i < names.Length; i++)
			{
				string newName = names[i];
				if (!list.Contains(newName))
					list.Add(newName);
			}
		}
	}
}