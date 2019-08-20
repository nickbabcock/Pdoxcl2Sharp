namespace Pdoxcl2Sharp
{
    public enum OperatorType: byte
    {
        Equal,
        Lesser,
        Greater,
        LesserEqual,
        GreaterEqual,

        // Not sure what is actually correct '<>' or '!='
        LesserGreater,
        NotEqual
    }
}
