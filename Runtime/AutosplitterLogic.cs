using System.Runtime.InteropServices;
using System;

namespace Asr;

public static class Autosplitter
{
    private static bool firstLoop = true;
    private static bool initCompleted = false;
    private static Runtime.Process? _process;
    private static AutosplitterLogic _autosplitterLogic = new AutosplitterLogic();

    [UnmanagedCallersOnly(EntryPoint = "update")]
    public static void InternalUpdate()
    {
        if (firstLoop)
        {
            firstLoop = false;
            // Stuff that we want to set once can be put in here
            _autosplitterLogic.Startup();            
        }

        if (_process is null)
        {
            foreach (string entry in _autosplitterLogic.processNames)
            {
                _process = Runtime.Process.AttachByName(entry);

                if (_process is not null)
                    break;
            }
        }


        if (_process is not null)
        {
            if (_process.IsOpen)
            {
                if (!initCompleted)
                {
                    try
                    {
                        _autosplitterLogic.Init(_process);
                        initCompleted = true;
                    }
                    catch
                    {
                        return;
                    }
                }


                try
                {
                    _autosplitterLogic.Update(_process);

                    Runtime.TimerState timerState = Runtime.GetTimerState();
                    if (timerState == Runtime.TimerState.RUNNING || timerState == Runtime.TimerState.PAUSED)
                    {
                        bool? isLoading = _autosplitterLogic.IsLoading(_process);
                        if (isLoading is not null)
                        {
                            if (isLoading.Value)
                                Runtime.Timer.PauseGameTime();
                            else
                                Runtime.Timer.ResumeGameTime();
                        }

                        TimeSpan? gameTime = _autosplitterLogic.GameTime(_process);
                        if (gameTime.HasValue)
                        {
                            Runtime.Timer.SetGameTime.FromTimeSpan(gameTime.Value);
                        }

                        if (_autosplitterLogic.Reset(_process))
                            Runtime.Timer.Reset();
                        else if (_autosplitterLogic.Split(_process))
                            Runtime.Timer.Split();
                    }

                    timerState = Runtime.GetTimerState();
                    if (timerState == Runtime.TimerState.NOT_RUNNING && _autosplitterLogic.Start(_process))
                    {
                        Runtime.Timer.Start();
                        Runtime.Timer.PauseGameTime();

                        bool? isLoading = _autosplitterLogic.IsLoading(_process);
                        if (isLoading is not null)
                        {
                            if (isLoading.Value)
                                Runtime.Timer.PauseGameTime();
                            else
                                Runtime.Timer.ResumeGameTime();
                        }
                    }
                    
                }
                catch (Exception e)
                {
                    Runtime.PrintMessage(e.Message);
                }
            }
            else
            {
                _process.Dispose();
                _process = null;
                _autosplitterLogic = new AutosplitterLogic();
                initCompleted = false;
            }
        }
    }
}


