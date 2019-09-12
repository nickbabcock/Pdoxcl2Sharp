using Pdoxcl2Sharp.Parsers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Pdoxcl2Sharp.Converters;
using Pdoxcl2Sharp.Naming;
using Pdoxcl2Sharp.Utils;
using BindingFlags = System.Reflection.BindingFlags;

namespace Pdoxcl2Sharp
{
    public class ParadoxSerializer
    {
        public static async ValueTask<TValue> DeserializeAsync<TValue>(Stream input, ParadoxSerializerOptions options = null, CancellationToken token = default)
        {
            if (options == null)
            {
                options = new ParadoxSerializerOptions();
            }

            var parser = GetParser(typeof(TValue), options);
            var state = new TextReaderState();
            using (var buffer = new ArrayBuffer(options.DefaultBufferSize, options.PoolAllocations))
            {
                bool isFinalBlock = false;
                while (!isFinalBlock)
                {
                    // If we can't consume a long string with a short buffer this line will grow
                    // the buffer based on how long the string is
                    buffer.EnsureAvailableSpace(buffer.ActiveLength * 2);

                    int bytesRead = await input.ReadAsync(buffer.AvailableMemory.Slice(0), token)
                        .ConfigureAwait(false);
                    isFinalBlock = bytesRead <= 0;
                    buffer.Commit(bytesRead);
                    var (consumed, newState) = Parse(buffer.ActiveSpan, isFinalBlock, state, parser);
                    state = newState;
                    buffer.Discard(consumed);
                }
            }

            return (TValue)parser.Result;
        }

        private static IParse<object> GetParser(Type type, ParadoxSerializerOptions options)
        {
            if (type == typeof(string))
            {
                return new TextStringParser();
            }

            return CreateObjectParser(type, options);
        }

        private static readonly ConcurrentDictionary<Type, Dictionary<ulong, DecodeProperty>> ClassProperties =
            new ConcurrentDictionary<Type, Dictionary<ulong, DecodeProperty>>();

        internal static Dictionary<ulong, DecodeProperty> GetOrAddClass(Type type, ParadoxSerializerOptions options)
        {
            return ClassProperties.GetOrAdd(type, (t) => DecodeProperties(t, options));
        }

        private static Dictionary<ulong, DecodeProperty> DecodeProperties(IReflect type, ParadoxSerializerOptions options)
        {
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var dict = new Dictionary<ulong, DecodeProperty>(props.Length);

            foreach (var propertyInfo in props)
            {
                if (propertyInfo.GetIndexParameters().Length > 0 || propertyInfo.SetMethod?.IsPublic != true)
                {
                    continue;
                }

                var attr = propertyInfo.GetCustomAttribute<ParadoxAliasAttribute>();
                var name = attr?.Alias ?? options.NamingConvention.ConvertName(propertyInfo.Name);
                var bytes = TextHelpers.Windows1252Encoding.GetBytes(name);
                var hash = Farmhash.Sharp.Farmhash.Hash64(new ReadOnlySpan<byte>(bytes));

                if (propertyInfo.PropertyType == typeof(string))
                {
                    dict.Add(hash, new DecodeSetter(propertyInfo, PropertyType.Scalar, new ConverterString()));
                }
                else if (propertyInfo.PropertyType == typeof(int))
                {
                    dict.Add(hash, new DecodeSetter(propertyInfo, PropertyType.Scalar, new ConverterInt32()));
                }
                else
                {
                    if (propertyInfo.PropertyType.IsGenericType &&
                        propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        var ty = propertyInfo.PropertyType.GetGenericArguments()[0];
                        dict.Add(hash, new DecodeList(propertyInfo, GetConverter(ty), ty, GetPropertyType(ty)));
                    }
                    else
                    {
                        dict.Add(hash, new DecodeObject(propertyInfo));
                    }
                }
            }

            return dict;
        }

        private static TextConvert GetConverter(Type type)
        {
            if (type == typeof(string))
            {
                return new ConverterString();
            }

            if (type == typeof(int))
            {
                return new ConverterInt32();
            }

            return new NullConvert();
        }

        private static PropertyType GetPropertyType(Type type)
        {
            if (type.IsValueType || type.IsPrimitive || type == typeof(string))
            {
                return PropertyType.Scalar;
            }

            if (type.IsGenericType)
            {
                return PropertyType.Array;
            }

            return PropertyType.Object;
        }

        private static IParse<object> CreateObjectParser(Type type, ParadoxSerializerOptions options)
        {
            var stack = new ReadStack(options, type);
            return new TextObjectParser(stack);
        }

        private static (int, TextReaderState) Parse(ReadOnlySpan<byte> span, bool isFinalBlock, TextReaderState state, IParse<object> parser)
        {
            var reader = new ParadoxTextReader(span, isFinalBlock, state);
            parser.Parse(ref reader);
            return (reader.Consumed, reader.State);
        }
    }
}
