using System;
using System.Runtime.InteropServices;

namespace Asr;

public static class Asr
{
    private static bool firstLoop = true;
    private static Runtime.Process? proc { get; set; }

    [UnmanagedCallersOnly(EntryPoint = "update")]
    public static void Update()
    {
        if (firstLoop)
        {
            firstLoop = false;
            Runtime.PrintMessage("Autosplitter loaded!");
            Runtime.SetTickRate(30);
        }

        if (proc is null)
        {
            proc = Runtime.Process.AttachByName("ASN_App_PcDx9_Final.exe");
            if (proc is not null)
            {
                Runtime.PrintMessage("Info to display when connected to the process: yada yada yada");
                Runtime.PrintMessage("  => Connected to process!");
            }
        }

        if (proc is not null)
        {
            if (proc.IsOpen)
            {
                var ok = proc.Read<int>((IntPtr)0x400000);
                Runtime.PrintMessage(ok.ToString());
            }
            else
            {
                proc = null;
            }
        }
    }
}


