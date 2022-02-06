using System;

namespace eiscp;

public class EiscpCommandEventArgs:EventArgs
{
    public EiscpZone Zone { get; init; }

    public string Command { get; init; }

    public string Parameter { get; init; }
}