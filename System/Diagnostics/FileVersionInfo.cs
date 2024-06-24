using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Diagnostics;

public sealed class FileVersionInfo
{
	private readonly string _fileName;

	private string _companyName;

	private string _fileDescription;

	private string _fileVersion;

	private string _internalName;

	private string _legalCopyright;

	private string _originalFilename;

	private string _productName;

	private string _productVersion;

	private string _comments;

	private string _legalTrademarks;

	private string _privateBuild;

	private string _specialBuild;

	private string _language;

	private int _fileMajor;

	private int _fileMinor;

	private int _fileBuild;

	private int _filePrivate;

	private int _productMajor;

	private int _productMinor;

	private int _productBuild;

	private int _productPrivate;

	private bool _isDebug;

	private bool _isPatched;

	private bool _isPrivateBuild;

	private bool _isPreRelease;

	private bool _isSpecialBuild;

	public string? Comments => _comments;

	public string? CompanyName => _companyName;

	public int FileBuildPart => _fileBuild;

	public string? FileDescription => _fileDescription;

	public int FileMajorPart => _fileMajor;

	public int FileMinorPart => _fileMinor;

	public string FileName => _fileName;

	public int FilePrivatePart => _filePrivate;

	public string? FileVersion => _fileVersion;

	public string? InternalName => _internalName;

	public bool IsDebug => _isDebug;

	public bool IsPatched => _isPatched;

	public bool IsPrivateBuild => _isPrivateBuild;

	public bool IsPreRelease => _isPreRelease;

	public bool IsSpecialBuild => _isSpecialBuild;

	public string? Language => _language;

	public string? LegalCopyright => _legalCopyright;

	public string? LegalTrademarks => _legalTrademarks;

	public string? OriginalFilename => _originalFilename;

	public string? PrivateBuild => _privateBuild;

	public int ProductBuildPart => _productBuild;

	public int ProductMajorPart => _productMajor;

	public int ProductMinorPart => _productMinor;

	public string? ProductName => _productName;

	public int ProductPrivatePart => _productPrivate;

	public string? ProductVersion => _productVersion;

	public string? SpecialBuild => _specialBuild;

	public static FileVersionInfo GetVersionInfo(string fileName)
	{
		if (!Path.IsPathFullyQualified(fileName))
		{
			fileName = Path.GetFullPath(fileName);
		}
		if (!File.Exists(fileName))
		{
			throw new FileNotFoundException(fileName);
		}
		return new FileVersionInfo(fileName);
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder(512);
		stringBuilder.Append("File:             ").AppendLine(FileName);
		stringBuilder.Append("InternalName:     ").AppendLine(InternalName);
		stringBuilder.Append("OriginalFilename: ").AppendLine(OriginalFilename);
		stringBuilder.Append("FileVersion:      ").AppendLine(FileVersion);
		stringBuilder.Append("FileDescription:  ").AppendLine(FileDescription);
		stringBuilder.Append("Product:          ").AppendLine(ProductName);
		stringBuilder.Append("ProductVersion:   ").AppendLine(ProductVersion);
		stringBuilder.Append("Debug:            ").AppendLine(IsDebug.ToString());
		stringBuilder.Append("Patched:          ").AppendLine(IsPatched.ToString());
		stringBuilder.Append("PreRelease:       ").AppendLine(IsPreRelease.ToString());
		stringBuilder.Append("PrivateBuild:     ").AppendLine(IsPrivateBuild.ToString());
		stringBuilder.Append("SpecialBuild:     ").AppendLine(IsSpecialBuild.ToString());
		stringBuilder.Append("Language:         ").AppendLine(Language);
		return stringBuilder.ToString();
	}

	private unsafe FileVersionInfo(string fileName)
	{
		_fileName = fileName;
		uint lpdwHandle;
		uint fileVersionInfoSizeEx = global::Interop.Version.GetFileVersionInfoSizeEx(1u, _fileName, out lpdwHandle);
		if (fileVersionInfoSizeEx == 0)
		{
			return;
		}
		void* ptr = NativeMemory.Alloc(fileVersionInfoSizeEx);
		try
		{
			if (global::Interop.Version.GetFileVersionInfoEx(3u, _fileName, 0u, fileVersionInfoSizeEx, ptr))
			{
				uint languageAndCodePage = GetLanguageAndCodePage(ptr);
				if (GetVersionInfoForCodePage(ptr, languageAndCodePage.ToString("X8")) || (languageAndCodePage != 67699888 && GetVersionInfoForCodePage(ptr, "040904B0")) || (languageAndCodePage != 67699940 && GetVersionInfoForCodePage(ptr, "040904E4")))
				{
					_ = 1;
				}
				else if (languageAndCodePage != 67698688)
				{
					GetVersionInfoForCodePage(ptr, "04090000");
				}
				else
					_ = 0;
			}
		}
		finally
		{
			NativeMemory.Free(ptr);
		}
	}

