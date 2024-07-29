using System;
using System.Runtime.InteropServices;

using static BizHawk.BizInvoke.MemoryBlock;
using static BizHawk.BizInvoke.POSIXLibC;

namespace BizHawk.BizInvoke
{
	internal sealed class MemoryBlockLinuxPal : IMemoryBlockPal
	{
		public ulong Start { get; }
		private readonly ulong _size;
		private bool _disposed;

		/// <summary>
		/// Map some bytes
		/// </summary>
		/// <param name="size"></param>
		/// <exception cref="InvalidOperationException">
		/// failed to mmap
		/// </exception>
		public MemoryBlockLinuxPal(ulong size)
		{
			var ptr = (ulong)mmap(IntPtr.Zero, Z.UU(size), MemoryProtection.None, 0x22 /* MAP_PRIVATE | MAP_ANON */, -1, IntPtr.Zero);
			if (ptr == ulong.MaxValue)
				throw new InvalidOperationException($"{nameof(mmap)}() failed with error {Marshal.GetLastWin32Error()}");
			_size = size;
			Start = ptr;
		}

		public void Dispose()
		{
			if (_disposed)
				return;
			_ = munmap(Z.US(Start), Z.UU(_size));
			_disposed = true;
			GC.SuppressFinalize(this);
		}

		~MemoryBlockLinuxPal()
		{
			Dispose();
		}

		private static MemoryProtection ToMemoryProtection(Protection prot)
		{
			switch (prot)
			{
				case Protection.None:
					return MemoryProtection.None;
				case Protection.R:
					return MemoryProtection.Read;
				case Protection.RW:
					return MemoryProtection.Read | MemoryProtection.Write;
				case Protection.RX:
					return MemoryProtection.Read | MemoryProtection.Execute;
				default:
					throw new ArgumentOutOfRangeException(nameof(prot));
			}
		}

		public void Protect(ulong start, ulong size, Protection prot)
		{
			var errorCode = mprotect(
				Z.US(start),
				Z.UU(size),
				ToMemoryProtection(prot)
			);
			if (errorCode != 0)
				throw new InvalidOperationException($"{nameof(mprotect)}() failed with error {Marshal.GetLastWin32Error()}!");
		}
	}

	public static class POSIXLibC
	{
		[DllImport("libc.so.6")]
		public static extern int close(int fd);

		[DllImport("libc.so.6")]
		public static extern int memfd_create(string name, uint flags);

		[DllImport("libc.so.6")]
		private static extern IntPtr mmap(IntPtr addr, UIntPtr length, int prot, int flags, int fd, IntPtr offset);

		public static IntPtr mmap(IntPtr addr, UIntPtr length, MemoryProtection prot, int flags, int fd, IntPtr offset) => mmap(addr, length, (int) prot, flags, fd, offset);

		[DllImport("libc.so.6")]
		private static extern int mprotect(IntPtr addr, UIntPtr len, int prot);

		public static int mprotect(IntPtr addr, UIntPtr len, MemoryProtection prot) => mprotect(addr, len, (int) prot);

		[DllImport("libc.so.6")]
		public static extern int munmap(IntPtr addr, UIntPtr length);
		[DllImport("libc.so.6")]
		public static extern int ftruncate(int fd, IntPtr length);

		/// <remarks>32-bit signed int</remarks>
		[Flags]
		public enum MemoryProtection : int { None = 0x0, Read = 0x1, Write = 0x2, Execute = 0x4 }
	}
}
