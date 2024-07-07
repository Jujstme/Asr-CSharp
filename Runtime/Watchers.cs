using System;
using System.Collections.Generic;

namespace Asr;

/// <summary>
/// A Dictionary of watchers
/// </summary>
public class WatcherList : Dictionary<string, Watcher>
{
    /// <summary>
    /// Updates every watcher conteined in the dictionary according
    /// the Update() method defined inside each watcher
    /// </summary>
    public void UpdateAll()
    {
        foreach (Watcher watcher in this.Values)
            watcher.Update();
    }

    /// <summary>
    /// Resets the values stored in all the contained watchers,
    /// including the associated Func&lt;<typeparamref name="T"/>&gt; if defined.
    /// </summary>
    public void ResetAll()
    {
        foreach (Watcher watcher in this.Values)
            watcher.Reset();
    }

    public void Add(Watcher watcher)
    {
        if (watcher.Name == null || watcher.Name == string.Empty)
            return;

        base[watcher.Name] = watcher;
    }
}

public abstract class Watcher
{
    public string Name { get; set; } = string.Empty;
    public object? Current { get; protected set; } = default;
    public object? Old { get; protected set; } = default;
    public bool Enabled { get; set; } = true;
    public bool Changed { get; protected set; } = default;
    public abstract bool Update();
    public abstract void Reset();
}

public class Watcher<T> : Watcher where T : struct
{
    public delegate void DataChangedEventHandler(T old, T current);
    public virtual event DataChangedEventHandler? OnChanged;

    public delegate void UpdateEventHandler(string old, string current);
    public virtual event DataChangedEventHandler? OnUpdate;

    public new T Current { get => (T)(base.Current ?? default(T)); set => base.Current = value; }
    public new T Old { get => (T)(base.Old ?? default(T)); set => base.Old = value; }

    private Func<T>? _func;

    /// <summary>
    /// Create a new Watcher object with default values for both .Old and .Current
    /// </summary>
    public Watcher() { }

    /// <summary>
    /// Create a new Watcher object and set a function to
    /// automatically get the current value when calling Update()
    /// </summary>
    public Watcher(Func<T> Func)
    {
        _func = Func;
    }

    /// <summary>
    /// Moves .Current to .Old and runs a previously defined Func to get the new .Current value
    /// </summary>
    public override bool Update()
    {
        Changed = false;

        if (!Enabled || _func is null)
            return false;

        UpdateInternal(_func.Invoke());
        return true;
    }

    /// <summary>
    /// Moves .Current to .Old and manually sets a new value for .Current
    /// </summary>
    public void Update(T newValue)
    {
        Changed = false;

        if (!Enabled)
            return;

        UpdateInternal(newValue);
    }

    private void UpdateInternal(T newValue)
    {
        Old = Current;
        Current = newValue;
        Changed = !Old.Equals(Current);

        OnUpdate?.Invoke(Old, Current);

        if (Changed)
            OnChanged?.Invoke(Old, Current);
    }

    /// <summary>
    /// Resets the values stored in current Watcher, including the
    /// associated Func&lt;<typeparamref name="T"/>&gt; if defined.
    /// </summary>
    public override void Reset()
    {
        Current = default;
        Old = default;
        Changed = default;
        _func = null;
    }

    /// <summary>
    /// Sets a new Func&lt;<typeparamref name="T"/>&gt; to be invoked when calling Update().
    /// </summary>
    /// <param name="func"></param>
    public void SetFunc(Func<T> func) => _func = func;
}

public class StringWatcher : Watcher
{
    public delegate void DataChangedEventHandler(string old, string current);
    public virtual event DataChangedEventHandler? OnChanged;

    public delegate void UpdateEventHandler(string old, string current);
    public virtual event DataChangedEventHandler? OnUpdate;

    public new string Current { get => (string)(base.Current ?? string.Empty); set => base.Current = value; }
    public new string Old { get => (string)(base.Old ?? string.Empty); set => base.Old = value; }

    private Func<string>? _func;

    /// <summary>
    /// Create a new Watcher object with default values for both .Old and .Current
    /// </summary>
    public StringWatcher() { }

    /// <summary>
    /// Create a new Watcher object and set a function to
    /// automatically get the current value when calling Update()
    /// </summary>
    public StringWatcher(Func<string> Func)
    {
        _func = Func;
    }

    /// <summary>
    /// Moves .Current to .Old and runs a previously defined Func to get the new .Current value
    /// </summary>
    public override bool Update()
    {
        Changed = false;

        if (!Enabled || _func is null)
            return false;

        UpdateInternal(_func.Invoke());
        return true;
    }

    /// <summary>
    /// Moves .Current to .Old and manually sets a new value for .Current
    /// </summary>
    public void Update(string newValue)
    {
        Changed = false;

        if (!Enabled)
            return;

        UpdateInternal(newValue);
    }

    private void UpdateInternal(string newValue)
    {
        Old = Current;
        Current = newValue;
        Changed = !Old.Equals(Current);

        OnUpdate?.Invoke(Old, Current);

        if (Changed)
            OnChanged?.Invoke(Old, Current);
    }

    /// <summary>
    /// Resets the values stored in current FakeMemoryWatcher, including the
    /// associated Func&lt;<typeparamref name="T"/>&gt; if defined.
    /// </summary>
    public override void Reset()
    {
        Current = string.Empty;
        Old = string.Empty;
        Changed = default;
        _func = null;
    }

    /// <summary>
    /// Sets a new Func&lt;<typeparamref name="T"/>&gt; to be invoked when calling Update().
    /// </summary>
    /// <param name="func"></param>
    public void SetFunc(Func<string> func) => _func = func;
}