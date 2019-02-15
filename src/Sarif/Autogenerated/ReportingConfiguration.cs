// Copyright (c) Microsoft.  All Rights Reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Sarif.Readers;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Sarif
{
    /// <summary>
    /// Information about a tool report that can be configured at runtime.
    /// </summary>
    [DataContract]
    [GeneratedCode("Microsoft.Json.Schema.ToDotNet", "0.61.0.0")]
    public partial class ReportingConfiguration : PropertyBagHolder, ISarifNode
    {
        public static IEqualityComparer<ReportingConfiguration> ValueComparer => ReportingConfigurationEqualityComparer.Instance;

        public bool ValueEquals(ReportingConfiguration other) => ValueComparer.Equals(this, other);
        public int ValueGetHashCode() => ValueComparer.GetHashCode(this);

        /// <summary>
        /// Gets a value indicating the type of object implementing <see cref="ISarifNode" />.
        /// </summary>
        public SarifNodeKind SarifNodeKind
        {
            get
            {
                return SarifNodeKind.ReportingConfiguration;
            }
        }

        /// <summary>
        /// Specifies whether the report may be produced during the scan.
        /// </summary>
        [DataMember(Name = "enabled", IsRequired = false, EmitDefaultValue = false)]
        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool Enabled { get; set; }

        /// <summary>
        /// Specifies the failure level for the report.
        /// </summary>
        [DataMember(Name = "level", IsRequired = false, EmitDefaultValue = false)]
        [DefaultValue(FailureLevel.Warning)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [JsonConverter(typeof(Microsoft.CodeAnalysis.Sarif.Readers.EnumConverter))]
        public FailureLevel Level { get; set; }

        /// <summary>
        /// Specifies the relative priority of the report. Used for analysis output only.
        /// </summary>
        [DataMember(Name = "rank", IsRequired = false, EmitDefaultValue = false)]
        [DefaultValue(-1)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public double Rank { get; set; }

        /// <summary>
        /// Contains configuration information specific to a report.
        /// </summary>
        [DataMember(Name = "parameters", IsRequired = false, EmitDefaultValue = false)]
        public IDictionary<string, SerializedPropertyInfo> Parameters { get; set; }

        /// <summary>
        /// Key/value pairs that provide additional information about the reporting configuration.
        /// </summary>
        [DataMember(Name = "properties", IsRequired = false, EmitDefaultValue = false)]
        internal override IDictionary<string, SerializedPropertyInfo> Properties { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportingConfiguration" /> class.
        /// </summary>
        public ReportingConfiguration()
        {
            Enabled = true;
            Level = FailureLevel.Warning;
            Rank = -1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportingConfiguration" /> class from the supplied values.
        /// </summary>
        /// <param name="enabled">
        /// An initialization value for the <see cref="P:Enabled" /> property.
        /// </param>
        /// <param name="level">
        /// An initialization value for the <see cref="P:Level" /> property.
        /// </param>
        /// <param name="rank">
        /// An initialization value for the <see cref="P:Rank" /> property.
        /// </param>
        /// <param name="parameters">
        /// An initialization value for the <see cref="P:Parameters" /> property.
        /// </param>
        /// <param name="properties">
        /// An initialization value for the <see cref="P:Properties" /> property.
        /// </param>
        public ReportingConfiguration(bool enabled, FailureLevel level, double rank, IDictionary<string, SerializedPropertyInfo> parameters, IDictionary<string, SerializedPropertyInfo> properties)
        {
            Init(enabled, level, rank, parameters, properties);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportingConfiguration" /> class from the specified instance.
        /// </summary>
        /// <param name="other">
        /// The instance from which the new instance is to be initialized.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="other" /> is null.
        /// </exception>
        public ReportingConfiguration(ReportingConfiguration other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            Init(other.Enabled, other.Level, other.Rank, other.Parameters, other.Properties);
        }

        ISarifNode ISarifNode.DeepClone()
        {
            return DeepCloneCore();
        }

        /// <summary>
        /// Creates a deep copy of this instance.
        /// </summary>
        public ReportingConfiguration DeepClone()
        {
            return (ReportingConfiguration)DeepCloneCore();
        }

        private ISarifNode DeepCloneCore()
        {
            return new ReportingConfiguration(this);
        }

        private void Init(bool enabled, FailureLevel level, double rank, IDictionary<string, SerializedPropertyInfo> parameters, IDictionary<string, SerializedPropertyInfo> properties)
        {
            Enabled = enabled;
            Level = level;
            Rank = rank;
            if (parameters != null)
            {
                Parameters = new Dictionary<string, SerializedPropertyInfo>(parameters);
            }

            if (properties != null)
            {
                Properties = new Dictionary<string, SerializedPropertyInfo>(properties);
            }
        }
    }
}