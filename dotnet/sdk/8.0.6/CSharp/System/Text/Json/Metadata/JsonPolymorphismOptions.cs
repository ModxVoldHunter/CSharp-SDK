using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Text.Json.Serialization.Metadata;

public class JsonPolymorphismOptions
{
	private sealed class DerivedTypeList : ConfigurationList<JsonDerivedType>
	{
		private readonly JsonPolymorphismOptions _parent;

		public override bool IsReadOnly => _parent.DeclaringTypeInfo?.IsReadOnly ?? false;

		public DerivedTypeList(JsonPolymorphismOptions parent)
			: base((IEnumerable<JsonDerivedType>)null)
		{
			_parent = parent;
		}

		protected override void OnCollectionModifying()
		{
			_parent.DeclaringTypeInfo?.VerifyMutable();
		}
	}

	private DerivedTypeList _derivedTypes;

	private bool _ignoreUnrecognizedTypeDiscriminators;

	private JsonUnknownDerivedTypeHandling _unknownDerivedTypeHandling;

	private string _typeDiscriminatorPropertyName;

	public IList<JsonDerivedType> DerivedTypes => _derivedTypes ?? (_derivedTypes = new DerivedTypeList(this));

	public bool IgnoreUnrecognizedTypeDiscriminators
	{
		get
		{
			return _ignoreUnrecognizedTypeDiscriminators;
		}
		set
		{
			VerifyMutable();
			_ignoreUnrecognizedTypeDiscriminators = value;
		}
	}

	public JsonUnknownDerivedTypeHandling UnknownDerivedTypeHandling
	{
		get
		{
			return _unknownDerivedTypeHandling;
		}
		set
		{
			VerifyMutable();
			_unknownDerivedTypeHandling = value;
		}
	}

	public string TypeDiscriminatorPropertyName
	{
		get
		{
			return _typeDiscriminatorPropertyName ?? "$type";
		}
		[param: AllowNull]
		set
		{
			VerifyMutable();
			_typeDiscriminatorPropertyName = value;
		}
	}

	internal JsonTypeInfo? DeclaringTypeInfo { get; set; }

	private void VerifyMutable()
	{
		DeclaringTypeInfo?.VerifyMutable();
	}

	internal static JsonPolymorphismOptions CreateFromAttributeDeclarations(Type baseType)
	{
		JsonPolymorphismOptions jsonPolymorphismOptions = null;
		JsonPolymorphicAttribute customAttribute = baseType.GetCustomAttribute<JsonPolymorphicAttribute>(inherit: false);
		if (customAttribute != null)
		{
			jsonPolymorphismOptions = new JsonPolymorphismOptions
			{
				IgnoreUnrecognizedTypeDiscriminators = customAttribute.IgnoreUnrecognizedTypeDiscriminators,
				UnknownDerivedTypeHandling = customAttribute.UnknownDerivedTypeHandling,
				TypeDiscriminatorPropertyName = customAttribute.TypeDiscriminatorPropertyName
			};
		}
		foreach (JsonDerivedTypeAttribute customAttribute2 in baseType.GetCustomAttributes<JsonDerivedTypeAttribute>(inherit: false))
		{
			(jsonPolymorphismOptions ?? (jsonPolymorphismOptions = new JsonPolymorphismOptions())).DerivedTypes.Add(new JsonDerivedType(customAttribute2.DerivedType, customAttribute2.TypeDiscriminator));
		}
		return jsonPolymorphismOptions;
	}
}
