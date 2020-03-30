using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.WorkItems.Pipeline.Steps
{
    public interface IStep<TIn, TOut>
    {
        int MaxDegreeOfParallelism { get; }
        TOut Process(TIn input);
        TOut ProcessInternal(TIn input, IDictionary<string, object> customDimensions);
    }
}
