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

        protected void ContructPipeline(PipelineOption options)
        {
            DataflowLinkOptions linkOptions = new DataflowLinkOptions() { PropagateCompletion = true };

            // The PipelineOptions defines the steps to run in a pipeline. For each step, we're going
            // to create a TranformBlock. The we'll link the TransformBlocks to form a pipeline.
            // However since the types that define the step implementation are provided as strings in the config
            // file, we need to use reflection to contstuct the TransformBlocks. The code below translates into something
            // similar to this.
            //var step1 = new TransformBlock<SarifLog, WorkItemContext>(new WorkItemContextFromSarifStep().Process);
            //var step2 = new TransformBlock<WorkItemContext, WorkItemContext>(new RemoveOptionalDataStep().Process);
            //step1.LinkTo(step2, linkOptions);

            object previousBlock = null;

            for (int i = 0; i < options.PipelineSteps.Length; i++)
            {
                PipelineStepOption stepOption = options.PipelineSteps[i];

                this.Logger.LogDebug($"Constructing PipelineStep {stepOption.Name}");

                // Get the service type. i.e. the type of the service interface.
                Type serviceType = Type.GetType(stepOption.ServiceType);
                Type stepInterfaceType = serviceType.GetInterface("Microsoft.WorkItems.Pipeline.Steps.IStep`2");

                // This is an instantiation of the step/service retrieved from the service factory.
                object stepObject = ServiceProviderFactory.ServiceProvider.GetService(serviceType);

                // Create the delegate for the step/service.
                Type funcType = typeof(Func<,>);
                Type genericFuncType = funcType.MakeGenericType(stepInterfaceType.GetGenericArguments());
                Delegate func = Delegate.CreateDelegate(genericFuncType, stepObject, "Process");

                // Get the TransformBlock type.
                Type blockType = typeof(System.Threading.Tasks.Dataflow.TransformBlock<,>);
                Type constructedType = blockType.MakeGenericType(stepInterfaceType.GetGenericArguments());

                // Construct the block options
                PropertyInfo maxDegreeOfParallelismProperty = stepInterfaceType.GetProperty("MaxDegreeOfParallelism");
                int maxDegreeOfParallelism = (int)maxDegreeOfParallelismProperty.GetValue(stepObject);
                ExecutionDataflowBlockOptions blockOptions = new ExecutionDataflowBlockOptions();
                blockOptions.MaxDegreeOfParallelism = maxDegreeOfParallelism;

                // Create the TranformBlock.
                object transformBlockObject = Activator.CreateInstance(constructedType, func, blockOptions);

                // Link the blocks
                if (previousBlock != null)
                {
                    MethodInfo linkToMethod = constructedType.GetMethod("LinkTo");
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
        }
    }
}
