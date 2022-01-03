using TMPro;
using UnityEngine;

namespace CardgameFramework
{
    public class ZoneCardCounter_Text : MonoBehaviour
    {
		[SerializeField] private Zone targetZone;
		[SerializeField] private TMP_Text textMesh;

		private void Awake ()
		{
			targetZone.OnCardCountChanged += CardCountChanged;
			textMesh.text = targetZone.CardCount.ToString();
		}

		private void OnDestroy ()
		{
			targetZone.OnCardCountChanged -= CardCountChanged;
		}

		private void CardCountChanged (int value)
		{
			textMesh.text = value.ToString();
		}
	}
}