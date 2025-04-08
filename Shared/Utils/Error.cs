namespace Shared.Utils
{
    public record Error(string Code, string? Message = null)
    {
        public ErrorList ToErrorList() => new([this]);
    }

}
