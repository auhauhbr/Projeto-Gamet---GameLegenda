namespace GameLegenda.Core.Services;

public interface ITextFilterService
{
    bool ShouldTranslate(string? text, DateTimeOffset seenAt);
}
