using System.Reflection;

namespace System.Runtime.InteropServices;

public delegate nint DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath);
