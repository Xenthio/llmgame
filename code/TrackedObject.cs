namespace LLMGame;

public sealed class TrackedObject : Component
{
	[Property] public string Name { get; set; } = "Cup";
	[RequireComponent] public ModelRenderer Model { get; set; }
	protected override void OnStart()
	{
		base.OnStart();
		if ( Name == null )
		{
			Name = Model.Model.Name;
		}
	}
}
