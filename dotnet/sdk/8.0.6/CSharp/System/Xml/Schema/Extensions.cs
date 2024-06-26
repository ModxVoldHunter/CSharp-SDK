using System.Xml.Linq;

namespace System.Xml.Schema;

public static class Extensions
{
	public static IXmlSchemaInfo? GetSchemaInfo(this XElement source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Annotation<IXmlSchemaInfo>();
	}

	public static IXmlSchemaInfo? GetSchemaInfo(this XAttribute source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return source.Annotation<IXmlSchemaInfo>();
	}

	public static void Validate(this XDocument source, XmlSchemaSet schemas, ValidationEventHandler? validationEventHandler)
	{
		source.Validate(schemas, validationEventHandler, addSchemaInfo: false);
	}

	public static void Validate(this XDocument source, XmlSchemaSet schemas, ValidationEventHandler? validationEventHandler, bool addSchemaInfo)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(schemas, "schemas");
		new XNodeValidator(schemas, validationEventHandler).Validate(source, null, addSchemaInfo);
	}

	public static void Validate(this XElement source, XmlSchemaObject partialValidationType, XmlSchemaSet schemas, ValidationEventHandler? validationEventHandler)
	{
		source.Validate(partialValidationType, schemas, validationEventHandler, addSchemaInfo: false);
	}

	public static void Validate(this XElement source, XmlSchemaObject partialValidationType, XmlSchemaSet schemas, ValidationEventHandler? validationEventHandler, bool addSchemaInfo)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(partialValidationType, "partialValidationType");
		ArgumentNullException.ThrowIfNull(schemas, "schemas");
		new XNodeValidator(schemas, validationEventHandler).Validate(source, partialValidationType, addSchemaInfo);
	}

	public static void Validate(this XAttribute source, XmlSchemaObject partialValidationType, XmlSchemaSet schemas, ValidationEventHandler? validationEventHandler)
	{
		source.Validate(partialValidationType, schemas, validationEventHandler, addSchemaInfo: false);
	}

	public static void Validate(this XAttribute source, XmlSchemaObject partialValidationType, XmlSchemaSet schemas, ValidationEventHandler? validationEventHandler, bool addSchemaInfo)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(partialValidationType, "partialValidationType");
		ArgumentNullException.ThrowIfNull(schemas, "schemas");
		new XNodeValidator(schemas, validationEventHandler).Validate(source, partialValidationType, addSchemaInfo);
	}
}
