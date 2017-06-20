﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using HeaderArrayConverter.Collections;
using HeaderArrayConverter.Types;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents a single entry from a Header Array (HAR) file.
    /// </summary>
    /// <typeparam name="TValue">
    /// The type of data in the array.
    /// </typeparam>
    [PublicAPI]
    [JsonObject(MemberSerialization.OptIn)]
    public class HeaderArray<TValue> : HeaderArray, IHeaderArray<TValue>
    {
        /// <summary>
        /// Gets the <see cref="IHeaderArray.JsonSchema"/> for this object.
        /// </summary>
        public static JSchema JsonSchema { get; } = GetJsonSchema();

        JSchema IHeaderArray.JsonSchema => JsonSchema;

        /// <summary>
        /// An immutable dictionary whose entries are stored by a sequence of the defining sets.
        /// </summary>
        [NotNull]
        [JsonProperty("Entries", Order = int.MaxValue)]
        private readonly IImmutableSequenceDictionary<string, TValue> _entries;

        /// <summary>
        /// The four character identifier for this <see cref="HeaderArray{T}"/>.
        /// </summary>
        [JsonProperty]
        public override string Header { get; }

        /// <summary>
        /// The long name description of the <see cref="HeaderArray{T}"/>.
        /// </summary>
        [JsonProperty]
        public override string Description { get; }

        /// <summary>
        /// The type of element stored in the array.
        /// </summary>
        [JsonProperty]
        public override HeaderArrayType Type { get; }

        /// <summary>
        /// The dimensions of the array.
        /// </summary>
        [JsonProperty]
        public override IImmutableList<int> Dimensions { get; }

        /// <summary>
        /// The sets defined on the array.
        /// </summary>
        [JsonProperty]
        public override IImmutableList<KeyValuePair<string, IImmutableList<string>>> Sets { get; }
        
        /// <summary>
        /// Gets the total number of entries in the array.
        /// </summary>
        public override int Total => _entries.Total;

        /// <summary>
        /// Gets the number of vectors used to store the array data in a binary HAR file.
        /// </summary>
        [JsonProperty]
        public override int SerializedVectors { get; }

        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="keys">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        public IImmutableSequenceDictionary<string, TValue> this[KeySequence<string> keys] => _entries[keys.ToArray()];

        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="keys">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        public IImmutableSequenceDictionary<string, TValue> this[params string[] keys] => this[(KeySequence<string>) keys];

        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="key">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        public TValue this[int key] => _entries.GetValueOrDefault(key.ToString());

        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="keys">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        IImmutableSequenceDictionary<string> IHeaderArray.this[params string[] keys] => this[(KeySequence<string>)keys];

        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="key">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        object IHeaderArray.this[int key] => this[key.ToString()];

        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> for the given keys.
        /// </summary>
        /// <param name="keys">
        /// The collection of keys.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> for the given keys.
        /// </returns>
        IEnumerable<KeyValuePair<KeySequence<string>, TValue>> ISequenceIndexer<string, TValue>.this[params string[] keys] => _entries[keys];

        /// <summary>
        /// Gets an <see cref="IEnumerable"/> for the given keys.
        /// </summary>
        /// <param name="keys">
        /// The collection of keys.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable"/> for the given keys.
        /// </returns>
        IEnumerable ISequenceIndexer<string>.this[params string[] keys] => this[keys];

        /// <summary>
        /// Represents one entry from a Header Array (HAR) file.
        /// </summary>
        /// <param name="header">
        /// The four character identifier for this <see cref="HeaderArray{TValue}"/>.
        /// </param>
        /// <param name="description">
        /// The long name description of the <see cref="HeaderArray{TValue}"/>.
        /// </param>
        /// <param name="type">
        /// The type of element stored in the array.
        /// </param>
        /// <param name="entries">
        /// The data in the array.
        /// </param>
        /// <param name="serializedVectors">
        /// The number of vectors used to store the array data in a binary HAR file.
        /// </param>
        /// <param name="dimensions">
        /// The dimensions of the array.
        /// </param>
        /// <param name="sets">
        /// The sets defined on the array.
        /// </param>
        public HeaderArray([NotNull] string header, [CanBeNull] string description, HeaderArrayType type, [NotNull] IEnumerable<KeyValuePair<KeySequence<string>, TValue>> entries, int serializedVectors, [NotNull] IImmutableList<int> dimensions, [NotNull] IImmutableList<KeyValuePair<string, IImmutableList<string>>> sets)
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }
            if (header is null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            if (dimensions is null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (sets is null)
            {
                throw new ArgumentNullException(nameof(sets));
            }
            
            Header = header;
            Description = description ?? string.Empty;
            Type = type;
            Dimensions = dimensions.ToImmutableArray();
            Sets = sets;
            _entries = entries.ToImmutableSequenceDictionary(sets);
            SerializedVectors = serializedVectors;
        }

        /// <summary>
        /// Returns an indented JSON representation of the contents of this <see cref="HeaderArray{TValue}"/>.
        /// </summary>
        [Pure]
        public override string ToString()
        {
            return Serialize(true);
        }

        /// <summary>
        /// Returns a JSON representation of the contents of this <see cref="HeaderArray{TValue}"/>.
        /// </summary>
        [Pure]
        public override string Serialize(bool indent)
        {
            return JsonConvert.SerializeObject(this, indent ? Formatting.Indented : Formatting.None);
        }

        /// <summary>
        /// Casts the <see cref="IHeaderArray"/> as an <see cref="IHeaderArray{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the array.
        /// </typeparam>
        /// <returns>
        /// An <see cref="IHeaderArray{TResult}"/>.
        /// </returns>
        public override IHeaderArray<TResult> As<TResult>()
        {
            if (!typeof(TResult).GetTypeInfo().IsEnum)
            {
                return base.As<TResult>();
            }

            IEnumerable<KeyValuePair<KeySequence<string>, TResult>> entries =
                _entries.Select(
                    x =>
                        new KeyValuePair<KeySequence<string>, TResult>(
                            x.Key,
                            (TResult) Enum.Parse(typeof(TResult), $"{Convert.ToInt32(Convert.ToChar(x.Value))}")));

            return new HeaderArray<TResult>(Header, Description, Type, entries, SerializedVectors, Dimensions, Sets);
        }

        /// <summary>
        /// Returns an enumerable that iterates through the logical collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerable that can be used to iterate through the logical collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </returns>
        public IEnumerable<KeyValuePair<KeySequence<string>, TValue>> GetLogicalEnumerable()
        {
            return _entries.GetLogicalEnumerable();
        }

        /// <summary>
        /// Returns an enumerable that iterates through the logical collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerable that can be used to iterate through the logical collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </returns>
        public IEnumerable<TValue> GetLogicalValuesEnumerable()
        {
            return _entries.GetLogicalValuesEnumerable();
        }

        /// <summary>
        /// Returns an enumerable that iterates through the logical collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerable that can be used to iterate through the logical collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </returns>
        public IEnumerable<TValue> GetLogicalValuesEnumerable(IComparer<KeySequence<string>> keyComparer)
        {
            return _entries.GetLogicalValuesEnumerable(keyComparer);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        [NotNull]
        public IEnumerator<KeyValuePair<KeySequence<string>, TValue>> GetEnumerator()
        {
            return _entries.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        [NotNull]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static JSchema GetJsonSchema()
        {
            JSchemaGenerator generator = new JSchemaGenerator
            {
                DefaultRequired = Required.Always,
                SchemaIdGenerationHandling = SchemaIdGenerationHandling.TypeName,
                SchemaPropertyOrderHandling = SchemaPropertyOrderHandling.Default,
                SchemaLocationHandling = SchemaLocationHandling.Definitions,
                SchemaReferenceHandling = SchemaReferenceHandling.All
            };
            generator.GenerationProviders.Add(new StringEnumGenerationProvider());

            return generator.Generate(typeof(HeaderArray<TValue>));
        }
    }
}