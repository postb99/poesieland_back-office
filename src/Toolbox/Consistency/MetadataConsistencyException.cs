namespace Toolbox.Consistency;

public class MetadataConsistencyException : ConsistencyException
{
    public MetadataConsistencyException(string message) : base(message)
    {
    }

    public MetadataConsistencyException(IEnumerable<string> messages) : base(string.Join(Environment.NewLine, messages))
    {
    }
}