using System.Collections;
using System.Linq;
using System.Text;

namespace System.IO.IsolatedStorage;

public sealed class IsolatedStorageFile : IsolatedStorage, IDisposable
{
	internal sealed class IsolatedStorageFileEnumerator : IEnumerator
	{
		public object Current
		{
			get
			{
				throw new InvalidOperationException();
			}
		}

		public bool MoveNext()
		{
			return false;
		}

		public void Reset()
		{
		}
	}

	private bool _disposed;

	private bool _closed;

	private readonly object _internalLock = new object();

	private readonly string _rootDirectory;

	private string RootDirectory => _rootDirectory;

	internal bool Disposed => _disposed;

	internal bool IsDeleted
	{
		get
		{
			try
			{
				return !Directory.Exists(RootDirectory);
			}
			catch (IOException)
			{
				return true;
			}
			catch (UnauthorizedAccessException)
			{
				return true;
			}
		}
	}

	public override long AvailableFreeSpace => Quota - UsedSize;

	[CLSCompliant(false)]
	[Obsolete("IsolatedStorageFile.MaximumSize has been deprecated because it is not CLS Compliant. To get the maximum size use IsolatedStorageFile.Quota instead.")]
	public override ulong MaximumSize => 9223372036854775807uL;

	public override long Quota => long.MaxValue;

	public override long UsedSize => 0L;

	[CLSCompliant(false)]
	[Obsolete("IsolatedStorageFile.CurrentSize has been deprecated because it is not CLS Compliant. To get the current size use IsolatedStorageFile.UsedSize instead.")]
	public override ulong CurrentSize => 0uL;

	public static bool IsEnabled => true;

	public void Close()
	{
		if (Helper.IsRoaming(base.Scope))
		{
			return;
		}
		lock (_internalLock)
		{
			if (!_closed)
			{
				_closed = true;
				GC.SuppressFinalize(this);
			}
		}
	}

	public void DeleteFile(string file)
	{
		ArgumentNullException.ThrowIfNull(file, "file");
		EnsureStoreIsValid();
		try
		{
			string fullPath = GetFullPath(file);
			File.Delete(fullPath);
		}
		catch (Exception rootCause)
		{
			throw GetIsolatedStorageException(System.SR.IsolatedStorage_DeleteFile, rootCause);
		}
	}

	public bool FileExists(string path)
	{
		ArgumentNullException.ThrowIfNull(path, "path");
		EnsureStoreIsValid();
		return File.Exists(GetFullPath(path));
	}

	public bool DirectoryExists(string path)
	{
		ArgumentNullException.ThrowIfNull(path, "path");
		EnsureStoreIsValid();
		return Directory.Exists(GetFullPath(path));
	}

	public void CreateDirectory(string dir)
	{
		ArgumentNullException.ThrowIfNull(dir, "dir");
		EnsureStoreIsValid();
		string fullPath = GetFullPath(dir);
		if (Directory.Exists(fullPath))
		{
			return;
		}
		try
		{
			Directory.CreateDirectory(fullPath);
		}
		catch (Exception rootCause)
		{
			throw GetIsolatedStorageException(System.SR.IsolatedStorage_CreateDirectory, rootCause);
		}
	}

	public void DeleteDirectory(string dir)
	{
		ArgumentNullException.ThrowIfNull(dir, "dir");
		EnsureStoreIsValid();
		try
		{
			string fullPath = GetFullPath(dir);
			Directory.Delete(fullPath, recursive: false);
		}
		catch (Exception rootCause)
		{
			throw GetIsolatedStorageException(System.SR.IsolatedStorage_DeleteDirectory, rootCause);
		}
	}

	public string[] GetFileNames()
	{
		return GetFileNames("*");
	}

	public string[] GetFileNames(string searchPattern)
	{
		ArgumentNullException.ThrowIfNull(searchPattern, "searchPattern");
		EnsureStoreIsValid();
		try
		{
			return (from f in Directory.EnumerateFiles(RootDirectory, searchPattern)
				select Path.GetFileName(f)).ToArray();
		}
		catch (UnauthorizedAccessException rootCause)
		{
			throw GetIsolatedStorageException(System.SR.IsolatedStorage_Operation, rootCause);
		}
	}

	public string[] GetDirectoryNames()
	{
		return GetDirectoryNames("*");
	}

