using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Asr;

public static partial class Runtime
{
    public enum TimerState : uint
    {
        NOT_RUNNING = 0,
        RUNNING = 1,
        PAUSED = 2,
        ENDED = 3,
    }

    public static TimerState GetTimerState()
    {
        return (TimerState)TimerGetState();

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "timer_get_state")]
        static extern uint TimerGetState();
    }

    public unsafe static void PrintMessage(string message)
    {
        byte[] buf = Encoding.UTF8.GetBytes(message);
        
        fixed (void* ptr = buf)
            RuntimePrintMessage(ptr, (nuint)buf.Length);

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "runtime_print_message")]
        static extern void RuntimePrintMessage(void* buf, nuint size);
    }

    public unsafe static string GetOS()
    {
        Span<byte> buf = stackalloc byte[128];
        nuint size = (nuint)buf.Length;

        fixed (void* ptr = buf)
        {
            RuntimeGetOS(ptr, &size);
            return new string((sbyte*)ptr, 0, (int)size);
        }

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "runtime_get_os")]
        static extern bool RuntimeGetOS(void* buf, nuint* size);
    }

    public unsafe static string GetArch()
    {
        Span<byte> buf = stackalloc byte[128];
        nuint size = (nuint)buf.Length;

        fixed (void* ptr = buf)
        {
            RuntimeGetArch(ptr, &size);
            return new string((sbyte*)ptr, 0, (int)size);
        }

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "runtime_get_arch")]
        static extern bool RuntimeGetArch(void* buf, nuint* size);
    }

    public unsafe static void SetVariable(string name, string value)
    {
        var _name = Encoding.UTF8.GetBytes(name);
        var _value = Encoding.UTF8.GetBytes(value);

        fixed (void* key = _name)
        {
            fixed (void* __value = _value)
            {
                TimerSetVariable(key, (nuint)_name.Length, __value, (nuint)_value.Length);
            }
        }
        return;

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "timer_set_variable")]
        static extern void TimerSetVariable(void* key_ptr, nuint key_len, void* value_ptr, nuint value_len);
    }

    public static void SetTickRate(double refreshRate)
    {
        NewTickRate(refreshRate);

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "runtime_set_tick_rate")]
        static extern void NewTickRate(double value);
    }

    public static class Timer
    {
        public static void Start()
        {
            TimerStart();
            return;

            [WasmImportLinkage]
            [DllImport("env", EntryPoint = "timer_start")]
            static extern void TimerStart();
        }

        public static void Split()
        {
            TimerSplit();
            return;

            [WasmImportLinkage]
            [DllImport("env", EntryPoint = "timer_split")]
            static extern void TimerSplit();
        }

        public static void SkipSplit()
        {
            SkipSplit();
            return;

            [WasmImportLinkage]
            [DllImport("env", EntryPoint = "timer_skip_split")]
            static extern void SkipSplit();
        }

        public static void UndoSplit()
        {
            UndoSplit();
            return;

            [WasmImportLinkage]
            [DllImport("env", EntryPoint = "timer_undo_split")]
            static extern void UndoSplit();
        }

        public static void Reset()
        {
            Reset();
            return;

            [WasmImportLinkage]
            [DllImport("env", EntryPoint = "timer_reset")]
            static extern void Reset();
        }

        public static class SetGameTime
        {
            public static void FromTimeSpan(TimeSpan timeSpan)
            {
                long secs = timeSpan.Seconds;
                int nanos = timeSpan.Nanoseconds;
                TimerSetGameTime(secs, nanos);
                return;

                [WasmImportLinkage]
                [DllImport("env", EntryPoint = "timer_set_game_time")]
                static extern void TimerSetGameTime(long secs, int nanos);
            }

            public static void FromSeconds(double seconds)
            {
                FromTimeSpan(TimeSpan.FromSeconds(seconds));
            }

            public static void FromMilliseconds(double milliseconds)
            {
                FromTimeSpan(TimeSpan.FromMilliseconds(milliseconds));
            }

            public static void FromTicks(long ticks)
            {
                FromTimeSpan(TimeSpan.FromTicks(ticks));
            }
        }

        public static void PauseGameTime()
        {
            TimerPauseGameTime();
            return;

            [WasmImportLinkage]
            [DllImport("env", EntryPoint = "timer_pause_game_time")]
            static extern void TimerPauseGameTime();
        }

        public static void ResumeGameTime()
        {
            TimerResumeGameTime();
            return;

            [WasmImportLinkage]
            [DllImport("env", EntryPoint = "timer_resume_game_time")]
            static extern void TimerResumeGameTime();
        }
    }
}
