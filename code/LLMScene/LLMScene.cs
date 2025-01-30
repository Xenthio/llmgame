namespace LLMGame;

public partial class LLMScene : SingletonComponent<LLMScene>
{
	[Property] public MeshComponent FloorMesh { get; set; }
	[Property] public MeshComponent CeilingMesh { get; set; }
	public static string GetPrompt()
	{
		return """
			You are {char} in this roleplay scenario. Be creative and realistic.
			""";
	}

	public async void BroadcastAudibleMessage( IThinker speaker, string content )
	{
		var message = Message.As( speaker, content );
		foreach ( var thinker in Scene.GetAllComponents<IThinker>() )
		{
			if ( thinker == speaker ) continue;
			thinker.Memory.Add( message );
		}
		//todo range.
	}
}


public class Message
{
	public string Role { get; set; }
	public string Name { get; set; }
	public string Content { get; set; }
	public static Message Assistant( string name, string content )
	{
		return new Message
		{
			Role = "assistant",
			Name = name,
			Content = content
		};
	}

	public static Message As( IThinker thinker, string content )
	{
		var role = "assistant";
		if ( thinker.IsUser() ) role = "user";
		return new Message
		{
			Role = role,
			Name = thinker.GetName(),
			Content = content
		};
	}
	public static Message User( string name, string content )
	{
		return new Message
		{
			Role = "user",
			Name = name,
			Content = content
		};
	}
	public static Message System( string content )
	{
		return new Message
		{
			Role = "system",
			Content = content
		};
	}
	public APIMessage ConvertToAPIMessage()
	{
		return new APIMessage()
		{
			role = Role,
			content = Content,
			name = Name,
		};
	}
}
