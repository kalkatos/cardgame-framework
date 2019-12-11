using UnityEngine;

namespace CGEngine
{
	public struct Message
	{
		public MessageType type;
		public double doubleValue;
		public string stringValue;
		public InputObject ioValue;

		public Message(InputObject io)
		{
			type = MessageType.None;
			doubleValue = 0;
			stringValue = "";
			ioValue = io;
		}
	}
}