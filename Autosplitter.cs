using System;

namespace Asr;

public class AutosplitterLogic
{
    public string[] processNames = { "ASN_App_PcDx9_Final.exe" };

    public AutosplitterLogic() { }

    public void Startup()
    {
        Runtime.SetTickRate(15);
    }

    public void Init(Runtime.Process process)
    {

    }

    public void Update(Runtime.Process process)
    {

    }

    public bool Start(Runtime.Process process)
    {
        return false;
    }

    public bool Split(Runtime.Process process)
    {
        return false;
    }

    public bool Reset(Runtime.Process process)
    {
        return false;
    }

    public bool? IsLoading(Runtime.Process process)
    {
        return false;
    }

    public TimeSpan? GameTime(Runtime.Process process)
    {
        return null;
    }
}
