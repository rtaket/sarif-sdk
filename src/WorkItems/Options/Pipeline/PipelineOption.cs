using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.WorkItems.Options.Pipeline
{
    /// <summary>
    /// These are settings that are hydrated from the pipelinesettings.json file.
    /// These describe the steps that comprise a pipeline.
    /// </summary>
    public abstract class PipelineOption
    {
        public PipelineStepOption[] PipelineSteps { get; set; }
    }
}
