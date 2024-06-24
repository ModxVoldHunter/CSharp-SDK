using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Microsoft.Win32.SafeHandles;

namespace System.IO;

public static class FileSystemAclExtensions
{
	public static DirectorySecurity GetAccessControl(this DirectoryInfo directoryInfo)
	{
		ArgumentNullException.ThrowIfNull(directoryInfo, "directoryInfo");
		return new DirectorySecurity(directoryInfo.FullName, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
	}

	public static DirectorySecurity GetAccessControl(this DirectoryInfo directoryInfo, AccessControlSections includeSections)
	{
		ArgumentNullException.ThrowIfNull(directoryInfo, "directoryInfo");
		return new DirectorySecurity(directoryInfo.FullName, includeSections);
	}

	public static void SetAccessControl(this DirectoryInfo directoryInfo, DirectorySecurity directorySecurity)
	{
		ArgumentNullException.ThrowIfNull(directorySecurity, "directorySecurity");
		string fullPath = Path.GetFullPath(directoryInfo.FullName);
		directorySecurity.Persist(fullPath);
	}

	public static FileSecurity GetAccessControl(this FileInfo fileInfo)
	{
		ArgumentNullException.ThrowIfNull(fileInfo, "fileInfo");
		return fileInfo.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
	}

	public static FileSecurity GetAccessControl(this FileInfo fileInfo, AccessControlSections includeSections)
	{
		ArgumentNullException.ThrowIfNull(fileInfo, "fileInfo");
		return new FileSecurity(fileInfo.FullName, includeSections);
	}

	public static void SetAccessControl(this FileInfo fileInfo, FileSecurity fileSecurity)
	{
		ArgumentNullException.ThrowIfNull(fileInfo, "fileInfo");
		ArgumentNullException.ThrowIfNull(fileSecurity, "fileSecurity");
		string fullPath = Path.GetFullPath(fileInfo.FullName);
		fileSecurity.Persist(fullPath);
	}

	public static FileSecurity GetAccessControl(this FileStream fileStream)
	{
		ArgumentNullException.ThrowIfNull(fileStream, "fileStream");
		SafeFileHandle safeFileHandle = fileStream.SafeFileHandle;
		if (safeFileHandle.IsClosed)
		{
			throw new ObjectDisposedException(null, System.SR.ObjectDisposed_FileClosed);
		}
		return new FileSecurity(safeFileHandle, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
	}

	public static void SetAccessControl(this FileStream fileStream, FileSecurity fileSecurity)
	{
		ArgumentNullException.ThrowIfNull(fileStream, "fileStream");
		ArgumentNullException.ThrowIfNull(fileSecurity, "fileSecurity");
		SafeFileHandle safeFileHandle = fileStream.SafeFileHandle;
		if (safeFileHandle.IsClosed)
		{
			throw new ObjectDisposedException(null, System.SR.ObjectDisposed_FileClosed);
		}
		fileSecurity.Persist(safeFileHandle, fileStream.Name);
	}

	public static void Create(this DirectoryInfo directoryInfo, DirectorySecurity directorySecurity)
	{
		ArgumentNullException.ThrowIfNull(directoryInfo, "directoryInfo");
		ArgumentNullException.ThrowIfNull(directorySecurity, "directorySecurity");
		System.IO.FileSystem.CreateDirectory(directoryInfo.FullName, directorySecurity.GetSecurityDescriptorBinaryForm());
	}

	public static FileStream Create(this FileInfo fileInfo, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options, FileSecurity? fileSecurity)
	{
		ArgumentNullException.ThrowIfNull(fileInfo, "fileInfo");
		FileShare fileShare = share & ~FileShare.Inheritable;
		if (mode < FileMode.CreateNew || mode > FileMode.Append)
		{
			throw new ArgumentOutOfRangeException("mode", System.SR.ArgumentOutOfRange_Enum);
		}
		if ((fileShare < FileShare.None) || fileShare > (FileShare.ReadWrite | FileShare.Delete))
		{
			throw new ArgumentOutOfRangeException("share", System.SR.ArgumentOutOfRange_Enum);
		}
		if (bufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException("bufferSize", System.SR.ArgumentOutOfRange_NeedPosNum);
		}
		if ((rights & FileSystemRights.Write) == 0 && (mode == FileMode.Truncate || mode == FileMode.CreateNew || mode == FileMode.Create || mode == FileMode.Append))
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_InvalidFileModeAndFileSystemRightsCombo, mode, rights));
		}
		if ((rights & FileSystemRights.ReadAndExecute) != 0 && mode == FileMode.Append)
		{
			throw new ArgumentException(System.SR.Argument_InvalidAppendMode);
		}
		if (mode == FileMode.Truncate && (rights & FileSystemRights.Write) != FileSystemRights.Write)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_InvalidFileModeAndFileSystemRightsCombo, mode, rights));
		}
		SafeFileHandle safeFileHandle = CreateFileHandle(fileInfo.FullName, mode, rights, share, options, fileSecurity);
		try
		{
			return new FileStream(safeFileHandle, GetFileAccessFromRights(rights), bufferSize, (options & FileOptions.Asynchronous) != 0);
		}
		catch
		{
			safeFileHandle.Dispose();
			throw;
		}
	}

	public static DirectoryInfo CreateDirectory(this DirectorySecurity directorySecurity, string path)
	{
		ArgumentNullException.ThrowIfNull(directorySecurity, "directorySecurity");
		ArgumentException.ThrowIfNullOrEmpty(path, "path");
		DirectoryInfo directoryInfo = new DirectoryInfo(path);
		directoryInfo.Create(directorySecurity);
		return directoryInfo;
	}

	private static FileAccess GetFileAccessFromRights(FileSystemRights rights)
	{
		FileAccess fileAccess = (FileAccess)0;
		if ((rights & FileSystemRights.FullControl) != 0 || (rights & FileSystemRights.Modify) != 0)
		{
			return FileAccess.ReadWrite;
		}
		if ((rights & FileSystemRights.ReadData) != 0 || (rights & FileSystemRights.ReadExtendedAttributes) != 0 || (rights & FileSystemRights.ExecuteFile) != 0 || (rights & FileSystemRights.ReadAttributes) != 0 || (rights & FileSystemRights.ReadPermissions) != 0 || (rights & FileSystemRights.TakeOwnership) != 0 || ((uint)rights & 0x80000000u) != 0)
		{
			fileAccess = FileAccess.Read;
		}
		if ((rights & FileSystemRights.AppendData) != 0 || (rights & FileSystemRights.ChangePermissions) != 0 || (rights & FileSystemRights.Delete) != 0 || (rights & FileSystemRights.DeleteSubdirectoriesAndFiles) != 0 || (rights & FileSystemRights.WriteAttributes) != 0 || (rights & FileSystemRights.WriteData) != 0 || (rights & FileSystemRights.WriteExtendedAttributes) != 0 || (rights & (FileSystemRights)1073741824) != 0)
		{
			fileAccess |= FileAccess.Write;
		}
		ArgumentOutOfRangeException.ThrowIfZero((int)fileAccess, "rights");
		return fileAccess;
	}

	private unsafe static SafeFileHandle CreateFileHandle(string fullPath, FileMode mode, FileSystemRights rights, FileShare share, FileOptions options, FileSecurity security)
	{
		if (mode == FileMode.Append)
		{
			mode = FileMode.OpenOrCreate;
		}
		int flagsAndAttributes2 = (int)(options | (FileOptions)1048576 | FileOptions.None);
		global::Interop.Kernel32.SECURITY_ATTRIBUTES sECURITY_ATTRIBUTES = default(global::Interop.Kernel32.SECURITY_ATTRIBUTES);
		sECURITY_ATTRIBUTES.nLength = (uint)sizeof(global::Interop.Kernel32.SECURITY_ATTRIBUTES);
		sECURITY_ATTRIBUTES.bInheritHandle = (((share & FileShare.Inheritable) != 0) ? global::Interop.BOOL.TRUE : global::Interop.BOOL.FALSE);
		global::Interop.Kernel32.SECURITY_ATTRIBUTES sECURITY_ATTRIBUTES2 = sECURITY_ATTRIBUTES;
		SafeFileHandle result;
		if (security != null)
		{
			fixed (byte* lpSecurityDescriptor = security.GetSecurityDescriptorBinaryForm())
			{
				sECURITY_ATTRIBUTES2.lpSecurityDescriptor = lpSecurityDescriptor;
				result = CreateFileHandleInternal(fullPath, mode, rights, share, flagsAndAttributes2, &sECURITY_ATTRIBUTES2);
			}
		}
		else
		{
			result = CreateFileHandleInternal(fullPath, mode, rights, share, flagsAndAttributes2, &sECURITY_ATTRIBUTES2);
		}
		return result;
		unsafe static SafeFileHandle CreateFileHandleInternal(string fullPath, FileMode mode, FileSystemRights rights, FileShare share, int flagsAndAttributes, global::Interop.Kernel32.SECURITY_ATTRIBUTES* secAttrs)
		{
			using (System.IO.DisableMediaInsertionPrompt.Create())
			{
				SafeFileHandle safeFileHandle = global::Interop.Kernel32.CreateFile(fullPath, (int)rights, share & ~FileShare.Inheritable, secAttrs, mode, flagsAndAttributes, IntPtr.Zero);
				ValidateFileHandle(safeFileHandle, fullPath);
				return safeFileHandle;
			}
		}
	}

	private static void ValidateFileHandle(SafeFileHandle handle, string fullPath)
	{
		if (handle.IsInvalid)
		{
			int num = Marshal.GetLastPInvokeError();
			if (num == 3 && fullPath.Length == Path.GetPathRoot(fullPath).Length)
			{
				num = 5;
			}
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(num, fullPath);
		}
	}
}
