using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp
{
    public static class Deserializer
    {
        public static T Deserialize<T>(Stream data) where T : new()
        {
            var result = new T();
            return Deserialize(data, result);
        }

        public static T Deserialize<T>(Stream data, T entity)
        {
            Type type = typeof(T);

            var actions = new Dictionary<string, Action<ParadoxParser>>();
            foreach (var property in type.GetProperties())
            {
                Type workType = property.PropertyType;
                TypeCode code = Type.GetTypeCode(workType);

                var alias = Attribute.GetCustomAttributes(property)
                    .OfType<ParadoxAliasAttribute>()
                    .FirstOrDefault();

                string name = alias != null ? alias.Alias : property.Name;

                switch (code)
                {
                    case TypeCode.String:
                        actions.Add(name, (x) => property.SetValue(entity, x.ReadString(), null));
                        break;
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.SByte:
                        actions.Add(name, (x) => property.SetValue(entity, x.ReadInt32(), null));
                        break;
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Byte:
                        actions.Add(name, (x) => property.SetValue(entity, x.ReadUInt32(), null));
                        break;
                    case TypeCode.Boolean:
                        actions.Add(name, (x) => property.SetValue(entity, x.ReadBool(), null));
                        break;
                    case TypeCode.DateTime:
                        actions.Add(name, (x) => property.SetValue(entity, x.ReadDateTime(), null));
                        break;
                    case TypeCode.Single:
                    case TypeCode.Double:
                        actions.Add(name, (x) => property.SetValue(entity, x.ReadDouble(), null));
                        break;
                    case TypeCode.Object:
                        if (!workType.IsGenericType)
                        {
                            break;
                        }

                        Type iter = typeof(IEnumerable<>);
                        iter = iter.MakeGenericType(workType.GetGenericArguments()[0]);
                        if (workType.IsAssignableFrom(iter) || workType.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                        {
                            code = Type.GetTypeCode(workType.GetGenericArguments()[0]);
                            switch (code)
                            {
                                case TypeCode.DateTime:
                                    actions.Add(name, (x) => property.SetValue(entity, x.ReadDateTimeList(), null));
                                    break;
                                case TypeCode.Double:
                                    actions.Add(name, (x) => property.SetValue(entity, x.ReadDoubleList(), null));
                                    break;
                                case TypeCode.String:
                                    actions.Add(name, (x) => property.SetValue(entity, x.ReadStringList(), null));
                                    break;
                                case TypeCode.Int32:
                                    actions.Add(name, (x) => property.SetValue(entity, x.ReadIntList(), null));
                                    break;
                                default:
                                    throw new ArgumentException(string.Format("{0} is not a valid list type", property.Name));
                            }
                        }

                        break;
                    case TypeCode.Char:
                    case TypeCode.DBNull:
                    case TypeCode.Empty:
                    default:
                        break;
                }
            }

            ParadoxParser.Parse(
                data, 
                (p, s) =>
                {
                    Action<ParadoxParser> act;

                    if (actions.TryGetValue(s, out act))
                    {
                        act(p);
                    }
                });

            return entity;
        }
    }
}
