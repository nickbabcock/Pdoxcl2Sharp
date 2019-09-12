namespace Pdoxcl2Sharp.Parsers
{
    internal interface IParse<out T>
    {
        void Parse(ref ParadoxTextReader reader);

        T Result { get; }
    }
}
