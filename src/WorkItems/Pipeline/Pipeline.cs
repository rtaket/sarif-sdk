using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WorkItems;
using Microsoft.WorkItems.Options.Pipeline;

namespace Microsoft.WorkItems.Pipeline
{
    public abstract class Pipeline<TIn, TOut> : IPipeline<TIn, TOut>
    {
        public Pipeline()
        {
            this.Logger = ServiceProviderFactory.ServiceProvider.GetService<ILogger<ILogger>>();
        }

        public ITargetBlock<TIn> StartBlock { get; protected set; }

        public ITargetBlock<TOut> EndBlock { get; protected set; }

        private ILogger Logger { get; }

        protected void Contruct(PipelineOption options)
        {
            DataflowLinkOptions linkOptions = new DataflowLinkOptions()
            {
                PropagateCompletion = true,
            };

            object previousBlock = null;

            for (int i = 0; i < options.PipelineSteps.Length; i++)
            {
                PipelineStepOption stepOption = options.PipelineSteps[i];

                this.Logger.LogDebug($"Constructing PipelineStep {stepOption.Name}");
                Type serviceType = Type.GetType(stepOption.ServiceType);
                Type stepInterfaceType = serviceType.GetInterface("Microsoft.WorkItems.Pipeline.Steps.IStep`2");

                object stepObject = ServiceProviderFactory.ServiceProvider.GetService(serviceType);

                Type d1 = typeof(System.Threading.Tasks.Dataflow.TransformBlock<,>);
                Type constructed = d1.MakeGenericType(stepInterfaceType.GetGenericArguments());

                Type funcType = typeof(Func<,>);
                Type genericFuncType = funcType.MakeGenericType(stepInterfaceType.GetGenericArguments());
                Delegate func = Delegate.CreateDelegate(genericFuncType, stepObject, "Process");

                var transformBlockObject = Activator.CreateInstance(constructed, func);

                if (previousBlock != null)
                {
                    MethodInfo linkToMethod = constructed.GetMethod("LinkTo");
                    linkToMethod.Invoke(previousBlock, new[] { transformBlockObject, linkOptions });
                }

                if (i == 0)
                {
                    this.StartBlock = (ITargetBlock<TIn>)transformBlockObject;
                }

                if (i == options.PipelineSteps.Length - 1)
                {
                    this.EndBlock = (ITargetBlock<TOut>)transformBlockObject;
                }

                previousBlock = transformBlockObject;
            }

            //var step1 = new TransformBlock<SarifLog, WorkItemContext>(new WorkItemContextFromSarifStep().Process);
            //var step2 = new TransformBlock<WorkItemContext, WorkItemContext>(new RemoveOptionalDataStep().Process);
            //var step3 = new TransformBlock<WorkItemContext, WorkItemContext>(new SplitResultsStep().Process);
            //var step4 = new TransformBlock<WorkItemContext, WorkItemContext>(new FilterWithPaasPolicyStep().Process);
            //var step5 = new TransformBlock<WorkItemContext, WorkItemContext>(new AddWorkItemAreaPathsStep().Process);
            //var step6 = new TransformBlock<WorkItemContext, WorkItemContext>(new AddWorkItemOwnersStep().Process);
            //var step7 = new TransformBlock<WorkItemContext, IEnumerable<WorkItemModel>>(new ConvertToWorkItemModelStep().Process);
            //var step8 = wifPipeline.StartBlock;

            //step1.LinkTo(step2, linkOptions);
            //step2.LinkTo(step3, linkOptions);
            //step3.LinkTo(step4, linkOptions);
            //step4.LinkTo(step5, linkOptions);
            //step5.LinkTo(step6, linkOptions);
            //step6.LinkTo(step7, linkOptions);
            //step7.LinkTo(step8, linkOptions);

            //this.StartBlock = step1;
            //this.EndBlock = wifPipeline.EndBlock;
        }
    }
}
