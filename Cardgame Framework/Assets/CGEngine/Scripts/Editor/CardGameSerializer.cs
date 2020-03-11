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

		public static string SerializeCardGame (CardGameData game)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("{\"cardgameID\":\"" + game.cardgameID + "\",");
			sb.Append("\"gameVariableNames\":[");
			if (game.gameVariableNames != null)
			{
				for (int i = 0; i < game.gameVariableNames.Count; i++)
				{
					sb.Append("\"");
					sb.Append(game.gameVariableNames[i]);
					sb.Append("\"");
					if (i < game.gameVariableNames.Count - 1)
						sb.Append(",");
				}
			}
			sb.Append("],");
			sb.Append("\"gameVariableValues\":[");
			if (game.gameVariableValues != null)
			{
				for (int i = 0; i < game.gameVariableValues.Count; i++)
				{
					sb.Append("\"");
					sb.Append(game.gameVariableValues[i]);
					sb.Append("\"");
					if (i < game.gameVariableValues.Count - 1)
						sb.Append(",");
				}
			}
			sb.Append("],");
			sb.Append("\"cardsets\":[");
			if (game.cardsets != null)
			{
				for (int i = 0; i < game.cardsets.Count; i++)
				{
					sb.Append("\"");
					sb.Append(game.cardsets[i].cardsetID);
					sb.Append("\"");
					if (i < game.cardsets.Count - 1)
						sb.Append(",");
				}
			}
			sb.Append("],");
			sb.Append("\"rulesets\":[");
			if (game.rulesets != null)
			{
				for (int i = 0; i < game.rulesets.Count; i++)
				{
					sb.Append(JsonUtility.ToJson(game.rulesets[i]));
				}
			}
			sb.Append("]}");
						
			return sb.ToString();
		}

		public static string SerializeCardset (Cardset cardset)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("{\"cardsetID\":\"" + cardset.cardsetID + "\",");
			sb.Append("\"description\":\"" + cardset.description + "\",");
			//Card template
			sb.Append("\"cardTemplate\":\"" + AssetDatabase.GetAssetPath(cardset.cardTemplate) + "\",");
			//Card fields
			sb.Append("\"cardFieldDefinitions\":[");
			if (cardset.cardFieldDefinitions != null)
			{
				for (int j = 0; j < cardset.cardFieldDefinitions.Count; j++)
				{
					sb.Append(SerializeCardField(cardset.cardFieldDefinitions[j]));

					if (j < cardset.cardFieldDefinitions.Count - 1)
						sb.Append(",");
				}
			}
			sb.Append("],");
			//Cards data
			sb.Append("\"cardsData\":[");
			if (cardset.cardsData != null)
			{
				for (int j = 0; j < cardset.cardsData.Count; j++)
				{
					sb.Append(SerializeCard(cardset.cardsData[j]));

					if (j < cardset.cardsData.Count - 1)
						sb.Append(",");
				}
			}
			sb.Append("]}");
			return sb.ToString();
		}

		static string SerializeCard (CardData card)
		{
			if (card == null)
				return "";

			StringBuilder sb = new StringBuilder();

			sb.Append("{\"cardDataID\":\"" + card.cardDataID + "\",");
			sb.Append("\"tags\":\"" + card.tags + "\",");
			sb.Append("\"fields\":[");
			if (card.fields != null)
			{
				for (int i = 0; i < card.fields.Count; i++)
				{
					sb.Append(SerializeCardField(card.fields[i]));

					if (i < card.fields.Count - 1)
						sb.Append(",");
				}
			}
			sb.Append("],");
			sb.Append("\"cardRules\":[");
			if (card.cardRules != null)
			{
				for (int i = 0; i < card.cardRules.Count; i++)
				{
					sb.Append(JsonUtility.ToJson(card.cardRules[i]));

					if (i < card.cardRules.Count - 1)
						sb.Append(",");
				}
			}
			sb.Append("]}");

			return sb.ToString();
		}

		static string SerializeCardField (CardField field)
		{
			StringBuilder sb = new StringBuilder();
			//	public string name;
			sb.Append("{\"fieldName\":\"" + field.fieldName + "\",");
			//public CardFieldDataType dataType;
			sb.Append("\"dataType\":" + (int)field.dataType + ",");
			//public float numValue;
			sb.Append("\"numValue\":" + field.numValue + ",");
			//public string stringValue;
			sb.Append("\"stringValue\":\"" + field.stringValue + "\",");
			//public Sprite imageValue;
			sb.Append("\"imageSourceName\":\"");
			if (field.imageValue)
			{
				string path = AssetDatabase.GetAssetPath(field.imageValue);
				int index = path.LastIndexOf('/') + 1;
				sb.Append(index > 0 && index < path.Length ? path.Substring(index) : "");
			}
			sb.Append("\",");
			//public CardFieldHideOption hideOption;
			sb.Append("\"hideOption\":" + (int)field.hideOption + "}");
			return sb.ToString();
		}

		#endregion

		#region // ===========================  D E S E R I A L I Z A T I O N ===================================

		public static CardGameData RecoverCardGameFromJson (string serializedGame)
		{
			CardGameData result = ScriptableObject.CreateInstance<CardGameData>();
			
			serializedGame = StringUtility.GetCleanStringForInstructions(serializedGame);

			result.cardgameID = FindFieldValue("cardgameID", serializedGame);
			
			result.gameVariableNames = GetArrayObjects(FindFieldValue("gameVariableNames", serializedGame));
			result.gameVariableValues = GetArrayObjects(FindFieldValue("gameVariableValues", serializedGame));

			//Cardsets
			result.cardsets = new List<Cardset>();
			List<string> cardsetNames = GetArrayObjects(FindFieldValue("cardsets", serializedGame));
			string[] foundAssets = AssetDatabase.FindAssets("t:Cardset");
			if (foundAssets != null)
			{
				foreach (string item in foundAssets)
				{
					Cardset data = AssetDatabase.LoadAssetAtPath<Cardset>(AssetDatabase.GUIDToAssetPath(item));
					if (cardsetNames.Contains(data.cardsetID) && !result.cardsets.Contains(data))
						result.cardsets.Add(data);
				}
			}

			//Rulesets
			List<string> stringArrayForObjects = GetArrayObjects(FindFieldValue("rulesets", serializedGame));
			result.rulesets = new List<Ruleset>();
			for (int i = 0; i < stringArrayForObjects.Count; i++)
			{
				result.rulesets.Add(JsonUtility.FromJson<Ruleset>(stringArrayForObjects[i]));
			}
			
			return result;
		}

		public static Cardset RecoverCardsetFromJson (string serializedCardset, string imagesFolder)
		{
			Cardset cardset = ScriptableObject.CreateInstance<Cardset>();
			cardset.cardsetID = FindFieldValue("cardsetID", serializedCardset);
			cardset.description = FindFieldValue("description", serializedCardset);
			cardset.cardTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(FindFieldValue("cardTemplate", serializedCardset));
			//Card Fields
			List<string> fields = GetArrayObjects(FindFieldValue("cardFieldDefinitions", serializedCardset));
			cardset.cardFieldDefinitions = new List<CardField>();
			for (int j = 0; j < fields.Count; j++)
			{
				cardset.cardFieldDefinitions.Add(JsonUtility.FromJson<CardField>(fields[j]));
			}
			//Cards data
			List<string> cardsDataInString = GetArrayObjects(FindFieldValue("cardsData", serializedCardset));
			cardset.cardsData = new List<CardData>();
			for (int j = 0; j < cardsDataInString.Count; j++)
			{
				cardset.cardsData.Add(RecoverCardDataFromJson(cardsDataInString[j], imagesFolder));
			}
			return cardset;
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

		static CardData RecoverCardDataFromJson (string str, string sourceImagesFolder)
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
				newField.numValue = float.Parse(FindFieldValue("numValue", objListForCard[j]));
				newField.imageSourceName = FindFieldValue("imageSourceName", objListForCard[j]);
				
				if (!string.IsNullOrEmpty(newField.imageSourceName))
				{
					if (!sourceImagesFolder.Contains(Application.dataPath))
					{
						if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
							AssetDatabase.CreateFolder("Assets", "Sprites");
						File.Copy(sourceImagesFolder + "/" + newField.imageSourceName, Application.dataPath + "/Sprites/" + newField.imageSourceName, true);
						newField.imageValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/" + newField.imageSourceName);
					}
					else
					{
						newField.imageValue = AssetDatabase.LoadAssetAtPath<Sprite>(sourceImagesFolder.Substring(sourceImagesFolder.IndexOf("Assets/")) + "/" + newField.imageSourceName);
					}
				}
				card.fields.Add(newField);
			}
			card.cardRules = new List<RuleData>();
			objListForCard = GetArrayObjects(FindFieldValue("cardRules", str));
			for (int j = 0; j < objListForCard.Count; j++)
			{
				card.cardRules.Add(JsonUtility.FromJson<RuleData>(objListForCard[j]));
			}
			return card;
		}

		static List<string> GetArrayObjects (string array)
		{
			List<string> result = new List<string>();
			if (!array.Contains("{") && !array.Contains("}"))
			{
				result.AddRange(array.Split(new char[] { '[', ']', '"', ',' }, System.StringSplitOptions.RemoveEmptyEntries));
				Debug.Log(StringUtility.PrintStringList(result));
				return result;
			}
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