using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMGame;

public interface ILanguageAPI
{
	public string Model { get; set; }
	public void Initialise();
	public Task<ChatResponse> GenerateChatResponseFromMessages( List<Message> Messages, string replyAs = null );
}