	public string[] GetDirectoryNames(string searchPattern)
	{
		ArgumentNullException.ThrowIfNull(searchPattern, "searchPattern");
		EnsureStoreIsValid();
		try
		{
			return (from m in Directory.EnumerateDirectories(RootDirectory, searchPattern)
				select m.Substring(Path.GetDirectoryName(m).Length + 1)).ToArray();
		}
		catch (UnauthorizedAccessException rootCause)
		{
			throw GetIsolatedStorageException(System.SR.IsolatedStorage_Operation, rootCause);
		}
	}

	public IsolatedStorageFileStream OpenFile(string path, FileMode mode)
	{
		EnsureStoreIsValid();
		return new IsolatedStorageFileStream(path, mode, this);
	}

	public IsolatedStorageFileStream OpenFile(string path, FileMode mode, FileAccess access)
	{
		EnsureStoreIsValid();
		return new IsolatedStorageFileStream(path, mode, access, this);
	}

	public IsolatedStorageFileStream OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
	{
		EnsureStoreIsValid();
		return new IsolatedStorageFileStream(path, mode, access, share, this);
	}

	public IsolatedStorageFileStream CreateFile(string path)
	{
		EnsureStoreIsValid();
		return new IsolatedStorageFileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, this);
	}

	public DateTimeOffset GetCreationTime(string path)
	{
		ArgumentException.ThrowIfNullOrEmpty(path, "path");
		EnsureStoreIsValid();
		try
		{
			return new DateTimeOffset(File.GetCreationTime(GetFullPath(path)));
		}
		catch (UnauthorizedAccessException)
		{
			return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
		}
	}

	public DateTimeOffset GetLastAccessTime(string path)
	{
		ArgumentException.ThrowIfNullOrEmpty(path, "path");
		EnsureStoreIsValid();
		try
		{
			return new DateTimeOffset(File.GetLastAccessTime(GetFullPath(path)));
		}
		catch (UnauthorizedAccessException)
		{
			return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
		}
	}

	public DateTimeOffset GetLastWriteTime(string path)
	{
		ArgumentException.ThrowIfNullOrEmpty(path, "path");
		EnsureStoreIsValid();
		try
		{
			return new DateTimeOffset(File.GetLastWriteTime(GetFullPath(path)));
		}
		catch (UnauthorizedAccessException)
		{
			return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
		}
	}

	public void CopyFile(string sourceFileName, string destinationFileName)
	{
		ArgumentException.ThrowIfNullOrEmpty(sourceFileName, "sourceFileName");
		ArgumentException.ThrowIfNullOrEmpty(destinationFileName, "destinationFileName");
		CopyFile(sourceFileName, destinationFileName, overwrite: false);
	}

	public void CopyFile(string sourceFileName, string destinationFileName, bool overwrite)
	{
		ArgumentException.ThrowIfNullOrEmpty(sourceFileName, "sourceFileName");
		ArgumentException.ThrowIfNullOrEmpty(destinationFileName, "destinationFileName");
		EnsureStoreIsValid();
		string fullPath = GetFullPath(sourceFileName);
		string fullPath2 = GetFullPath(destinationFileName);
		try
		{
			File.Copy(fullPath, fullPath2, overwrite);
		}
		catch (FileNotFoundException)
		{
			throw new FileNotFoundException(System.SR.Format(System.SR.PathNotFound_Path, sourceFileName));
		}
		catch (PathTooLongException)
		{
			throw;
		}
		catch (Exception rootCause)
		{
			throw GetIsolatedStorageException(System.SR.IsolatedStorage_Operation, rootCause);
		}
	}

	public void MoveFile(string sourceFileName, string destinationFileName)
	{
		ArgumentException.ThrowIfNullOrEmpty(sourceFileName, "sourceFileName");
		ArgumentException.ThrowIfNullOrEmpty(destinationFileName, "destinationFileName");
		EnsureStoreIsValid();
		string fullPath = GetFullPath(sourceFileName);
		string fullPath2 = GetFullPath(destinationFileName);
		try
		{
			File.Move(fullPath, fullPath2);
		}
		catch (FileNotFoundException)
		{
			throw new FileNotFoundException(System.SR.Format(System.SR.PathNotFound_Path, sourceFileName));
		}
		catch (PathTooLongException)
		{
			throw;
		}
		catch (Exception rootCause)
		{
			throw GetIsolatedStorageException(System.SR.IsolatedStorage_Operation, rootCause);
		}
	}

	public void MoveDirectory(string sourceDirectoryName, string destinationDirectoryName)
	{
		ArgumentException.ThrowIfNullOrEmpty(sourceDirectoryName, "sourceDirectoryName");
		ArgumentException.ThrowIfNullOrEmpty(destinationDirectoryName, "destinationDirectoryName");
		EnsureStoreIsValid();
		string fullPath = GetFullPath(sourceDirectoryName);
		string fullPath2 = GetFullPath(destinationDirectoryName);
		try
		{
			Directory.Move(fullPath, fullPath2);
		}
		catch (DirectoryNotFoundException)
		{
			throw new DirectoryNotFoundException(System.SR.Format(System.SR.PathNotFound_Path, sourceDirectoryName));
		}
		catch (PathTooLongException)
		{
			throw;
		}
		catch (Exception rootCause)
		{
			throw GetIsolatedStorageException(System.SR.IsolatedStorage_Operation, rootCause);
		}
	}

	public static IEnumerator GetEnumerator(IsolatedStorageScope scope)
	{
		return new IsolatedStorageFileEnumerator();
	}

	public static IsolatedStorageFile GetUserStoreForApplication()
	{
		return GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Application);
	}

	public static IsolatedStorageFile GetUserStoreForAssembly()
	{
		return GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly);
	}

	public static IsolatedStorageFile GetUserStoreForDomain()
	{
		return GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly);
	}

	public static IsolatedStorageFile GetUserStoreForSite()
	{
		throw new NotSupportedException(System.SR.IsolatedStorage_NotValidOnDesktop);
	}

	public static IsolatedStorageFile GetMachineStoreForApplication()
	{
		return GetStore(IsolatedStorageScope.Machine | IsolatedStorageScope.Application);
	}

	public static IsolatedStorageFile GetMachineStoreForAssembly()
	{
		return GetStore(IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine);
	}

	public static IsolatedStorageFile GetMachineStoreForDomain()
	{
		return GetStore(IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine);
	}

	private static IsolatedStorageFile GetStore(IsolatedStorageScope scope)
	{
		return new IsolatedStorageFile(scope);
	}

	public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, Type? applicationEvidenceType)
	{
		if (!(applicationEvidenceType == null))
		{
			throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CAS);
		}
		return GetStore(scope);
	}

	public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, object? applicationIdentity)
	{
		if (applicationIdentity != null)
		{
			throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CAS);
		}
		return GetStore(scope);
	}

	public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, Type? domainEvidenceType, Type? assemblyEvidenceType)
	{
		if (!(domainEvidenceType == null) || !(assemblyEvidenceType == null))
		{
			throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CAS);
		}
		return GetStore(scope);
	}

	public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, object? domainIdentity, object? assemblyIdentity)
	{
		if (domainIdentity != null || assemblyIdentity != null)
		{
			throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CAS);
		}
		return GetStore(scope);
	}

	internal string GetFullPath(string partialPath)
	{
		int i;
		for (i = 0; i < partialPath.Length && (partialPath[i] == Path.DirectorySeparatorChar || partialPath[i] == Path.AltDirectorySeparatorChar); i++)
		{
		}
		partialPath = partialPath.Substring(i);
		return Path.Combine(RootDirectory, partialPath);
	}

	internal void EnsureStoreIsValid()
	{
		if (Disposed)
		{
			throw new ObjectDisposedException(null, System.SR.IsolatedStorage_StoreNotOpen);
		}
		if (_closed || IsDeleted)
		{
			throw new InvalidOperationException(System.SR.IsolatedStorage_StoreNotOpen);
		}
	}

	public void Dispose()
	{
		Close();
		_disposed = true;
	}

	internal static Exception GetIsolatedStorageException(string exceptionMsg, Exception rootCause)
	{
		IsolatedStorageException ex = new IsolatedStorageException(exceptionMsg, rootCause);
		ex._underlyingException = rootCause;
		return ex;
	}

	public override bool IncreaseQuotaTo(long newQuotaSize)
	{
		return true;
	}

	public override void Remove()
	{
		try
		{
			Directory.Delete(RootDirectory, recursive: true);
		}
		catch
		{
			throw new IsolatedStorageException(System.SR.IsolatedStorage_DeleteDirectories);
		}
		Close();
		string parentDirectory = GetParentDirectory();
		if (ContainsUnknownFiles(parentDirectory))
		{
			return;
		}
		try
		{
			Directory.Delete(parentDirectory, recursive: true);
		}
		catch
		{
			return;
		}
		if (!Helper.IsDomain(base.Scope))
		{
			return;
		}
		parentDirectory = Path.GetDirectoryName(parentDirectory);
		if (ContainsUnknownFiles(parentDirectory))
		{
			return;
		}
		try
		{
			Directory.Delete(parentDirectory, recursive: true);
		}
		catch
		{
		}
	}

	public static void Remove(IsolatedStorageScope scope)
	{
		VerifyGlobalScope(scope);
		string rootDirectory = Helper.GetRootDirectory(scope);
		try
		{
			Directory.Delete(rootDirectory, recursive: true);
			Directory.CreateDirectory(rootDirectory);
		}
		catch
		{
			throw new IsolatedStorageException(System.SR.IsolatedStorage_DeleteDirectories);
		}
	}

	private static void VerifyGlobalScope(IsolatedStorageScope scope)
	{
		if (scope != IsolatedStorageScope.User && scope != (IsolatedStorageScope.User | IsolatedStorageScope.Roaming) && scope != IsolatedStorageScope.Machine)
		{
			throw new ArgumentException(System.SR.IsolatedStorage_Scope_U_R_M);
		}
	}

	private bool ContainsUnknownFiles(string directory)
	{
		string[] files;
		string[] directories;
		try
		{
			files = Directory.GetFiles(directory);
			directories = Directory.GetDirectories(directory);
		}
		catch
		{
			throw new IsolatedStorageException(System.SR.IsolatedStorage_DeleteDirectories);
		}
		if (directories.Length > 1 || (directories.Length != 0 && !IsMatchingScopeDirectory(directories[0])))
		{
			return true;
		}
		if (files.Length == 0)
		{
			return false;
		}
		if (Helper.IsRoaming(base.Scope))
		{
			if (files.Length <= 1)
			{
				return !IsIdFile(files[0]);
			}
			return true;
		}
		if (files.Length <= 2 && (IsIdFile(files[0]) || IsInfoFile(files[0])))
		{
			if (files.Length == 2 && !IsIdFile(files[1]))
			{
				return !IsInfoFile(files[1]);
			}
			return false;
		}
		return true;
	}

	private static bool IsIdFile(string file)
	{
		return string.Equals(Path.GetFileName(file), "identity.dat");
	}

	private static bool IsInfoFile(string file)
	{
		return string.Equals(Path.GetFileName(file), "info.dat");
	}

	internal IsolatedStorageFile(IsolatedStorageScope scope)
	{
		InitStore(scope, null, null);
		StringBuilder stringBuilder = new StringBuilder(GetIsolatedStorageRoot());
		stringBuilder.Append(SeparatorExternal);
		if (Helper.IsApplication(scope))
		{
			stringBuilder.Append("AppFiles");
		}
		else if (Helper.IsDomain(scope))
		{
			stringBuilder.Append("Files");
		}
		else
		{
			stringBuilder.Append("AssemFiles");
		}
		stringBuilder.Append(SeparatorExternal);
		_rootDirectory = stringBuilder.ToString();
		Helper.CreateDirectory(_rootDirectory, scope);
	}

	private string GetIsolatedStorageRoot()
	{
		StringBuilder stringBuilder = new StringBuilder(Helper.GetRootDirectory(base.Scope));
		stringBuilder.Append(SeparatorExternal);
		stringBuilder.Append(base.IdentityHash);
		return stringBuilder.ToString();
	}

	private bool IsMatchingScopeDirectory(string directory)
	{
		string fileName = Path.GetFileName(directory);
		if ((!Helper.IsApplication(base.Scope) || !string.Equals(fileName, "AppFiles", StringComparison.Ordinal)) && (!Helper.IsAssembly(base.Scope) || !string.Equals(fileName, "AssemFiles", StringComparison.Ordinal)))
		{
			if (Helper.IsDomain(base.Scope))
			{
				return string.Equals(fileName, "Files", StringComparison.Ordinal);
			}
			return false;
		}
		return true;
	}

	private string GetParentDirectory()
	{
		return Path.GetDirectoryName(RootDirectory.TrimEnd(Path.DirectorySeparatorChar));
	}
}
