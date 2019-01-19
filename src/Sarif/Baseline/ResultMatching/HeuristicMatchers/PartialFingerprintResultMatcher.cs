// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Sarif.Baseline.ResultMatching.ExactMatchers;

namespace Microsoft.CodeAnalysis.Sarif.Baseline.ResultMatching.HeuristicMatchers
{
    /// <summary>
    /// Compares two results, and declares them equal if all of their partial fingerprints match.
    /// 
    /// TODO:  Handle versioning of partial fingerprints.
    /// </summary>
    internal class PartialFingerprintResultMatcher : HeuristicMatcher
    {
        public PartialFingerprintResultMatcher() : base(PartialFingerprintResultComparer.Instance) { }
        
        public class PartialFingerprintResultComparer : IResultMatchingComparer
        {
            public static readonly PartialFingerprintResultComparer Instance = new PartialFingerprintResultComparer();

            public bool Equals(ExtractedResult x, ExtractedResult y)
            {
                return CompareDictionaries(x.Result.PartialFingerprints, y.Result.PartialFingerprints);
            }

            private bool CompareDictionaries(IDictionary<string, string> x, IDictionary<string, string> y)
            {
                if (x == null && y == null)
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                if (x.Keys.Count != y.Keys.Count)
                {
                    return false;
                }

                foreach (string key in x.Keys)
                {
                    if (!y.ContainsKey(key))
                    {
                        return false;
                    }
                    if (y[key] != x[key])
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(ExtractedResult obj)
            {
                return obj.HashCode;
            }

            public int GetHashCode(ExtractedResult obj, IEnumerable<ExtractedResult> allResults, int index = 0)
            {
                if (obj == null || obj.Result == null || ((obj.Result.PartialFingerprints == null || !obj.Result.PartialFingerprints.Any()) && (obj.Result.Fingerprints == null || !obj.Result.Fingerprints.Any())))
                {
                    return 0;
                }

                int hash = -1324097150;

                if (obj.Result.Fingerprints != null)
                {
                    foreach (string key in obj.Result.Fingerprints.Keys)
                    {
                        int keyHash = key.GetHashCode();
                        int resultHash = obj.Result.Fingerprints[key].GetHashCode();

                        // hash = current hash XOR hash of the key rotated by 16 bits XOR the hash of the result
                        hash ^= (keyHash << 16 | keyHash >> (32 - 16)) ^ resultHash;
                    }
                }

                if (obj.Result.PartialFingerprints != null)
                {
                    foreach (string key in obj.Result.PartialFingerprints.Keys)
                    {
                        int keyHash = key.GetHashCode();
                        int resultHash = obj.Result.PartialFingerprints[key].GetHashCode();

                        // hash = current hash XOR hash of the key rotated by 16 bits XOR the hash of the result
                        hash ^= (keyHash << 16 | keyHash >> (32 - 16)) ^ resultHash;
                    }
                }

                if (obj.Result.Locations != null && obj.Result.Locations.Count > 0)
                {
                    //var location = obj.Result.Locations[0];

                    //int lineRange = 20;
                    //int lineKey = nameof(location.PhysicalLocation.Region.StartLine).GetHashCode();
                    //int lineNumber = location.PhysicalLocation.Region.StartLine;
                    //lineNumber = ((lineNumber / lineRange) * lineRange) + (lineRange / 2);
                    //hash ^= (lineKey << 16 | lineKey >> (32 - 16)) ^ lineNumber;

                    //int columnRange = 20;
                    //int columnKey = nameof(location.PhysicalLocation.Region.StartColumn).GetHashCode();
                    //int columnNumber = location.PhysicalLocation.Region.StartColumn;
                    //columnNumber = ((columnNumber / columnRange) * columnRange) + (columnRange / 2);
                    //hash ^= (columnKey << 16 | columnKey >> (32 - 16)) ^ columnNumber;

                    //hash ^= obj.Result.RuleId.GetHashCode();
                }

                hash ^= obj.Result.RuleId.GetHashCode();
                hash ^= index;

                foreach (var otherResult in allResults)
                {
                    if (hash == otherResult.HashCode)
                    {
                        hash = this.GetHashCode(obj, allResults, ++index);
                        break;
                    }
                }

                return hash;
            }

            public bool ResultMatcherApplies(ExtractedResult result)
            {
                return ((result.Result.Locations != null && result.Result.Locations.Any()) || (result.Result.Fingerprints != null && result.Result.Fingerprints.Any()) || (result.Result.PartialFingerprints != null && result.Result.PartialFingerprints.Any()));
            }
        }
    }
}
