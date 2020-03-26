using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.WorkItems.Options.Pipeline
{
    /// <summary>
    /// These are settings that are hydrated from the appsettings.json file.
    /// These describe a step in a pipeline.
    /// </summary>
    public class PipelineStepOption
    {
        public string Name { get; set; }
        public string ServiceType { get; set; }
    }
}
