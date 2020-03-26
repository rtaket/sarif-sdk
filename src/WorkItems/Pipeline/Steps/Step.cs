using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.WorkItems.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.WorkItems;

namespace Microsoft.WorkItems.Pipeline.Steps
{
    public abstract class Step<TIn, TOut> : IStep<TIn, TOut>
    {
        public Step()
        {
            this.Logger = ServiceProviderFactory.ServiceProvider.GetService<ILogger<ILogger>>();
        }

        public ILogger Logger { get; }

        public virtual TOut Process(TIn input)
        {
            string stepName = this.GetType().Name;

            using (TimingLog timing = new TimingLog(new EventId(9000, stepName)))
            {
                timing.CustomDimensions.Add("StepName", stepName);

                return this.ProcessInternal(input, timing.CustomDimensions);
            }
        }

        public abstract TOut ProcessInternal(TIn input, IDictionary<string, object> customDimensions);
    }
}
