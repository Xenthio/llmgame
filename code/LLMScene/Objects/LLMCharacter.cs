using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMGame;

public class LLMCharacter : Component, ILLMBeing
{
	[Property, ReadOnly] public List<Message> Memory { get; set; } = new();
	[Property] public string CardPath { get; set; } = "default_character.png";
	[Property, ReadOnly] public CharacterCard Card { get; set; }
	protected override void OnStart()
	{
		base.OnStart();
		Log.Info( $"{this} is now Loading" );
		Card = CharacterCard.LoadFromPNG( CardPath );

		Memory.Add( Message.System( BuildInfo( Card ) ) );
		if ( Card.data.group_only_greetings != null && Card.data.group_only_greetings.Any() )
		{
			LLMScene.Instance.BroadcastAudibleMessage( this, Card.data.group_only_greetings.OrderBy( x => Game.Random.Int( 0, 100 ) ).First() );
		}
	}

	public static string BuildInfo( CharacterCard card )
	{
		string info = "";
		var context = $"""
			{card.data.description}
			{card.data.personality}
			{card.data.scenario} 
			
			""";

		var prompt = LLMScene.GetPrompt();

		info += context;
		info += prompt;

		info.Replace( "{{char}}", card.data.name );
		info.Replace( "{{user}}", "John" );

		return info;
	}
	public string GetName()
	{
		return Card.data.name;
	}
	public bool IsUser()
	{
		return false;
	}
	public async Task Think()
	{
		Log.Info( $"{GetName()} is Thinking... (Last: {Memory.Last().Owner.GetName()})" );
		Memory.First().Content = BuildInfo( Card );

		var msgsAsAPIMsgs = Memory.Select( msg => msg.ConvertToAPIMessage() ).ToList();
		var response = await LanguageModel.GenerateFromMessages( msgsAsAPIMsgs );
		await LLMScene.Instance.RunCommandsInResponse( response, sender: this );
		var message = response.choices.First().message;

		LLMScene.Instance.BroadcastAudibleMessage( this, message.content, think: false );
	}
}
