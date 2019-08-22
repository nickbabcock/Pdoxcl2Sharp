using Pdoxcl2Sharp.Parsers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BindingFlags = System.Reflection.BindingFlags;

namespace Pdoxcl2Sharp
{
    public class Scratch
    {
        public static async ValueTask<TValue> DeserializeAsync<TValue>(Stream input, ParadoxSerializerOptions options = null, CancellationToken token = default)
        {
            if (options == null)
            {
                options = new ParadoxSerializerOptions();
            }

            var parser = GetParser(typeof(TValue), options);
            var state = new TextReaderState();
            using (var buffer = new ArrayBuffer(options.DefaultBufferSize * 2, options.PoolAllocations))
            {
                bool isFinalBlock = false;
                while (!isFinalBlock)
                {
                    buffer.EnsureAvailableSpace(options.DefaultBufferSize);
                    int leftOver = buffer.ActiveSpan.Length;
                    int bytesRead = await input.ReadAsync(buffer.AvailableMemory.Slice(leftOver), token)
                        .ConfigureAwait(false);
                    isFinalBlock = bytesRead <= 0;
                    buffer.Commit(bytesRead);
                    var (consumed, newState) = parse(buffer.ActiveSpan, isFinalBlock, state, parser);
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
                if (propertyInfo.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                if (propertyInfo.SetMethod?.IsPublic == true)
                {
                    var name = options.NamingConvention.ConvertName(propertyInfo.Name);
                    var bytes = TextHelpers.Windows1252Encoding.GetBytes(name);
                    var hash = Farmhash.Sharp.Farmhash.Hash64(new ReadOnlySpan<byte>(bytes));

                    if (propertyInfo.PropertyType == typeof(string))
                    {
                        dict.Add(hash, new DecodeString(propertyInfo));
                    }
                    else if (propertyInfo.PropertyType == typeof(int))
                    {
                        dict.Add(hash, new DecodeInt32(propertyInfo));
                    }
                    else
                    {
                        dict.Add(hash, new DecodeObject(propertyInfo));
                    }
                }
            }

            return dict;
        }



        private static IParse<object> CreateObjectParser(Type type, ParadoxSerializerOptions options)
        {
            var stack = new ReadStack(options, type);
            return new TextObjectParser(stack);
        }

        private static (int, TextReaderState) parse(ReadOnlySpan<byte> span, bool isFinalBlock, TextReaderState state, IParse<object> parser)
        {
            var reader = new ParadoxTextReader(span, isFinalBlock, state);
            parser.Parse(ref reader);
            return (reader.Consumed, reader.State);
        }
    }
}
