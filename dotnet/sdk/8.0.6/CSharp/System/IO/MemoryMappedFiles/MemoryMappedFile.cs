using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.IO.MemoryMappedFiles;

public class MemoryMappedFile : IDisposable
{
	private readonly SafeMemoryMappedFileHandle _handle;

	private readonly bool _leaveOpen;

	private readonly SafeFileHandle _fileHandle;

	public SafeMemoryMappedFileHandle SafeMemoryMappedFileHandle => _handle;

	private MemoryMappedFile(SafeMemoryMappedFileHandle handle)
	{
		_handle = handle;
		_leaveOpen = true;
	}

	private MemoryMappedFile(SafeMemoryMappedFileHandle handle, SafeFileHandle fileHandle, bool leaveOpen)
	{
		_handle = handle;
		_fileHandle = fileHandle;
		_leaveOpen = leaveOpen;
	}

	[SupportedOSPlatform("windows")]
	public static MemoryMappedFile OpenExisting(string mapName)
	{
		return OpenExisting(mapName, MemoryMappedFileRights.ReadWrite, HandleInheritability.None);
	}

	[SupportedOSPlatform("windows")]
	public static MemoryMappedFile OpenExisting(string mapName, MemoryMappedFileRights desiredAccessRights)
	{
		return OpenExisting(mapName, desiredAccessRights, HandleInheritability.None);
	}

	[SupportedOSPlatform("windows")]
	public static MemoryMappedFile OpenExisting(string mapName, MemoryMappedFileRights desiredAccessRights, HandleInheritability inheritability)
	{
		ArgumentException.ThrowIfNullOrEmpty(mapName, "mapName");
		if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
		{
			throw new ArgumentOutOfRangeException("inheritability");
		}
		if (((uint)desiredAccessRights & 0xFEF0FFF0u) != 0)
		{
			throw new ArgumentOutOfRangeException("desiredAccessRights");
		}
		SafeMemoryMappedFileHandle handle = OpenCore(mapName, inheritability, desiredAccessRights, createOrOpen: false);
		return new MemoryMappedFile(handle);
	}

	public static MemoryMappedFile CreateFromFile(string path)
	{
		return CreateFromFile(path, FileMode.Open, null, 0L, MemoryMappedFileAccess.ReadWrite);
	}

	public static MemoryMappedFile CreateFromFile(string path, FileMode mode)
	{
		return CreateFromFile(path, mode, null, 0L, MemoryMappedFileAccess.ReadWrite);
	}

	public static MemoryMappedFile CreateFromFile(string path, FileMode mode, string? mapName)
	{
		return CreateFromFile(path, mode, mapName, 0L, MemoryMappedFileAccess.ReadWrite);
	}

	public static MemoryMappedFile CreateFromFile(string path, FileMode mode, string? mapName, long capacity)
	{
		return CreateFromFile(path, mode, mapName, capacity, MemoryMappedFileAccess.ReadWrite);
	}

	public static MemoryMappedFile CreateFromFile(string path, FileMode mode, string? mapName, long capacity, MemoryMappedFileAccess access)
	{
		ArgumentNullException.ThrowIfNull(path, "path");
		ValidateCreateFile(mapName, capacity, access);
		bool existed = mode switch
		{
			FileMode.Append => throw new ArgumentException(System.SR.Argument_NewMMFAppendModeNotAllowed, "mode"), 
			FileMode.Truncate => throw new ArgumentException(System.SR.Argument_NewMMFTruncateModeNotAllowed, "mode"), 
			FileMode.Open => true, 
			FileMode.CreateNew => false, 
			_ => File.Exists(path), 
		};
		SafeFileHandle safeFileHandle = File.OpenHandle(path, mode, GetFileAccess(access), FileShare.Read, FileOptions.None, 0L);
		long num = 0L;
		if ((uint)(mode - 1) > 1u)
		{
			try
			{
				num = RandomAccess.GetLength(safeFileHandle);
			}
			catch
			{
				safeFileHandle.Dispose();
				throw;
			}
		}
		if (capacity == 0L && num == 0L)
		{
			CleanupFile(safeFileHandle, existed, path);
			throw new ArgumentException(System.SR.Argument_EmptyFile);
		}
		if (capacity == 0L)
		{
			capacity = num;
		}
		SafeMemoryMappedFileHandle handle;
		try
		{
			handle = CreateCore(safeFileHandle, mapName, HandleInheritability.None, access, MemoryMappedFileOptions.None, capacity, num);
		}
		catch
		{
			CleanupFile(safeFileHandle, existed, path);
			throw;
		}
		return new MemoryMappedFile(handle, safeFileHandle, leaveOpen: false);
	}

