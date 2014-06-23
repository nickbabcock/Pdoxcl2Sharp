namespace Pdoxcl2Sharp
{
    /// <summary>
    /// Defines how to transform a given property name into the appropriate
    /// representation to be deserialized
    /// </summary>
    public interface INamingConvention
    {
        string Apply(string name);  
    }
}
