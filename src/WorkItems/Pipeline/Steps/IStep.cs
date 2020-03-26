using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.WorkItems.Pipeline.Steps
{
    public interface IStep<TIn, TOut>
    {
        TOut Process(TIn input);
    }
}
