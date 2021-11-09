using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameFramework
{
    [CreateAssetMenu(fileName = "New Component Bundle", menuName = "Cardgame/Component Bundle", order = 2)]
    public class ComponentBundle : ScriptableObject
    {
        public CardData[] cards;
    }
}
