using System;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
sealed class CommandHandlerAttribute : Attribute
{
    public string CommandName { get; }

    public CommandHandlerAttribute(string commandName)
    {
        CommandName = commandName;
    }
}
