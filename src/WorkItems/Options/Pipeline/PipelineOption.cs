using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.WorkItems.Options.Pipeline
{
    public abstract class PipelineOption
    {
        public PipelineStepOption[] PipelineSteps { get; set; }
    }
}
