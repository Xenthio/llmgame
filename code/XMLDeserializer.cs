using System;
using System.Collections.Generic;

public static class XmlDeserializer
{
	[ConVar( "xml_debug" )] public static bool DebugSerialisation { get; set; } = false;
	public static T Deserialize<T>( string xml )//, T anonymousTypeTemplate )
	{
		if ( DebugSerialisation ) Log.Info( $"Deserialising {xml}..." );
		xml = xml.Trim().Replace( "\n", "" ).Replace( "\r", "" );

		var typedesc = TypeLibrary.GetType<T>();

		var properties = typedesc.Properties;

		var propertyValues = new Dictionary<string, object>();

		// Parse XML and extract values
		foreach ( var property in properties )
		{
			// wtf lua brain bs was this
			//var ty = anonymousTypeTemplate[property.Name];

			// previously wanted to use anonymous types but that doesn't work properly with s&box's reflection
			// worked with system.reflection
			//var type = typedesc.Ge( anonymousTypeTemplate, property.Name );

			if ( DebugSerialisation ) Log.Info( $"Deserialising {property.Name} into {property.PropertyType}..." );

			string propertyName = property.Name;
			string startTag = $"<{propertyName}>";
			string endTag = $"</{propertyName}>";

			int startIndex = xml.IndexOf( startTag ) + startTag.Length;
			int endIndex = xml.IndexOf( endTag );

			if ( startIndex >= 0 && endIndex >= 0 )
			{
				string value = xml.Substring( startIndex, endIndex - startIndex );
				object convertedValue = ConvertValue( value, property.PropertyType );
				propertyValues[propertyName] = convertedValue;
			}
		}

		var c = typedesc.Create<T>( null );

		foreach ( var value in propertyValues )
		{
			typedesc.SetValue( c, value.Key, value.Value );
		}
		return (T)c;
	}

	private static object ConvertValue( string value, Type targetType )
	{
		if ( targetType == typeof( string ) )
			return value;
		else if ( targetType == typeof( int ) )
			return int.Parse( value );
		else if ( targetType == typeof( double ) )
			return double.Parse( value );
		else if ( targetType == typeof( bool ) )
			return bool.Parse( value );
		else if ( targetType == typeof( DateTime ) )
			return DateTime.Parse( value );
		else if ( targetType.IsEnum )
			return Enum.Parse( targetType, value );

		Log.Error( $"Type {targetType.Name} is not supported for conversion." );
		return value;
	}
}
