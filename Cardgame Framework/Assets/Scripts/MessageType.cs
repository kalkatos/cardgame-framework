using System;

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
}