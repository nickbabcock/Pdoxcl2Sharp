using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Pdoxcl2Sharp
{
    internal delegate object FnPtr(ParadoxParser parser);

    internal class Deserializer
    {
        private static readonly INamingConvention Naming = new ParadoxNamingConvention();

        private static ConcurrentDictionary<Type, FnPtr> cache =
            new ConcurrentDictionary<Type, FnPtr>();

        /// <summary>
        /// Creates a function that will create an object of a given type when given a parser
        /// </summary>
        /// <param name="type">THe type to be deserialized</param>
        /// <returns>The function to create <typeparamref name="type"/> from a parser</returns>
        internal static FnPtr Parse(Type type)
        {
            return cache.GetOrAdd(
                type, 
                typ => {
                var fn = MakeMethod("ParsePrimitive", null, typ) ??
                    MakeMethod("ParseCollection", null, typ);

                if (fn != null)
                    return fn;

                if (type.GetInterface("IParadoxRead") != null)
                    return MakeMethod("ParseIParadoxRead", null, typ);
                return MakeMethod("ParseObject", null, typ);
            });
        }

        /// <summary>
        /// Creates and invokes a function that will create a function to
        /// deserialize a parser into an object. This is needed as we don't know
        /// all the types we'll be working with and so we create functions as
        /// need be.
        /// </summary>
        /// <param name="name">The name of the function to create</param>
        /// <param name="parameters">The parameters to be passed to the created function</param>
        /// <param name="typeArguments">The types which are used to fill in the
        /// generic arguments of the newly created function</param>
        /// <returns>A function used to deserialize an object from a parser</returns>
        private static FnPtr MakeMethod(string name, object[] parameters, params Type[] typeArguments)
        {
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var fn = typeof(Deserializer).GetMethod(name, flags);
            return (FnPtr)fn.MakeGenericMethod(typeArguments).Invoke(null, parameters);
        }

        /// <summary>
        /// If <typeparamref name="T"/> is a primitive that is supported
        /// (Null-able included), the function to parse a primitive of the type
        /// is created. There are some primitives that don't have a logical
        /// mapping such as <see cref="Char"/> and  <see cref="DBNull"/>. If
        /// <typeparamref name="T"/> isn't recognized then null is returned.
        /// </summary>
        /// <typeparam name="T">The type to be deserialized</typeparam>
        /// <returns>Function to create <typeparamref name="T"/> from parser</returns>
        private static FnPtr ParsePrimitive<T>()
        {
            var typecode = Type.GetTypeCode(
                Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
            switch (typecode)
            {
                case TypeCode.Boolean: return p => p.ReadBool();
                case TypeCode.Byte: return p => p.ReadByte();
                case TypeCode.DateTime: return p => p.ReadDateTime();
                case TypeCode.Decimal: return p => (decimal)p.ReadDouble();
                case TypeCode.Double: return p => p.ReadDouble();
                case TypeCode.Int16: return p => p.ReadInt16();
                case TypeCode.Int32: return p => p.ReadInt32();
                case TypeCode.Int64: return p => (long)p.ReadInt32();
                case TypeCode.SByte: return p => p.ReadSByte();
                case TypeCode.Single: return p => p.ReadFloat();
                case TypeCode.String: return p => p.ReadString();
                case TypeCode.UInt16: return p => p.ReadUInt16();
                case TypeCode.UInt32: return p => p.ReadUInt32();
                case TypeCode.UInt64: return p => (ulong)p.ReadUInt32();
            }

            return null;
        }

        private static FnPtr ParseCollection<T>()
        {
            Type t = typeof(T);
            if (t.IsArray)
                return MakeMethod(
                    "ParseCollectionInner", 
                    new object[] { true },
                    t.GetElementType());
            else if (t.IsGenericType)
            {
                var td = t.GetGenericTypeDefinition();
                if (td == typeof(ICollection<>) || td == typeof(IEnumerable<>)
                    || td == typeof(IList<>) || td == typeof(List<>))
                    return MakeMethod(
                        "ParseCollectionInner",
                        new object[] { false },
                        t.GetGenericArguments());
                else if (td == typeof(IDictionary<,>) || td == typeof(Dictionary<,>))
                    return MakeMethod("ParseDictionary", null, t.GetGenericArguments());
            }

            return null;
        }

        private static FnPtr ParseDictionary<TKey, TElement>()
        {
            return p =>
            {
                var kfn = Deserializer.Parse(typeof(TKey));
                var vfn = Deserializer.Parse(typeof(TElement));
                return p.ReadDictionary(
                    (p2) => (TKey)kfn(p2),
                    (p2) => (TElement)vfn(p2));
            };
        }

        private static FnPtr ParseCollectionInner<TElement>(bool toArray)
        {
            return p =>
            {
                var fn = Deserializer.Parse(typeof(TElement));
                var list = p.ReadList<TElement>(() => (TElement)fn(p));
                return toArray ? list.ToArray() : list;
            };
        }

        private static FnPtr ParseIParadoxRead<T>() 
            where T : class, IParadoxRead, new()
        {
            return p => p.Parse(new T());
        }

        private static FnPtr ParseObject<T>() where T : class, new()
        {
            return p =>
            {
                T obj = new T();
                var props = typeof(T).GetProperties()
                    .ToDictionary<PropertyInfo, string, Action<ParadoxParser>>(
                        x => Attribute.GetCustomAttributes(x)
                                .OfType<ParadoxAliasAttribute>()
                                .Select(y => y.Alias).FirstOrDefault() ??
                                Deserializer.Naming.Apply(x.Name), 
                        x => (p2) =>
                        x.SetValue(obj, Deserializer.Parse(x.PropertyType)(p2), null));

                p.Parse((p2, s) =>
                {
                    Action<ParadoxParser> fn;
                    if (props.TryGetValue(s, out fn))
                        fn(p2);
                });

                return obj;
            };
        }
    }
}
