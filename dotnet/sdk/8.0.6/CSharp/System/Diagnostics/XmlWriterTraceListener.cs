using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace System.Diagnostics;

public class XmlWriterTraceListener : TextWriterTraceListener
{
	private static volatile string s_processName;

	private readonly string _machineName = Environment.MachineName;

	private StringBuilder _strBldr;

	private XmlTextWriter _xmlBlobWriter;

	public XmlWriterTraceListener(Stream stream)
		: base(stream)
	{
	}

	public XmlWriterTraceListener(Stream stream, string? name)
		: base(stream, name)
	{
	}

	public XmlWriterTraceListener(TextWriter writer)
		: base(writer)
	{
	}

	public XmlWriterTraceListener(TextWriter writer, string? name)
		: base(writer, name)
	{
	}

	public XmlWriterTraceListener(string? filename)
		: base(filename)
	{
	}

	public XmlWriterTraceListener(string? filename, string? name)
		: base(filename, name)
	{
	}

	public override void Write(string? message)
	{
		WriteLine(message);
	}

	public override void WriteLine(string? message)
	{
		TraceEvent(null, System.SR.TraceAsTraceSource, TraceEventType.Information, 0, message);
	}

	public override void Fail(string? message, string? detailMessage)
	{
		if (message == null)
		{
			message = string.Empty;
		}
		int length = ((detailMessage != null) ? (message.Length + 1 + detailMessage.Length) : message.Length);
		TraceEvent(null, System.SR.TraceAsTraceSource, TraceEventType.Error, 0, string.Create(length, (message, detailMessage), delegate(Span<char> dst, (string message, string detailMessage) v)
		{
			var (text, _) = v;
			text.CopyTo(dst);
			if (v.detailMessage != null)
			{
				dst[text.Length] = ' ';
				string item = v.detailMessage;
				item.CopyTo(dst.Slice(text.Length + 1, item.Length));
			}
		}));
	}

