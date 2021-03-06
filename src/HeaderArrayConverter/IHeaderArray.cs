﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HeaderArrayConverter.Collections;
using JetBrains.Annotations;
using Newtonsoft.Json.Schema;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents a header array.
    /// </summary>
    [PublicAPI]
    public interface IHeaderArray : ISequenceIndexer<string>
    {
        /// <summary>
        /// Gets the <see cref="JsonSchema"/> for this object.
        /// </summary>
        [NotNull]
        JSchema JsonSchema { get; }

        /// <summary>
        /// The header of the array.
        /// </summary>
        [NotNull]
        string Header { get; }

        /// <summary>
        /// The coeffecient related to this <see cref="IHeaderArray"/>
        /// </summary>
        [NotNull]
        string Coefficient { get; }

        /// <summary>
        /// An optional description of the array.
        /// </summary>
        [NotNull]
        string Description { get; }

        /// <summary>
        /// The type of the array.
        /// </summary>
        HeaderArrayType Type { get; }
        
        /// <summary>
        /// The dimensions of the array.
        /// </summary>
        [NotNull]
        IImmutableList<int> Dimensions { get; }

        /// <summary>
        /// The sets of the array.
        /// </summary>
        [NotNull]
        IImmutableList<KeyValuePair<string, IImmutableList<string>>> Sets { get; }

        /// <summary>
        /// Gets the total number of entries in the array.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="keys">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        [NotNull]
        new IImmutableSequenceDictionary<string> this[params string[] keys] { get; }

        /// <summary>
        /// Casts the <see cref="IHeaderArray"/> as an <see cref="IHeaderArray{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the array.
        /// </typeparam>
        /// <returns>
        /// An <see cref="IHeaderArray{TResult}"/>.
        /// </returns>
        [Pure]
        [NotNull]
        IHeaderArray<TResult> As<TResult>() where TResult : IEquatable<TResult>;

        /// <summary>
        /// Returns a copy of this <see cref="IHeaderArray"/> with the header modified.
        /// </summary>
        /// <param name="header">
        /// The new header.
        /// </param>
        /// <returns>
        /// A copy of this <see cref="IHeaderArray"/> with a new name.
        /// </returns>
        [Pure]
        [NotNull]
        IHeaderArray With([NotNull] string header);

        /// <summary>
        /// Returns a JSON representation of the contents of this <see cref="HeaderArray{TValue}"/>.
        /// </summary>
        [Pure]
        [NotNull]
        string Serialize(bool indent);
    }
}