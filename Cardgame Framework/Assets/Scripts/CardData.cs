using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Card", order = 1)]
public class CardData : ScriptableObject
{
	public string id;
	public string tags;
	public string description;
	[SerializeField]
	public CardField[] fields;
}
