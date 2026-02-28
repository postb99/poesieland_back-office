using System;
using System.Collections.Generic;

namespace Toolbox.Consistency;

public class MetadataConsistencyException : ConsistencyException
{
    public MetadataConsistencyException(string message) : base(message)
    {
    }

    public MetadataConsistencyException(IEnumerable<string> messages) : base(string.Join(Environment.NewLine, messages))
    {
    }
    
    public MetadataConsistencyException(string contentFilePath, IEnumerable<string> messages) 
        : base($"{contentFilePath} {string.Join(Environment.NewLine, messages)}")
    {
    }
}