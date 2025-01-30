using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMGame;

public interface ILLMBeing
{
	public List<Message> Memory { get; set; }
	public string GetName();
	public bool IsUser();
	public async Task Think() { }
}
