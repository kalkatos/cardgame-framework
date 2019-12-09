using System;

namespace CGEngine
{
	[Flags]
	public enum MessageType
	{
		None = 0,
		GameStart = 1,
		MatchStart = 2,
		MatchEnd = 4,
		GameEnd = 8,
		CardEnterZone = 16,
		CardLeaveZone = 32,
		CardUsed = 64,
		ObjectClicked = 128,
		ObjectHoverStart = 256,
		ObjectHoverEnd = 512,
		ObjectMoveStart = 1024,
		ObjectMoveEnd = 2048,
		//4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576, 2097152, 4194304, 8388608, 16777216, 33554432, 67108864
	}
}