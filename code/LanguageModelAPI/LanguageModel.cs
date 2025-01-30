using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMGame;

public class LanguageModel : SingletonComponent<LanguageModel>
{
	private Dictionary<string, string> Headers = new();
	private string Token = "null";
	public string Model = "anthropic/claude-3.5-sonnet";
	public List<APIMessage> Messages = new();
	protected override void OnStart()
	{
		base.OnStart();
		Initialise();
	}
	public void Initialise()
	{
		if ( Token == "null" )
		{
			Token = FileSystem.Data.ReadAllText( "llm_token.txt" );
		}
		if ( Headers.Count > 0 ) return;
		Headers.Clear();
		Headers.Add( "Content-Type:", "application/json" );
		Headers.Add( "Authorization", $"Bearer {Token}" );
	}
	public static void AddMessage( string role, string content )
	{
		var b = new APIMessage();
		b.role = role;
		b.content = content;
		Instance.Messages.Add( b );
	}

	public static async Task<APIChatResponse> Generate()
	{
		var newmessage = await GenerateOnly();
		Instance.Messages.Add( newmessage.choices.First().message );
		return newmessage;
	}

	public static async Task<APIChatResponse> GenerateOnly()
	{
		return await GenerateFromMessages( Instance.Messages );
	}

	public static async Task<APIChatResponse> GenerateFromMessages( List<APIMessage> Messages )
	{
		Instance.Initialise();
		// Prepare the request
		var requestBody = new
		{
			model = Instance.Model,
			messages = Messages,
			provider = new
			{
				sort = "throughput",
			}
			//mode = "instruct",
			//max_tokens = 2048,
			//temperature = 0.6,
			//min_p = 0.03
		};

		var content = Http.CreateJsonContent( requestBody );
		Log.Info( content.ReadAsStringAsync().Result );
		// Send the request
		//var response = await Http.RequestAsync( "http://127.0.0.1:7864/v1/chat/completions", "POST", content, Instance.Headers );
		var response = await Http.RequestAsync( "https://openrouter.ai/api/v1/chat/completions", "POST", content, headers: Instance.Headers );

		if ( response.IsSuccessStatusCode )
		{
			var json = await response.Content.ReadAsStringAsync();
			Log.Info( json );
			var deserialized = Json.Deserialize<APIChatResponse>( json );

			/*if ( deserialized.choices.First().message.content.Contains( "<think>" ) )
			{
				deserialized.choices.First().message.content = Regex.Replace( deserialized.choices.First().message.content, @"<think>[\s\S]*?</think>", string.Empty, RegexOptions.Multiline );
				deserialized.choices.First().message.content = deserialized.choices.First().message.content.TrimStart( '\n' );
			}*/
			return deserialized;
		}
		else
		{
			var json = await response.Content.ReadAsStringAsync();
			throw new Exception( $"Failed to get response from API: {response.StatusCode} {json}" );
		}
	}

	[ConCmd( "llm_generate" )]
	public static void GenerateNowConCmd()
	{
		Generate();
	}
}

public class APIMessage
{
	public string role { get; set; }
	public string content { get; set; }
	public string name { get; set; }

	public static APIMessage Custom( string role, string content )
	{
		return new APIMessage
		{
			role = role,
			content = content
		};
	}
	public static APIMessage Assistant( string name, string content )
	{
		return new APIMessage
		{
			role = "assistant",
			content = content
		};
	}

	public static APIMessage User( string name, string content )
	{
		return new APIMessage
		{
			role = "user",
			content = content
		};
	}
}

public class APIChatResponse
{
	public string id { get; set; }
	public int created { get; set; }
	public string model { get; set; }
	public List<ChatResponseChoice> choices { get; set; }
	public class ChatResponseChoice
	{
		public int index { get; set; }
		public string finish_reason { get; set; }
		public APIMessage message { get; set; }
	}
}

