using System;

namespace eiscp;

public class PwrCommand : IEiscpCommand
{
    public string Name => "system-power";

    public string[] Aliases => new[]
    {
        "power"
    };

    public string Eiscp => "PWR";

    public EiscpZone Zone => EiscpZone.Main;

    public EiscpCommandArgument[] Arguments => new[]
    {
        new EiscpCommandArgument { Eiscp = "00", Name = new[] { "standby", "off" } },
        new EiscpCommandArgument { Eiscp = "01", Name = new[] { "on" } },
        new EiscpCommandArgument { Eiscp = "ALL", Name = new[] { "standby-all" } },
        new EiscpCommandArgument { Eiscp = "QSTN", Name = new[] { "query" } },
    };

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