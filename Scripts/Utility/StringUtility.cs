﻿using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CardgameCore
{
	public class StringUtility
	{
		static StringBuilder sb = new StringBuilder();
		public static string[] comparisonOperators = new string[] { ">=", "<=", "!=", "=", ">", "<" };
		public static string[] logicOperators = new string[] { "&", "|", "!" };
		public static string[] mathOperators = new string[] { "+", "-", "*", "/", "%", "^" };

		public static string[] ArgumentsBreakdown (string clause, int commaLevel = 1)
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
						if (parCounter == commaLevel)
						{
							sub = clause.Substring(start, i - start);
							start = i + 1;
							if (!string.IsNullOrEmpty(sub))
								result.Add(sub);
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
						break;
				}
			}
			return result.ToArray();
		}
		public static int GetStringParenthesisLevel (string s, string value)
		{
			int index = s.IndexOf(value);
			if (index >= 0)
			{
				int parCounter = 0;
				for (int i = 0; i < index; i++)
				{
					if (s[i] == '(')
						parCounter++;
					else if (s[i] == ')')
						parCounter--;
				}
				return parCounter;
			}
			return -1;
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
		public static string GetMainComparisonOperator (string value)
		{
			int parCounter = 0;
			for (int i = 0; i < value.Length; i++)
			{
				char c = value[i];
				if (c == '(')
					parCounter++;
				else if (c == ')')
					parCounter--;
				else if (parCounter == 0)
				{
					if (c == '=' || c == '>' || c == '<')
					{
						if (value[i + 1] == '=')
							return value.Substring(i, 2);
						return value.Substring(i, 1);
					}
					else if (c == '!' && value[i + 1] == '=')
						return value.Substring(i, 2);
				}
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
					if ((clause[start] == '(' && counter == 0) || (clause[start] != '(' && counter == -1))
						return i;
				}
			}
			return -1;
		}
		public static List<string> ExtractZoneTags (Game gameData)
		{
			List<string> zoneTags = new List<string>();
			//ADD zone tags from the open scene
			Zone[] zones = UnityEngine.Object.FindObjectsOfType<Zone>();
			if (zones != null)
				for (int i = 0; i < zones.Length; i++)
					AddUnique(zoneTags, zones[i].tags);
			if (gameData != null)
			{
				//TODO ADD zone tags from the game itself
			}
			return zoneTags;
		}
		static void AddUnique (List<string> list, List<string> names)
		{
			for (int i = 0; i < names.Count; i++)
			{
				string newName = names[i];
				if (!list.Contains(newName))
					list.Add(newName);
			}
		}
		public static string[] GetSplitStringArray (string str)
		{
			str = str.Replace("&", "§&§");
			str = str.Replace("|", "§|§");
			//str = str.Replace("(", "§(§");
			//str = str.Replace(")", "§)§");
			//str = str.Replace(",", "§,§");
			str = str.Replace(";", "§;§");
			//str = str.Replace("+", "§+§");
			//str = str.Replace("-", "§-§");
			//str = str.Replace("*", "§*§");
			//str = str.Replace("/", "§/§");
			//str = str.Replace("%", "§%§");
			//str = str.Replace("^", "§^§");
			while (str.Contains("))"))
				str = str.Replace("))", ")§)");
			str = str.Replace("=", "§=§");
			str = str.Replace("!", "§!§");
			str = str.Replace(">", "§>§");
			str = str.Replace("<", "§<§");
			str = str.Replace("§!§§=§", "§!=§");
			str = str.Replace("§<§§=§", "§<=§");
			str = str.Replace("§>§§=§", "§>=§");
			str = str.Replace("§=§§>§", "§=>§");
			str = str.Replace("§&§(", "§&(§");
			str = str.Replace("§|§(", "§|(§");
			str = str.Replace("§§", "§");
			return str.Split(new char[] { '§' }, System.StringSplitOptions.RemoveEmptyEntries);
		}
		public static string[] GetSplitStringArray (string str, params char[] splitChars)
		{
			int parCounter = 0;
			for (int i = 0; i < str.Length; i++)
			{
				char c = str[i];

				if (c == '(')
					parCounter++;
				else if (c == ')')
					parCounter--;
				else if (parCounter == 0)
				{
					for (int j = 0; j < splitChars.Length; j++)
					{
						if (c == splitChars[j])
						{
							int nextCut = 1;
							if (str[i + 1] == '(')
							{
								nextCut = 2;
								int closing = GetClosingParenthesisIndex(str, i + 1);
								str = str.Insert(closing, "§~");
							}
							if (i + nextCut < str.Length)
								str = str.Insert(i + nextCut, "§");
							str = str.Insert(i, "§");
							i++;
							break;
						}
					}
				}
			}
			str = str.Replace("§~)", "");
			for (int i = 0; i < splitChars.Length; i++)
			{
				for (int j = 0; j < splitChars.Length; j++)
				{
					char c1 = splitChars[i];
					char c2 = splitChars[j];
					str = str.Replace("§" + c1 + "§§" + c2 + "§", "§" + c1 + c2 + "§");
				}
			}
			//str = str.Replace("§!§§=§", "§!=§");
			//str = str.Replace("§<§§=§", "§<=§");
			//str = str.Replace("§>§§=§", "§>=§");
			//str = str.Replace("§=§§>§", "§=>§");
			return str.Split(new char[] { '§' }, System.StringSplitOptions.RemoveEmptyEntries);
		}
		public static string ListComponentSelection (ComponentSelector componentSelector, int maxQty)
		{
			sb.Clear();
			List<CGComponent> components = (List<CGComponent>)componentSelector.Get();
			for (int i = 0; i < components.Count; i++)
			{
				if (i == maxQty)
				{
					sb.Append($" and {components.Count - maxQty} more");
					break;
				}
				if (i > 0)
					sb.Append(", ");
				sb.Append(components[i].ToString());
			}
			return sb.ToString();
		}
		public static string ListZoneSelection (ZoneSelector zoneSelector, int maxQty)
		{
			sb.Clear();
			List<Zone> zones = (List<Zone>)zoneSelector.Get();
			for (int i = 0; i < zones.Count; i++)
			{
				if (i == maxQty)
				{
					sb.Append($" and {zones.Count - maxQty} more");
					break;
				}
				if (i > 0)
					sb.Append(", ");
				sb.Append(zones[i].ToString());
			}
			return sb.ToString();
		}
	}

	public static class Extension
	{
		public static T[] SubArray<T> (this T[] array, int offset)
		{
			int length = array.Length - offset;
			T[] result = new T[length];
			System.Array.Copy(array, offset, result, 0, length);
			return result;
		}
	}
}