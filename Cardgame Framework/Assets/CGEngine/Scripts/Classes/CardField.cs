using TMPro;
using UnityEngine;

namespace CardGameFramework
{
	[System.Serializable]
	public class CardField
	{
		public string fieldName;
		public CardFieldDataType dataType;
		public double numValue;
		public string stringValue;
		public Sprite imageValue;
		public string imageSourceName;
		public CardFieldHideOption hideOption;
		//public TextMeshPro linkedTextElement;
		//public SpriteRenderer linkedImageElement;

		public CardField () { }

		public CardField (CardField other)
		{
			fieldName = other.fieldName;
			dataType = other.dataType;
			numValue = other.numValue;
			stringValue = other.stringValue;
			imageValue = other.imageValue;
			hideOption = other.hideOption;
		}
	}
}
