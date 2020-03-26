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
    public class SarifWorkItemFilingPipeline : Pipeline<SarifWorkItemContextEx, SarifWorkItemContextEx>
    {
        public SarifWorkItemFilingPipeline(Action<SarifWorkItemContextEx> resultCallback)
        {
            IOptions<PreprocessPipelineOption> workItemOptions = ServiceProviderFactory.ServiceProvider.GetService<IOptions<PreprocessPipelineOption>>();

            this.Contruct(workItemOptions.Value);

            var callbackStep = new ActionBlock<SarifWorkItemContextEx>(resultCallback);

            DataflowLinkOptions linkOptions = new DataflowLinkOptions()
            {
                PropagateCompletion = true,
            };

            ((ISourceBlock<SarifWorkItemContextEx>)this.EndBlock).LinkTo(callbackStep, linkOptions);
            this.EndBlock = callbackStep;
        }
    }
}
