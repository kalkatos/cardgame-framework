using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CardGameFramework
{
	public class StringUtility
	{
		static StringBuilder sb = new StringBuilder();
		public static string[] comparisonOperators = new string[] { "!=", ">=", "<=", "=", ">", "<" };
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
					if (counter == 0)
						return i;
				}
			}
			return -1;
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
				//TODO ADD zone tags from the game itself
			}
			return zoneTags;
		}
		public static void ExtractCardInfoLists (CardGameData gameData, out List<string> tags, out List<string> fields, out List<string> rules)
		{
			tags = new List<string>();
			fields = new List<string>();
			rules = new List<string>();
			if (gameData != null && gameData.cardsets != null)
			{
				for (int i = 0; i < gameData.cardsets.Count; i++)
				{
					Cardset cardset = gameData.cardsets[i];
					if (cardset != null)
					{
						for (int j = 0; j < cardset.cardFieldDefinitions.Count; j++)
						{
							string fieldName = cardset.cardFieldDefinitions[j].fieldName;
							if (!fields.Contains(fieldName))
								fields.Add(fieldName);
						}
						for (int j = 0; j < cardset.cardsData.Count; j++)
						{
							CardData cardData = cardset.cardsData[j];
							AddUnique(tags, cardData.tags.Split(','));
							if (cardData.cardRules != null)
							{
								for (int k = 0; k < cardData.cardRules.Count; k++)
								{
									string ruleID = cardData.cardRules[k].ruleID;
									if (!rules.Contains(ruleID))
										rules.Add(ruleID);
								}
							}
						}
					}
				}
			}
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
	}
}