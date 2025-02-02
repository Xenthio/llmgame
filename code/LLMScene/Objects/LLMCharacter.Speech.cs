using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMGame;

public partial class LLMCharacter
{

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
