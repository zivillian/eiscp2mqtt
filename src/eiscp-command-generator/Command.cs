using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace eiscp_command_generator
{
    public class Command
    {
        [YamlMember(Alias = "name")]
        public object YamlName { get; set; }

        public string[] Name
        {
            get
            {
                if (YamlName is string) return new[] { (string)YamlName };
                if (YamlName is null) return Array.Empty<string>();
                return ((List<object>)YamlName).Select(x=>x.ToString()).ToArray();
            }
        }

        public string Description { get; set; }

        public string[] Models { get; set; }
    }
}