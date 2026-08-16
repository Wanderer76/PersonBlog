namespace Shared.Utils;

/// <summary>
/// Marks a type as one atomic error that can be carried by a result.
/// Collections of errors must be passed separately and must not implement this interface.
/// </summary>
public interface IResultError
{
}
