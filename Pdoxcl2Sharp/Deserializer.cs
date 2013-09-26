using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pdoxcl2Sharp
{
    public class Deserializer
    {
        private readonly INamingConvention namingConvention;

        public Deserializer()
        {
            this.namingConvention = new NullNamingConvention();
        }

        public Deserializer(INamingConvention namingConvention)
        {
            this.namingConvention = namingConvention ?? new NullNamingConvention();
        }

        public static T Run<T>(Stream data, INamingConvention convention) where T : new()
        {
            var deserializer = new Deserializer(convention);
            return deserializer.Deserialize<T>(data);
        }

        public static T Run<T>(Stream data) where T : new()
        {
            var deserializer = new Deserializer();
            return deserializer.Deserialize<T>(data);
        }

        public T Deserialize<T>(Stream data) where T : new()
        {
            var result = new T();
            return this.Deserialize(data, result);
        }

        public T Deserialize<T>(Stream data, T entity)
        {
            var actions = this.GetDeserializationDictionary(entity);
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

        private T DeserializeInner<T>(ParadoxParser parser, T entity)
        {
            var getDict = this.GetDeserializationDictionary(entity);
            parser.Parse((ip, s) =>
                {
                    Action<ParadoxParser> act;
                    if (getDict.TryGetValue(s, out act))
                    {
                        act(ip);
                    }
                });
            return entity;
        }

        private IDictionary<string, Action<ParadoxParser>> GetDeserializationDictionary(object entity)
        {
            Type type = entity.GetType();

            var actions = new Dictionary<string, Action<ParadoxParser>>();
            foreach (var property in type.GetProperties())
            {
                Type workType = property.PropertyType;
                TypeCode code = Type.GetTypeCode(workType);

                var alias = Attribute.GetCustomAttributes(property)
                    .OfType<ParadoxAliasAttribute>()
                    .FirstOrDefault();

                string name = alias != null ? alias.Alias : this.namingConvention.Apply(property.Name);

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
                        if (workType.GenericTypeImplementation(typeof(IEnumerable<>)) != null)
                        {
                            code = Type.GetTypeCode(workType.HasElementType ? workType.GetElementType() : workType.GetGenericArguments()[0]);
                            switch (code)
                            {
                                case TypeCode.DateTime:
                                    actions.Add(name, (x) => property.SetValue(entity, this.Encap(x.ReadDateTimeList(), workType), null));
                                    break;
                                case TypeCode.Double:
                                    actions.Add(name, (x) => property.SetValue(entity, this.Encap(x.ReadDoubleList(), workType), null));
                                    break;
                                case TypeCode.String:
                                    actions.Add(name, (x) => property.SetValue(entity, this.Encap(x.ReadStringList(), workType), null));
                                    break;
                                case TypeCode.Int32:
                                    actions.Add(name, (x) => property.SetValue(entity, this.Encap(x.ReadIntList(), workType), null));
                                    break;
                                default:
                                    throw new ArgumentException(string.Format("{0} is not a valid list type", property.Name));
                            }
                        }
                        else
                        {
                            actions.Add(name, (x) => property.SetValue(entity, DeserializeInner(x, Activator.CreateInstance(workType)), null));
                        }

                        break;
                    case TypeCode.Char:
                    case TypeCode.DBNull:
                    case TypeCode.Empty:
                    default:
                        break;
                }
            }

            return actions;
        }

        private object Encap<T>(IEnumerable<T> vals, Type endType)
        {
            if (endType.IsArray)
            {
                return vals.ToArray();
            }

            var implementationType = endType.GenericTypeImplementation(typeof(ICollection<>));
            if (implementationType == null)
            {
                implementationType = endType.GenericTypeImplementation(typeof(IEnumerable<>));
                if (implementationType != null)
                {
                    return vals;
                }
                
                return null;
            }

            var result = (endType.IsInterface ? this.Create(implementationType) : Activator.CreateInstance(endType)) as ICollection<T>;

            foreach (var val in vals)
            {
                result.Add(val);
            }

            return result;
        }

        private object Create(Type type)
        {
            Type createType = type;
            if (type.IsInterface)
            {
                Type typeDefinition = type.GetGenericTypeDefinition();
                if (typeDefinition == typeof(IEnumerable<>)
                    || typeDefinition == typeof(ICollection<>)
                    || typeDefinition == typeof(IList<>))
                {
                    createType = typeof(List<>);
                }
                else if (typeDefinition == typeof(IDictionary<,>))
                {
                    createType = typeof(IDictionary<,>);
                }

                createType = createType.MakeGenericType(type.GetGenericArguments());
            }

            return Activator.CreateInstance(createType);
        }
    }
}
