using System;

namespace eiscp;

public class DebugEventArgs : EventArgs
{
    public string Command { get; init; }

    public bool Send { get; set; }
}