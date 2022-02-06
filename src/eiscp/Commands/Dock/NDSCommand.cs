namespace eiscp.Commands.Dock
{
    public partial class NDSCommand
    {
        protected override bool TryGetCustomArgument(string eiscp, out EiscpCommandArgument arg)
        {
            if (eiscp.Length == 6)
            {
                arg = new EiscpCommandArgument
                {
                    Name = new[]
                    {
                        eiscp.Substring(3)
                    }
                };
                return true;
            }
            return base.TryGetCustomArgument(eiscp, out arg);
        }
    }
}
