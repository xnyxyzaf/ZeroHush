using System;
using System.Collections.Generic;

namespace PixelStudio;

public class HistoryEntry
{
    public string Name { get; set; }
    public byte[] Snapshot { get; set; }
    public int Size => Snapshot.Length;

    public HistoryEntry(string name, byte[] snapshot)
    {
        Name = name;
        Snapshot = snapshot;
    }
}

public class HistoryManager
{
    private const int MinSteps = 5;
    private const int MaxSteps = 60;

    private readonly List<HistoryEntry> _entries = new();
    private int _currentIndex = -1;

    public event Action? Changed;

    public IReadOnlyList<HistoryEntry> Entries => _entries;
    public int CurrentIndex => _currentIndex;
    public bool CanUndo => _currentIndex > 0;
    public bool CanRedo => _currentIndex < _entries.Count - 1;

    /// <summary>
    /// 当前生效的步数上限
    /// </summary>
    public int CurrentMaxSteps { get; private set; } = MaxSteps;

    /// <summary>
    /// 当前历史记录占用字节数
    /// </summary>
    public long CurrentMemoryBytes
    {
        get
        {
            long total = 0;
            foreach (var e in _entries) total += e.Size;
            return total;
        }
    }

    /// <summary>
    /// 从设置读取内存预算（字节）
    /// </summary>
    private static long MemoryBudgetBytes
        => SettingsManager.Current.HistoryMemoryBudgetMB * 1024L * 1024;

    public void Push(string name, byte[] snapshot)
    {
        while (_entries.Count > _currentIndex + 1)
            _entries.RemoveAt(_entries.Count - 1);

        _entries.Add(new HistoryEntry(name, snapshot));

        int snapshotSize = snapshot.Length;
        int limit;
        if (snapshotSize <= 0)
        {
            limit = MaxSteps;
        }
        else
        {
            long computed = MemoryBudgetBytes / snapshotSize;
            limit = (int)Math.Min(MaxSteps, Math.Max(MinSteps, computed));
        }
        CurrentMaxSteps = limit;

        while (_entries.Count > limit)
            _entries.RemoveAt(0);

        _currentIndex = _entries.Count - 1;
        Changed?.Invoke();
    }

    public byte[]? Undo()
    {
        if (!CanUndo) return null;
        _currentIndex--;
        Changed?.Invoke();
        return _entries[_currentIndex].Snapshot;
    }

    public byte[]? Redo()
    {
        if (!CanRedo) return null;
        _currentIndex++;
        Changed?.Invoke();
        return _entries[_currentIndex].Snapshot;
    }

    public byte[]? JumpTo(int index)
    {
        if (index < 0 || index >= _entries.Count) return null;
        _currentIndex = index;
        Changed?.Invoke();
        return _entries[index].Snapshot;
    }

    public void Clear()
    {
        _entries.Clear();
        _currentIndex = -1;
        CurrentMaxSteps = MaxSteps;
        Changed?.Invoke();
    }
}