using System.Text;

namespace Pdoxcl2Sharp.Parsers
{
    public class SnakeNaming : INamingConvention
    {
        public string ConvertName(string name)
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
