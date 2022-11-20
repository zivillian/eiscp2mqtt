namespace eiscp.Commands.Main
{
    public partial class DSNCommand
    {
        /// <inheritdoc />
        protected override bool TryGetCustomArgument(string eiscp, out EiscpCommandArgument arg)
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
    }
}