	public static MemoryMappedFile CreateFromFile(SafeFileHandle fileHandle, string? mapName, long capacity, MemoryMappedFileAccess access, HandleInheritability inheritability, bool leaveOpen)
	{
		ArgumentNullException.ThrowIfNull(fileHandle, "fileHandle");
		ValidateCreateFile(mapName, capacity, access);
		long length = RandomAccess.GetLength(fileHandle);
		if (capacity == 0L && length == 0L)
		{
			throw new ArgumentException(System.SR.Argument_EmptyFile);
		}
		if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
		{
			throw new ArgumentOutOfRangeException("inheritability");
		}
		if (capacity == 0L)
		{
			capacity = length;
		}
		SafeMemoryMappedFileHandle handle = CreateCore(fileHandle, mapName, inheritability, access, MemoryMappedFileOptions.None, capacity, length);
		return new MemoryMappedFile(handle, fileHandle, leaveOpen);
	}

	public static MemoryMappedFile CreateFromFile(FileStream fileStream, string? mapName, long capacity, MemoryMappedFileAccess access, HandleInheritability inheritability, bool leaveOpen)
	{
		ArgumentNullException.ThrowIfNull(fileStream, "fileStream");
		ValidateCreateFile(mapName, capacity, access);
		long length = fileStream.Length;
		if (capacity == 0L && length == 0L)
		{
			throw new ArgumentException(System.SR.Argument_EmptyFile);
		}
		if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
		{
			throw new ArgumentOutOfRangeException("inheritability");
		}
		fileStream.Flush();
		if (capacity == 0L)
		{
			capacity = length;
		}
		SafeFileHandle safeFileHandle = fileStream.SafeFileHandle;
		SafeMemoryMappedFileHandle handle = CreateCore(safeFileHandle, mapName, inheritability, access, MemoryMappedFileOptions.None, capacity, length);
		return new MemoryMappedFile(handle, safeFileHandle, leaveOpen);
	}

	public static MemoryMappedFile CreateNew(string? mapName, long capacity)
	{
		return CreateNew(mapName, capacity, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);
	}

	public static MemoryMappedFile CreateNew(string? mapName, long capacity, MemoryMappedFileAccess access)
	{
		return CreateNew(mapName, capacity, access, MemoryMappedFileOptions.None, HandleInheritability.None);
	}

