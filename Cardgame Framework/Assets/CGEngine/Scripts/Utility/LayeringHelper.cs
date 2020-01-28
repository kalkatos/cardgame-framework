using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace CardGameFramework
{
	public class LayeringHelper : MonoBehaviour
	{
		int multiplier = 1000;
		public SortingGroup group;
		public TextMeshPro text;
		public SpriteRenderer sprite;

		private void Start()
		{
			group = GetComponent<SortingGroup>();
			text = GetComponent<TextMeshPro>();
			sprite = GetComponent<SpriteRenderer>();
		}

		private void Update()
		{
			if (transform.hasChanged)
			{
				int sorting = Mathf.RoundToInt(transform.position.y * multiplier);
				if (group) group.sortingOrder = sorting;
				else if (text) text.sortingOrder = sorting;
				else if (sprite) sprite.sortingOrder = sorting;
			}
		}
	}
}