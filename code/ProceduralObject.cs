namespace LLMGame;

public sealed class ProceduralObject : Component
{
	[Property] public string Name { get; set; } = "Cup";
	[RequireComponent] public Prop Prop { get; set; }
	protected override void OnStart()
	{
		base.OnStart();
		Prop.Enabled = false;
		LoadModel();
	}
	public async void LoadModel()
	{
		var mdl = await CloudLookup.GetModelFromName( Name );
		Prop.Model = mdl;
		Prop.Enabled = true;
	}
}
