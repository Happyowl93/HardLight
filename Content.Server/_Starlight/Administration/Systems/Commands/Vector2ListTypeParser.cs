using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Server._Starlight.Administration.Systems.Commands;

public sealed class Vector2ListTypeParser : TypeParser<Vector2List>
{
    public override bool TryParse(ParserContext ctx, out Vector2List result)
    {
        result = default;
        var restore = ctx.Save();

        // Read identifier
        var word = ctx.GetWord(ParserContext.IsToken);
        if (word is null)
        {
            ctx.Error = ctx.PeekRune() is null
                ? new OutOfInputError()
                : new InvalidVector2List(ctx.GetWord() ?? string.Empty);
            return false;
        }

        if (!word.Equals("vector2list", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Restore(restore);
            ctx.Error = new InvalidVector2List(word);
            return false;
        }

        if (!ctx.EatMatch('['))
        {
            ctx.Restore(restore);
            ctx.Error = new InvalidVector2List("Expected '['");
            return false;
        }

        var vertices = new List<Vector2>();

        // Empty list
        if (ctx.EatMatch(']'))
        {
            result = new Vector2List(vertices);
            return true;
        }

        while (true)
        {
            if (!ctx.EatMatch('{'))
            {
                ctx.Restore(restore);
                ctx.Error = new InvalidVector2List("Expected '{'");
                return false;
            }

            if (!TryReadFloat(ctx, out var x) ||
                !ctx.EatMatch(',') ||
                !TryReadFloat(ctx, out var y))
            {
                ctx.Restore(restore);
                ctx.Error = new InvalidVector2List("Invalid box format");
                return false;
            }

            vertices.Add(new Vector2(x,y));

            if (ctx.EatMatch(']'))
                break;

            if (!ctx.EatMatch(','))
            {
                ctx.Restore(restore);
                ctx.Error = new InvalidVector2List("Expected ',' or ']'");
                return false;
            }
        }

        result = new Vector2List(vertices);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        var hint = GetArgHint(arg);
        parserContext.ConsumeWhitespace();
        return CompletionResult.FromHint($"{hint} | Vector2List[{{x}},{{y}},{{x}},{{y}},...]");
    }
    
    private static bool TryReadFloat(ParserContext ctx, out float value)
    {
        value = 0;
        var num = ctx.GetWord(ParserContext.IsNumeric);
        return num != null && float.TryParse(num, out value);
    }
}

public readonly record struct Vector2List(List<Vector2> Vertices)
{
    public override string ToString()
    {
        var str = Vertices.Aggregate("Vector2List[",
            (current, vec2) => current + $"{{{vec2.X},{vec2.Y}}}");
        if (str.EndsWith(',')) str = str.Remove(str.Length - 1);
        str += ']';
        return str;
    }
}

public record InvalidVector2List(string Value) : IConError
{
    public FormattedMessage DescribeInner() =>
        FormattedMessage.FromUnformatted(
            $"The value {Value} is not a valid Vector2List.");

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}