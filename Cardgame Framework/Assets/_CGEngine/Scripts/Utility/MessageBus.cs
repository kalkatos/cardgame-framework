using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CGEngine
{
	public class MessageBus : MonoBehaviour
	{
		static MessageBus instance;
		public static MessageBus Instance
		{
			get { if (instance == null) instance = new GameObject("MessageBus").AddComponent<MessageBus>(); return instance; }
		}

		Dictionary<string, List<IMessageReceiver>> receivers;
		Dictionary<string, List<IMessageReceiver>> Receivers { get { if (receivers == null) receivers = new Dictionary<string, List<IMessageReceiver>>(); return receivers; } }

		private void Awake()
		{
			if (instance == null)
				instance = this;
			else if (instance != this)
				DestroyImmediate(gameObject);
		}

		public static void Send(string type, InputObject inputObject = null)
		{
			if (Instance.Receivers.ContainsKey("All"))
			{
				for (int i = 0; i < Instance.Receivers["All"].Count; i++)
				{
					Instance.Receivers["All"][i].TreatMessage(type, inputObject);
				}
			}

			if (Instance.Receivers.ContainsKey(type))
			{
				for (int i = 0; i < Instance.Receivers[type].Count; i++)
				{
					Instance.Receivers[type][i].TreatMessage(type, inputObject);
				}
			}
		}

		public static void Register(string type, IMessageReceiver receiver)
		{
			if (Instance.Receivers.ContainsKey(type))
				Instance.Receivers[type].Add(receiver);
			else
			{
				List<IMessageReceiver> list = new List<IMessageReceiver>();
				list.Add(receiver);
				Instance.Receivers.Add(type, list);
			}
		}
	}
}