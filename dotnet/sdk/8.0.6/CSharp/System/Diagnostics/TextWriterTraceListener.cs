using System.IO;
using System.Text;

namespace System.Diagnostics;

public class TextWriterTraceListener : TraceListener
{
	internal TextWriter _writer;

	private string _fileName;

	public TextWriter? Writer
	{
		get
		{
			EnsureWriter();
			return _writer;
		}
		set
		{
			_writer = value;
		}
	}

	public TextWriterTraceListener()
	{
	}

	public TextWriterTraceListener(Stream stream)
		: this(stream, string.Empty)
	{
	}

	public TextWriterTraceListener(Stream stream, string? name)
		: base(name)
	{
		ArgumentNullException.ThrowIfNull(stream, "stream");
		_writer = new StreamWriter(stream);
	}

	public TextWriterTraceListener(TextWriter writer)
		: this(writer, string.Empty)
	{
	}

	public TextWriterTraceListener(TextWriter writer, string? name)
		: base(name)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		_writer = writer;
	}

	public TextWriterTraceListener(string? fileName)
	{
		_fileName = fileName;
	}

	public TextWriterTraceListener(string? fileName, string? name)
		: base(name)
	{
		_fileName = fileName;
	}

	public override void Close()
	{
		if (_writer != null)
		{
			try
			{
				_writer.Close();
			}
			catch (ObjectDisposedException)
			{
			}
			_writer = null;
		}
		_fileName = null;
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing && _writer != null)
			{
				_writer.Dispose();
			}
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	public override void Flush()
	{
		EnsureWriter();
		try
		{
			_writer?.Flush();
		}
		catch (ObjectDisposedException)
		{
		}
	}

	public override void Write(string? message)
	{
		EnsureWriter();
		if (_writer != null)
		{
			if (base.NeedIndent)
			{
				WriteIndent();
			}
			try
			{
				_writer.Write(message);
			}
			catch (ObjectDisposedException)
			{
			}
		}
	}

	public override void WriteLine(string? message)
	{
		EnsureWriter();
		if (_writer != null)
		{
			if (base.NeedIndent)
			{
				WriteIndent();
			}
			try
			{
				_writer.WriteLine(message);
				base.NeedIndent = true;
			}
			catch (ObjectDisposedException)
			{
			}
		}
	}

	internal void EnsureWriter()
	{
		if (_writer == null)
		{
			InitializeWriter();
		}
		void InitializeWriter()
		{
			bool flag = false;
			if (_fileName != null)
			{
				UTF8Encoding uTF8Encoding = (UTF8Encoding)new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).Clone();
				uTF8Encoding.EncoderFallback = EncoderFallback.ReplacementFallback;
				uTF8Encoding.DecoderFallback = DecoderFallback.ReplacementFallback;
				string path = Path.GetFullPath(_fileName);
				string directoryName = Path.GetDirectoryName(path);
				string text = Path.GetFileName(path);
				for (int i = 0; i < 2; i++)
				{
					try
					{
						_writer = new StreamWriter(path, append: true, uTF8Encoding, 4096);
						flag = true;
					}
					catch (IOException)
					{
						text = $"{Guid.NewGuid()}{text}";
						path = Path.Combine(directoryName, text);
						continue;
					}
					catch (UnauthorizedAccessException)
					{
					}
					catch (Exception)
					{
					}
					break;
				}
				if (!flag)
				{
					_fileName = null;
				}
			}
		}
	}

	internal bool IsEnabled(TraceOptions opts)
	{
		return (opts & base.TraceOutputOptions) != 0;
	}
}
