using System;
using static Runtime.Runtime;

namespace Asr;

public class AutosplitterLogic
{
    public string[] processNames = { "ASN_App_PcDx9_Final.exe" };

    public AutosplitterLogic() { }

    public void Startup()
    {
        SetTickRate(60);
    }

    public void Init(Process process)
    {

    }

    public void Update(Process process)
    {

    }

    public bool Start(Process process)
    {
        return false;
    }

    public bool Split(Process process)
    {
        return false;
    }

    public bool Reset(Process process)
    {
        return false;
    }

    public bool? IsLoading(Process process)
    {
        return false;
    }

    public TimeSpan? GameTime(Process process)
    {
        return null;
    }
}
