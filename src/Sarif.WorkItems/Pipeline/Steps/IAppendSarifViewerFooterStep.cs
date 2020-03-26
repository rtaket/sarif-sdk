using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WorkItems.Pipeline.Steps;

namespace Microsoft.CodeAnalysis.Sarif.WorkItems.Pipeline.Steps
{
    public interface IAppendSarifViewerFooterStep : IStep<SarifWorkItemContextEx, SarifWorkItemContextEx>
    {
    }
}
