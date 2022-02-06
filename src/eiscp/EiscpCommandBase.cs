using System;
using System.Globalization;
using System.Linq;

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
    public virtual bool HasNumericArgument { get; } = false;

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
        return TryGetCustomArgument(eiscp, out arg);
    }

    protected virtual bool TryGetCustomArgument(string eiscp, out EiscpCommandArgument arg)
    {
        if (HasNumericArgument)
        {
            if (Byte.TryParse(eiscp.AsSpan(3), NumberStyles.HexNumber, null, out var value))
            {
                arg = new EiscpCommandArgument
                {
                    Name = new[]
                    {
                        value.ToString()
                    }
                };
                return true;
            }
        }
        arg = null;
        return false;
    }

    public bool TryTranslateToEiscp(string argument, out string eiscp)
    {
        foreach (var arg in Arguments)
        {
            if (arg.Name.Any(x => x.Equals(argument, StringComparison.OrdinalIgnoreCase)))
            {
                eiscp = Eiscp + arg.Eiscp;
                return true;
            }
        }
        return TryTranslateCustomToEiscp(argument, out eiscp);
    }

    protected virtual bool TryTranslateCustomToEiscp(string argument, out string eiscp)
    {
        if (HasNumericArgument)
        {
            if (Byte.TryParse(argument, out var value))
            {
                eiscp = Eiscp + value.ToString("X2");
                return true;
            }
        }
        eiscp = null;
        return false;
    }
}