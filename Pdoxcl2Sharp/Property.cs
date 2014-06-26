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
                return (this.Type.Contains("ICollection<") ||
                    this.Type.Contains("IList<") ||
                    this.Type.Contains("List<")) &&
                    !this.Type.Contains("[ConsecutiveElements]");
            }
        }

        public string GetStr(INamingConvention naming)
        {
            if (!string.IsNullOrEmpty(this.Alias))
                return this.Alias;
            else if (this.IsNonConsecutiveList)
                return naming.Apply(this.Name).Singularize(Plurality.CouldBeEither);
            else
                return naming.Apply(this.Name);
        }

        public string ExtractInnerListType()
        {
            var str = this.Type;
            str = str.Substring(str.IndexOf('<') + 1);
            return str.Remove(str.LastIndexOf('>'));
        }
    }
}
