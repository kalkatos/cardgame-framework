using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
    public class Zone : CGObject
    {
        public string[] tags;

        private List<CGComponent> components = new List<CGComponent>();

        public void Shuffle ()
		{
            //TODO Shuffle
		}

        public void Push (CGComponent component, RevealStatus revealStatus, bool toBottom)
		{
            //TODO Push
            components.Add(component);
            component.zone = this;
        }

        public void Pop (CGComponent component)
		{
            //TODO Pop
            components.Remove(component);
            component.zone = null;
        }

        public int GetIndexOf (CGComponent component)
		{
            return components.IndexOf(component);
		}

		public override string ToString ()
		{
			return $"Zone: {name} (id: {id})";
		}
	}
}
