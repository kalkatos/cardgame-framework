using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardgameCore
{
    public class Zone : CardgameObject
    {
        public List<string> tags;

        private List<Component> components = new List<Component>();

        public void Shuffle ()
		{
            //TODO Shuffle
		}

        public void Push (Component component)
		{
            //TODO Push
            components.Add(component);
            component.zone = this;
        }

        public void Pop (Component component)
		{
            //TODO Pop
            components.Remove(component);
            component.zone = null;
        }

		public override string ToString ()
		{
			return $"Zone: {name} (id: {id})";
		}
	}
}
