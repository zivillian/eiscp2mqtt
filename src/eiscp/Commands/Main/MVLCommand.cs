using System;

namespace eiscp.Commands.Main;

public partial class MVLCommand
{
    protected override bool TryTranslateCustomToEiscp(string argument, out string eiscp)
    {
        if (Byte.TryParse(argument, out var value))
        {
            eiscp = Eiscp + argument;
            return true;
        }
        return base.TryTranslateCustomToEiscp(argument, out eiscp);
    }

    protected override bool TryGetCustomArgument(string eiscp, out EiscpCommandArgument arg)
    {
        if (byte.TryParse(eiscp.AsSpan(3), out var value))
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
        return base.TryGetCustomArgument(eiscp, out arg);
    }
}