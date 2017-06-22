﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AD.IO;
using HeaderArrayConverter.Collections;
using HeaderArrayConverter.Extensions;
using HeaderArrayConverter.Types;
using JetBrains.Annotations;

namespace HeaderArrayConverter.IO
{
    /// <summary>
    /// Implements a <see cref="HeaderArrayReader"/> for reading Header Array (HARX) files in zipped JSON format.
    /// </summary>
    [PublicAPI]
    public class BinarySolutionReader : HeaderArrayReader
    {
        /// <summary>
        /// A <see cref="BinaryHeaderArrayReader"/> that reads the SL4 file in literal format.
        /// </summary>
        [NotNull]
        private static HeaderArrayReader BinaryReader { get; } = new BinaryHeaderArrayReader();

        /// <summary>
        /// Reads <see cref="IHeaderArray"/> collections from file..
        /// </summary>
        /// <param name="file">
        /// The file to read.
        /// </param>
        /// <return>
        /// A <see cref="HeaderArrayFile"/> representing the contents of the file.
        /// </return>
        public override HeaderArrayFile Read(FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return ReadAsync(file).Result;
        }

        /// <summary>
        /// Asynchronously reads <see cref="IHeaderArray"/> collections from file..
        /// </summary>
        /// <param name="file">
        /// The file to read.
        /// </param>
        /// <return>
        /// A task that upon completion returns a <see cref="HeaderArrayFile"/> representing the contents of the file.
        /// </return>
        public override async Task<HeaderArrayFile> ReadAsync(FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return new HeaderArrayFile(await Task.WhenAll(ReadArraysAsync(file)));
        }

        /// <summary>
        /// Enumerates the <see cref="IHeaderArray"/> collection from file.
        /// </summary>
        /// <param name="file">
        /// The file from which to read arrays.
        /// </param>
        /// <returns>
        /// A <see cref="IHeaderArray"/> collection from the file.
        /// </returns>
        public override IEnumerable<IHeaderArray> ReadArrays(FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return ReadArraysAsync(file).Select(x => x.Result);
        }

        /// <summary>
        /// Asynchronously enumerates the arrays from file.
        /// </summary>
        /// <param name="file">
        /// The file from which to read arrays.
        /// </param>
        /// <returns>
        /// An enumerable collection of tasks that when completed return an <see cref="IHeaderArray"/> from file.
        /// </returns>
        public override IEnumerable<Task<IHeaderArray>> ReadArraysAsync(FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return BuildHeaderArrays(file).Select(Task.FromResult);
        }

