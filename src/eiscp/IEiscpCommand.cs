namespace eiscp;

public interface IEiscpCommand
{
    string Name { get; }

    string[] Aliases { get; }

    string Eiscp { get; }

    EiscpZone Zone { get; }

    EiscpCommandArgument[] Arguments { get; }

    bool TryGetArgument(string eiscp, out EiscpCommandArgument arg);
}