	private unsafe static global::Interop.Version.VS_FIXEDFILEINFO GetFixedFileInfo(void* memPtr)
	{
		if (global::Interop.Version.VerQueryValue(memPtr, "\\", out var lplpBuffer, out var _))
		{
			return *(global::Interop.Version.VS_FIXEDFILEINFO*)lplpBuffer;
		}
		return default(global::Interop.Version.VS_FIXEDFILEINFO);
	}

	private unsafe static string GetFileVersionLanguage(void* memPtr)
	{
		uint wLang = GetLanguageAndCodePage(memPtr) >> 16;
		char* ptr = stackalloc char[256];
		int length = global::Interop.Kernel32.VerLanguageName(wLang, ptr, 256u);
		return new string(ptr, 0, length);
	}

	private unsafe static string GetFileVersionString(void* memPtr, string name)
	{
		if (global::Interop.Version.VerQueryValue(memPtr, name, out var lplpBuffer, out var _) && lplpBuffer != null)
		{
			return Marshal.PtrToStringUni((nint)lplpBuffer);
		}
		return string.Empty;
	}

	private unsafe static uint GetLanguageAndCodePage(void* memPtr)
	{
		if (global::Interop.Version.VerQueryValue(memPtr, "\\VarFileInfo\\Translation", out var lplpBuffer, out var _))
		{
			return (uint)((*(ushort*)lplpBuffer << 16) + *(ushort*)((byte*)lplpBuffer + 2));
		}
		return 67699940u;
	}

