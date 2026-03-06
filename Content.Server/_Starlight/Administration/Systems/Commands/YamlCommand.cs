using System.IO;
using System.Linq;
using System.Text;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Content.Server._Starlight.Administration.Systems.Commands;

[ToolshedCommand]
[AdminCommand(AdminFlags.Fun)]
public sealed class YamlCommand : ToolshedCommand
{
    [Dependency] private readonly ISerializationManager _serMan = default!;
    
    [CommandImplementation("serialize")]
    public string Serialize<T>(IInvocationContext ctx, [PipedArgument] VarRef<T> varRef)
    {
        var val = ctx.ReadVar(varRef.VarName);
        if(val is T resolved) return DoSerialize(ctx, resolved);
        return "null";
    }

    [CommandImplementation("serialize")]
    public string Serialize<T>(IInvocationContext ctx, T value)
        => DoSerialize(ctx, value);

    [CommandImplementation("serialize")]
    public IEnumerable<string> Serialize<T>(IInvocationContext ctx, [PipedArgument] VarRef<IEnumerable<T>> varRef) =>
        ctx.ReadVar(varRef.VarName) is not IEnumerable<T> val
            ? throw new NullReferenceException("how the hell did you even manage this?")
            : val.Select(x => DoSerialize(ctx, x));
    
    [CommandImplementation("serialize")]
    public IEnumerable<string> Serialize<T>(IInvocationContext ctx, [PipedArgument] IEnumerable<T> value) =>
        value.Select(x => DoSerialize(ctx, x));

    [CommandImplementation("deserialize")]
    public T? Deserialize<T>(IInvocationContext ctx, [PipedArgument] string value)
        => DeserializeValue<T>(value);

    [CommandImplementation("deserialize")]
    public IEnumerable<T?> Deserialize<T>(IInvocationContext ctx, [PipedArgument] IEnumerable<string> value)
        => value.Select(DeserializeValue<T>);

    private string DoSerialize<T>(IInvocationContext ctx, T? value)
    {
        if (value is null)
            return "null";
        try
        {
            return SerializeValue<T>(value) ?? "null";
        }
        catch (Exception)
        {
            return value.ToString() ?? "null";
        }
    }
    
    //thanks ViewVariablesManager
    private T? DeserializeValue<T>(string value)
    {
        try
        {
            // Here we go serialization moment
            using TextReader stream = new StringReader(value);
            var yamlStream = new YamlStream();
            yamlStream.Load(stream);
            var document = yamlStream.Documents[0];
            var rootNode = document.RootNode;
            var result = _serMan.Read(typeof(T), rootNode.ToDataNode());
            if (result?.GetType() is T resolved) return resolved;
            return default;
        }
        catch (Exception)
        {
            return default;
        }
    }

    private string? SerializeValue<T>(T value, string? nodeTag = null)
    {
        if (value == null || typeof(T) == typeof(void))
            return null;

        var node = _serMan.WriteValue<T>(value, true);

        // Don't replace an existing tag if it's null.
        if(!string.IsNullOrEmpty(nodeTag))
            node.Tag = nodeTag;

        var document = new YamlDocument(node.ToYamlNode());
        var stream = new YamlStream {document};

        using var writer = new StringWriter(new StringBuilder());

        // Remove the three funny dots from the end of the string...
        stream.Save(new YamlNoDocEndDotsFix(new YamlMappingFix(new Emitter(writer))), false);
        return writer.ToString();
    }
}