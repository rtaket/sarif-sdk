using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.WorkItems;
using Microsoft.WorkItems.Options.Pipeline;

namespace Microsoft.WorkItems.Pipeline
{
    public class WorkItemFilerPipeline : Pipeline<IEnumerable<WorkItemContext>, IEnumerable<WorkItemContext>>
    {
        public WorkItemFilerPipeline(Action<IEnumerable<WorkItemContext>> resultCallback)
        {
            // Retrieve the pipeline steps that were defined in the pipelinesettings.json file.
            IOptions<WorkItemFilerPipelineOption> workItemOptions = ServiceProviderFactory.ServiceProvider.GetService<IOptions<WorkItemFilerPipelineOption>>();

            // Construct the pipeline
            this.ContructPipeline(workItemOptions.Value);

            // Create the ActionBlock for the callback delegate.
            var callbackStep = new ActionBlock<IEnumerable<WorkItemContext>>(resultCallback);
            DataflowLinkOptions linkOptions = new DataflowLinkOptions() { PropagateCompletion = true };

            // Add the callback block to the end of the pipeline.
            ((ISourceBlock<IEnumerable<WorkItemContext>>)this.EndBlock).LinkTo(callbackStep, linkOptions);

            this.EndBlock = callbackStep;
        }
    }
}
