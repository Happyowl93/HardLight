using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Storage;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Server._Starlight.Administration.Systems.Commands;

public sealed class BoxListTypeParser : TypeParser<BoxList>
{
    public override bool TryParse(ParserContext ctx, out BoxList result)
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

        if (!word.Equals("boxlist", StringComparison.OrdinalIgnoreCase))
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
            result = new BoxList(boxes);
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

        result = new BoxList(boxes);
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

public record InvalidBoxList(string Value) : IConError
{
    public FormattedMessage DescribeInner() =>
        FormattedMessage.FromUnformatted(
            $"The value {Value} is not a valid boxlist.");

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