	public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, [StringSyntax("CompositeFormat")] string? format, params object?[]? args)
	{
		if (base.Filter == null || base.Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
		{
			WriteHeader(source, eventType, id, eventCache);
			WriteEscaped((args != null && args.Length != 0) ? string.Format(CultureInfo.InvariantCulture, format, args) : format);
			WriteFooter(eventCache);
		}
	}

	public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
	{
		if (base.Filter == null || base.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
		{
			WriteHeader(source, eventType, id, eventCache);
			WriteEscaped(message);
			WriteFooter(eventCache);
		}
	}

	public override void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, object? data)
	{
		if (base.Filter == null || base.Filter.ShouldTrace(eventCache, source, eventType, id, null, null, data, null))
		{
			WriteHeader(source, eventType, id, eventCache);
			InternalWrite("<TraceData>");
			if (data != null)
			{
				InternalWrite("<DataItem>");
				WriteData(data);
				InternalWrite("</DataItem>");
			}
			InternalWrite("</TraceData>");
			WriteFooter(eventCache);
		}
	}

	public override void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, params object?[]? data)
	{
		if (base.Filter != null && !base.Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data))
		{
			return;
		}
		WriteHeader(source, eventType, id, eventCache);
		InternalWrite("<TraceData>");
		if (data != null)
		{
			for (int i = 0; i < data.Length; i++)
			{
				InternalWrite("<DataItem>");
				if (data[i] != null)
				{
					WriteData(data[i]);
				}
				InternalWrite("</DataItem>");
			}
		}
		InternalWrite("</TraceData>");
		WriteFooter(eventCache);
	}

	private void WriteData(object data)
	{
		if (!(data is XPathNavigator xPathNavigator))
		{
			WriteEscaped(data.ToString());
			return;
		}
		if (_strBldr == null)
		{
			_strBldr = new StringBuilder();
			_xmlBlobWriter = new XmlTextWriter(new StringWriter(_strBldr, CultureInfo.CurrentCulture));
		}
		else
		{
			_strBldr.Length = 0;
		}
		try
		{
			xPathNavigator.MoveToRoot();
			_xmlBlobWriter.WriteNode(xPathNavigator, defattr: false);
			InternalWrite(_strBldr);
		}
		catch (Exception)
		{
			InternalWrite(data.ToString());
		}
	}

	public override void Close()
	{
		base.Close();
		_xmlBlobWriter?.Close();
		_xmlBlobWriter = null;
		_strBldr = null;
	}

	public override void TraceTransfer(TraceEventCache? eventCache, string source, int id, string? message, Guid relatedActivityId)
	{
		if (base.Filter == null || base.Filter.ShouldTrace(eventCache, source, TraceEventType.Transfer, id, message, null, null, null))
		{
			WriteHeader(source, TraceEventType.Transfer, id, eventCache, relatedActivityId);
			WriteEscaped(message);
			WriteFooter(eventCache);
		}
	}

	private void WriteHeader(string source, TraceEventType eventType, int id, TraceEventCache eventCache, Guid relatedActivityId)
	{
		WriteStartHeader(source, eventType, id, eventCache);
		InternalWrite("\" RelatedActivityID=\"");
		InternalWrite(relatedActivityId);
		WriteEndHeader();
	}

	private void WriteHeader(string source, TraceEventType eventType, int id, TraceEventCache eventCache)
	{
		WriteStartHeader(source, eventType, id, eventCache);
		WriteEndHeader();
	}

	private void WriteStartHeader(string source, TraceEventType eventType, int id, TraceEventCache eventCache)
	{
		InternalWrite("<E2ETraceEvent xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\"><System xmlns=\"http://schemas.microsoft.com/2004/06/windows/eventlog/system\">");
		InternalWrite("<EventID>");
		InternalWrite((uint)id);
		InternalWrite("</EventID>");
		InternalWrite("<Type>3</Type>");
		InternalWrite("<SubType Name=\"");
		InternalWrite(eventType.ToString());
		InternalWrite("\">0</SubType>");
		InternalWrite("<Level>");
		InternalWrite(Math.Clamp((int)eventType, 0, 255));
		InternalWrite("</Level>");
		InternalWrite("<TimeCreated SystemTime=\"");
		InternalWrite(eventCache?.DateTime ?? DateTime.Now);
		InternalWrite("\" />");
		InternalWrite("<Source Name=\"");
		WriteEscaped(source);
		InternalWrite("\" />");
		InternalWrite("<Correlation ActivityID=\"");
		InternalWrite((eventCache != null) ? Trace.CorrelationManager.ActivityId : Guid.Empty);
	}

	private void WriteEndHeader()
	{
		string text = s_processName;
		if (text == null)
		{
			if (OperatingSystem.IsBrowser())
			{
				text = (s_processName = string.Empty);
			}
			else
			{
				using Process process = Process.GetCurrentProcess();
				text = (s_processName = process.ProcessName);
			}
		}
		InternalWrite("\" />");
		InternalWrite("<Execution ProcessName=\"");
		InternalWrite(text);
		InternalWrite("\" ProcessID=\"");
		InternalWrite((uint)Environment.ProcessId);
		InternalWrite("\" ThreadID=\"");
		InternalWrite((uint)Environment.CurrentManagedThreadId);
		InternalWrite("\" />");
		InternalWrite("<Channel/>");
		InternalWrite("<Computer>");
		InternalWrite(_machineName);
		InternalWrite("</Computer>");
		InternalWrite("</System>");
		InternalWrite("<ApplicationData>");
	}

	private void WriteFooter(TraceEventCache eventCache)
	{
		if (eventCache != null)
		{
			bool flag = IsEnabled(TraceOptions.LogicalOperationStack);
			bool flag2 = IsEnabled(TraceOptions.Callstack);
			if (flag || flag2)
			{
				InternalWrite("<System.Diagnostics xmlns=\"http://schemas.microsoft.com/2004/08/System.Diagnostics\">");
				if (flag)
				{
					InternalWrite("<LogicalOperationStack>");
					foreach (object item in eventCache.LogicalOperationStack)
					{
						InternalWrite("<LogicalOperation>");
						WriteEscaped(item?.ToString());
						InternalWrite("</LogicalOperation>");
					}
					InternalWrite("</LogicalOperationStack>");
				}
				InternalWrite("<Timestamp>");
				InternalWrite(eventCache.Timestamp);
				InternalWrite("</Timestamp>");
				if (flag2)
				{
					InternalWrite("<Callstack>");
					WriteEscaped(eventCache.Callstack);
					InternalWrite("</Callstack>");
				}
				InternalWrite("</System.Diagnostics>");
			}
		}
		InternalWrite("</ApplicationData></E2ETraceEvent>");
	}

	private void WriteEscaped(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < str.Length; i++)
		{
			switch (str[i])
			{
			case '&':
				InternalWrite(str.AsSpan(num, i - num));
				InternalWrite("&amp;");
				num = i + 1;
				break;
			case '<':
				InternalWrite(str.AsSpan(num, i - num));
				InternalWrite("&lt;");
				num = i + 1;
				break;
			case '>':
				InternalWrite(str.AsSpan(num, i - num));
				InternalWrite("&gt;");
				num = i + 1;
				break;
			case '"':
				InternalWrite(str.AsSpan(num, i - num));
				InternalWrite("&quot;");
				num = i + 1;
				break;
			case '\'':
				InternalWrite(str.AsSpan(num, i - num));
				InternalWrite("&apos;");
				num = i + 1;
				break;
			case '\r':
				InternalWrite(str.AsSpan(num, i - num));
				InternalWrite("&#xD;");
				num = i + 1;
				break;
			case '\n':
				InternalWrite(str.AsSpan(num, i - num));
				InternalWrite("&#xA;");
				num = i + 1;
				break;
			}
		}
		InternalWrite(str.AsSpan(num, str.Length - num));
	}

	private void InternalWrite(string message)
	{
		EnsureWriter();
		_writer?.Write(message);
	}

	private void InternalWrite(ReadOnlySpan<char> message)
	{
		EnsureWriter();
		_writer?.Write(message);
	}

	private void InternalWrite<T>(T message) where T : ISpanFormattable?
	{
		EnsureWriter();
		TextWriter writer = _writer;
		if (writer != null)
		{
			Span<char> destination = stackalloc char[20];
			ref T reference = ref message;
			T val = default(T);
			if (val == null)
			{
				val = reference;
				reference = ref val;
			}
			reference.TryFormat(destination, out var charsWritten, default(ReadOnlySpan<char>), CultureInfo.InvariantCulture);
			writer.Write(destination.Slice(0, charsWritten));
		}
	}

	private void InternalWrite(Guid message)
	{
		EnsureWriter();
		TextWriter writer = _writer;
		if (writer != null)
		{
			Span<char> span = stackalloc char[38];
			message.TryFormat(span, out var _, "B");
			writer.Write(span);
		}
	}

	private void InternalWrite(DateTime message)
	{
		EnsureWriter();
		TextWriter writer = _writer;
		if (writer != null)
		{
			Span<char> destination = stackalloc char[33];
			message.TryFormat(destination, out var charsWritten, "o");
			writer.Write(destination.Slice(0, charsWritten));
		}
	}

	private void InternalWrite(StringBuilder message)
	{
		EnsureWriter();
		TextWriter writer = _writer;
		if (writer != null)
		{
			StringBuilder.ChunkEnumerator enumerator = message.GetChunks().GetEnumerator();
			while (enumerator.MoveNext())
			{
				writer.Write(enumerator.Current.Span);
			}
		}
	}
}
