using System.Collections.Generic;
using System.Threading.Tasks;

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

public static class CloudLookup
{
	public static async Task<Model> GetModelFromName( string name )
	{
		string ident = "";
		if ( !DoOverrideCheck( name, out ident ) )
		{
			var results = await Package.FindAsync( $"{name} type:model" );
			var choice = Chooser( results.Packages, name );
			if ( choice == null ) return null;
			ident = choice.FullIdent;
		}

		var package = await Package.Fetch( ident, false );
		await package.MountAsync();
		var mdlname = package.GetMeta<string>( "PrimaryAsset" );
		Log.Info( $"Loading: {package.FullIdent} - {mdlname}" );

		var model = Model.Load( mdlname );
		return model;
	}
	public static async Task<Material> GetMaterialFromName( string name )
	{
		string ident = "";
		if ( !DoOverrideCheck( name, out ident ) )
		{
			var results = await Package.FindAsync( $"{name} type:material" );
			var choice = Chooser( results.Packages, name );
			if ( choice == null ) return null;
			ident = choice.FullIdent;
		}

		var package = await Package.Fetch( ident, false );
		await package.MountAsync();
		var mdlname = package.GetMeta<string>( "PrimaryAsset" );
		Log.Info( $"Loading: {package.FullIdent} - {mdlname}" );

		var material = Material.Load( mdlname );
		return material;
	}
	public static Package Chooser( Package[] allpackages, string name, bool prioritiseExact = false, bool prioritiseFacepunch = true )
	{
		var packages = allpackages;

		if ( prioritiseFacepunch && packages.Where( x => x.Org.Ident.ToUpper() == "FACEPUNCH" ).Any() )
		{
			packages = packages.Where( x => x.Org.Ident.ToUpper() == "FACEPUNCH" ).ToArray();
		}

		if ( prioritiseExact && packages.Where( x => x.Ident.ToUpper() == name.ToUpper() ).Any() )
		{
			packages = packages.Where( x => x.Ident.ToUpper() == name.ToUpper() ).ToArray();
		}

		if ( !packages.Any() ) return null;
		return packages.OrderBy( x => Game.Random.Int( 0, 1000 ) ).First();
	}
	// we can override specific queries to return a random selection from a curated collection of results

	private static readonly Dictionary<string, List<string>> lookupTable = new Dictionary<string, List<string>>
	{
		{ "lamp", new List<string> { "luke.plain_lamp", "smartmario.old_lamp", "luke.fwlamp", "tesssssssssst.untitled_14029841313", "thieves.joe_table_lamp", "fpopium.lamp_02_var" } },
		{ "desk lamp", new List<string> { "thieves.joe_table_lamp", "fpopium.lamp_02_var" } },
		{ "table lamp", new List<string> { "thieves.joe_table_lamp", "fpopium.lamp_02_var" } },
		{ "floor lamp", new List<string> { "luke.plain_lamp", "smartmario.old_lamp", "luke.fwlamp", "tesssssssssst.untitled_14029841313" } },
	};
	public static bool DoOverrideCheck( string name, out string ident )
	{
		ident = "error";
		if ( lookupTable.TryGetValue( name, out var list ) )
		{
			ident = list.OrderBy( x => Game.Random.Int( 0, 1000 ) ).First();
			return true;
		}
		return false;
	}
}
