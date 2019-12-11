using TMPro;
using UnityEngine;

namespace CGEngine
{
	[System.Serializable]
	public class CardField
	{
		public string name;
		public CardFieldDataType dataType;
		public double numValue;
		public string stringValue;
		public TextMeshPro linkedTextElement;
		public Sprite imageValue;
		public SpriteRenderer linkedImageElement;
	}
}
