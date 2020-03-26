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
    public class WorkItemFilingPipeline : Pipeline<IEnumerable<WorkItemContext>, IEnumerable<WorkItemContext>>
    {
        public WorkItemFilingPipeline(Action<IEnumerable<WorkItemContext>> resultCallback)
        {
            IOptions<WorkItemFilerPipelineOption> workItemOptions = ServiceProviderFactory.ServiceProvider.GetService<IOptions<WorkItemFilerPipelineOption>>();

            this.Contruct(workItemOptions.Value);

            var callbackStep = new ActionBlock<IEnumerable<WorkItemContext>>(resultCallback);

            DataflowLinkOptions linkOptions = new DataflowLinkOptions()
            {
                PropagateCompletion = true,
            };

            ((ISourceBlock<IEnumerable<WorkItemContext>>)this.EndBlock).LinkTo(callbackStep, linkOptions);
            this.EndBlock = callbackStep;
        }
    }
}
