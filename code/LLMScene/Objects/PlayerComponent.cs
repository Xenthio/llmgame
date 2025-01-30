using System.Collections.Generic;

namespace LLMGame;

public class PlayerComponent : Component, IThinker
{
	public List<Message> Memory { get; set; } = new();
	public string GetName()
	{
		return "Player";
	}
	public bool IsUser()
	{
		return true;
	}
	public void Think()
	{
		// Players don't think ;)
	}
}
