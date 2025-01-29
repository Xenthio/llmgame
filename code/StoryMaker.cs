using LLMGame;


public class StoryMaker : SingletonComponent<StoryMaker>
{
	protected override void OnStart()
	{
		base.OnStart();
		AddPrompt();
	}
	void AddPrompt()
	{
		var prompt = """
			This is a story.
			""";
		LanguageModel.AddMessage( "System", prompt );
	}
}