	private unsafe bool GetVersionInfoForCodePage(void* memIntPtr, string codepage)
	{
		Span<char> span = stackalloc char[256];
		IFormatProvider formatProvider = null;
		IFormatProvider provider = formatProvider;
		Span<char> span2 = span;
		Span<char> initialBuffer = span2;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(31, 1, formatProvider, span2);
		handler.AppendLiteral("\\\\StringFileInfo\\\\");
		handler.AppendFormatted(codepage);
		handler.AppendLiteral("\\\\CompanyName");
		_companyName = GetFileVersionString(memIntPtr, string.Create(provider, initialBuffer, ref handler));
		formatProvider = null;
		IFormatProvider provider2 = formatProvider;
		span2 = span;
		Span<char> initialBuffer2 = span2;
		handler = new DefaultInterpolatedStringHandler(35, 1, formatProvider, span2);
		handler.AppendLiteral("\\\\StringFileInfo\\\\");
		handler.AppendFormatted(codepage);
		handler.AppendLiteral("\\\\FileDescription");
		_fileDescription = GetFileVersionString(memIntPtr, string.Create(provider2, initialBuffer2, ref handler));
		formatProvider = null;
		IFormatProvider provider3 = formatProvider;
		span2 = span;
		Span<char> initialBuffer3 = span2;
		handler = new DefaultInterpolatedStringHandler(31, 1, formatProvider, span2);
		handler.AppendLiteral("\\\\StringFileInfo\\\\");
		handler.AppendFormatted(codepage);
		handler.AppendLiteral("\\\\FileVersion");
		_fileVersion = GetFileVersionString(memIntPtr, string.Create(provider3, initialBuffer3, ref handler));
		formatProvider = null;
		IFormatProvider provider4 = formatProvider;
		span2 = span;
		Span<char> initialBuffer4 = span2;
		handler = new DefaultInterpolatedStringHandler(32, 1, formatProvider, span2);
		handler.AppendLiteral("\\\\StringFileInfo\\\\");
		handler.AppendFormatted(codepage);
		handler.AppendLiteral("\\\\InternalName");
		_internalName = GetFileVersionString(memIntPtr, string.Create(provider4, initialBuffer4, ref handler));
		formatProvider = null;
		IFormatProvider provider5 = formatProvider;
		span2 = span;
		Span<char> initialBuffer5 = span2;
		handler = new DefaultInterpolatedStringHandler(34, 1, formatProvider, span2);
		handler.AppendLiteral("\\\\StringFileInfo\\\\");
		handler.AppendFormatted(codepage);
		handler.AppendLiteral("\\\\LegalCopyright");
		_legalCopyright = GetFileVersionString(memIntPtr, string.Create(provider5, initialBuffer5, ref handler));
		formatProvider = null;
		IFormatProvider provider6 = formatProvider;
		span2 = span;
		Span<char> initialBuffer6 = span2;
		handler = new DefaultInterpolatedStringHandler(36, 1, formatProvider, span2);
		handler.AppendLiteral("\\\\StringFileInfo\\\\");
		handler.AppendFormatted(codepage);
		handler.AppendLiteral("\\\\OriginalFilename");
		_originalFilename = GetFileVersionString(memIntPtr, string.Create(provider6, initialBuffer6, ref handler));
		formatProvider = null;
		IFormatProvider provider7 = formatProvider;
		span2 = span;
		Span<char> initialBuffer7 = span2;
		handler = new DefaultInterpolatedStringHandler(31, 1, formatProvider, span2);
		handler.AppendLiteral("\\\\StringFileInfo\\\\");
		handler.AppendFormatted(codepage);
		handler.AppendLiteral("\\\\ProductName");
		_productName = GetFileVersionString(memIntPtr, string.Create(provider7, initialBuffer7, ref handler));
		formatProvider = null;
		IFormatProvider provider8 = formatProvider;
		span2 = span;
		Span<char> initialBuffer8 = span2;
		handler = new DefaultInterpolatedStringHandler(34, 1, formatProvider, span2);
		handler.AppendLiteral("\\\\StringFileInfo\\\\");
		handler.AppendFormatted(codepage);
		handler.AppendLiteral("\\\\ProductVersion");
		_productVersion = GetFileVersionString(memIntPtr, string.Create(provider8, initialBuffer8, ref handler));
		formatProvider = null;
		IFormatProvider provider9 = formatProvider;
		span2 = span;
		Span<char> initialBuffer9 = span2;
		handler = new DefaultInterpolatedStringHandler(28, 1, formatProvider, span2);
		handler.AppendLiteral("\\\\StringFileInfo\\\\");
		handler.AppendFormatted(codepage);
		handler.AppendLiteral("\\\\Comments");
		_comments = GetFileVersionString(memIntPtr, string.Create(provider9, initialBuffer9, ref handler));
		formatProvider = null;
		IFormatProvider provider10 = formatProvider;
		span2 = span;
		Span<char> initialBuffer10 = span2;
		handler = new DefaultInterpolatedStringHandler(35, 1, formatProvider, span2);
		handler.AppendLiteral("\\\\StringFileInfo\\\\");
		handler.AppendFormatted(codepage);
		handler.AppendLiteral("\\\\LegalTrademarks");
		_legalTrademarks = GetFileVersionString(memIntPtr, string.Create(provider10, initialBuffer10, ref handler));
		formatProvider = null;
		IFormatProvider provider11 = formatProvider;
		span2 = span;
		Span<char> initialBuffer11 = span2;
		handler = new DefaultInterpolatedStringHandler(32, 1, formatProvider, span2);
		handler.AppendLiteral("\\\\StringFileInfo\\\\");
		handler.AppendFormatted(codepage);
		handler.AppendLiteral("\\\\PrivateBuild");
		_privateBuild = GetFileVersionString(memIntPtr, string.Create(provider11, initialBuffer11, ref handler));
		formatProvider = null;
		IFormatProvider provider12 = formatProvider;
		span2 = span;
		Span<char> initialBuffer12 = span2;
		handler = new DefaultInterpolatedStringHandler(32, 1, formatProvider, span2);
		handler.AppendLiteral("\\\\StringFileInfo\\\\");
		handler.AppendFormatted(codepage);
		handler.AppendLiteral("\\\\SpecialBuild");
		_specialBuild = GetFileVersionString(memIntPtr, string.Create(provider12, initialBuffer12, ref handler));
		_language = GetFileVersionLanguage(memIntPtr);
		global::Interop.Version.VS_FIXEDFILEINFO fixedFileInfo = GetFixedFileInfo(memIntPtr);
		_fileMajor = (int)HIWORD(fixedFileInfo.dwFileVersionMS);
		_fileMinor = (int)LOWORD(fixedFileInfo.dwFileVersionMS);
		_fileBuild = (int)HIWORD(fixedFileInfo.dwFileVersionLS);
		_filePrivate = (int)LOWORD(fixedFileInfo.dwFileVersionLS);
		_productMajor = (int)HIWORD(fixedFileInfo.dwProductVersionMS);
		_productMinor = (int)LOWORD(fixedFileInfo.dwProductVersionMS);
		_productBuild = (int)HIWORD(fixedFileInfo.dwProductVersionLS);
		_productPrivate = (int)LOWORD(fixedFileInfo.dwProductVersionLS);
		_isDebug = (fixedFileInfo.dwFileFlags & 1) != 0;
		_isPatched = (fixedFileInfo.dwFileFlags & 4) != 0;
		_isPrivateBuild = (fixedFileInfo.dwFileFlags & 8) != 0;
		_isPreRelease = (fixedFileInfo.dwFileFlags & 2) != 0;
		_isSpecialBuild = (fixedFileInfo.dwFileFlags & 0x20) != 0;
		return _fileVersion != string.Empty;
	}

	private static uint HIWORD(uint dword)
	{
		return (dword >> 16) & 0xFFFFu;
	}

	private static uint LOWORD(uint dword)
	{
		return dword & 0xFFFFu;
	}
}
