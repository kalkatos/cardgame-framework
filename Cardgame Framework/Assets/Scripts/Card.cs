using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
	public CardData data;
	public Zone currentZone;

	public string inGameId;

	public Dictionary<string, Text> alfanumericFields;
	public Dictionary<string, SpriteRenderer> imageFields;
}
