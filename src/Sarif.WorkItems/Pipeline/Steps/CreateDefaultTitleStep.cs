using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Sarif.Visitors;
using Microsoft.VisualStudio.Services.Account;
using Microsoft.WorkItems.Pipeline.Steps;

namespace Microsoft.CodeAnalysis.Sarif.WorkItems.Pipeline.Steps
{
    public class CreateDefaultTitleStep : Step<SarifWorkItemContextEx, SarifWorkItemContextEx>, ICreateDefaultTitleStep
    {
        public override SarifWorkItemContextEx ProcessInternal(SarifWorkItemContextEx input, IDictionary<string, object> customDimensions)
        {
            for (int i = 0; i < input.WorkItemContextsToProcess.Count; i++)
            {
                SarifWorkItemModel model = (SarifWorkItemModel)input.WorkItemContextsToProcess[i].Model;
                model.Title = model.SarifLog.Runs?[0]?.CreateWorkItemTitle();
                model.Title = model.Title ?? "[ERROR GENERATING TITLE]";
            }

            return input;
        }
    }
}
