//Using a decompiled version of Unity's Editor ExpressionEvaluator which doesn't compile for WebGL
//Returns only float values
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace CardgameCore
{
	internal class ExpressionEvaluator
	{
		private static readonly Operator[] s_Operators = new Operator[7] { new Operator('-', 2, 2, Associativity.Left), new Operator('+', 2, 2, Associativity.Left), new Operator('/', 3, 2, Associativity.Left), new Operator('*', 3, 2, Associativity.Left), new Operator('%', 3, 2, Associativity.Left), new Operator('^', 4, 2, Associativity.Right), new Operator('u', 4, 1, Associativity.Left) };

		public static float Evaluate(string expression)
		{
			float result = default;
			if (!TryParse(expression, out result))
			{
				expression = PreFormatExpression(expression);
				result = Evaluate(InfixToRPN(FixUnaryOperators(ExpressionToTokens(expression))));
			}
			return result;
		}

		private static float Evaluate(string[] tokens)
		{
			Stack<string> source = new Stack<string>();
			foreach (string token in tokens)
			{
				if (IsOperator(token))
				{
					Operator @operator = CharToOperator(token[0]);
					List<float> objList = new List<float>();
					bool flag = true;
					while (source.LongCount<string>() > 0L && !IsCommand(source.Peek()) && objList.Count < @operator.inputs)
					{
						float result;
						flag &= TryParse(source.Pop(), out result);
						objList.Add(result);
					}
					objList.Reverse();
					if (!flag || objList.Count != @operator.inputs)
						return default(float);
					source.Push(Evaluate(objList.ToArray(), token[0]).ToString());
				}
				else
					source.Push(token);
			}
			float result1;
			if (source.LongCount<string>() == 1L && TryParse(source.Pop(), out result1))
				return result1;
			return default;
		}

		private static string[] InfixToRPN(string[] tokens)
		{
			Stack<char> charStack = new Stack<char>();
			Stack<string> source = new Stack<string>();
			foreach (string token in tokens)
			{
				if (IsCommand(token))
				{
					char character = token[0];
					switch (character)
					{
						case '(':
							charStack.Push(character);
							continue;
						case ')':
							while (charStack.LongCount<char>() > 0L && (int)charStack.Peek() != 40)
								source.Push(charStack.Pop().ToString());
							if (charStack.LongCount<char>() > 0L)
							{
								int num = (int)charStack.Pop();
								continue;
							}
							continue;
						default:
							Operator newOperator = CharToOperator(character);
							while (NeedToPop(charStack, newOperator))
								source.Push(charStack.Pop().ToString());
							charStack.Push(character);
							continue;
					}
				}
				else
					source.Push(token);
			}
			while (charStack.LongCount<char>() > 0L)
				source.Push(charStack.Pop().ToString());
			return source.Reverse<string>().ToArray<string>();
		}

		private static bool NeedToPop(Stack<char> operatorStack, Operator newOperator)
		{
			if (operatorStack.LongCount<char>() > 0L)
			{
				Operator @operator = CharToOperator(operatorStack.Peek());
				if (IsOperator(@operator.character) && (newOperator.associativity == Associativity.Left && newOperator.presedence <= @operator.presedence || newOperator.associativity == Associativity.Right && newOperator.presedence < @operator.presedence))
					return true;
			}
			return false;
		}

		private static string[] ExpressionToTokens(string expression)
		{
			List<string> stringList = new List<string>();
			string empty = string.Empty;
			for (int index = 0; index < expression.Length; ++index)
			{
				char character = expression[index];
				if (IsCommand(character))
				{
					if (empty.Length > 0)
						stringList.Add(empty);
					stringList.Add(character.ToString());
					empty = string.Empty;
				}
				else if ((int)character != 32)
					empty += character.ToString();
			}
			if (empty.Length > 0)
				stringList.Add(empty);
			return stringList.ToArray();
		}

		private static bool IsCommand(string token)
		{
			if (token.Length != 1)
				return false;
			return IsCommand(token[0]);
		}

		private static bool IsCommand(char character)
		{
			if ((int)character == 40 || (int)character == 41)
				return true;
			return IsOperator(character);
		}

		private static bool IsOperator(string token)
		{
			if (token.Length != 1)
				return false;
			return IsOperator(token[0]);
		}

		private static bool IsOperator(char character)
		{
			foreach (Operator @operator in s_Operators)
			{
				if ((int)@operator.character == (int)character)
					return true;
			}
			return false;
		}

		private static Operator CharToOperator(char character)
		{
			foreach (Operator @operator in s_Operators)
			{
				if ((int)@operator.character == (int)character)
					return @operator;
			}
			return new Operator();
		}

		private static string PreFormatExpression(string expression)
		{
			string str = expression.Trim();
			if (str.Length == 0)
				return str;
			char character = str[str.Length - 1];
			if (IsOperator(character))
				str = str.TrimEnd(character);
			return str;
		}

		private static string[] FixUnaryOperators(string[] tokens)
		{
			if (tokens.Length == 0)
				return tokens;
			if (tokens[0] == "-")
				tokens[0] = "u";
			for (int index = 1; index < tokens.Length - 1; ++index)
			{
				string token1 = tokens[index];
				string token2 = tokens[index - 1];
				string token3 = tokens[index - 1];
				if (token1 == "-" && (IsCommand(token2) || token3 == "(" || token3 == ")"))
					tokens[index] = "u";
			}
			return tokens;
		}

		private static float Evaluate(float[] values, char oper)
		{
			if (values.Length == 1)
			{
				if ((int)oper == 117)
					return values[0] * -1.0f;
			}
			else if (values.Length == 2)
			{
				char ch = oper;
				switch (ch)
				{
					case '*':
						return values[0] * values[1];
					case '+':
						return values[0] + values[1];
					case '-':
						return values[0] - values[1];
					case '/':
						return values[0] / values[1];
					default:
						if ((int)ch == 37)
							return values[0] % values[1];
						if ((int)ch == 94)
							return Mathf.Pow(values[0], values[1]);
						break;
				}
			}

			return default;
		}

		private static bool TryParse(string expression, out float result)
		{
			expression = expression.Replace(',', '.');
			bool flag = false;
			result = default;

			float result1 = 0.0f;
			flag = float.TryParse(expression, NumberStyles.Float, (IFormatProvider)CultureInfo.InvariantCulture.NumberFormat, out result1);
			result = result1;

			return flag;
		}

		private enum Associativity
		{
			Left,
			Right,
		}

		private struct Operator
		{
			public char character;
			public int presedence;
			public Associativity associativity;
			public int inputs;

			public Operator(char character, int presedence, int inputs, Associativity associativity)
			{
				this.character = character;
				this.presedence = presedence;
				this.inputs = inputs;
				this.associativity = associativity;
			}
		}
	}
}