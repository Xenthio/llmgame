namespace LLMGame;

public partial class LLMScene : SingletonComponent<LLMScene>
{
	public static readonly float METERS_2_INCH = 39.3701f;
	public static readonly float INCH_2_METERS = 00.0254f;
	[Property] public MeshComponent FloorMesh { get; set; }
	[Property] public MeshComponent CeilingMesh { get; set; }

	public async void BroadcastAudibleMessage( ILLMBeing speaker, string content, bool think = true )
	{
		var message = Message.As( speaker, content );
		speaker.Memory.Add( message );
		foreach ( var thinker in Scene.GetAllComponents<ILLMBeing>() )
		{
			if ( thinker == speaker ) continue;
			thinker.Memory.Add( message );
			if ( think ) await thinker.Think();
		}
		//todo range.
	}
}


public class Message
{
	public ILLMBeing Owner { get; set; }
	public string Content { get; set; }
	public static Message As( ILLMBeing thinker, string content )
	{
		return new Message
		{
			Owner = thinker,
			Content = content
		};
	}
	public static Message System( string content )
	{
		return new Message
		{
			Owner = null,
			Content = content
		};
	}
	public APIMessage ConvertToAPIMessage()
	{
		var role = "system";
		string name = null;
		if ( Owner != null )
		{
			role = "assistant";
			if ( Owner.IsUser() ) role = "user";
			name = Owner.GetName();
		}
		return new APIMessage()
		{
			role = role,
			content = Content,
			name = name,
		};
	}
}
