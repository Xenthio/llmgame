using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace LLMGame;

public class LMStudioAPI : ILanguageAPI
{
	public string Model { get; set; } = "null";
	public string Endpoint = "http://127.0.0.1:1234/v1/chat/completions";
	bool Initialised = false;
	public void Initialise()
	{
	}

	public async Task<ChatResponse> GenerateChatResponseFromMessages( List<Message> Messages, string replyAs = null )
	{
		if ( !Initialised ) Initialise();
		Dictionary<string, string> headers = new();

		headers.Add( "Content-Type:", "application/json" );

		var APIMessages = new List<LMStudioMessage>();
		foreach ( Message m in Messages )
		{
			APIMessages.Add( LMStudioMessage.ConvertTo( m ) );
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
public class LMStudioMessage
{
	public string role { get; set; }
	public string content { get; set; }
	public string name { get; set; }
	public static LMStudioMessage ConvertTo( Message message )
	{
		var b = new LMStudioMessage()
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

public class LMStudioChatResponse
{
	public string id { get; set; }
	public int created { get; set; }
	public string model { get; set; }
	public List<ChatResponseChoice> choices { get; set; }
	public class ChatResponseChoice
	{
		public int index { get; set; }
		public string finish_reason { get; set; }
		public LMStudioMessage message { get; set; }
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
