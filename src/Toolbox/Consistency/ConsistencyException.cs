using System;

namespace Toolbox.Consistency;

public abstract class ConsistencyException(string message) : InvalidOperationException(message);