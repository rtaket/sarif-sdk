using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.WorkItems.Pipeline
{
    public interface IPipeline<TIn, TOut>
    {
        ITargetBlock<TIn> StartBlock { get; }
        ITargetBlock<TOut> EndBlock { get; }
    }
}