        /// <summary>
        /// Builds an <see cref="IHeaderArray"/> sequence from the <see cref="SolutionFile"/>.
        /// </summary>
        /// <param name="file">
        /// A solution file (SL4).
        /// </param>
        /// <returns>
        /// An enumerable collection of tasks that when completed return an <see cref="IHeaderArray"/> from file.
        /// </returns>
        [NotNull]
        private IEnumerable<IHeaderArray> BuildHeaderArrays(FilePath file)
        {
            HeaderArrayFile arrayFile = BinaryReader.Read(file);

            int[] pointersToCumulative = arrayFile["PCUM"].As<int>().GetLogicalValuesEnumerable().ToArray();

            int[] countInCumulative = arrayFile["CMND"].As<int>().GetLogicalValuesEnumerable().ToArray();

            float[] cumulativeResults = arrayFile["CUMS"].As<float>().GetLogicalValuesEnumerable().ToArray();

            IHeaderArray<string> commandFile = arrayFile["CMDF"].As<string>();

            IImmutableList<SolutionArray> solutionArrays = BuildSolutionArrays(arrayFile).ToImmutableArray();

            IImmutableList<KeyValuePair<string, IImmutableList<string>>> sets =
                solutionArrays.SelectMany(
                                  x =>
                                      x.Sets.Select(y => new KeyValuePair<string, IImmutableList<string>>(y.Name, y.Elements)))
                              .Distinct()
                              .ToImmutableArray();

            ModelCommandFile modelCommandFile = new ModelCommandFile(commandFile, sets);

            return
                solutionArrays.AsParallel()
                              .Where(x => x.IsBacksolvedOrCondensed)
                              .OrderBy(x => x.VariableIndex)
                              .Select(BuildNextArray);

            // Local method here to limit passing arrays as parameters.
            IHeaderArray BuildNextArray(SolutionArray array, int index)
            {
                float[] values = new float[array.Count];

                int pointer = pointersToCumulative[index] - 1;

                // TODO: When the array is condensed/backsolved and the pointer is empty, its probably a shocked variable (PSHK, SHCK, SHCL, SHOC).
                if (pointer != -1)
                {
                    Array.Copy(cumulativeResults, pointer, values, 0, countInCumulative[index]);
                }

                IImmutableList<KeyValuePair<string, IImmutableList<string>>> set =
                    array.Sets
                         .Select(x => new KeyValuePair<string, IImmutableList<string>>(x.Name, x.Elements))
                         .ToImmutableArray();

                ImmutableArray<KeySequence<string>> expandedSets = 
                    set.AsExpandedSet().ToImmutableArray();

                foreach (VariableDefinition definition in modelCommandFile.ExogenousDefinitions.Where(x => x.Name == array.Name))
                {
                    int indexOf = 
                        expandedSets.IndexOf(
                            new KeySequence<string>(definition.Indexes.ToArray()), 
                            0,
                            expandedSets.Count(), 
                            KeySequence<string>.OrdinalIgnoreCaseEquality);
                        
                    Array.Copy(values, indexOf, values, indexOf + 1, values.Length - indexOf - 1);
                    values[indexOf] = default(float);
                }

                foreach (VariableDefinition definition in modelCommandFile.ShockedDefinitions.Where(x => x.Name == array.Name))
                {
                    int indexOf =
                        expandedSets.IndexOf(
                            new KeySequence<string>(definition.Indexes.ToArray()),
                            0,
                            expandedSets.Count(),
                            KeySequence<string>.OrdinalIgnoreCaseEquality);

                    values[indexOf] = definition.Values.First();
                }

                IImmutableList<KeyValuePair<KeySequence<string>, float>> entries =
                    expandedSets.Select((x, i) => new KeyValuePair<KeySequence<string>, float>(x, values[i]))
                                .ToImmutableArray();

                return
                    new HeaderArray<float>(
                        array.Name,
                        array.Description,
                        HeaderArrayType.RE,
                        entries,
                        default(int),
                        array.Sets.Select(x => x.Count).Concat(Enumerable.Repeat(1, 7)).Take(7).ToImmutableArray(),
                        set);
            }
        }

