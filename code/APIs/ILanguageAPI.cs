using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMGame;

public interface ILanguageAPI
{
	public void Initialise();
	public Task<ChatResponse> GenerateChatResponseFromMessages( List<Message> Messages, string replyAs = null );
}
