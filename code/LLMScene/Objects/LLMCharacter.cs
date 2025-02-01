using Sandbox.Citizen;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMGame;

public partial class LLMCharacter : Component, ILLMBeing
{
	[ConVar( "llm_being_think_limit" )]
	public static float ThinkingLimit { get; set; } = 1.0f;

	[Property, ReadOnly] public List<Message> Memory { get; set; } = new();
	[Property] public string CardPath { get; set; } = "default_character.png";
	[Property] public CitizenAnimationHelper Animation { get; set; }
	[Property, ReadOnly] public CharacterCard Card { get; set; }
	public bool HasThoughtOnce = false;
	TimeSince TimeSinceLastThought;
	protected override void OnStart()
	{
		base.OnStart();
		InitialiseSpeech();
		Log.Info( $"{this} is now Loading" );
		Card = CharacterCard.LoadFromPNG( CardPath );

		Memory.Add( Message.System( BuildInfo( Card ) ) );
		if ( Card.data.group_only_greetings != null && Card.data.group_only_greetings.Any() )
		{
			LLMScene.Instance.BroadcastAudibleMessage( this, Card.data.group_only_greetings.OrderBy( x => Game.Random.Int( 0, 100 ) ).First() );
		}
	}
	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if ( IdleThinking ) DoIdleThinking();
		Animate();
		UpdateSpeech();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		UpdatePos();
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
			you can split your response with two new lines to run a command like so: 
			```
			"Where does that door go?"
			
			<lookat><target>door</target></lookat>"
			
			I tried to open it but it was locked."
			```

			# Look at Command
			<lookat><target>object</target></lookat> 
			
			- Look at an object or character in the room.

			# Walk to object Command
			<walkto><target>object</target></walkto> 

			- Walk to (and look at) an object or character in the room.
			
			# Walk to position Command
			<walkto><position>-2,4</position></walkto> 
			
			- Walk to a spot in the room.

			Commands are in XML format.

