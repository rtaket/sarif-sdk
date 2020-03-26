using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Sarif.Visitors;
using Microsoft.WorkItems;
using Microsoft.WorkItems.Pipeline.Steps;
using Microsoft.WorkItems.Logging;
using Microsoft.CodeAnalysis.WorkItems.Logging;

namespace Microsoft.CodeAnalysis.Sarif.WorkItems.Pipeline.Steps
{
    public class ConvertSarifLogToWorkItemContextStep : Step<SarifWorkItemContextEx, SarifWorkItemContextEx>, IConvertSarifLogToWorkItemContextStep
    {
        public override SarifWorkItemContextEx Process(SarifWorkItemContextEx input)
        {
            using (TimingLog timing = new TimingLog(EventIds.ConvertSarifLogToWorkItemContextStep))
            {
                timing.CustomDimensions.Add("SarifLogsToProcessCount", input.SarifLogsToProcess.Count);

                for (int i = 0; i < input.SarifLogsToProcess.Count; i++)
                {
                    SarifLog sarifLog = input.SarifLogsToProcess[i];
                    FilingClient filingClient = FilingClientFactory.Create(input.HostUri);

                    input.CurrentProvider = filingClient.Provider;

                    var workItemModel = new SarifWorkItemModel(sarifLog, input);
                    workItemModel.OwnerOrAccount = filingClient.AccountOrOrganization;
                    workItemModel.RepositoryOrProject = filingClient.ProjectOrRepository;

                    var workItemContext = new WorkItemContext(workItemModel, input.HostUri);

                    input.WorkItemContextsToProcess.Add(workItemContext);
                }
            }

            return input;
        }
    }
}
