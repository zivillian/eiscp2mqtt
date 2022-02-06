using System;
using System.Collections.Generic;
using System.Linq;

namespace eiscp;

public class CommandRegistry
{
    private readonly IList<IEiscpCommand> _commands = new List<IEiscpCommand>();

    public CommandRegistry()
    {
        _commands.Add(new PwrCommand());
    }

    public bool TryGetEiscp(string eiscp, out IEiscpCommand command)
    {
        var group = eiscp.Substring(0, 3);
        command = _commands.Where(x => x.Eiscp == group)
            .OrderBy(x => x.Zone)
            .FirstOrDefault();
        return command is not null;
    }

    private static readonly char[] CommandSeparator = new[] { ' ', '\t', '.', '=', ':' };
    public string TranslateToEiscp(string command)
    {
        var parts = command.Split(CommandSeparator,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        IEnumerable<IEiscpCommand> query = _commands;
        int offset = 0;
        if (parts.Length == 3)
        {
            if (Enum.TryParse(parts[0], true, out EiscpZone zone))
            {
                query = _commands.Where(x => x.Zone == zone);
                offset = 1;
            }
            else
            {
                return command;
            }
        }
        else if (parts.Length != 2)
        {
            return command;
        }
        query = query.Where(x => x.Name.Equals(parts[offset], StringComparison.OrdinalIgnoreCase) ||
                                 x.Aliases.Any(a => a.Equals(parts[offset], StringComparison.OrdinalIgnoreCase)));
        foreach (var eiscp in query)
        {
            foreach (var argument in eiscp.Arguments)
            {
                if (argument.Name.Any(x => x.Equals(parts[offset + 1], StringComparison.OrdinalIgnoreCase)))
                {
                    return eiscp.Eiscp + argument.Eiscp;
                }
            }
        }
        return command;
    }
}