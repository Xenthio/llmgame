using System.Collections.Generic;

namespace LLMGame;

public interface IThinker
{
	public List<Message> Memory { get; set; }
	public string GetName();
	public void Think();
}
