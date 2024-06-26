using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO;

internal static class FileSystem
{
	internal static void VerifyValidPath(string path, string argName)
	{
		ArgumentException.ThrowIfNullOrEmpty(path, argName);
		if (path.Contains('\0'))
		{
			throw new ArgumentException(SR.Argument_NullCharInPath, argName);
		}
	}

	internal static void MoveDirectory(string sourceFullPath, string destFullPath)
	{
		ReadOnlySpan<char> readOnlySpan = Path.TrimEndingDirectorySeparator(sourceFullPath.AsSpan());
		ReadOnlySpan<char> readOnlySpan2 = Path.TrimEndingDirectorySeparator(destFullPath.AsSpan());
		bool _ = false;
		if (MemoryExtensions.Equals(readOnlySpan, readOnlySpan2, StringComparison.OrdinalIgnoreCase))
		{
			int num = 0;
			if (Path.GetFileName(readOnlySpan).SequenceEqual(Path.GetFileName(readOnlySpan2)))
			{
				throw new IOException(SR.IO_SourceDestMustBeDifferent);
			}
			_ = true;
		}
		MoveDirectory(sourceFullPath, destFullPath, _);
	}

	public static bool DirectoryExists(string fullPath)
	{
		int lastError;
		return DirectoryExists(fullPath, out lastError);
	}

	private static bool DirectoryExists(string path, out int lastError)
	{
		Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default(Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA);
		lastError = FillAttributeInfo(path, ref data, returnErrorOnNotFound: true);
		if (lastError == 0 && data.dwFileAttributes != -1)
		{
			return (data.dwFileAttributes & 0x10) != 0;
		}
		return false;
	}

	public static bool FileExists(string fullPath)
	{
		Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default(Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA);
		if (FillAttributeInfo(fullPath, ref data, returnErrorOnNotFound: true) == 0 && data.dwFileAttributes != -1)
		{
			return (data.dwFileAttributes & 0x10) == 0;
		}
		return false;
	}

	internal static int FillAttributeInfo(string path, ref Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data, bool returnErrorOnNotFound)
	{
		int num = 0;
		path = PathInternal.TrimEndingDirectorySeparator(path);
		using (DisableMediaInsertionPrompt.Create())
		{
			if (!Interop.Kernel32.GetFileAttributesEx(path, Interop.Kernel32.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, ref data))
			{
				num = Marshal.GetLastPInvokeError();
				if (!IsPathUnreachableError(num))
				{
					Interop.Kernel32.WIN32_FIND_DATA data2 = default(Interop.Kernel32.WIN32_FIND_DATA);
					using SafeFindHandle safeFindHandle = Interop.Kernel32.FindFirstFile(path, ref data2);
					if (safeFindHandle.IsInvalid)
					{
						num = Marshal.GetLastPInvokeError();
					}
					else
					{
						num = 0;
						data.PopulateFrom(ref data2);
					}
				}
			}
		}
		if (num != 0 && !returnErrorOnNotFound && ((uint)(num - 2) <= 1u || num == 21))
		{
			data.dwFileAttributes = -1;
			return 0;
		}
		return num;
	}

	internal static bool IsPathUnreachableError(int errorCode)
	{
		switch (errorCode)
		{
		case 2:
		case 3:
		case 6:
		case 21:
		case 53:
		case 65:
		case 67:
		case 87:
		case 123:
		case 161:
		case 206:
		case 1231:
			return true;
		default:
			return false;
		}
	}