        [Pure]
        [NotNull]
        private static IEnumerable<SolutionArray> BuildSolutionArrays(HeaderArrayFile arrayFile)
        {
            IHeaderArray<int> numberOfSets = arrayFile["VCNI"].As<int>();
            IHeaderArray<string> names = arrayFile["VCNM"].As<string>();
            IHeaderArray<string> descriptions = arrayFile["VCL0"].As<string>();
            IHeaderArray<string> unitTypes = arrayFile["VCLE"].As<string>();
            IHeaderArray<ModelChangeType> changeTypes = arrayFile["VCT0"].As<ModelChangeType>();
            IHeaderArray<ModelVariableType> variableTypes = arrayFile["VCS0"].As<ModelVariableType>();

            IImmutableDictionary<KeySequence<string>, IImmutableList<SetInformation>> sets = VariableIndexedCollectionsOfSets(arrayFile);
            
            return
                names.Select(
                    x =>
                        new SolutionArray(
                            int.Parse(x.Key.Single()),
                            numberOfSets[x.Key].SingleOrDefault().Value,
                            x.Value,
                            descriptions[x.Key].SingleOrDefault().Value,
                            unitTypes[x.Key].SingleOrDefault().Value,
                            changeTypes[x.Key].SingleOrDefault().Value,
                            variableTypes[x.Key].SingleOrDefault().Value,
                            sets[x.Key]));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arrayFile">
        /// 
        /// </param>
        /// <remarks>
        /// VCSP - VCSTNP(NUMVC) - pointers into VCSTN array for each variable
        /// VCNI - VCNIND(NUMVC) - how many arguments each variable has
        /// VCSN - VCSTN(NVCSTN) - set numbers arguments range over var1, var2 etc
        /// </remarks>
        [Pure]
        [NotNull]
        private static IImmutableDictionary<KeySequence<string>, IImmutableList<SetInformation>> VariableIndexedCollectionsOfSets(HeaderArrayFile arrayFile)
        {
            IImmutableList<SetInformation> setInformation = BuildAllSets(arrayFile);

            int[] pointerIntoVcstn = arrayFile["VCSP"].As<int>().GetLogicalValuesEnumerable().ToArray();

            int[] setsPerVariable = arrayFile["VCNI"].As<int>().GetLogicalValuesEnumerable().ToArray();

            int[] setPositions = arrayFile["VCSN"].As<int>().GetLogicalValuesEnumerable().ToArray();

            IDictionary<KeySequence<string>, IImmutableList<SetInformation>> sets = new Dictionary<KeySequence<string>, IImmutableList<SetInformation>>();

            for (int i = 0; i < pointerIntoVcstn.Length; i++)
            {
                SetInformation[] arraySetInfo = new SetInformation[setsPerVariable[i]];

                int pointer = pointerIntoVcstn[i] - 1;

                for (int j = 0; j < arraySetInfo.Length; j++)
                {
                    int setPosition= setPositions[pointer + j] - 1;

                    arraySetInfo[j] = setInformation[setPosition];
                }

                sets.Add(i.ToString(), arraySetInfo.ToImmutableArray());
            }

            return sets.ToImmutableDictionary();
        }

        /// <summary>
        /// Constructs a <see cref="SetInformation"/> sequence from arrays in the <see cref="HeaderArrayFile"/>.
        /// </summary>
        /// <param name="arrayFile">
        /// The file from which set information is found.
        /// </param>
        /// <returns>
        /// A <see cref="SetInformation"/> sequence (ordered).
        /// </returns>
        /// <remarks>
        /// STNAM(NUMST) - names of the sets
        /// STLB(NUMST) - labelling information for the sets
        /// STTP(NUMST) - set types (n=nonintertemporal, i=intertemporal)
        /// SSZ(NUMST) - sizes of the sets
        /// STEL array - set elements from index position of the name in 'STNM' to value at the index position in 'STEL'
        /// </remarks>
        [Pure]
        [NotNull]
        private static IImmutableList<SetInformation> BuildAllSets(HeaderArrayFile arrayFile)
        {
            string[] names = arrayFile["STNM"].As<string>().GetLogicalValuesEnumerable().ToArray();

            string[] descriptions = arrayFile["STLB"].As<string>().GetLogicalValuesEnumerable().ToArray();

            bool[] intertemporal = arrayFile["STTP"].As<string>().GetLogicalValuesEnumerable().Select(x => x == "i").ToArray();

            int[] sizes = arrayFile["SSZ "].As<int>().GetLogicalValuesEnumerable().ToArray();

            string[] elements = arrayFile["STEL"].As<string>().GetLogicalValuesEnumerable().ToArray();

            SetInformation[] setInformation = new SetInformation[names.Length];

            int counter = 0;
            for (int i = 0; i < names.Length; i++)
            {
                setInformation[i] =
                    new SetInformation(
                        names[i],
                        descriptions[i],
                        intertemporal[i],
                        sizes[i],
                        elements.Skip(counter).Take(sizes[i]).ToImmutableArray());

                counter += sizes[i];
            }

            return setInformation.ToImmutableArray();
        }
    }
}