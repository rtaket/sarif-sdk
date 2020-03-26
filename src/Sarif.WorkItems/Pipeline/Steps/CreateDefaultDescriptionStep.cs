using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Sarif.Visitors;
using Microsoft.VisualStudio.Services.Account;
using Microsoft.WorkItems.Pipeline.Steps;

namespace Microsoft.CodeAnalysis.Sarif.WorkItems.Pipeline.Steps
{
    public class CreateDefaultDescriptionStep : Step<SarifWorkItemContextEx, SarifWorkItemContextEx>, ICreateDefaultDescriptionStep
    {
        public override SarifWorkItemContextEx Process(SarifWorkItemContextEx input)
        {
            for (int i = 0; i < input.WorkItemContextsToProcess.Count; i++)
            {
                SarifWorkItemModel model = (SarifWorkItemModel)input.WorkItemContextsToProcess[i].Model;
                model.BodyOrDescription = this.CreateWorkItemDescription(model.SarifLog, model.LocationUri);
            }

            return input;
        }

        private Dictionary<Run, int> ComputeRunResultCounts(SarifLog log)
        {
            if (log == null) { throw new ArgumentNullException(nameof(log)); }
            if (log.Runs == null) { throw new ArgumentNullException(nameof(log.Runs)); }

            var resultCountsByRun = new Dictionary<Run, int>();

            foreach (Run run in log?.Runs)
            {
                if (run != null && run.Results != null)
                {
                    resultCountsByRun.Add(run, run.Results.Count);
                }
            }

            return resultCountsByRun;
        }
        
        private string CreateWorkItemDescription(SarifLog log, Uri locationUri)
        {
            Dictionary<Run, int> resultCountsByRun = this.ComputeRunResultCounts(log);
            StringBuilder templateText = new StringBuilder();
            int runningResults = 0;
            string toolNames = string.Empty;
            string multipleToolsFooter = string.Empty;

            Uri runRepositoryUri = resultCountsByRun.FirstOrDefault().Key?.VersionControlProvenance?.FirstOrDefault().RepositoryUri;
            string detectionLocation = !string.IsNullOrEmpty(runRepositoryUri?.OriginalString) ? runRepositoryUri?.OriginalString : locationUri?.OriginalString;

            toolNames = string.Format("'{0}'", resultCountsByRun.FirstOrDefault().Key?.Tool?.Driver?.Name);
            runningResults = resultCountsByRun.FirstOrDefault().Value;

            if (resultCountsByRun.Count > 1)
            {
                Run lastrun = resultCountsByRun.Last().Key;
                multipleToolsFooter = " (and other locations)";
                foreach (KeyValuePair<Run, int> run in resultCountsByRun)
                {
                    if (run.Key == resultCountsByRun.First().Key)
                    {
                        continue;
                    }
                    else if (run.Key == lastrun)
                    {
                        toolNames = string.Join(" and ", toolNames, string.Format("'{0}'", run.Key?.Tool?.Driver?.Name));
                        runningResults += run.Value;
                    }
                    else
                    {
                        toolNames = string.Join(", ", toolNames, string.Format("'{0}'", run.Key?.Tool?.Driver?.Name));
                        runningResults += run.Value;
                    }
                }
            }

            templateText = new StringBuilder(string.Format(@"This work item contains {0} {1} issue(s) detected in '{2}'{3}.  ", runningResults, toolNames, detectionLocation, multipleToolsFooter));
            templateText.Append("Click the 'Scans' tab to review results.");
            templateText.AppendLine();

            return templateText.ToString();
        }
    }
}