	public static MemoryMappedFile CreateNew(string? mapName, long capacity, MemoryMappedFileAccess access, MemoryMappedFileOptions options, HandleInheritability inheritability)
	{
		if (mapName != null && mapName.Length == 0)
		{
			throw new ArgumentException(System.SR.Argument_MapNameEmptyString);
		}
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity, "capacity");
		if (IntPtr.Size == 4 && capacity > uint.MaxValue)
		{
			throw new ArgumentOutOfRangeException("capacity", System.SR.ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed);
		}
		switch (access)
		{
		default:
			throw new ArgumentOutOfRangeException("access");
		case MemoryMappedFileAccess.Write:
			throw new ArgumentException(System.SR.Argument_NewMMFWriteAccessNotAllowed, "access");
		case MemoryMappedFileAccess.ReadWrite:
		case MemoryMappedFileAccess.Read:
		case MemoryMappedFileAccess.CopyOnWrite:
		case MemoryMappedFileAccess.ReadExecute:
		case MemoryMappedFileAccess.ReadWriteExecute:
		{
			if (((uint)options & 0xFBFFFFFFu) != 0)
			{
				throw new ArgumentOutOfRangeException("options");
			}
			if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
			{
				throw new ArgumentOutOfRangeException("inheritability");
			}
			SafeMemoryMappedFileHandle handle = CreateCore(null, mapName, inheritability, access, options, capacity, -1L);
			return new MemoryMappedFile(handle);
		}
		}
	}

	[SupportedOSPlatform("windows")]
	public static MemoryMappedFile CreateOrOpen(string mapName, long capacity)
	{
		return CreateOrOpen(mapName, capacity, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);
	}

	[SupportedOSPlatform("windows")]
	public static MemoryMappedFile CreateOrOpen(string mapName, long capacity, MemoryMappedFileAccess access)
	{
		return CreateOrOpen(mapName, capacity, access, MemoryMappedFileOptions.None, HandleInheritability.None);
	}

	[SupportedOSPlatform("windows")]
	public static MemoryMappedFile CreateOrOpen(string mapName, long capacity, MemoryMappedFileAccess access, MemoryMappedFileOptions options, HandleInheritability inheritability)
	{
		ArgumentException.ThrowIfNullOrEmpty(mapName, "mapName");
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity, "capacity");
		if (IntPtr.Size == 4 && capacity > uint.MaxValue)
		{
			throw new ArgumentOutOfRangeException("capacity", System.SR.ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed);
		}
		if (access < MemoryMappedFileAccess.ReadWrite || access > MemoryMappedFileAccess.ReadWriteExecute)
		{
			throw new ArgumentOutOfRangeException("access");
		}
		if (((uint)options & 0xFBFFFFFFu) != 0)
		{
			throw new ArgumentOutOfRangeException("options");
		}
		if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
		{
			throw new ArgumentOutOfRangeException("inheritability");
		}
		SafeMemoryMappedFileHandle handle = ((access != MemoryMappedFileAccess.Write) ? CreateOrOpenCore(mapName, inheritability, access, options, capacity) : OpenCore(mapName, inheritability, access, createOrOpen: true));
		return new MemoryMappedFile(handle);
	}

	public MemoryMappedViewStream CreateViewStream()
	{
		return CreateViewStream(0L, 0L, MemoryMappedFileAccess.ReadWrite);
	}

	public MemoryMappedViewStream CreateViewStream(long offset, long size)
	{
		return CreateViewStream(offset, size, MemoryMappedFileAccess.ReadWrite);
	}

	public MemoryMappedViewStream CreateViewStream(long offset, long size, MemoryMappedFileAccess access)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		if (size < 0)
		{
			throw new ArgumentOutOfRangeException("size", System.SR.ArgumentOutOfRange_PositiveOrDefaultSizeRequired);
		}
		if (access < MemoryMappedFileAccess.ReadWrite || access > MemoryMappedFileAccess.ReadWriteExecute)
		{
			throw new ArgumentOutOfRangeException("access");
		}
		if (IntPtr.Size == 4 && size > uint.MaxValue)
		{
			throw new ArgumentOutOfRangeException("size", System.SR.ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed);
		}
		MemoryMappedView view = MemoryMappedView.CreateView(_handle, access, offset, size);
		return new MemoryMappedViewStream(view);
	}

	public MemoryMappedViewAccessor CreateViewAccessor()
	{
		return CreateViewAccessor(0L, 0L, MemoryMappedFileAccess.ReadWrite);
	}

	public MemoryMappedViewAccessor CreateViewAccessor(long offset, long size)
	{
		return CreateViewAccessor(offset, size, MemoryMappedFileAccess.ReadWrite);
	}

	public MemoryMappedViewAccessor CreateViewAccessor(long offset, long size, MemoryMappedFileAccess access)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		if (size < 0)
		{
			throw new ArgumentOutOfRangeException("size", System.SR.ArgumentOutOfRange_PositiveOrDefaultSizeRequired);
		}
		if (access < MemoryMappedFileAccess.ReadWrite || access > MemoryMappedFileAccess.ReadWriteExecute)
		{
			throw new ArgumentOutOfRangeException("access");
		}
		if (IntPtr.Size == 4 && size > uint.MaxValue)
		{
			throw new ArgumentOutOfRangeException("size", System.SR.ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed);
		}
		MemoryMappedView view = MemoryMappedView.CreateView(_handle, access, offset, size);
		return new MemoryMappedViewAccessor(view);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		try
		{
			if (!_handle.IsClosed)
			{
				_handle.Dispose();
			}
		}
		finally
		{
			if (!_leaveOpen)
			{
				_fileHandle?.Dispose();
			}
		}
	}

	internal static FileAccess GetFileAccess(MemoryMappedFileAccess access)
	{
		switch (access)
		{
		case MemoryMappedFileAccess.Read:
		case MemoryMappedFileAccess.ReadExecute:
			return FileAccess.Read;
		case MemoryMappedFileAccess.ReadWrite:
		case MemoryMappedFileAccess.CopyOnWrite:
		case MemoryMappedFileAccess.ReadWriteExecute:
			return FileAccess.ReadWrite;
		default:
			return FileAccess.Write;
		}
	}

	private static void CleanupFile(SafeFileHandle fileHandle, bool existed, string path)
	{
		fileHandle.Dispose();
		if (!existed)
		{
			File.Delete(path);
		}
	}

	private static void ValidateCreateFile(string mapName, long capacity, MemoryMappedFileAccess access)
	{
		if (mapName != null && mapName.Length == 0)
		{
			throw new ArgumentException(System.SR.Argument_MapNameEmptyString);
		}
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException("capacity", System.SR.ArgumentOutOfRange_PositiveOrDefaultCapacityRequired);
		}
		switch (access)
		{
		default:
			throw new ArgumentOutOfRangeException("access");
		case MemoryMappedFileAccess.Write:
			throw new ArgumentException(System.SR.Argument_NewMMFWriteAccessNotAllowed, "access");
		case MemoryMappedFileAccess.ReadWrite:
		case MemoryMappedFileAccess.Read:
		case MemoryMappedFileAccess.CopyOnWrite:
		case MemoryMappedFileAccess.ReadExecute:
		case MemoryMappedFileAccess.ReadWriteExecute:
			break;
		}
	}

	private static void VerifyMemoryMappedFileAccess(MemoryMappedFileAccess access, long capacity, long fileSize)
	{
		if (access == MemoryMappedFileAccess.Read && capacity > fileSize)
		{
			throw new ArgumentException(System.SR.Argument_ReadAccessWithLargeCapacity);
		}
		if (fileSize > capacity)
		{
			throw new ArgumentOutOfRangeException("capacity", System.SR.ArgumentOutOfRange_CapacityGEFileSizeRequired);
		}
	}

	private static SafeMemoryMappedFileHandle CreateCore(SafeFileHandle fileHandle, string mapName, HandleInheritability inheritability, MemoryMappedFileAccess access, MemoryMappedFileOptions options, long capacity, long fileSize)
	{
		global::Interop.Kernel32.SECURITY_ATTRIBUTES securityAttributes = GetSecAttrs(inheritability);
		if (fileHandle != null)
		{
			VerifyMemoryMappedFileAccess(access, capacity, fileSize);
		}
		SafeMemoryMappedFileHandle safeMemoryMappedFileHandle = ((fileHandle != null) ? global::Interop.CreateFileMapping(fileHandle, ref securityAttributes, GetPageAccess(access) | (int)options, capacity, mapName) : global::Interop.CreateFileMapping(new IntPtr(-1), ref securityAttributes, GetPageAccess(access) | (int)options, capacity, mapName));
		int lastPInvokeError = Marshal.GetLastPInvokeError();
		if (!safeMemoryMappedFileHandle.IsInvalid)
		{
			if (lastPInvokeError == 183)
			{
				safeMemoryMappedFileHandle.Dispose();
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
			}
			return safeMemoryMappedFileHandle;
		}
		safeMemoryMappedFileHandle.Dispose();
		throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
	}

	private static SafeMemoryMappedFileHandle OpenCore(string mapName, HandleInheritability inheritability, MemoryMappedFileAccess access, bool createOrOpen)
	{
		return OpenCore(mapName, inheritability, GetFileMapAccess(access), createOrOpen);
	}

	private static SafeMemoryMappedFileHandle OpenCore(string mapName, HandleInheritability inheritability, MemoryMappedFileRights rights, bool createOrOpen)
	{
		return OpenCore(mapName, inheritability, GetFileMapAccess(rights), createOrOpen);
	}

	private static SafeMemoryMappedFileHandle CreateOrOpenCore(string mapName, HandleInheritability inheritability, MemoryMappedFileAccess access, MemoryMappedFileOptions options, long capacity)
	{
		SafeMemoryMappedFileHandle safeMemoryMappedFileHandle = null;
		global::Interop.Kernel32.SECURITY_ATTRIBUTES securityAttributes = GetSecAttrs(inheritability);
		int num = 14;
		int num2 = 0;
		while (num > 0)
		{
			safeMemoryMappedFileHandle = global::Interop.CreateFileMapping(new IntPtr(-1), ref securityAttributes, GetPageAccess(access) | (int)options, capacity, mapName);
			if (!safeMemoryMappedFileHandle.IsInvalid)
			{
				break;
			}
			safeMemoryMappedFileHandle.Dispose();
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			if (lastPInvokeError != 5)
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
			}
			safeMemoryMappedFileHandle = global::Interop.OpenFileMapping(GetFileMapAccess(access), (inheritability & HandleInheritability.Inheritable) != 0, mapName);
			if (!safeMemoryMappedFileHandle.IsInvalid)
			{
				break;
			}
			safeMemoryMappedFileHandle.Dispose();
			int lastPInvokeError2 = Marshal.GetLastPInvokeError();
			if (lastPInvokeError2 != 2)
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError2);
			}
			num--;
			if (num2 == 0)
			{
				num2 = 10;
				continue;
			}
			Thread.Sleep(num2);
			num2 *= 2;
		}
		if (safeMemoryMappedFileHandle == null || safeMemoryMappedFileHandle.IsInvalid)
		{
			safeMemoryMappedFileHandle?.Dispose();
			throw new InvalidOperationException(System.SR.InvalidOperation_CantCreateFileMapping);
		}
		return safeMemoryMappedFileHandle;
	}

	private static int GetFileMapAccess(MemoryMappedFileRights rights)
	{
		return (int)rights;
	}

	internal static int GetFileMapAccess(MemoryMappedFileAccess access)
	{
		return access switch
		{
			MemoryMappedFileAccess.Read => 4, 
			MemoryMappedFileAccess.Write => 2, 
			MemoryMappedFileAccess.ReadWrite => 6, 
			MemoryMappedFileAccess.CopyOnWrite => 1, 
			MemoryMappedFileAccess.ReadExecute => 36, 
			_ => 38, 
		};
	}

	internal static int GetPageAccess(MemoryMappedFileAccess access)
	{
		return access switch
		{
			MemoryMappedFileAccess.Read => 2, 
			MemoryMappedFileAccess.ReadWrite => 4, 
			MemoryMappedFileAccess.CopyOnWrite => 8, 
			MemoryMappedFileAccess.ReadExecute => 32, 
			_ => 64, 
		};
	}

	private static SafeMemoryMappedFileHandle OpenCore(string mapName, HandleInheritability inheritability, int desiredAccessRights, bool createOrOpen)
	{
		SafeMemoryMappedFileHandle safeMemoryMappedFileHandle = global::Interop.OpenFileMapping(desiredAccessRights, (inheritability & HandleInheritability.Inheritable) != 0, mapName);
		int lastPInvokeError = Marshal.GetLastPInvokeError();
		if (safeMemoryMappedFileHandle.IsInvalid)
		{
			safeMemoryMappedFileHandle.Dispose();
			if (createOrOpen && lastPInvokeError == 2)
			{
				throw new ArgumentException(System.SR.Argument_NewMMFWriteAccessNotAllowed, "access");
			}
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
		}
		return safeMemoryMappedFileHandle;
	}

	private unsafe static global::Interop.Kernel32.SECURITY_ATTRIBUTES GetSecAttrs(HandleInheritability inheritability)
	{
		global::Interop.Kernel32.SECURITY_ATTRIBUTES result = default(global::Interop.Kernel32.SECURITY_ATTRIBUTES);
		if ((inheritability & HandleInheritability.Inheritable) != 0)
		{
			result = default(global::Interop.Kernel32.SECURITY_ATTRIBUTES);
			result.nLength = (uint)sizeof(global::Interop.Kernel32.SECURITY_ATTRIBUTES);
			result.bInheritHandle = global::Interop.BOOL.TRUE;
		}
		return result;
	}
}
