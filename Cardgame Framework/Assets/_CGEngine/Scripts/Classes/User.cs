using UnityEngine;

namespace CGEngine
{
	public class User
	{
		/*
		id : string
		name : string
		avatar : Sprite
		currentDeck : Deck
		infoOnGames : UserInfo[]
		*/
		public string id;
		public string name;
		public UserType type;
		public Sprite avatar;
		public Deck currentDeck;

		public User(UserType type)
		{
			this.type = type;
		}
		//public UserInfo[] infoOnGames;
	}
}