using System;
using System.Collections.Generic;

public static class XmlAnonymousDeserializer
{
	public static T Deserialize<T>( string xml, T anonymousTypeTemplate )
	{
		Log.Info( $"Deserialising {xml}..." );
		// Remove whitespace and newlines
		xml = xml.Trim().Replace( "\n", "" ).Replace( "\r", "" );

		var typedesc = TypeLibrary.GetType<T>();

		// Get the properties of the anonymous type
		var properties = typedesc.Properties;

		// Create a dictionary to store property values
		var propertyValues = new Dictionary<string, object>();

		// Parse XML and extract values
		foreach ( var property in properties )
		{
			// wtf lua brain bs was this
			//var ty = anonymousTypeTemplate[property.Name];

			//var type = typedesc.Ge( anonymousTypeTemplate, property.Name );
			Log.Info( $"Deserialising {property.Name} into {property.PropertyType}..." );
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

		// Create new instance of anonymous type with extracted values
		//var constructorInfo = anonymousTypeTemplate.GetType().GetConstructors()[0];
		//var constructorInfo = TypeLibrary.GetType(anonymousTypeTemplate.GetType()).Create
		//var parameters = constructorInfo.GetParameters();
		//var constructorArgs = parameters.Select( p => propertyValues[p.Name] ).ToArray();

		//return (T)constructorInfo.Invoke( constructorArgs );

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
