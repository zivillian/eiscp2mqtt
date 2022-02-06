using System.Collections.Generic;

namespace eiscp_command_generator
{
    public class CommandGroup
    {
        public string Description { get; set; }

        public string[] Aliases { get; set; }

        public string Name { get; set; }

        public Dictionary<object, Command> Values { get; set; }
    }
}
