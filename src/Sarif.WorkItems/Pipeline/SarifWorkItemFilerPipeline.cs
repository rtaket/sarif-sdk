using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.WorkItems;
using Microsoft.WorkItems.Options.Pipeline;
using Microsoft.WorkItems.Pipeline;

namespace Microsoft.CodeAnalysis.Sarif.WorkItems.Pipeline
{
    public class SarifWorkItemFilerPipeline : Pipeline<SarifWorkItemContextEx, SarifWorkItemContextEx>
    {
        public SarifWorkItemFilerPipeline(Action<SarifWorkItemContextEx> resultCallback)
        {
            // Retrieve the pipeline steps that were defined in the pipelinesettings.json file.
            IOptions<PreprocessPipelineOption> workItemOptions = ServiceProviderFactory.ServiceProvider.GetService<IOptions<PreprocessPipelineOption>>();

            // Construct the pipeline
            this.ContructPipeline(workItemOptions.Value);

            // Create the ActionBlock for the callback delegate.
            var callbackStep = new ActionBlock<SarifWorkItemContextEx>(resultCallback);
            DataflowLinkOptions linkOptions = new DataflowLinkOptions() { PropagateCompletion = true };

            // Add the callback block to the end of the pipeline.
            ((ISourceBlock<SarifWorkItemContextEx>)this.EndBlock).LinkTo(callbackStep, linkOptions);
            
            this.EndBlock = callbackStep;
        }
    }
}
