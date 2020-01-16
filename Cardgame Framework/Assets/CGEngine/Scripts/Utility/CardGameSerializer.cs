using System.IO;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace CardGameFramework
{
	public class CardGameSerializer
	{

		#region // ===========================  S E R I A L I Z A T I O N ===================================

		public static string SaveToJson(CardGameData game)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("{\"cardgameID\":\"" + game.cardgameID + "\",");
			sb.Append("\"allCardsData\":[");
			if (game.allCardsData != null)
			{
				for (int i = 0; i < game.allCardsData.Count; i++)
				{
					sb.Append(SerializeCard(game.allCardsData[i]));

					if (i < game.allCardsData.Count - 1)
						sb.Append(",");
				}
			}
			sb.Append("],");
			sb.Append("\"cardTemplate\":\"" + AssetDatabase.GetAssetPath(game.cardTemplate) + "\",");
			sb.Append("\"cardFieldDefinitions\":[");
			if (game.cardFieldDefinitions != null)
			{
				for (int i = 0; i < game.cardFieldDefinitions.Count; i++)
				{
					sb.Append(JsonUtility.ToJson(game.cardFieldDefinitions[i]));

					if (i < game.cardFieldDefinitions.Count - 1)
						sb.Append(",");
				}
			}
			sb.Append("],");
			sb.Append("\"rules\":[");
			if (game.rules != null)
			{
				for (int i = 0; i < game.rules.Count; i++)
				{
					sb.Append(JsonUtility.ToJson(game.rules[i]));
					//sb.Append("{\"rulesetID\":\"" + game.rules[i].rulesetID + "\",");
					//sb.Append("\"description\":\"" + game.rules[i].description + "\",");
					//sb.Append("\"turnStructure\":\"" + game.rules[i].turnStructure + "\",");
					//sb.Append("\"matchModifiers\":[");
					//if (game.rules[i].matchModifiers != null)
					//{
					//	for (int j = 0; j < game.rules[i].matchModifiers.Count; j++)
					//	{
					//		sb.Append(JsonUtility.ToJson(game.rules[i].matchModifiers[j]));

					//		if (j < game.rules[i].matchModifiers.Count - 1)
					//			sb.Append(",");
					//	}
					//}
					//sb.Append("]}");

					//if (i < game.rules.Count - 1)
					//	sb.Append(",");
				}
			}
			sb.Append("]}");
			return sb.ToString();
		}

		public static string SerializeCard (CardData card)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("{\"cardPath\":\"" + AssetDatabase.GetAssetPath(card) + "\",");
			sb.Append("\"cardDataID\":\""+card.cardDataID+"\",");
			sb.Append("\"tags\":\"" + card.tags + "\",");
			sb.Append("\"fields\":[");
			if (card.fields != null)
			{
				for (int i = 0; i < card.fields.Count; i++)
				{
					sb.Append(SerializeFilledCardField(card.fields[i]));

					if (i < card.fields.Count - 1)
						sb.Append(",");
				}
			}
			sb.Append("],");
			sb.Append("\"cardModifiers\":[");
			if (card.cardModifiers != null)
			{
				for (int i = 0; i < card.cardModifiers.Count; i++)
				{
					sb.Append(JsonUtility.ToJson(card.cardModifiers[i]));

					if (i < card.cardModifiers.Count - 1)
						sb.Append(",");
				}
			}
			sb.Append("]}");
			return sb.ToString();
		}

		static string SerializeFilledCardField (CardField field)
		{
			StringBuilder sb = new StringBuilder();
			//	public string name;
			sb.Append("{\"fieldName\":\"" + field.fieldName+"\",");
			//public CardFieldDataType dataType;
			sb.Append("\"dataType\":" + (int)field.dataType + ",");
			//public double numValue;
			sb.Append("\"numValue\":" + field.numValue + ",");
			//public string stringValue;
			sb.Append("\"stringValue\":\"" + field.stringValue + "\",");
			//public Sprite imageValue;
			sb.Append("\"imageValue\":\"" + AssetDatabase.GetAssetPath(field.imageValue) + "\",");
			//public CardFieldHideOption hideOption;
			sb.Append("\"hideOption\":" + (int)field.hideOption + "}");
			return sb.ToString();
		}

		#endregion

		#region // ===========================  D E S E R I A L I Z A T I O N ===================================

		public static CardGameData RecoverFromJson (string serializedGame)
		{
			CardGameData result = ScriptableObject.CreateInstance<CardGameData>();
			serializedGame = serializedGame.Replace(" ", "").Replace("\n", "").Replace("\n\r", "").Replace(System.Environment.NewLine, "");

			result.cardgameID = FindFieldValue("cardgameID", serializedGame);
			result.cardTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(FindFieldValue("cardTemplate", serializedGame));
			//CardFields
			List<string> stringArrayForObjects = GetArrayObjects(FindFieldValue("cardFieldDefinitions", serializedGame));
			result.cardFieldDefinitions = new List<CardField>();
			for (int i = 0; i < stringArrayForObjects.Count; i++)
			{
				result.cardFieldDefinitions.Add(JsonUtility.FromJson<CardField>(stringArrayForObjects[i]));
			}
			//Rulesets
			stringArrayForObjects = GetArrayObjects(FindFieldValue("rules", serializedGame));
			result.rules = new List<Ruleset>();
			for (int i = 0; i < stringArrayForObjects.Count; i++)
			{
				result.rules.Add(JsonUtility.FromJson<Ruleset>(stringArrayForObjects[i]));
			}
			//All Cards
			stringArrayForObjects = GetArrayObjects(FindFieldValue("allCardsData", serializedGame));
			result.allCardsData = new List<CardData>();
			for (int i = 0; i < stringArrayForObjects.Count; i++)
			{
				CardData card = AssetDatabase.LoadAssetAtPath<CardData>(FindFieldValue("cardPath", stringArrayForObjects[i]));
				if (card == null)
				{
					card = GetCardDataFromString(stringArrayForObjects[i]);
				}
				result.allCardsData.Add(card);
			}

			return result;
		}

		public static List<CardData> RecoverListOfCardsFromJson (TextAsset list)
		{
			List<string> stringArrayForObjects = GetArrayObjects(list.text);
			List<CardData> result = new List<CardData>();
			for (int i = 0; i < stringArrayForObjects.Count; i++)
			{
				result.Add(GetCardDataFromString(stringArrayForObjects[i]));
			}
			return result;
		}

		static string FindFieldValue (string name, string objString)
		{
			int index = objString.IndexOf(name);
			if (index == -1)
			{
				Debug.LogError("Couldn't find field: " + name);
				return "";
			}
			index = objString.IndexOf(":", index) + 1;

			while (objString[index] == ' ' || objString[index] == '\n')
				index++;

			string value = "";

			if (objString[index] == '[') //Array
			{
				int endIndex = FindEndBracketIndex(objString, index, '[', ']');
				value = objString.Substring(index, endIndex - index + 1);
			}
			else // Object, Number or String
			{
				int endOfField = index;
				if (objString[index] == '\"')
					endOfField = objString.IndexOf("\"", index + 1);
				else
					endOfField = objString.IndexOf(",", index);
				if (endOfField == -1)
					endOfField = objString.IndexOf("}", index);
				value = objString.Substring(index, endOfField - index);

				if (value.Contains("\""))
					value = value.Replace("\"", "");
				return value;
			}
			return value;
		}

		static CardData GetCardDataFromString (string str)
		{
			CardData card = ScriptableObject.CreateInstance<CardData>();
			card.cardDataID = FindFieldValue("cardDataID", str);
			card.tags = FindFieldValue("tags", str);
			card.fields = new List<CardField>();
			List<string> objListForCard = GetArrayObjects(FindFieldValue("fields", str));
			for (int j = 0; j < objListForCard.Count; j++)
			{
				CardField newField = new CardField();
				newField.fieldName = FindFieldValue("fieldName", objListForCard[j]);
				newField.dataType = (CardFieldDataType)int.Parse(FindFieldValue("dataType", objListForCard[j]));
				newField.hideOption = (CardFieldHideOption)int.Parse(FindFieldValue("hideOption", objListForCard[j]));
				newField.stringValue = FindFieldValue("stringValue", objListForCard[j]);
				newField.numValue = double.Parse(FindFieldValue("numValue", objListForCard[j]));
				newField.imageValue = AssetDatabase.LoadAssetAtPath<Sprite>(FindFieldValue("imageValue", objListForCard[j]));
				card.fields.Add(newField);
			}
			card.cardModifiers = new List<ModifierData>();
			objListForCard = GetArrayObjects(FindFieldValue("cardModifiers", str));
			for (int j = 0; j < objListForCard.Count; j++)
			{
				card.cardModifiers.Add(JsonUtility.FromJson<ModifierData>(objListForCard[j]));
			}
			return card;
		}

		static List<string> GetArrayObjects (string array)
		{
			List<string> result = new List<string>();
			int objStart = array.IndexOf('{');
			int objEnd = -1;
			while (objStart != -1 && objStart < array.Length)
			{
				objEnd = FindEndBracketIndex(array, objStart, '{', '}');
				if (objEnd == -1)
					break;
				else
				{
					result.Add(array.Substring(objStart, objEnd - objStart + 1));
					objStart = array.IndexOf('{', objEnd);
				}
			}
			return result;
		}

		static int FindEndBracketIndex (string array, int start, char openingChar, char closingChar)
		{
			start = array.IndexOf(openingChar, start);
			int bracketCounter = 1;
			int end = start;
			while (start != -1 && bracketCounter > 0)
			{
				end++;
				if (end < array.Length)
				{
					if (array[end] == openingChar)
						bracketCounter++;
					else if (array[end] == closingChar)
						bracketCounter--;
				}
				else
				{
					Debug.Log("Syntax error on JSON, looking for array: " + array.PadLeft(10) + " ... " + array.PadRight(10));
					return -1;
				}
			}
			return end;
		}

		#endregion

	}
}