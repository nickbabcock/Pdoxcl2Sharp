  
using System;
using Pdoxcl2Sharp;
using System.Collections.Generic;

namespace Pdoxcl2Sharp.Test
{
    public partial class Province : IParadoxRead, IParadoxWrite
    {
        public string Name { get; set; }
        public double Tax { get; set; }
        public IList<string> Cores { get; set; }
        public IList<string> TopProvinces { get; set; }
        public IList<Army> Armies { get; set; }

        public Province()
        {
            Cores = new List<string>();
            Armies = new List<Army>();
        }

        public void TokenCallback(ParadoxParser parser, string token)
        {
            switch (token)
            {
            case "name": Name = parser.ReadString(); break;
            case "tax": Tax = parser.ReadDouble(); break;
            case "add_core": Cores.Add(parser.ReadString()); break;
            case "top_provinces": TopProvinces = parser.ReadStringList(); break;
            case "army": Armies.Add(parser.Parse(new Army())); break;
            }
        }

        public void Write(ParadoxStreamWriter writer)
        {
            if (Name != null)
            {
                writer.WriteLine("name", Name, ValueWrite.Quoted);
            }
            writer.WriteLine("tax", Tax);
            foreach(var val in Cores)
            {
                writer.WriteLine("add_core", val);
            }
            if (TopProvinces != null)
            {
                writer.Write("top_provinces={ ");
                foreach (var val in TopProvinces)
                {
                    writer.Write(val, ValueWrite.Quoted);
                    writer.Write(" ");
                }
                writer.WriteLine("}");
            }
            foreach(var val in Armies)
            {
                writer.Write("army", val);
            }
        }
    }

    public partial class Army : IParadoxRead, IParadoxWrite
    {
        public string Name { get; set; }
        public ParadoxId Leader { get; set; }
        public IList<Unit> Units { get; set; }
        public IList<ParadoxId> Attachments { get; set; }

        public Army()
        {
            Units = new List<Unit>();
        }

        public void TokenCallback(ParadoxParser parser, string token)
        {
            switch (token)
            {
            case "name": Name = parser.ReadString(); break;
            case "leader": Leader = parser.Parse(new ParadoxId()); break;
            case "unit": Units.Add(parser.Parse(new Unit())); break;
            case "attachments": Attachments = parser.ReadList(() => parser.Parse (new ParadoxId())); break;
            }
        }

        public void Write(ParadoxStreamWriter writer)
        {
            if (Name != null)
            {
                writer.WriteLine("name", Name, ValueWrite.Quoted);
            }
            if (Leader != null)
            {
                writer.Write("leader", Leader);
            }
            foreach(var val in Units)
            {
                writer.Write("unit", val);
            }
            if (Attachments != null)
            {
                writer.Write("attachments", w =>
                {
                    foreach (var val in Attachments)
                    {
                        w.Write(String.Empty, val);
                    }
                });
            }
        }
    }

    public partial class Unit : IParadoxRead, IParadoxWrite
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public double Morale { get; set; }
        public double Strength { get; set; }

        public Unit()
        {
        }

        public void TokenCallback(ParadoxParser parser, string token)
        {
            switch (token)
            {
            case "name": Name = parser.ReadString(); break;
            case "type": Type = parser.ReadString(); break;
            case "morale": Morale = parser.ReadDouble(); break;
            case "strength": Strength = parser.ReadDouble(); break;
            }
        }

        public void Write(ParadoxStreamWriter writer)
        {
            if (Name != null)
            {
                writer.WriteLine("name", Name, ValueWrite.Quoted);
            }
            if (Type != null)
            {
                writer.WriteLine("type", Type);
            }
            writer.WriteLine("morale", Morale);
            writer.WriteLine("strength", Strength);
        }
    }

    public partial class ParadoxId : IParadoxRead, IParadoxWrite
    {
        public int Id { get; set; }
        public int Type { get; set; }

        public ParadoxId()
        {
        }

        public void TokenCallback(ParadoxParser parser, string token)
        {
            switch (token)
            {
            case "id": Id = parser.ReadInt32(); break;
            case "type": Type = parser.ReadInt32(); break;
            }
        }

        public void Write(ParadoxStreamWriter writer)
        {
            writer.WriteLine("id", Id);
            writer.WriteLine("type", Type);
        }
    }

}