	public unsafe static void CreateDirectory(string fullPath, byte[] securityDescriptor = null)
	{
		if (DirectoryExists(fullPath))
		{
			return;
		}
		List<string> list = new List<string>();
		bool flag = false;
		int num = fullPath.Length;
		if (num >= 2 && PathInternal.EndsInDirectorySeparator(fullPath.AsSpan()))
		{
			num--;
		}
		int rootLength = PathInternal.GetRootLength(fullPath.AsSpan());
		if (num > rootLength)
		{
			int num2 = num - 1;
			while (num2 >= rootLength && !flag)
			{
				string text = fullPath.Substring(0, num2 + 1);
				if (!DirectoryExists(text))
				{
					list.Add(text);
				}
				else
				{
					flag = true;
				}
				while (num2 > rootLength && !PathInternal.IsDirectorySeparator(fullPath[num2]))
				{
					num2--;
				}
				num2--;
			}
		}
		int count = list.Count;
		bool flag2 = true;
		int num3 = 0;
		string path = fullPath;
		fixed (byte* lpSecurityDescriptor = securityDescriptor)
		{
			Interop.Kernel32.SECURITY_ATTRIBUTES sECURITY_ATTRIBUTES = default(Interop.Kernel32.SECURITY_ATTRIBUTES);
			sECURITY_ATTRIBUTES.nLength = (uint)sizeof(Interop.Kernel32.SECURITY_ATTRIBUTES);
			sECURITY_ATTRIBUTES.lpSecurityDescriptor = lpSecurityDescriptor;
			Interop.Kernel32.SECURITY_ATTRIBUTES sECURITY_ATTRIBUTES2 = sECURITY_ATTRIBUTES;
			while (list.Count > 0)
			{
				string text2 = list[list.Count - 1];
				list.RemoveAt(list.Count - 1);
				flag2 = Interop.Kernel32.CreateDirectory(text2, &sECURITY_ATTRIBUTES2);
				if (!flag2 && num3 == 0)
				{
					int lastError = Marshal.GetLastPInvokeError();
					if (lastError != 183)
					{
						num3 = lastError;
					}
					else if (FileExists(text2) || (!DirectoryExists(text2, out lastError) && lastError == 5))
					{
						num3 = lastError;
						path = text2;
					}
				}
			}
		}
		if (count == 0 && !flag)
		{
			string pathRoot = Path.GetPathRoot(fullPath);
			if (!DirectoryExists(pathRoot))
			{
				throw Win32Marshal.GetExceptionForWin32Error(3, pathRoot);
			}
		}
		else if (!flag2 && num3 != 0)
		{
			throw Win32Marshal.GetExceptionForWin32Error(num3, path);
		}
	}

	public static void Encrypt(string path)
	{
		string fullPath = Path.GetFullPath(path);
		if (!Interop.Advapi32.EncryptFile(fullPath))
		{
			ThrowExceptionEncryptDecryptFail(fullPath);
		}
	}

	public static void Decrypt(string path)
	{
		string fullPath = Path.GetFullPath(path);
		if (!Interop.Advapi32.DecryptFile(fullPath))
		{
			ThrowExceptionEncryptDecryptFail(fullPath);
		}
	}

