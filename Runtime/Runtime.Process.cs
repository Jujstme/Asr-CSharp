using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace Asr
{
    public static partial class Runtime
    {
        public class Process : IDisposable
        {
            ulong _pid;

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

            private Process(ulong pid)
            {
                _pid = pid;
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
                [DllImport("env", EntryPoint = "process_attach")]
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
                    return ProcessIsOpen(_pid);

                    [WasmImportLinkage]
                    [DllImport("env", EntryPoint = "process_is_open")]
                    static unsafe extern bool ProcessIsOpen(ulong pid);
                }
            }

            public void Dispose()
            {
                ProcessDetach(_pid);

                [WasmImportLinkage]
                [DllImport("env", EntryPoint = "process_detach")]
                static unsafe extern void ProcessDetach(ulong pid);
            }

            public T Read<T>(IntPtr address) where T : unmanaged
            {
                return Read(address, out T value) ? value : default;
            }

            public IntPtr ReadPointer(IntPtr address, PointerSize pointerSize)
            {
                return pointerSize switch
                {
                    PointerSize.Bit64 => Read(address, out long value) ? (IntPtr)value : IntPtr.Zero,
                    PointerSize.Bit16 => Read(address, out short value) ? (IntPtr)value : IntPtr.Zero,
                    _ or PointerSize.Bit32 => Read(address, out int value) ? (IntPtr)value : IntPtr.Zero,
                };
            }

            [System.Runtime.CompilerServices.SkipLocalsInit]
            public bool Read<T>(IntPtr address, out T value) where T : unmanaged
            {
                nuint size = (nuint)Marshal.SizeOf<T>();
                T result;
                bool success;
                
                unsafe
                {
                    success = ReadInternal(_pid, (ulong)address, &result, size);
                }
                
                value = result;
                return success;
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
                    success = ReadInternal(_pid, (ulong)address, ptr, (nuint)maxLength * 2);
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

            [WasmImportLinkage]
            [DllImport("env", EntryPoint = "process_read")]
            private static unsafe extern bool ReadInternal(ulong pid, ulong address, void* buf, nuint size);
        }
    }
}
