namespace Pdoxcl2Sharp.Parsers
{
    public class NoopNaming : INamingConvention
    {
        public string ConvertName(string name) => name;
    }
}
