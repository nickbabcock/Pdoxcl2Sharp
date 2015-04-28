namespace Pdoxcl2Sharp
{
    public class Property
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public bool Quoted { get; set; }

        public bool IsNonConsecutiveList
        {
            get
            {
                return (Type.Contains("ICollection<") ||
                    Type.Contains("IList<") ||
                    Type.Contains("List<")) &&
                    !Type.Contains("[ConsecutiveElements]");
            }
        }

        public string GetStr(INamingConvention naming)
        {
            if (!string.IsNullOrEmpty(Alias))
                return Alias;
            else if (IsNonConsecutiveList)
                return naming.Apply(Name).Singularize(Plurality.CouldBeEither);
            else
                return naming.Apply(Name);
        }

        public string ExtractInnerListType()
        {
            var str = Type;
            str = str.Substring(str.IndexOf('<') + 1);
            return str.Remove(str.LastIndexOf('>'));
        }
    }
}
