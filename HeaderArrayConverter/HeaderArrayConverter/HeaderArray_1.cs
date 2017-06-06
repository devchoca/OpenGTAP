﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents a single entry from a Header Array (HAR) file.
    /// </summary>
    /// <typeparam name="T">
    /// The type of data in the array.
    /// </typeparam>
    [PublicAPI]
    [JsonObject]
    public class HeaderArray<T> : IHeaderArray<T>
    {
        /// <summary>
        /// An immutable dictionary whose entries are stored by a sequence of the defining sets.
        /// </summary>
        [NotNull]
        [JsonProperty(Order = int.MaxValue)]
        private readonly ImmutableSequenceDictionary<string, T> _entries;

        /// <summary>
        /// The four character identifier for this <see cref="HeaderArray{T}"/>.
        /// </summary>
        public string Header { get; }

        /// <summary>
        /// The long name description of the <see cref="HeaderArray{T}"/>.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The type of element stored in the array.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// The dimensions of the array.
        /// </summary>
        public IImmutableList<int> Dimensions { get; }

        /// <summary>
        /// The sets defined on the array.
        /// </summary>
        public IImmutableList<IImmutableList<string>> Sets { get; }
        
        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="keys">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        public ImmutableSequenceDictionary<string, T> this[params string[] keys] => _entries[keys];

        ImmutableSequenceDictionary<string, object> IHeaderArray.this[params string[] keys] => (ImmutableSequenceDictionary<string, object>)_entries[keys];

        IEnumerable<KeyValuePair<KeySequence<string>, T>> ISequenceIndexer<string, T>.this[params string[] keys] => _entries[keys];

        IEnumerable ISequenceIndexer<string>.this[params string[] keys] => this[keys];

        /// <summary>
        /// Represents one entry from a Header Array (HAR) file.
        /// </summary>
        /// <param name="header">
        /// The four character identifier for this <see cref="HeaderArray"/>.
        /// </param>
        /// <param name="description">
        /// The long name description of the <see cref="HeaderArray"/>.
        /// </param>
        /// <param name="type">
        /// The type of element stored in the array.
        /// </param>
        /// <param name="dimensions">
        /// The dimensions of the array.
        /// </param>
        /// <param name="entries">
        /// The data in the array.
        /// </param>
        /// <param name="sets">
        /// The sets defined on the array.
        /// </param>
        public HeaderArray([NotNull] string header, [CanBeNull] string description, [NotNull] string type, int[] dimensions, [NotNull] IEnumerable<T> entries, [NotNull] IEnumerable<IEnumerable<string>> sets)
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }
            if (header is null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
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
            Description = description?.Trim('\u0000', '\u0002', '\u0020');
            Dimensions = dimensions.ToImmutableArray();
            Sets = sets.Select(x => (IImmutableList<string>) x.ToImmutableArray()).ToImmutableArray();
            Type = type;

            _entries =
                Sets.AsExpandedSet()
                    .FullOuterZip(
                        entries,
                        x => x.ToString())
                    .ToImmutableSequenceDictionary(
                        x => x.Left.Split('*') as IEnumerable<string>,
                        x => x.Right);
        }

        public HeaderArray(IEnumerable<KeyValuePair<KeySequence<string>, T>> entries)
        {
            _entries = entries.ToImmutableSequenceDictionary();
            Sets = Enumerable.Empty<IImmutableList<string>>().ToImmutableArray();
        }

        public HeaderArray(params KeyValuePair<KeySequence<string>, T>[] entries) 
            : this(entries as IEnumerable<KeyValuePair<KeySequence<string>, T>>) { }

        IHeaderArray<TResult> IHeaderArray.As<TResult>()
        {
            return (IHeaderArray<TResult>)this;
        }

        /// <summary>
        /// Returns a JSON representation of this <see cref="IHeaderArray{T}"/>.
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// Returns a string representation of the contents of this <see cref="HeaderArray"/>.
        /// </summary>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{nameof(Header)}: {Header}");
            stringBuilder.AppendLine($"{nameof(Description)}: {Description}");
            stringBuilder.AppendLine($"{nameof(Type)}: {Type}");
            stringBuilder.AppendLine($"{nameof(Sets)}: {string.Join(" * ", Sets.Select(x => $"{{ {string.Join(", ", x)} }}"))}");
            //stringBuilder.AppendLine($"{nameof(Dimensions)}: {Dimensions.Aggregate(string.Empty, (current, next) => $"{current}[{next}]")}");
            stringBuilder.AppendLine($"{nameof(Dimensions)}: {Sets.Select(x => x.Count).Aggregate(string.Empty, (current, next) => $"{current}[{next}]")}");
            stringBuilder.AppendLine(_entries.ToString());
            
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<KeyValuePair<KeySequence<string>, T>> GetEnumerator()
        {
            return _entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}