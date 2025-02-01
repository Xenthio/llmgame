using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMGame;

public class AllTalkTTS : ISpeechAPI
{
	public string Endpoint = "http://127.0.0.1:7851/api/tts-generate";
	public void Initialise()
	{
	}
	public async Task<bool> IsReady()
	{
		//Prepare the request
		/*		var requestBody = new
				{
					model = Model,
					messages = APIMessages
				};*/

		//var content = Http.CreateJsonContent( requestBody );

		//Log.Info( content.ReadAsStringAsync().Result );

		try
		{
			var response = await Http.RequestAsync( "http://127.0.0.1:7851/api/ready", "POST" );

			if ( response.IsSuccessStatusCode )
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
	}

	public async Task<string> GenerateSpeechFromText( string text, string speakAs = null )
	{
		Dictionary<string, string> headers = new();

		headers.Add( "Content-Type:", "x-www-form-urlencoded" );

		var outputFile = "temp.wav";
		var language = "en";
		var encodedText = text.UrlEncode();
		var url = $"http://localhost:7851/api/tts-generate-streaming?text=${encodedText}&voice={speakAs}.wav&language={language}&output_file={outputFile}";

		return url;

		// Prepare the request
		/*		var requestBody = new
				{
					model = Model,
					messages = APIMessages
				};*/

		//var content = Http.CreateJsonContent( requestBody );

		//Log.Info( content.ReadAsStringAsync().Result );

		/*var response = await Http.RequestAsync( Endpoint, "POST", null, headers: headers );

		if ( response.IsSuccessStatusCode )
		{
			var json = await response.Content.ReadAsStringAsync();
			Log.Info( json );
			var deserialized = Json.Deserialize<OpenRouterChatResponse>( json );
			return deserialized.ConvertFrom();
		}
		else
		{
			var json = await response.Content.ReadAsStringAsync();
			throw new Exception( $"Failed to get response from API: {response.StatusCode} {json}" );
		}
		return "null";*/
	}
}
