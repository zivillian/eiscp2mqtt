using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eiscp.Commands.Main
{
    public partial class DSNCommand
    {
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
