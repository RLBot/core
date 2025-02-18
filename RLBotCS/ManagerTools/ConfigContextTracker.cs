using System.Text;

namespace RLBotCS.ManagerTools;

/// <summary>
/// The ConfigContextTracker is a small util to create better error messages about
/// fields in config files. Contexts (such a table names) must be pushed and popped
/// to create the context. It is also possible to push a "Link" context instead for
/// when a path is followed. A textual representation of the context is available
/// using <see cref="ToString"/> and <see cref="ToStringWithEnd"/>. Example context:
/// "cars[0].config->settings.agent_id"
/// </summary>
public class ConfigContextTracker
{
    public enum Type { Normal, Link }
    
    private record ContextPart(string text, Type type);
    
    private List<ContextPart> parts = new();
    
    public void Push(string context, Type type = Type.Normal) => parts.Add(new(context, type));
    
    public void Pop() => parts.RemoveAt(parts.Count - 1);

    public int Count => parts.Count;
    
    public bool IsEmpty => parts.Count == 0;
    
    public void Clear() => parts.Clear();

    public IDisposable Begin(string context, Type type = Type.Normal)
    {
        Push(context, type);
        return new DisposableContext(this);
    }

    public override string ToString()
    {
        // Example output: cars[0].config->settings.agent_id
        StringBuilder sb = new();
        sb.Append(parts[0].text);
        for (var i = 1; i < parts.Count; i++)
        {
            sb.Append(parts[i - 1].type switch
            {
                Type.Normal => ".",
                Type.Link => "->",
                _ => throw new ArgumentOutOfRangeException()
            });
            sb.Append(parts[i].text);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Get the current context terminated with the given part.
    /// This saves you a push and pop, if you just want to append a context for one output.
    /// </summary>
    public string ToStringWithEnd(string text)
    {
        Push(text);
        var str = ToString();
        Pop();
        return str;
    }
    
    private class DisposableContext(ConfigContextTracker ctx) : IDisposable
    {
        public void Dispose()
        {
            ctx.Pop();
        }
    }
}
