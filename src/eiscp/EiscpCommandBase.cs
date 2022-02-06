using System;

namespace eiscp;

public abstract class EiscpCommandBase
{
    /// <summary>
    /// TODO
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// TODO
    /// </summary>
    public abstract string[] Aliases { get; }

    /// <summary>
    /// TODO
    /// </summary>
    public abstract string Eiscp { get; }

    /// <summary>
    /// TODO
    /// </summary>
    public abstract EiscpZone Zone { get; }

    /// <summary>
    /// TODO
    /// </summary>
    public abstract EiscpCommandArgument[] Arguments { get; }

    /// <summary>
    /// TODO
    /// </summary>
    public bool TryGetArgument(string eiscp, out EiscpCommandArgument arg)
    {
        arg = null;
        var span = eiscp.AsSpan();
        if (!span.StartsWith(Eiscp)) return false;
        span = span.Slice(3);
        foreach (var argument in Arguments)
        {
            if (span.SequenceEqual(argument.Eiscp))
            {
                arg = argument;
                return true;
            }
        }
        return false;
    }
}