using System;
using System.Collections.Generic;
using System.Text;
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
        
        public abstract TOut Process(TIn input);
    }
}
