using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Storage;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Server._Starlight.Administration.Systems.Commands;

public sealed class Box2IListTypeParser : TypeParser<Box2IList>
{
    public override bool TryParse(ParserContext ctx, out Box2IList result)
    {
        result = default;
        var restore = ctx.Save();

        // Read identifier
        var word = ctx.GetWord(ParserContext.IsToken);
        if (word is null)
        {
            ctx.Error = ctx.PeekRune() is null
                ? new OutOfInputError()
                : new InvalidBoxList(ctx.GetWord() ?? string.Empty);
            return false;
        }

        if (!word.Equals("box2ilist", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Restore(restore);
            ctx.Error = new InvalidBoxList(word);
            return false;
        }

        if (!ctx.EatMatch('['))
        {
            ctx.Restore(restore);
            ctx.Error = new InvalidBoxList("Expected '['");
            return false;
        }

        var boxes = new List<Box2i>();

        // Empty list
        if (ctx.EatMatch(']'))
        {
            result = new Box2IList(boxes);
            return true;
        }

        while (true)
        {
            if (!ctx.EatMatch('{'))
            {
                ctx.Restore(restore);
                ctx.Error = new InvalidBoxList("Expected '{'");
                return false;
            }

            if (!TryReadInt(ctx, out var top) ||
                !ctx.EatMatch(',') ||
                !TryReadInt(ctx, out var left) ||
                !ctx.EatMatch(',') ||
                !TryReadInt(ctx, out var bottom) ||
                !ctx.EatMatch(',') ||
                !TryReadInt(ctx, out var right) ||
                !ctx.EatMatch('}'))
            {
                ctx.Restore(restore);
                ctx.Error = new InvalidBoxList("Invalid box format");
                return false;
            }

            boxes.Add(new Box2i(top, left, bottom, right));

            if (ctx.EatMatch(']'))
                break;

            if (!ctx.EatMatch(','))
            {
                ctx.Restore(restore);
                ctx.Error = new InvalidBoxList("Expected ',' or ']'");
                return false;
            }
        }

        result = new Box2IList(boxes);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg) =>
        CompletionResult.Empty;
    
    private static bool TryReadInt(ParserContext ctx, out int value)
    {
        value = 0;
        var num = ctx.GetWord(ParserContext.IsNumeric);
        return num != null && int.TryParse(num, out value);
    }
}

public sealed class Box2ListTypeParser : TypeParser<Box2List>
{
    public override bool TryParse(ParserContext ctx, out Box2List result)
    {
        result = default;
        var restore = ctx.Save();

        // Read identifier
        var word = ctx.GetWord(ParserContext.IsToken);
        if (word is null)
        {
            ctx.Error = ctx.PeekRune() is null
                ? new OutOfInputError()
                : new InvalidBoxList(ctx.GetWord() ?? string.Empty);
            return false;
        }

        if (!word.Equals("box2list", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Restore(restore);
            ctx.Error = new InvalidBoxList(word);
            return false;
        }

        if (!ctx.EatMatch('['))
        {
            ctx.Restore(restore);
            ctx.Error = new InvalidBoxList("Expected '['");
            return false;
        }

        var boxes = new List<Box2>();

        // Empty list
        if (ctx.EatMatch(']'))
        {
            result = new Box2List(boxes);
            return true;
        }

        while (true)
        {
            if (!ctx.EatMatch('{'))
            {
                ctx.Restore(restore);
                ctx.Error = new InvalidBoxList("Expected '{'");
                return false;
            }

            if (!TryReadFloat(ctx, out var top) ||
                !ctx.EatMatch(',') ||
                !TryReadFloat(ctx, out var left) ||
                !ctx.EatMatch(',') ||
                !TryReadFloat(ctx, out var bottom) ||
                !ctx.EatMatch(',') ||
                !TryReadFloat(ctx, out var right) ||
                !ctx.EatMatch('}'))
            {
                ctx.Restore(restore);
                ctx.Error = new InvalidBoxList("Invalid box format");
                return false;
            }

            boxes.Add(new Box2(top, left, bottom, right));

            if (ctx.EatMatch(']'))
                break;

            if (!ctx.EatMatch(','))
            {
                ctx.Restore(restore);
                ctx.Error = new InvalidBoxList("Expected ',' or ']'");
                return false;
            }
        }

        result = new Box2List(boxes);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg) =>
        CompletionResult.Empty;
    
    private static bool TryReadFloat(ParserContext ctx, out float value)
    {
        value = 0;
        var num = ctx.GetWord(ParserContext.IsNumeric);
        return num != null && float.TryParse(num, out value);
    }
}

public readonly record struct Box2IList(List<Box2i> Boxes)
{
    public override string ToString()
    {
        var str = Boxes.Aggregate("Box2IList[",
            (current, box) => current + $"{{{box.Top},{box.Left},{box.Bottom},{box.Right}}},");
        if (str.EndsWith(',')) str = str.Remove(str.Length - 1);
        str += ']';
        return str;
    }
}

public readonly record struct Box2List(List<Box2> Boxes)
{
    public override string ToString()
    {
        var str = Boxes.Aggregate("Box2List[",
            (current, box) => current + $"{{{box.Top},{box.Left},{box.Bottom},{box.Right}}},");
        if (str.EndsWith(',')) str = str.Remove(str.Length - 1);
        str += ']';
        return str;
    }
}

public record InvalidBoxList(string Value) : IConError
{
    public FormattedMessage DescribeInner() =>
        FormattedMessage.FromUnformatted(
            $"The value {Value} is not a valid boxlist.");

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
