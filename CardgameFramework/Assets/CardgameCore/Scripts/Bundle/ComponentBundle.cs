using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
    [CreateAssetMenu(fileName = "New Component Bundle", menuName = "Cardgame/Component Bundle", order = 2)]
    public class ComponentBundle : ScriptableObject
    {
        public ComponentData[] components;
    }
}
