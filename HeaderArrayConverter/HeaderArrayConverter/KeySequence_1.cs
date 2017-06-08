﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents a sequence of zero or more keys. This type is suitable for use in a <see cref="IDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of key in the sequence.
    /// </typeparam>
    [PublicAPI]
    public struct KeySequence<TKey> : IEnumerable<TKey>, IEquatable<TKey>, IEquatable<KeySequence<TKey>>, IStructuralEquatable
    {
        /// <summary>
        /// The sequence values.
        /// </summary>
        [CanBeNull]
        private readonly IImmutableList<TKey> _keys;

        /// <summary>
        /// The sequence values or an empty sequence.
        /// </summary>
        [NotNull]
        private IImmutableList<TKey> Keys => _keys ?? EmptyArray;

        /// <summary>
        /// Returns an empty <see cref="KeySequence{TKey}"/> with the specified type argument.
        /// </summary>
        [NotNull]
        private static IImmutableList<TKey> EmptyArray { get; } = new TKey[0].ToImmutableArray();

        /// <summary>
        /// Returns an empty <see cref="KeySequence{TKey}"/> with the specified type argument.
        /// </summary>
        public static KeySequence<TKey> Empty { get; } = new KeySequence<TKey>(new TKey[0]);

        /// <summary>
        /// Compares sequences with <see cref="StringComparison.OrdinalIgnoreCase"/> semantics.
        /// </summary>
        [NotNull]
        public static IComparer<KeySequence<TKey>> ForwardComparer = new Comparer(StringComparer.OrdinalIgnoreCase.Compare);

        /// <summary>
        /// Compares sequences with reverse <see cref="StringComparison.OrdinalIgnoreCase"/> semantics.
        /// </summary>
        [NotNull]
        public static IComparer<KeySequence<TKey>> ReverseComparer = new Comparer(StringComparer.OrdinalIgnoreCase.Compare, Enumerable.Reverse);

        /// <summary>
        /// Gets the number of items contained in the sequence.
        /// </summary>
        public int Count => Keys.Count;

        /// <summary>
        /// Returns the values at the specified index.
        /// </summary>
        [NotNull]
        public IEnumerable<TKey> this[params int[] index]
        {
            get
            {
                foreach (int i in index)
                {
                    yield return Keys[i];
                }
            }
        }

        /// <summary>
        /// Constructs a <see cref="KeySequence{TKey}"/> from the collection.
        /// </summary>
        /// <param name="keys">
        /// The key collection.
        /// </param>
        public KeySequence([NotNull] IEnumerable<TKey> keys) 
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            _keys = keys.ToImmutableArray();
        }

        /// <summary>
        /// Constructs a <see cref="KeySequence{TKey}"/> from the collection.
        /// </summary>
        /// <param name="keys">
        /// The key collection.
        /// </param>
        public KeySequence(params TKey[] keys) : this(keys as IEnumerable<TKey>) { }

        /// <summary>
        /// Implicitly casts the value to a <see cref="KeySequence{TKey}"/>.
        /// </summary>
        /// <param name="value">
        /// The value used to construct the <see cref="KeySequence{TKey}"/>.
        /// </param>
        public static implicit operator KeySequence<TKey>(TKey value)
        {
            return new KeySequence<TKey>(value);
        }

        /// <summary>
        /// Implicitly casts the values to a <see cref="KeySequence{TKey}"/>.
        /// </summary>
        /// <param name="value">
        /// The values used to construct the <see cref="KeySequence{TKey}"/>.
        /// </param>
        public static implicit operator KeySequence<TKey>(TKey[] value)
        {
            return new KeySequence<TKey>(value);
        }

        /// <summary>
        /// Implicitly casts the sequence to a <see cref="KeySequence{TKey}"/>.
        /// </summary>
        /// <param name="value">
        /// The sequence used to construct the <see cref="KeySequence{TKey}"/>.
        /// </param>
        public static implicit operator KeySequence<TKey>(KeySequence<object> value)
        {
            return new KeySequence<TKey>((IEnumerable<TKey>)value.Keys);
        }

        /// <summary>
        /// Implicitly casts the <see cref="KeySequence{TKey}"/> to a string.
        /// </summary>
        /// <param name="value">
        /// The sequence create a string.
        /// </param>
        public static explicit operator string(KeySequence<TKey> value)
        {
            return value.ToString();
        }
        
        /// <summary>
        /// Parses the string to a <see cref="KeySequence{String}"/>.
        /// </summary>
        /// <param name="value">
        /// The <see cref="JToken"/> from which to parse a <see cref="KeySequence{String}"/>.
        /// </param>
        public static KeySequence<string> Parse(JToken value)
        {
            string name = ((JProperty) value).Name;

            return new KeySequence<string>(Parse(name));
        }

        /// <summary>
        /// Parses the string to a <see cref="KeySequence{TKey}"/>.
        /// </summary>
        /// <param name="value">
        /// The sequence create a string.
        /// </param>
        /// <param name="parser">
        /// A function that parses one string component into a key.
        /// </param>
        public static KeySequence<TKey> Parse(string value, Func<string, TKey> parser)
        {
            return new KeySequence<TKey>(Parse(value).Select(parser));
        }

        /// <summary>
        /// Parses the string to a <see cref="KeySequence{TKey}"/>.
        /// </summary>
        /// <param name="value">
        /// The sequence create a string.
        /// </param>
        private static string[] Parse(string value)
        {
            return value.Replace(" ", null).Trim('[', ']').Split(new string[] { "*", "][" }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Returns a new <see cref="KeySequence{TKey}"/> that is a combination of this and the next sequence.
        /// </summary>
        /// <param name="next">
        /// The next keys to combine.
        /// </param>
        /// <returns>
        /// Returns a new <see cref="KeySequence{TKey}"/> that is a combination of this and the next sequence.
        /// </returns>
        public KeySequence<TKey> Combine([NotNull] IEnumerable<TKey> next)
        {
            if (next is null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            return new KeySequence<TKey>(Keys.Concat(next));
        }

        /// <summary>
        /// Returns a new <see cref="KeySequence{TKey}"/> that is a combination of this and the next sequence.
        /// </summary>
        /// <param name="next">
        /// The next keys to combine.
        /// </param>
        /// <returns>
        /// Returns a new <see cref="KeySequence{TKey}"/> that is a combination of this and the next sequence.
        /// </returns>
        public KeySequence<TKey> Combine(params TKey[] next)
        {
            return Combine(next as IEnumerable<TKey>);
        }

        /// <summary>
        /// Returns a string representation of this sequence.
        /// </summary>
        public override string ToString()
        {
            return ToString(x => x);
        }

        /// <summary>
        /// Returns a string representation of this sequence with the transform function applied.
        /// </summary>
        public string ToString(Func<IEnumerable<TKey>, IEnumerable<TKey>> transform)
        {
            return transform(Keys).Aggregate(string.Empty, (current, next) => $"{current}[{next}]");
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        [NotNull]
        public IEnumerator<TKey> GetEnumerator()
        {
            return Keys.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">
        /// An object to compare with this object.
        /// </param>
        /// <returns>
        /// True if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(KeySequence<TKey> other)
        {
            return Keys.SequenceEqual(other);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">
        /// An object to compare with this object.
        /// </param>
        /// <returns>
        /// True if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(TKey other)
        {
            return Keys.SequenceEqual(Enumerable.Empty<TKey>().Append(other));
        }

        /// <summary>
        /// Determines whether an object is structurally equal to the current instance.
        /// </summary>
        /// <param name="other">
        /// The object to compare with the current instance.
        /// </param>
        /// <param name="comparer">
        /// An object that determines whether the current instance and <paramref name="other"/> are equal.
        /// </param>
        /// <returns>
        /// True if the two objects are equal; otherwise, false.
        /// </returns>
        public bool Equals(object other, IEqualityComparer comparer)
        {
            return comparer.Equals(this, other);
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with the current instance.
        /// </param>
        /// <returns>
        /// True if <paramref name="obj" /> and this instance are the same type and represent the same value; otherwise, false. 
        /// </returns>
        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case null:
                {
                    return Equals(Empty);
                }
                case TKey value:
                {
                    return Equals(value);
                }
                case KeySequence<TKey> value:
                {
                    return Equals(value);
                }
                default:
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        public override int GetHashCode()
        {
            return Keys.GetHashCode();
        }

        /// <summary>
        /// Returns a hash code for the current instance.
        /// </summary>
        /// <param name="comparer">
        /// An object that computes the hash code of the current object.
        /// </param>
        /// <returns>
        /// The hash code for the current instance.
        /// </returns>
        public int GetHashCode(IEqualityComparer comparer)
        {
            return comparer.GetHashCode(this);
        }

        /// <summary>
        /// Compares two <see cref="KeySequence{TKey}"/> objects.
        /// </summary>
        private sealed class Comparer : IComparer<KeySequence<TKey>>
        {
            /// <summary>
            /// The function applied to compare keys.
            /// </summary>
            [NotNull]
            private readonly Func<string, string, int> _comparer;

            /// <summary>
            /// A transform function applied to the keys before comparison.
            /// </summary>
            [CanBeNull]
            private readonly Func<IEnumerable<TKey>, IEnumerable<TKey>> _transform;

            /// <summary>
            /// Constructs a <see cref="IComparable{TKey}"/>.
            /// </summary>
            /// <param name="comparer"></param>
            /// <param name="transform"></param>
            public Comparer([NotNull] Func<string, string, int> comparer, [CanBeNull] Func<IEnumerable<TKey>, IEnumerable<TKey>> transform = null)
            {
                _comparer = comparer;
                _transform = transform;
            }

            /// <summary>
            /// Compares two sequences.
            /// </summary>
            [Pure]
            public int Compare(KeySequence<TKey> x, KeySequence<TKey> y)
            {
                return
                    _transform is null
                        ? _comparer(x.ToString(), y.ToString())
                        : _comparer(x.ToString(_transform), y.ToString(_transform));
            }
        }
    }
}