			### Immersive Chat
			Your job as assistant is be control {{char}} in this simulated world, use commands to do actions. Be creative and realistic. This is not a roleplay, use commands for actions and put speech in quotes.
			""";
	}

	public void IsBeingLookedAt( ILLMBeing looker )
	{
		if ( looker is Component lookercomponent )
		{
			LookAtObject( lookercomponent.GameObject );
		}
	}

	public string NearbyObjectsPrompt()
	{
		List<string> objs = new();

		foreach ( var obj in Scene.Components.GetAll<LLMCharacter>() )
		{
			if ( obj == this ) continue;
			var pos = obj.WorldPosition * LLMScene.INCH_2_METERS;
			objs.Add( $"<character><name>{obj.GetName()}</name><position>{pos.x},{pos.y}</position></character>" );
		}
		foreach ( var obj in Scene.Components.GetAll<LLMObject>() )
		{
			var pos = obj.WorldPosition * LLMScene.INCH_2_METERS;
			objs.Add( $"<object><name>{obj.Name}</name><position>{pos.x},{pos.y}</position></object>" );
		}


		var selfpos = WorldPosition * LLMScene.INCH_2_METERS;
		objs.Add( $"<yourself><name>{this.GetName()}</name><position>{selfpos.x},{selfpos.y}</position></yourself>" );

		return String.Join( '|', objs );
	}
	public async Task Think()
	{
		if ( !HasThoughtOnce ) HasThoughtOnce = true;
		if ( TimeSinceLastThought < ThinkingLimit ) return;
		TimeSinceLastThought = 0;
		Log.Info( $"{GetName()} is Thinking..." );
		Memory.First().Content = BuildInfo( Card );

		var response = await LLMScene.Instance.LanguageAPI.GenerateChatResponseFromMessages( Memory, replyAs: GetName() );
		var message = response.Messages.First();
		SpeakAndActMessage( message );
	}

	public async Task SpeakAndActMessage( Message message )
	{
		var split = message.Content.Split( "\n\n" );

		foreach ( var segment in split )
		{
			try
			{
				Log.Info( segment );
				var wascommand = await LLMScene.Instance.RunCommand( segment, this, false ); // wait until we finish our action
				if ( !wascommand )
				{
					LLMScene.Instance.BroadcastAudibleMessage( this, segment, think: false );
					await Speak( segment ); // wait until we finish talking
				}
			}
			catch ( System.Exception e )
			{
				Log.Error( e.Message );
			}
		}
	}
	private bool SpeechEnabled = true;
	private float[] morphs;
	private float[] morphVelocity;

	private static readonly string[] VisemeNames = new string[15]
	{
		"viseme_sil", "viseme_PP", "viseme_FF", "viseme_TH", "viseme_DD", "viseme_KK", "viseme_CH", "viseme_SS", "viseme_NN", "viseme_RR",
		"viseme_AA", "viseme_E", "viseme_I", "viseme_O", "viseme_U"
	};
	private float MorphScale = 1.0f;
	private float MorphSmoothTime = 0.1f;
	async void InitialiseSpeech()
	{
		SpeechEnabled = await LLMScene.Instance.SpeechAPI.IsReady();
		morphs = new float[BodyModelRenderer.Model.MorphCount];
		morphVelocity = new float[BodyModelRenderer.Model.MorphCount];
	}
	MusicPlayer CurrentSpeech;
	RealTimeSince LastPlayedSpeech;

	void UpdateSpeech()
	{
		ApplyVisemes();
		if ( CurrentSpeech != null ) CurrentSpeech.Position = Eye.WorldPosition;
		FadeMorphs();
	}
	void ApplyVisemes()
	{
		if ( CurrentSpeech == null || !BodyModelRenderer.IsValid() || morphs == null )
		{
			return;
		}

		Model model = BodyModelRenderer.Model;
		if ( model == null )
		{
			return;
		}

		int morphCount = model.MorphCount;
		if ( morphCount == 0 || morphCount != morphs.Length )
		{
			return;
		}

		IReadOnlyList<float> visemes = CurrentSpeech.Visemes;
		if ( visemes == null )
		{
			return;
		}

		for ( int i = 0; i < morphCount; i++ )
		{
			float num = 0f;
			for ( int j = 0; j < visemes.Count; j++ )
			{
				float visemeMorph = model.GetVisemeMorph( VisemeNames[j], i );
				num += visemeMorph * visemes[j];
			}

			morphs[i] = (num * MorphScale).Clamp( 0f, 1f );
		}
	}
	private void FadeMorphs()
	{
		if ( !BodyModelRenderer.IsValid() || morphs == null )
		{
			return;
		}

		Model model = BodyModelRenderer.Model;
		if ( model == null )
		{
			return;
		}

		int morphCount = model.MorphCount;
		if ( morphCount == 0 )
		{
			return;
		}

		if ( morphCount != morphs.Length )
		{
			morphs = new float[morphCount];
			morphVelocity = new float[morphCount];
		}

		SceneModel sceneModel = BodyModelRenderer.SceneModel;
		if ( !sceneModel.IsValid() )
		{
			return;
		}

		RealTimeSince ts = LastPlayedSpeech;
		if ( !(ts > 1f) )
		{
			for ( int i = 0; i < morphCount; i++ )
			{
				float current = sceneModel.Morphs.Get( i );
				ts = LastPlayedSpeech;
				float target = ((ts < 0.2f) ? morphs[i] : 0f);
				current = MathX.SmoothDamp( current, target, ref morphVelocity[i], MorphSmoothTime, Time.Delta );
				sceneModel.Morphs.Set( i, Math.Max( 0f, current ) );
			}
		}
	}

	void _speechStopped()
	{
		Speaking = false;
	}
	bool Speaking = false;
	public async Task<bool> Speak( string content )
	{
		if ( !SpeechEnabled ) return true;
		var url = await LLMScene.Instance.SpeechAPI.GenerateSpeechFromText( content, GetName() );
		Log.Info( $"speaking... {url}" );
		Speaking = true;
		CurrentSpeech = MusicPlayer.PlayUrl( url );
		CurrentSpeech.LipSync = true;
		CurrentSpeech.Position = Eye.WorldPosition;
		CurrentSpeech.OnFinished += _speechStopped;
		LastPlayedSpeech = 0f;
		Log.Info( CurrentSpeech.Duration );
		while ( Speaking )
		{
			await Task.Delay( 100 );
		}
		return true;
	}


}
