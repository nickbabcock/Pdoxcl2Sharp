using System;

namespace Pdoxcl2Sharp
{
    /// <summary>
    /// Instructs the <see cref="Deserializer"/> to use a different field name for serialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ParadoxAliasAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParadoxAliasAttribute"/> class.
        /// </summary>
        /// <param name="alias">The alias to use for this field.</param>
        public ParadoxAliasAttribute(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentException("Cannot be empty", "alias");
            Alias = alias;
        }

        /// <summary>
        /// Gets or sets the alias name.
        /// </summary>
        public string Alias { get; set; }
    }
}
