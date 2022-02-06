using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace eiscp_command_generator
{
    [Generator]
    public class CommandGenerator:ISourceGenerator
    {
        private IDeserializer _deserializer;

        public void Initialize(GeneratorInitializationContext context)
        {
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var files = context.AdditionalFiles.Where(x =>
                x.Path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase));
            var classes = new List<string>();
            foreach (var file in files)
            {
                var folder = Path.GetFileName(Path.GetDirectoryName(file.Path));
                var name = Path.GetFileNameWithoutExtension(file.Path);
                var source = GetCommand(file, folder, name, context);
                if (source is null) continue;
                classes.Add($"{folder.Casing()}.{name.Casing()}");
                context.AddSource($"{folder}_{name}.g.cs", source);
            }
            context.AddSource("CommandRegistry.RegisterCommands.cs", GetRegistry(classes));
        }

        private string GetRegistry(IEnumerable<string> classes)
        {
            var sb = new StringBuilder();
            sb.AppendLine("//AUTO GENERATED")
                .AppendLine("namespace eiscp;")
                .AppendLine()
                .AppendLine("public partial class CommandRegistry")
                .AppendLine("{")
                .AppendLine("\tpartial void RegisterCommands()")
                .AppendLine("\t{");
            foreach (var command in classes)
            {
                sb.Append("\t\t_commands.Add(new Commands.")
                    .Append(command)
                    .AppendLine("Command());");
            }
            sb.AppendLine("\t}")
                .AppendLine("}");
            return sb.ToString();
        }

        private string GetCommand(AdditionalText file, string folder, string name, GeneratorExecutionContext context)
        {
            var description = Deserialize(file, context);
            if (description is null) return null;

            var sb = new StringBuilder()
                .Append("//AUTO GENERATED FROM ")
                .Append(folder)
                .Append('\\')
                .Append(name)
                .AppendLine(".yaml")
                .AppendLine()
                .AppendLine("using System.Collections.Generic;")
                .AppendLine()
                .Append("namespace eiscp.Commands.").AppendLine(folder.Casing())
                .AppendLine("{")
                .AppendLine("\t///<summary>")
                .Append("\t///").AppendLine(description.Description.Escape())
                .AppendLine("\t///</summary>")
                .Append("\tpublic partial class ").Append(name.Casing()).AppendLine("Command : EiscpCommandBase")
                .AppendLine("\t{")
                .AppendLine("\t\t///<inheritdoc/>")
                .Append("\t\tpublic override string Name => \"").Append(description.Name).AppendLine("\";");

            sb.AppendLine()
                .AppendLine("\t\t///<inheritdoc/>")
                .AppendLine("\t\tpublic override string[] Aliases => new string[]")
                .AppendLine("\t\t{");
            foreach (var @alias in description.Aliases ?? Array.Empty<string>())
            {
                sb.Append("\t\t\t\"").Append(alias).AppendLine("\",");
            }
            sb.AppendLine("\t\t};");
            sb.AppendLine()
                .AppendLine("\t\t///<inheritdoc/>")
                .Append("\t\tpublic override string Eiscp => \"").Append(name).AppendLine("\";")
                .AppendLine()
                .AppendLine("\t\t///<inheritdoc/>")
                .Append("\t\tpublic override EiscpZone Zone => EiscpZone.").Append(folder.Casing()).AppendLine(";")
                .AppendLine()
                .AppendLine("\t\t///<inheritdoc/>")
                .AppendLine("\t\tpublic override EiscpCommandArgument[] Arguments => new[]")
                .AppendLine("\t\t{");
            foreach (var command in description.Values)
            {
                sb.Append("\t\t\tnew EiscpCommandArgument { Eiscp = \"")
                    .Append(command.Key)
                    .Append("\", Name = new string[] {");
                foreach (var alias in command.Value.Name ?? Array.Empty<string>())
                {
                    sb.Append(" \"").Append(alias).Append("\",");
                }
                sb.AppendLine(" } },");
            }
            sb.AppendLine("\t\t};")
                .AppendLine("\t}")
                .AppendLine("}");
            return sb.ToString();
        }

        private static readonly DiagnosticDescriptor _diagnosticDescriptor = new DiagnosticDescriptor(
            id: "YML0001",
            title:$"Failed to parse yaml file",
            messageFormat: "{0}: {1}",
            category:"CommandGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault:true
        );

        private CommandGroup Deserialize(AdditionalText file, GeneratorExecutionContext context)
        {
            try
            {
                return _deserializer.Deserialize<CommandGroup>(File.OpenText(file.Path));
            }
            catch (YamlException ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(_diagnosticDescriptor, Location.None, file.Path, ex.Message));
                return null;
            }
        }
    }

    public static class Extensions
    {
        public static string Casing(this string input)
        {
            return Char.ToUpper(input[0]) + input.Substring(1);
        }

        public static string Escape(this string input)
        {
            return input.Replace('"', '\'').Replace("\r\n", "\\r\\n").Replace("\n", "\\n");
        }
    }
}