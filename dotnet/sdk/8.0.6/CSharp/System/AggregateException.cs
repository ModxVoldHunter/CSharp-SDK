using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Text;

namespace System;

[Serializable]
[DebuggerDisplay("Count = {InnerExceptionCount}")]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class AggregateException : Exception
{
	private readonly Exception[] _innerExceptions;

	private ReadOnlyCollection<Exception> _rocView;

	public ReadOnlyCollection<Exception> InnerExceptions => _rocView ?? (_rocView = new ReadOnlyCollection<Exception>(_innerExceptions));

	public override string Message
	{
		get
		{
			if (_innerExceptions.Length == 0)
			{
				return base.Message;
			}
			Span<char> initialBuffer = stackalloc char[256];
			ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
			valueStringBuilder.Append(base.Message);
			valueStringBuilder.Append(' ');
			for (int i = 0; i < _innerExceptions.Length; i++)
			{
				valueStringBuilder.Append('(');
				valueStringBuilder.Append(_innerExceptions[i].Message);
				valueStringBuilder.Append(") ");
			}
			valueStringBuilder.Length--;
			return valueStringBuilder.ToString();
		}
	}

	internal int InnerExceptionCount => _innerExceptions.Length;

	internal Exception[] InternalInnerExceptions => _innerExceptions;

	public AggregateException()
		: this(SR.AggregateException_ctor_DefaultMessage)
	{
	}

	public AggregateException(string? message)
		: base(message)
	{
		_innerExceptions = Array.Empty<Exception>();
	}

	public AggregateException(string? message, Exception innerException)
		: base(message, innerException)
	{
		ArgumentNullException.ThrowIfNull(innerException, "innerException");
		_innerExceptions = new Exception[1] { innerException };
	}

	public AggregateException(IEnumerable<Exception> innerExceptions)
		: this(SR.AggregateException_ctor_DefaultMessage, innerExceptions ?? throw new ArgumentNullException("innerExceptions"))
	{
	}

	public AggregateException(params Exception[] innerExceptions)
		: this(SR.AggregateException_ctor_DefaultMessage, innerExceptions ?? throw new ArgumentNullException("innerExceptions"))
	{
	}

	public AggregateException(string? message, IEnumerable<Exception> innerExceptions)
		: this(message, new List<Exception>(innerExceptions ?? throw new ArgumentNullException("innerExceptions")).ToArray(), cloneExceptions: false)
	{
	}

	public AggregateException(string? message, params Exception[] innerExceptions)
		: this(message, innerExceptions ?? throw new ArgumentNullException("innerExceptions"), cloneExceptions: true)
	{
	}

	private AggregateException(string message, Exception[] innerExceptions, bool cloneExceptions)
		: base(message, (innerExceptions.Length != 0) ? innerExceptions[0] : null)
	{
		_innerExceptions = (cloneExceptions ? new Exception[innerExceptions.Length] : innerExceptions);
		for (int i = 0; i < _innerExceptions.Length; i++)
		{
			_innerExceptions[i] = innerExceptions[i];
			if (innerExceptions[i] == null)
			{
				throw new ArgumentException(SR.AggregateException_ctor_InnerExceptionNull);
			}
		}
	}

	internal AggregateException(List<ExceptionDispatchInfo> innerExceptionInfos)
		: this(SR.AggregateException_ctor_DefaultMessage, innerExceptionInfos)
	{
	}

	internal AggregateException(string message, List<ExceptionDispatchInfo> innerExceptionInfos)
		: base(message, (innerExceptionInfos.Count != 0) ? innerExceptionInfos[0].SourceException : null)
	{
		_innerExceptions = new Exception[innerExceptionInfos.Count];
		for (int i = 0; i < _innerExceptions.Length; i++)
		{
			_innerExceptions[i] = innerExceptionInfos[i].SourceException;
		}
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected AggregateException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		if (!(info.GetValue("InnerExceptions", typeof(Exception[])) is Exception[] innerExceptions))
		{
			throw new SerializationException(SR.AggregateException_DeserializationFailure);
		}
		_innerExceptions = innerExceptions;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("InnerExceptions", _innerExceptions, typeof(Exception[]));
	}

	public override Exception GetBaseException()
	{
		Exception ex = this;
		AggregateException ex2 = this;
		while (ex2 != null && ex2.InnerExceptions.Count == 1)
		{
			ex = ex.InnerException;
			ex2 = ex as AggregateException;
		}
		return ex;
	}

	public void Handle(Func<Exception, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate, "predicate");
		List<Exception> list = null;
		for (int i = 0; i < _innerExceptions.Length; i++)
		{
			if (!predicate(_innerExceptions[i]))
			{
				if (list == null)
				{
					list = new List<Exception>();
				}
				list.Add(_innerExceptions[i]);
			}
		}
		if (list != null)
		{
			throw new AggregateException(Message, list.ToArray(), cloneExceptions: false);
		}
	}

	public AggregateException Flatten()
	{
		List<Exception> list = new List<Exception>();
		List<AggregateException> list2 = new List<AggregateException> { this };
		int num = 0;
		while (list2.Count > num)
		{
			ReadOnlyCollection<Exception> innerExceptions = list2[num++].InnerExceptions;
			for (int i = 0; i < innerExceptions.Count; i++)
			{
				Exception ex = innerExceptions[i];
				if (ex != null)
				{
					if (ex is AggregateException item)
					{
						list2.Add(item);
					}
					else
					{
						list.Add(ex);
					}
				}
			}
		}
		return new AggregateException((GetType() == typeof(AggregateException)) ? base.Message : Message, list.ToArray(), cloneExceptions: false);
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(base.ToString());
		for (int i = 0; i < _innerExceptions.Length; i++)
		{
			if (_innerExceptions[i] != base.InnerException)
			{
				stringBuilder.Append("\r\n ---> ");
				stringBuilder.AppendFormat(CultureInfo.InvariantCulture, SR.AggregateException_InnerException, i);
				stringBuilder.Append(_innerExceptions[i].ToString());
				stringBuilder.Append("<---");
				stringBuilder.AppendLine();
			}
		}
		return stringBuilder.ToString();
	}
}
