﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AD.IO;
using HeaderArrayConverter.Types;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace HeaderArrayConverter.IO
{
    /// <summary>
    /// Implements a <see cref="HeaderArrayReader"/> for reading Header Array (HARX) files in zipped JSON format.
    /// </summary>
    [PublicAPI]
    public class BinarySolutionReader : HeaderArrayReader
    {
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

            foreach (Task<IHeaderArray> array in ReadArraysAsync(file))
            {
                yield return array.Result;
            }
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

            return BuildHeaderArraysAsync(file);
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
        private IEnumerable<Task<IHeaderArray>> BuildHeaderArraysAsync(FilePath file)
        {
            HeaderArrayFile arrayFile = BinaryReader.Read(file);

            IEnumerable<EndogenousArray> endogenousArrays =
                BuildSolutionArrays(arrayFile).Where(
                                                  x => x.IsEndogenous)
                                              .Select(
                                                  (x, i) =>
                                                      BuildNextArray(arrayFile, x, i).Result);

            foreach (var a in endogenousArrays.Where(x => x.Name == "p3cs"))
            {
                Console.WriteLine(JsonConvert.SerializeObject(a, Formatting.Indented));
            }

            return null;
        }

        private IEnumerable<SolutionArray> BuildSolutionArrays(HeaderArrayFile arrayFile)
        {
            IHeaderArray<ModelChangeType> changeTypes = arrayFile["VCT0"].As<ModelChangeType>();

            IHeaderArray<ModelVariableType> variableTypes = arrayFile["VCS0"].As<ModelVariableType>();

            IImmutableDictionary<string, IImmutableList<SetInformation>> sets = VariableIndexedCollectionsOfSets(arrayFile);
            
            return
                arrayFile["VCNM"].As<string>()
                                 .Select(
                                     x =>
                                         new SolutionArray(
                                             int.Parse(x.Key.Single()),
                                             arrayFile["VCNI"].As<int>()[x.Key].Single().Value,
                                             arrayFile["VCNM"].As<string>()[x.Key].Single().Value,
                                             arrayFile["VCL0"].As<string>()[x.Key].Single().Value,
                                             arrayFile["VCLE"].As<string>()[x.Key].Single().Value,
                                             changeTypes[x.Key].Single().Value,
                                             variableTypes[x.Key].Single().Value,
                                             sets[x.Key.Single()]));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arrayFile">
        /// 
        /// </param>
        /// <remarks>
        /// VNCP - Number of components of variables at header VARS
        /// VCSP - VCSTNP(NUMVC) - pointers into VCSTN array for each variable
        /// VCAR - VCSTN - arguments for variables(c + b)
        /// VCNI - VCNIND(numvc) - how many arguments each variable has
        /// VCSN - VCSTN(NVCSTN) - set numbers arguments range over var1, var2 etc
        /// STNM - STNAM(NUMST) - names of the sets
        /// SSZ  - SSZ(NUMST) - sizes of the sets
        /// STEL - STEL array 
        /// </remarks>
        private static IImmutableDictionary<string, IImmutableList<SetInformation>> VariableIndexedCollectionsOfSets(HeaderArrayFile arrayFile)
        {
            IImmutableList<SetInformation> setInformation = BuildAllSets(arrayFile);

            int[] pointerIntoVcstn = arrayFile["VCSP"].As<int>().GetLogicalValuesEnumerable().ToArray();

            int[] setsPerVariable = arrayFile["VCNI"].As<int>().GetLogicalValuesEnumerable().ToArray();

            int[] setPositions = arrayFile["VCSN"].As<int>().GetLogicalValuesEnumerable().ToArray();

            IDictionary<string, IImmutableList<SetInformation>> sets = new Dictionary<string, IImmutableList<SetInformation>>();

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
        private static IImmutableList<SetInformation> BuildAllSets(HeaderArrayFile arrayFile)
        {
            string[] names = arrayFile["STNM"].As<string>().GetLogicalValuesEnumerable().ToArray();

            string[] descriptions = arrayFile["STLB"].As<string>().GetLogicalValuesEnumerable().ToArray();

            bool[] temporal = arrayFile["STTP"].As<string>().GetLogicalValuesEnumerable().Select(x => x == "i").ToArray();

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
                        temporal[i],
                        sizes[i],
                        elements.Skip(counter).Take(sizes[i]).ToImmutableArray());

                counter += sizes[i];
            }

            return setInformation.ToImmutableArray();
        }

        private static async Task<EndogenousArray> BuildNextArray(HeaderArrayFile arrayFile, SolutionArray endogenous, int index)
        {
            // VARS - names of variables(condensed+backsolved)
            string name = arrayFile["VARS"].As<string>()[index];

            // VCLB - VCLB - labelling information for variables(condensed + backsolved)
            string description = arrayFile["VCLB"].As<string>()[index];

            // VCTP - BVCTP(numbvc) - p =% -change, c = change[condensed + backsolved var only]
            ModelChangeType changeType = arrayFile["VCTP"].As<ModelChangeType>()[index];

            // VCNA - VCNIND - number of arguments for variables (condensed+backsolved)
            int numberOfSets = arrayFile["VCNA"].As<int>()[index];

            if (name != endogenous.Name)
            {
                throw DataValidationException.Create(endogenous, x => x.Name, name);
            }
            if (description != endogenous.Description)
            {
                throw DataValidationException.Create(endogenous, x => x.Description, description);
            }
            if (changeType != endogenous.ChangeType)
            {
                throw DataValidationException.Create(endogenous, x => x.ChangeType, changeType);
            }
            if (numberOfSets != endogenous.NumberOfSets)
            {
                throw DataValidationException.Create(endogenous, x => x.NumberOfSets, numberOfSets);
            }
            
            return await Task.FromResult(new EndogenousArray(endogenous, index));
        }
    }
}