using System.Collections.Generic;

namespace LLMGame;

public class CharacterAI : Component, IThinker
{
	[Property, ReadOnly] public List<Message> Memory { get; set; } = new();
	[Property] public string CardPath { get; set; } = "default_character.png";
	[Property, ReadOnly] public CharacterCard Card { get; set; }
	protected override void OnStart()
	{
		base.OnStart();
		Card = CharacterCard.LoadFromPNG( CardPath );

		Memory.Add( Message.System( BuildInfo( Card ) ) );
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

	public void Think()
	{
	}
}
