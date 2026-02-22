namespace Toolbox.Consistency;

public class CustomPageConsistencyException : ConsistencyException
{
    public CustomPageConsistencyException(string message) : base(message)
    {
    }

    public CustomPageConsistencyException(IEnumerable<string> messages) : base(string.Join(Environment.NewLine,
        messages))
    {
    }
}