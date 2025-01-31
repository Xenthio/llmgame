using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace LLMGame;

public class OpenRouterAPI : ILanguageAPI
{
	private string Token = "null";
	public string Model = "anthropic/claude-3.5-sonnet";
	public string Endpoint = "https://openrouter.ai/api/v1/chat/completions";
	bool Initialised = false;
	public void Initialise()
	{
		if ( !FileSystem.Data.FileExists( "openrouter_token.txt" ) )
		{
			FileSystem.Data.WriteAllText( "openrouter_token.txt", "<YOUR_TOKEN_HERE>" );
		}
		if ( Token == "null" )
		{
			Token = FileSystem.Data.ReadAllText( "openrouter_token.txt" );
		}
	}

	public async Task<ChatResponse> GenerateChatResponseFromMessages( List<Message> Messages, string replyAs = null )
	{
		if ( !Initialised ) Initialise();
		Dictionary<string, string> headers = new();

		headers.Add( "Content-Type:", "application/json" );
		headers.Add( "Authorization", $"Bearer {Token}" );

		var APIMessages = new List<OpenRouterMessage>();
		foreach ( Message m in Messages )
		{
			APIMessages.Add( OpenRouterMessage.ConvertTo( m ) );
		}

		// OpenRouter Assistant Prefill
		if ( replyAs != null )
		{
			var prefilmessage = new OpenRouterMessage()
			{
				role = "assistant",
				content = $"{replyAs}: "
			};
			APIMessages.Add( prefilmessage );
		}

		// Prepare the request
		var requestBody = new
		{
			model = Model,
			messages = APIMessages
		};

		var content = Http.CreateJsonContent( requestBody );
		Log.Info( content.ReadAsStringAsync().Result );

		var response = await Http.RequestAsync( Endpoint, "POST", content, headers: headers );

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
	}
}
public class OpenRouterMessage
{
	public string role { get; set; }
	public string content { get; set; }
	public string name { get; set; }
	public static OpenRouterMessage ConvertTo( Message message )
	{
		var b = new OpenRouterMessage()
		{
			content = message.Content,
			name = null,
			role = "system"
		};

		if ( message.Owner != null )
		{
			b.role = "assistant";
			if ( message.Owner.IsUser() ) b.role = "user";
			b.name = message.Owner.GetName();
		}
		return b;
	}

	public Message ConvertFrom()
	{
		var b = new Message()
		{
			Content = this.content,
		};

		return b;
	}

}

public class OpenRouterChatResponse
{
	public string id { get; set; }
	public int created { get; set; }
	public string model { get; set; }
	public List<ChatResponseChoice> choices { get; set; }
	public class ChatResponseChoice
	{
		public int index { get; set; }
		public string finish_reason { get; set; }
		public OpenRouterMessage message { get; set; }
	}
	public ChatResponse ConvertFrom()
	{
		var b = new ChatResponse()
		{
			Model = this.model,
			Messages = this.choices.Select( x => x.message ).Select( x => x.ConvertFrom() ).ToList()
		};

		return b;
	}
}
