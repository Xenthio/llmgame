namespace LLMGame;

public sealed class LLMObject : Component
{
	[Property] public string Name { get; set; } = "Cup";
	[Property] public ModelRenderer Model { get; set; }
	protected override void OnStart()
	{
		base.OnStart();
		if ( Name == null )
		{
			Name = Model.Model.Name;
		}
	}
}
