using Sandbox.Citizen;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMGame;

public class LLMCharacter : Component, ILLMBeing
{
	[Property, ReadOnly] public List<Message> Memory { get; set; } = new();
	[Property] public string CardPath { get; set; } = "default_character.png";
	[Property] public CitizenAnimationHelper Animation { get; set; }
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

	public string BuildInfo( CharacterCard card )
	{
		string info = "";
		var context = $"""
			{card.data.description}
			{card.data.personality}
			{card.data.scenario} 
			
			""";

		var prompt = GetPrompt();

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
	public string GetPrompt()
	{
		var nearby = $"""
			### Nearby things
			{NearbyObjectsPrompt()}

			- Positions are top down in meters.

			""";
		return nearby + """ 

			### Commands 
			you can split your response with | to run a command (like "blah blah blah|<lookat>door</lookat>|blah blah blah blah")

			# Look at Command
			<lookat>object</lookat> - Look at an object or character in the room.

			Commands are in XML format.

			### Immersive Chat
			Your job as assistant is to control {{char}} in this simulated world scenario, use commands to do actions. Be creative and realistic.
			""";
	}

	public string NearbyObjectsPrompt()
	{
		List<string> objs = new();

		foreach ( var obj in Scene.Components.GetAll<LLMCharacter>() )
		{
			var pos = obj.WorldPosition * LLMScene.INCH_2_METERS;
			objs.Add( $"<character><name>{obj.GetName()}</name><position>{pos.x},{pos.y}</position></object>" );
		}
		foreach ( var obj in Scene.Components.GetAll<LLMObject>() )
		{
			var pos = obj.WorldPosition * LLMScene.INCH_2_METERS;
			objs.Add( $"<object><name>{obj.Name}</name><position>{pos.x},{pos.y}</position></object>" );
		}

		return String.Join( '|', objs );
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
