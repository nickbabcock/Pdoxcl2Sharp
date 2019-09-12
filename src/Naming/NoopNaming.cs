namespace Pdoxcl2Sharp.Naming
{
    public class NoopNaming : INamingConvention
    {
        public string ConvertName(string name) => name;
    }
}
