using System.Threading.Tasks;

namespace LLMGame;

public interface ISpeechAPI
{
	public void Initialise();
	public Task<bool> IsReady();
	public Task<string> GenerateSpeechFromText( string text, string speakAs = null );
}
