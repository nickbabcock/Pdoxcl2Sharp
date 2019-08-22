namespace Pdoxcl2Sharp.Parsers
{
    internal class TextStringParser : IParse<string>
    {

        public void Parse(ref ParadoxTextReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == TextTokenType.Scalar && Result == null)
                {
                    Result = reader.GetString();
                }
            }
        }

        public string Result { get; private set; }
    }
}
