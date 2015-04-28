using System.Text;

namespace Pdoxcl2Sharp
{
    /// <summary>
    /// Converts a string that contains uppercase letters to a string that
    /// contains only lowercase letters with an underscore prefixing the
    /// previously uppercase letters
    /// </summary>
    public sealed class ParadoxNamingConvention : INamingConvention
    {
        public string Apply(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var builder = new StringBuilder();
            builder.Append(char.ToLowerInvariant(name[0]));
            for (int i = 1; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]))
                {
                    builder.Append('_');
                    builder.Append(char.ToLowerInvariant(name[i]));
                }
                else
                {
                    builder.Append(name[i]);
                }
            }

            return builder.ToString();
        }
    }
}