	private unsafe static void ThrowExceptionEncryptDecryptFail(string fullPath)
	{
		int lastPInvokeError = Marshal.GetLastPInvokeError();
		if (lastPInvokeError == 5)
		{
			string text = DriveInfoInternal.NormalizeDriveName(Path.GetPathRoot(fullPath));
			using (DisableMediaInsertionPrompt.Create())
			{
				if (!Interop.Kernel32.GetVolumeInformation(text, null, 0, null, null, out var fileSystemFlags, null, 0))
				{
					lastPInvokeError = Marshal.GetLastPInvokeError();
					throw Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, text);
				}
				if (((ulong)fileSystemFlags & 0x20000uL) == 0L)
				{
					throw new NotSupportedException(SR.PlatformNotSupported_FileEncryption);
				}
			}
		}
		throw Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, fullPath);
	}

	public static void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
	{
		int num = Interop.Kernel32.CopyFile(sourceFullPath, destFullPath, !overwrite);
		if (num == 0)
		{
			return;
		}
		string path = destFullPath;
		if (num != 80)
		{
			using (SafeFileHandle safeFileHandle = Interop.Kernel32.CreateFile(sourceFullPath, int.MinValue, FileShare.Read, FileMode.Open, 0))
			{
				if (safeFileHandle.IsInvalid)
				{
					path = sourceFullPath;
				}
			}
			if (num == 5 && DirectoryExists(destFullPath))
			{
				throw new IOException(SR.Format(SR.Arg_FileIsDirectory_Name, destFullPath), 5);
			}
		}
		throw Win32Marshal.GetExceptionForWin32Error(num, path);
	}

	public static void ReplaceFile(string sourceFullPath, string destFullPath, string destBackupFullPath, bool ignoreMetadataErrors)
	{
		int dwReplaceFlags = (ignoreMetadataErrors ? 2 : 0);
		if (!Interop.Kernel32.ReplaceFile(destFullPath, sourceFullPath, destBackupFullPath, dwReplaceFlags, IntPtr.Zero, IntPtr.Zero))
		{
			throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
		}
	}

	public static void DeleteFile(string fullPath)
	{
		if (!Interop.Kernel32.DeleteFile(fullPath))
		{
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			if (lastPInvokeError != 2)
			{
				throw Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, fullPath);
			}
		}
	}

	public static FileAttributes GetAttributes(string fullPath)
	{
		return (FileAttributes)GetAttributeData(fullPath, returnErrorOnNotFound: true).dwFileAttributes;
	}

	public static FileAttributes GetAttributes(SafeFileHandle fileHandle)
	{
		return (FileAttributes)GetAttributeData(fileHandle).dwFileAttributes;
	}

	public static DateTimeOffset GetCreationTime(string fullPath)
	{
		return GetAttributeData(fullPath).ftCreationTime.ToDateTimeOffset();
	}

	public static DateTimeOffset GetCreationTime(SafeFileHandle fileHandle)
	{
		return GetAttributeData(fileHandle).ftCreationTime.ToDateTimeOffset();
	}

	public static DateTimeOffset GetLastAccessTime(string fullPath)
	{
		return GetAttributeData(fullPath).ftLastAccessTime.ToDateTimeOffset();
	}

	public static DateTimeOffset GetLastAccessTime(SafeFileHandle fileHandle)
	{
		return GetAttributeData(fileHandle).ftLastAccessTime.ToDateTimeOffset();
	}

	public static DateTimeOffset GetLastWriteTime(string fullPath)
	{
		return GetAttributeData(fullPath).ftLastWriteTime.ToDateTimeOffset();
	}

	public static DateTimeOffset GetLastWriteTime(SafeFileHandle fileHandle)
	{
		return GetAttributeData(fileHandle).ftLastWriteTime.ToDateTimeOffset();
	}

	internal static Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA GetAttributeData(string fullPath, bool returnErrorOnNotFound = false)
	{
		Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default(Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA);
		int num = FillAttributeInfo(fullPath, ref data, returnErrorOnNotFound);
		if (num == 0)
		{
			return data;
		}
		throw Win32Marshal.GetExceptionForWin32Error(num, fullPath);
	}

	internal static Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA GetAttributeData(SafeFileHandle fileHandle)
	{
		Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default(Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA);
		int num = FillAttributeInfo(fileHandle, ref data);
		if (num == 0)
		{
			return data;
		}
		throw Win32Marshal.GetExceptionForWin32Error(num, fileHandle.Path);
	}

	private static int FillAttributeInfo(SafeFileHandle fileHandle, ref Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data)
	{
		if (!Interop.Kernel32.GetFileInformationByHandle(fileHandle, out var lpFileInformation))
		{
			return Marshal.GetLastPInvokeError();
		}
		PopulateAttributeData(ref data, in lpFileInformation);
		return 0;
	}

	private static void PopulateAttributeData(ref Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data, in Interop.Kernel32.BY_HANDLE_FILE_INFORMATION fileInformationData)
	{
		data.dwFileAttributes = (int)fileInformationData.dwFileAttributes;
		data.ftCreationTime = fileInformationData.ftCreationTime;
		data.ftLastAccessTime = fileInformationData.ftLastAccessTime;
		data.ftLastWriteTime = fileInformationData.ftLastWriteTime;
		data.nFileSizeHigh = fileInformationData.nFileSizeHigh;
		data.nFileSizeLow = fileInformationData.nFileSizeLow;
	}

	private static void MoveDirectory(string sourceFullPath, string destFullPath, bool _)
	{
		ReadOnlySpan<char> span = Path.GetPathRoot(sourceFullPath);
		ReadOnlySpan<char> other = Path.GetPathRoot(destFullPath);
		if (!MemoryExtensions.Equals(span, other, StringComparison.OrdinalIgnoreCase))
		{
			throw new IOException(SR.IO_SourceDestMustHaveSameRoot);
		}
		if (!Interop.Kernel32.MoveFile(sourceFullPath, destFullPath, overwrite: false))
		{
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			switch (lastPInvokeError)
			{
			case 2:
				throw Win32Marshal.GetExceptionForWin32Error(3, sourceFullPath);
			case 183:
				throw Win32Marshal.GetExceptionForWin32Error(183, destFullPath);
			case 5:
				throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, sourceFullPath), Win32Marshal.MakeHRFromErrorCode(lastPInvokeError));
			default:
				throw Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
			}
		}
	}

	public static void MoveFile(string sourceFullPath, string destFullPath, bool overwrite)
	{
		if (!Interop.Kernel32.MoveFile(sourceFullPath, destFullPath, overwrite))
		{
			throw Win32Marshal.GetExceptionForLastWin32Error();
		}
	}

	private static SafeFileHandle OpenHandleToWriteAttributes(string fullPath, bool asDirectory)
	{
		if (fullPath.Length == PathInternal.GetRootLength(fullPath) && fullPath[1] == Path.VolumeSeparatorChar)
		{
			throw new ArgumentException(SR.Arg_PathIsVolume, "path");
		}
		int num = 2097152;
		if (asDirectory)
		{
			num |= 0x2000000;
		}
		SafeFileHandle safeFileHandle = Interop.Kernel32.CreateFile(fullPath, 256, FileShare.ReadWrite | FileShare.Delete, FileMode.Open, num);
		if (safeFileHandle.IsInvalid)
		{
			int num2 = Marshal.GetLastPInvokeError();
			if (!asDirectory && num2 == 3 && fullPath.Equals(Directory.GetDirectoryRoot(fullPath)))
			{
				num2 = 5;
			}
			safeFileHandle.Dispose();
			throw Win32Marshal.GetExceptionForWin32Error(num2, fullPath);
		}
		return safeFileHandle;
	}

	public static void RemoveDirectory(string fullPath, bool recursive)
	{
		if (!recursive)
		{
			RemoveDirectoryInternal(fullPath, topLevel: true);
			return;
		}
		Interop.Kernel32.WIN32_FIND_DATA findData = default(Interop.Kernel32.WIN32_FIND_DATA);
		GetFindData(fullPath, isDirectory: true, ignoreAccessDenied: true, ref findData);
		if (IsNameSurrogateReparsePoint(ref findData))
		{
			RemoveDirectoryInternal(fullPath, topLevel: true);
			return;
		}
		fullPath = PathInternal.EnsureExtendedPrefix(fullPath);
		RemoveDirectoryRecursive(fullPath, ref findData, topLevel: true);
	}

	private static void GetFindData(string fullPath, bool isDirectory, bool ignoreAccessDenied, ref Interop.Kernel32.WIN32_FIND_DATA findData)
	{
		using SafeFindHandle safeFindHandle = Interop.Kernel32.FindFirstFile(Path.TrimEndingDirectorySeparator(fullPath), ref findData);
		if (safeFindHandle.IsInvalid)
		{
			int num = Marshal.GetLastPInvokeError();
			if (isDirectory && num == 2)
			{
				num = 3;
			}
			if (!ignoreAccessDenied || num != 5)
			{
				throw Win32Marshal.GetExceptionForWin32Error(num, fullPath);
			}
		}
	}

	private static bool IsNameSurrogateReparsePoint(ref Interop.Kernel32.WIN32_FIND_DATA data)
	{
		if ((data.dwFileAttributes & 0x400u) != 0)
		{
			return (data.dwReserved0 & 0x20000000) != 0;
		}
		return false;
	}

	private static void RemoveDirectoryRecursive(string fullPath, ref Interop.Kernel32.WIN32_FIND_DATA findData, bool topLevel)
	{
		Exception ex = null;
		using (SafeFindHandle safeFindHandle = Interop.Kernel32.FindFirstFile(Path.Join(fullPath, "*"), ref findData))
		{
			if (safeFindHandle.IsInvalid)
			{
				throw Win32Marshal.GetExceptionForLastWin32Error(fullPath);
			}
			int lastPInvokeError;
			do
			{
				if ((findData.dwFileAttributes & 0x10) == 0)
				{
					string stringFromFixedBuffer = findData.cFileName.GetStringFromFixedBuffer();
					if (!Interop.Kernel32.DeleteFile(Path.Combine(fullPath, stringFromFixedBuffer)) && ex == null)
					{
						lastPInvokeError = Marshal.GetLastPInvokeError();
						if (lastPInvokeError != 2)
						{
							ex = Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, stringFromFixedBuffer);
						}
					}
				}
				else
				{
					if (findData.cFileName.FixedBufferEqualsString(".") || findData.cFileName.FixedBufferEqualsString(".."))
					{
						continue;
					}
					string stringFromFixedBuffer2 = findData.cFileName.GetStringFromFixedBuffer();
					if (!IsNameSurrogateReparsePoint(ref findData))
					{
						try
						{
							RemoveDirectoryRecursive(Path.Combine(fullPath, stringFromFixedBuffer2), ref findData, topLevel: false);
						}
						catch (Exception ex2)
						{
							if (ex == null)
							{
								ex = ex2;
							}
						}
						continue;
					}
					if (findData.dwReserved0 == 2684354563u)
					{
						string mountPoint = Path.Join(fullPath, stringFromFixedBuffer2, "\\");
						if (!Interop.Kernel32.DeleteVolumeMountPoint(mountPoint) && ex == null)
						{
							lastPInvokeError = Marshal.GetLastPInvokeError();
							if (lastPInvokeError != 0 && lastPInvokeError != 3)
							{
								ex = Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, stringFromFixedBuffer2);
							}
						}
					}
					if (!Interop.Kernel32.RemoveDirectory(Path.Combine(fullPath, stringFromFixedBuffer2)) && ex == null)
					{
						lastPInvokeError = Marshal.GetLastPInvokeError();
						if (lastPInvokeError != 3)
						{
							ex = Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, stringFromFixedBuffer2);
						}
					}
				}
			}
			while (Interop.Kernel32.FindNextFile(safeFindHandle, ref findData));
			if (ex != null)
			{
				throw ex;
			}
			lastPInvokeError = Marshal.GetLastPInvokeError();
			if (lastPInvokeError != 0 && lastPInvokeError != 18)
			{
				throw Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, fullPath);
			}
		}
		RemoveDirectoryInternal(fullPath, topLevel, allowDirectoryNotEmpty: true);
	}

	private static void RemoveDirectoryInternal(string fullPath, bool topLevel, bool allowDirectoryNotEmpty = false)
	{
		if (Interop.Kernel32.RemoveDirectory(fullPath))
		{
			return;
		}
		int num = Marshal.GetLastPInvokeError();
		switch (num)
		{
		case 2:
			num = 3;
			goto case 3;
		case 3:
			if (!topLevel)
			{
				return;
			}
			break;
		case 145:
			if (allowDirectoryNotEmpty)
			{
				return;
			}
			break;
		case 5:
			throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, fullPath));
		}
		throw Win32Marshal.GetExceptionForWin32Error(num, fullPath);
	}

	public static void SetAttributes(string fullPath, FileAttributes attributes)
	{
		if (Interop.Kernel32.SetFileAttributes(fullPath, (int)attributes))
		{
			return;
		}
		int lastPInvokeError = Marshal.GetLastPInvokeError();
		if (lastPInvokeError == 87)
		{
			throw new ArgumentException(SR.Arg_InvalidFileAttrs, "attributes");
		}
		throw Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, fullPath);
	}

	public unsafe static void SetAttributes(SafeFileHandle fileHandle, FileAttributes attributes)
	{
		Interop.Kernel32.FILE_BASIC_INFO fILE_BASIC_INFO = default(Interop.Kernel32.FILE_BASIC_INFO);
		fILE_BASIC_INFO.FileAttributes = (uint)attributes;
		Interop.Kernel32.FILE_BASIC_INFO fILE_BASIC_INFO2 = fILE_BASIC_INFO;
		if (!Interop.Kernel32.SetFileInformationByHandle(fileHandle, 0, &fILE_BASIC_INFO2, (uint)sizeof(Interop.Kernel32.FILE_BASIC_INFO)))
		{
			throw Win32Marshal.GetExceptionForLastWin32Error(fileHandle.Path);
		}
	}

	private static void SetFileTime(string fullPath, bool asDirectory, long creationTime = -1L, long lastAccessTime = -1L, long lastWriteTime = -1L, long changeTime = -1L, uint fileAttributes = 0u)
	{
		using SafeFileHandle fileHandle = OpenHandleToWriteAttributes(fullPath, asDirectory);
		SetFileTime(fileHandle, fullPath, creationTime, lastAccessTime, lastWriteTime, changeTime, fileAttributes);
	}

	private unsafe static void SetFileTime(SafeFileHandle fileHandle, string fullPath = null, long creationTime = -1L, long lastAccessTime = -1L, long lastWriteTime = -1L, long changeTime = -1L, uint fileAttributes = 0u)
	{
		Interop.Kernel32.FILE_BASIC_INFO fILE_BASIC_INFO = default(Interop.Kernel32.FILE_BASIC_INFO);
		fILE_BASIC_INFO.CreationTime = creationTime;
		fILE_BASIC_INFO.LastAccessTime = lastAccessTime;
		fILE_BASIC_INFO.LastWriteTime = lastWriteTime;
		fILE_BASIC_INFO.ChangeTime = changeTime;
		fILE_BASIC_INFO.FileAttributes = fileAttributes;
		Interop.Kernel32.FILE_BASIC_INFO fILE_BASIC_INFO2 = fILE_BASIC_INFO;
		if (!Interop.Kernel32.SetFileInformationByHandle(fileHandle, 0, &fILE_BASIC_INFO2, (uint)sizeof(Interop.Kernel32.FILE_BASIC_INFO)))
		{
			throw Win32Marshal.GetExceptionForLastWin32Error(fullPath ?? fileHandle.Path);
		}
	}

	public static void SetCreationTime(string fullPath, DateTimeOffset time, bool asDirectory)
	{
		SetFileTime(fullPath, asDirectory, time.ToFileTime(), -1L, -1L, -1L);
	}

	public static void SetCreationTime(SafeFileHandle fileHandle, DateTimeOffset time)
	{
		SetFileTime(fileHandle, null, time.ToFileTime(), -1L, -1L, -1L);
	}

	public static void SetLastAccessTime(string fullPath, DateTimeOffset time, bool asDirectory)
	{
		SetFileTime(fullPath, asDirectory, -1L, time.ToFileTime(), -1L, -1L);
	}

	public static void SetLastAccessTime(SafeFileHandle fileHandle, DateTimeOffset time)
	{
		SetFileTime(fileHandle, null, -1L, time.ToFileTime(), -1L, -1L);
	}

	public static void SetLastWriteTime(string fullPath, DateTimeOffset time, bool asDirectory)
	{
		SetFileTime(fullPath, asDirectory, -1L, -1L, time.ToFileTime(), -1L);
	}

	public static void SetLastWriteTime(SafeFileHandle fileHandle, DateTimeOffset time)
	{
		SetFileTime(fileHandle, null, -1L, -1L, time.ToFileTime(), -1L);
	}

	public static string[] GetLogicalDrives()
	{
		return DriveInfoInternal.GetLogicalDrives();
	}

	internal static void CreateSymbolicLink(string path, string pathToTarget, bool isDirectory)
	{
		Interop.Kernel32.CreateSymbolicLink(path, pathToTarget, isDirectory);
	}

	internal static FileSystemInfo ResolveLinkTarget(string linkPath, bool returnFinalTarget, bool isDirectory)
	{
		string text = (returnFinalTarget ? GetFinalLinkTarget(linkPath, isDirectory) : GetImmediateLinkTarget(linkPath, isDirectory, throwOnError: true, returnFullPath: true));
		if (text != null)
		{
			if (!isDirectory)
			{
				return new FileInfo(text);
			}
			return new DirectoryInfo(text);
		}
		return null;
	}

	internal static string GetLinkTarget(string linkPath, bool isDirectory)
	{
		return GetImmediateLinkTarget(linkPath, isDirectory, throwOnError: false, returnFullPath: false);
	}

	internal unsafe static string GetImmediateLinkTarget(string linkPath, bool isDirectory, bool throwOnError, bool returnFullPath)
	{
		using (SafeFileHandle safeFileHandle = OpenSafeFileHandle(linkPath, 35651584))
		{
			if (safeFileHandle.IsInvalid)
			{
				if (!throwOnError)
				{
					return null;
				}
				int num = Marshal.GetLastPInvokeError();
				if (isDirectory && num == 2)
				{
					num = 3;
				}
				throw Win32Marshal.GetExceptionForWin32Error(num, linkPath);
			}
			byte[] array = ArrayPool<byte>.Shared.Rent(16384);
			try
			{
				bool flag;
				fixed (byte* lpOutBuffer = array)
				{
					flag = Interop.Kernel32.DeviceIoControl(safeFileHandle, 589992u, null, 0u, lpOutBuffer, 16384u, out var _, IntPtr.Zero);
				}
				if (!flag)
				{
					if (!throwOnError)
					{
						return null;
					}
					int lastPInvokeError = Marshal.GetLastPInvokeError();
					if (lastPInvokeError == 4390)
					{
						return null;
					}
					throw Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, linkPath);
				}
				Span<byte> span = new Span<byte>(array);
				flag = MemoryMarshal.TryRead<Interop.Kernel32.SymbolicLinkReparseBuffer>(span, out var value);
				if (value.ReparseTag == 2684354572u)
				{
					int start = sizeof(Interop.Kernel32.SymbolicLinkReparseBuffer) + value.SubstituteNameOffset;
					int substituteNameLength = value.SubstituteNameLength;
					Span<char> span2 = MemoryMarshal.Cast<byte, char>(span.Slice(start, substituteNameLength));
					if ((value.Flags & 1) == 0)
					{
						if (span2.StartsWith("\\??\\UNC\\".AsSpan()))
						{
							return Path.Join("\\\\".AsSpan(), span2.Slice("\\??\\UNC\\".Length));
						}
						return GetTargetPathWithoutNTPrefix(span2);
					}
					if (returnFullPath)
					{
						return Path.Join(Path.GetDirectoryName(linkPath.AsSpan()), span2);
					}
					return span2.ToString();
				}
				if (value.ReparseTag == 2684354563u)
				{
					flag = MemoryMarshal.TryRead<Interop.Kernel32.MountPointReparseBuffer>(span, out var value2);
					int start2 = sizeof(Interop.Kernel32.MountPointReparseBuffer) + value2.SubstituteNameOffset;
					int substituteNameLength2 = value2.SubstituteNameLength;
					Span<char> span3 = MemoryMarshal.Cast<byte, char>(span.Slice(start2, substituteNameLength2));
					return GetTargetPathWithoutNTPrefix(span3);
				}
				return null;
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
		static string GetTargetPathWithoutNTPrefix(ReadOnlySpan<char> targetPath)
		{
			return targetPath.Slice("\\??\\".Length).ToString();
		}
	}

	private static string GetFinalLinkTarget(string linkPath, bool isDirectory)
	{
		Interop.Kernel32.WIN32_FIND_DATA findData = default(Interop.Kernel32.WIN32_FIND_DATA);
		GetFindData(linkPath, isDirectory, ignoreAccessDenied: false, ref findData);
		if ((findData.dwFileAttributes & 0x400) == 0 || (findData.dwReserved0 != 2684354572u && findData.dwReserved0 != 2684354563u))
		{
			return null;
		}
		using (SafeFileHandle safeFileHandle = OpenSafeFileHandle(linkPath, 33554435))
		{
			if (safeFileHandle.IsInvalid)
			{
				int lastPInvokeError = Marshal.GetLastPInvokeError();
				if (IsPathUnreachableError(lastPInvokeError))
				{
					return GetFinalLinkTargetSlow(linkPath);
				}
				throw Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, linkPath);
			}
			char[] array = ArrayPool<char>.Shared.Rent(4096);
			try
			{
				uint num = GetFinalPathNameByHandle(safeFileHandle, array);
				if (num > array.Length)
				{
					char[] array2 = array;
					array = ArrayPool<char>.Shared.Rent((int)num);
					ArrayPool<char>.Shared.Return(array2);
					num = GetFinalPathNameByHandle(safeFileHandle, array);
				}
				if (num == 0)
				{
					throw Win32Marshal.GetExceptionForLastWin32Error(linkPath);
				}
				int num2 = ((!PathInternal.IsExtended(linkPath.AsSpan())) ? 4 : 0);
				return new string(array, num2, (int)num - num2);
			}
			finally
			{
				ArrayPool<char>.Shared.Return(array);
			}
		}
		string GetFinalLinkTargetSlow(string linkPath)
		{
			string immediateLinkTarget = GetImmediateLinkTarget(linkPath, isDirectory, throwOnError: false, returnFullPath: true);
			string result = null;
			while (immediateLinkTarget != null)
			{
				result = immediateLinkTarget;
				immediateLinkTarget = GetImmediateLinkTarget(immediateLinkTarget, isDirectory, throwOnError: false, returnFullPath: true);
			}
			return result;
		}
		unsafe static uint GetFinalPathNameByHandle(SafeFileHandle handle, char[] buffer)
		{
			fixed (char* lpszFilePath = buffer)
			{
				return Interop.Kernel32.GetFinalPathNameByHandle(handle, lpszFilePath, (uint)buffer.Length, 0u);
			}
		}
	}

	private unsafe static SafeFileHandle OpenSafeFileHandle(string path, int flags)
	{
		return Interop.Kernel32.CreateFile(path, 0, FileShare.ReadWrite | FileShare.Delete, null, FileMode.Open, flags, IntPtr.Zero);
	}
}
