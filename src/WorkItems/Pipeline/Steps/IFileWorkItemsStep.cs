using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WorkItems.Pipeline.Steps;

namespace Microsoft.WorkItems.Pipeline.Steps
{
    public interface IFileWorkItemsStep : IStep<IEnumerable<WorkItemContext>, IEnumerable<WorkItemContext>>
    {
    }
}
