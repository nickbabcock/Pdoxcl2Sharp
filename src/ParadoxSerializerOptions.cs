using Pdoxcl2Sharp.Parsers;

namespace Pdoxcl2Sharp
{
    public sealed class ParadoxSerializerOptions
    {
        public int DefaultBufferSize { get; set; } = 16 * 1024;
        public bool PoolAllocations { get; set; } = true;
        public INamingConvention NamingConvention { get; set; } = new SnakeNaming();
    }
}
