using System.Collections;
using System.Collections.Generic;

namespace System.Xml.Schema;

internal sealed class AllElementsContentValidator : ContentValidator
{
	private readonly Dictionary<XmlQualifiedName, int> _elements;

	private readonly object[] _particles;

	private readonly BitSet _isRequired;

	private int _countRequired;

	public override bool IsEmptiable
	{
		get
		{
			if (!base.IsEmptiable)
			{
				return _countRequired == 0;
			}
			return true;
		}
	}

	public AllElementsContentValidator(XmlSchemaContentType contentType, int size, bool isEmptiable)
		: base(contentType, isOpen: false, isEmptiable)
	{
		_elements = new Dictionary<XmlQualifiedName, int>(size);
		_particles = new object[size];
		_isRequired = new BitSet(size);
	}

	public bool AddElement(XmlQualifiedName name, object particle, bool isEmptiable)
	{
		int count = _elements.Count;
		if (_elements.TryAdd(name, count))
		{
			_particles[count] = particle;
			if (!isEmptiable)
			{
				_isRequired.Set(count);
				_countRequired++;
			}
			return true;
		}
		return false;
	}

	public override void InitValidation(ValidationState context)
	{
		context.AllElementsSet = new BitSet(_elements.Count);
		context.CurrentState.AllElementsRequired = -1;
	}

	public override object ValidateElement(XmlQualifiedName name, ValidationState context, out int errorCode)
	{
		errorCode = 0;
		if (!_elements.TryGetValue(name, out var value))
		{
			context.NeedValidateChildren = false;
			return null;
		}
		if (context.AllElementsSet[value])
		{
			errorCode = -2;
			return null;
		}
		if (context.CurrentState.AllElementsRequired == -1)
		{
			context.CurrentState.AllElementsRequired = 0;
		}
		context.AllElementsSet.Set(value);
		if (_isRequired[value])
		{
			context.CurrentState.AllElementsRequired++;
		}
		return _particles[value];
	}

	public override bool CompleteValidation(ValidationState context)
	{
		if (context.CurrentState.AllElementsRequired == _countRequired || (IsEmptiable && context.CurrentState.AllElementsRequired == -1))
		{
			return true;
		}
		return false;
	}

	public override ArrayList ExpectedElements(ValidationState context, bool isRequiredOnly)
	{
		ArrayList arrayList = null;
		foreach (KeyValuePair<XmlQualifiedName, int> element in _elements)
		{
			if (!context.AllElementsSet[element.Value] && (!isRequiredOnly || _isRequired[element.Value]))
			{
				if (arrayList == null)
				{
					arrayList = new ArrayList();
				}
				arrayList.Add(element.Key);
			}
		}
		return arrayList;
	}

	public override ArrayList ExpectedParticles(ValidationState context, bool isRequiredOnly, XmlSchemaSet schemaSet)
	{
		ArrayList arrayList = new ArrayList();
		foreach (KeyValuePair<XmlQualifiedName, int> element in _elements)
		{
			if (!context.AllElementsSet[element.Value] && (!isRequiredOnly || _isRequired[element.Value]))
			{
				ContentValidator.AddParticleToExpected(_particles[element.Value] as XmlSchemaParticle, schemaSet, arrayList);
			}
		}
		return arrayList;
	}
}
