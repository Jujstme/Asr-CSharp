using System;
using System.Buffers;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Asr;

public class Process : IDisposable
{
    ulong _handle;

    public enum PointerSize
    {
        Bit16,
        Bit32,
        Bit64,
    }

    public enum StringType
    {
        Ansi,
        Unicode,
        Autodetect,
    }

    private Process(ulong handle)
    {
        _handle = handle;
    }

    public unsafe static Process? AttachByName(string processName)
    {
        var _processName = Encoding.UTF8.GetBytes(processName);

        ulong process;

        fixed (void* ptr = _processName)
            process = ProcessAttach(ptr, (nuint)_processName.Length);

        if (process == 0)
            return null;
        else
            return new Process(process);

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "process_attach")]
        static unsafe extern ulong ProcessAttach(void* name_ptr, nuint name_len);
    }

    public static Process? AttachByPID(ulong pid)
    {
        var id = ProcessAttachByID(pid);
        return id == 0 ? null : new Process(id);

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "process_attach_by_id")]
        static unsafe extern ulong ProcessAttachByID(ulong pid);
    }

    [System.Runtime.CompilerServices.SkipLocalsInit]
    public static unsafe Process[] GetProcessesByName(string processName)
    {
        byte[] _process = Encoding.UTF8.GetBytes(processName);

        Span<ulong> processIDs = stackalloc ulong[128];
        nuint length = default;
        bool success;

        fixed (void* ptr = _process)
        {
            fixed (ulong* ids = processIDs)
            {
                success = ProcessListByName(ptr, (nuint)_process.Length, ids, &length);
            }
        }

        var val = new Process[length];

        if (!success)
            return val;

        for (int i = 0; i < (int)length; i++)
        {
            if (processIDs[i] != 0)
                val[i] = new Process(processIDs[i]);
        }

        return val;

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "process_list_by_name")]
        static extern bool ProcessListByName(void* name_ptr, nuint name_len, ulong* list_ptr, nuint* length);
    }

    public bool IsOpen
    {
        get
        {
            return ProcessIsOpen(_handle);

            [WasmImportLinkage]
            [DllImport("env", EntryPoint = "process_is_open")]
            static unsafe extern bool ProcessIsOpen(ulong pid);
        }
    }

    public void Dispose()
    {
        ProcessDetach(_handle);

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "process_detach")]
        static unsafe extern void ProcessDetach(ulong pid);
    }

    public T? Read<T>(IntPtr address) where T : unmanaged
    {
        return Read(address, out T value) ? value : null;
    }

    public IntPtr? ReadPointer(IntPtr address, PointerSize pointerSize)
    {
        return ReadPointer(address, pointerSize, out IntPtr value) ? value : null;
    }

    public bool ReadPointer(IntPtr address, PointerSize pointerSize, out IntPtr value)
    {
        value = IntPtr.Zero;
            
        if (address == IntPtr.Zero)
            return false;

        bool success;

        if (pointerSize == PointerSize.Bit64)
        {
            success = Read(address, out long tempVal);
            value = (IntPtr)tempVal;
        }
        else if (pointerSize == PointerSize.Bit16)
        {
            success = Read(address, out short tempVal);
            value = (IntPtr)tempVal;

        }
        else // PointerSize.Bit32 assumed
        {
            success = Read(address, out int tempVal);
            value = (IntPtr)tempVal;
        }

        return success;
    }

    public T? ReadPointerPath<T>(IntPtr baseAddress, PointerSize pointerSize, params int[] offsets) where T : unmanaged
    {
        IntPtr addr = baseAddress;

        for (int i = 0; i < offsets.Length - 1; i++)
        {
            if (!ReadPointer(addr + offsets[i], pointerSize, out addr))
                return null;
        }

        return Read<T>(addr + offsets.Last());
    }

    [System.Runtime.CompilerServices.SkipLocalsInit]
    public bool Read<T>(IntPtr address, out T value) where T : unmanaged
    {
        nuint size = (nuint)Marshal.SizeOf<T>();
        T result;
        bool success;
            
        unsafe
        {
            success = ReadInternal(_handle, (ulong)address, &result, size);
        }
            
        value = result;
        return success;
    }

    public string? ReadString(IntPtr address, int maxLength)
    {
        if (ReadString(address, maxLength, out string value))
            return value;
        else
            return null;
    }

    public bool ReadString(IntPtr address, int maxLength, out string value)
    {
        return ReadString(address, maxLength, StringType.Autodetect, out value);
    }

    [System.Runtime.CompilerServices.SkipLocalsInit]
    public unsafe bool ReadString(IntPtr address, int maxLength, StringType stringType, out string value)
    {
        if (maxLength <= 0)
        {
            value = string.Empty;
            return false;
        }

        byte[]? rented = null;
        Span<byte> buffer = maxLength * 2 <= 1024
            ? stackalloc byte[1024]
            : (rented = ArrayPool<byte>.Shared.Rent(maxLength * 2));

        bool success;
        fixed (byte* ptr = buffer)
        {
            success = ReadInternal(_handle, (ulong)address, ptr, (nuint)maxLength * 2);
        }

        if (!success)
        {
            value = string.Empty;
            return false;
        }

        StringType sType = stringType;
        if (sType == StringType.Autodetect)
        {
            if (maxLength >=2 && buffer is [> 0, 0, > 0, 0, ..])
                sType = StringType.Unicode;
            else
                sType = StringType.Ansi;
        }

        if (sType == StringType.Unicode)
        {
            Span<char> charBuffer = MemoryMarshal.Cast<byte, char>(buffer);
            int length = charBuffer.IndexOf('\0');
            value = length == -1 ? buffer.ToString() : buffer[..length].ToString();
        }
        else
        {
            int length = buffer.IndexOf((byte)'\0');
            fixed (byte* ptr = buffer)
            {
                value = new string((sbyte*)ptr, 0, length == -1 ? buffer.Length : length);
            }
        }

        if (rented is not null)
            ArrayPool<byte>.Shared.Return(rented);

        return true;
    }

    [System.Runtime.CompilerServices.SkipLocalsInit]
    public T[]? ReadArray<T>(IntPtr address, int arrayLength) where T : unmanaged
    {
        if (arrayLength <= 0)
            return null;

        int byteSize = Marshal.SizeOf<T>() * arrayLength;

        T[]? rented = null;
        Span<T> buf = byteSize <= 1024
            ? stackalloc T[arrayLength]
            : (rented = ArrayPool<T>.Shared.Rent(arrayLength));

        bool success;

        unsafe
        {
            fixed (T* ptr = buf)
            {
                success = ReadInternal(_handle, (ulong)address, ptr, (nuint)byteSize);
            }
        }

        if (!success)
        {
            if (rented is not null)
                ArrayPool<T>.Shared.Return(rented);
            return null;
        }

        var val = buf.ToArray();
        if (rented is not null)
            ArrayPool<T>.Shared.Return(rented);
        return val;
    }


    public unsafe IntPtr? GetModuleAddress(string moduleName)
    {
        byte[] _moduleName = Encoding.UTF8.GetBytes(moduleName);

        IntPtr address;
        fixed (void* ptr = _moduleName)
            address = (IntPtr)ProcessGetModuleAddress(_handle, ptr, (nuint)moduleName.Length);

        return address == IntPtr.Zero ? null : address;


        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "process_get_module_address")]
        static unsafe extern ulong ProcessGetModuleAddress(ulong pid, void* name_ptr, nuint size);
    }

    public unsafe int? GetModuleSize(string moduleName)
    {
        byte[] _moduleName = Encoding.UTF8.GetBytes(moduleName);

        int size;
        fixed (void* ptr = _moduleName)
            size = (int)ProcessGetModuleSize(_handle, ptr, (nuint)moduleName.Length);

        return size == IntPtr.Zero ? null : size;

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "process_get_module_size")]
        static unsafe extern ulong ProcessGetModuleSize(ulong pid, void* name_ptr, nuint size);
    }

    public unsafe string? GetModulePath(string moduleName)
    {
        byte[] _moduleName = Encoding.UTF8.GetBytes(moduleName);

        Span<byte> buffer = stackalloc byte[512];
        nuint size = (nuint)buffer.Length;

        fixed (void* name_ptr = _moduleName)
        {
            fixed (void* buf_ptr = buffer)
            {
                if (!ProcessGetModulePath(_handle, name_ptr, (nuint)_moduleName.Length, buf_ptr, &size))
                    return null;
                else
                    return new string((sbyte*)buf_ptr, 0, (int)size);

            }
        }

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "process_get_module_path")]
        static unsafe extern bool ProcessGetModulePath(ulong pid, void* name_ptr, nuint name_size, void* buf_ptr, nuint* buf_len_ptr);
    }

    [WasmImportLinkage]
    [DllImport("env", EntryPoint = "process_read")]
    private static unsafe extern bool ReadInternal(ulong pid, ulong address, void* buf, nuint size);
}