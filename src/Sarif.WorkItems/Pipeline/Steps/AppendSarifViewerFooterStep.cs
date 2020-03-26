using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Sarif.Visitors;
using Microsoft.VisualStudio.Services.Account;
using Microsoft.WorkItems;
using Microsoft.WorkItems.Pipeline.Steps;

namespace Microsoft.CodeAnalysis.Sarif.WorkItems.Pipeline.Steps
{
    public class AppendSarifViewerFooterStep : Step<SarifWorkItemContextEx, SarifWorkItemContextEx>, IAppendSarifViewerFooterStep
    {
        public override SarifWorkItemContextEx ProcessInternal(SarifWorkItemContextEx input, IDictionary<string, object> customDimensions)
        {
            for (int i = 0; i < input.WorkItemContextsToProcess.Count; i++)
            {
                string footer;

                if (input.CurrentProvider == FilingClient.SourceControlProvider.AzureDevOps)
                {
                    StringBuilder azureDevOpsFooter = new StringBuilder();
                    azureDevOpsFooter.Append(@"<br><br>Other viewing options:<br>");
                    azureDevOpsFooter.AppendLine();
                    azureDevOpsFooter.AppendLine();
                    azureDevOpsFooter.Append(@"<li>Examine the complete <a href=""https://dev.azure.com/office/Office/_componentGovernance/Office?_a=alerts&typeId=1731351&alerts-view-option=active"">CG Scan</a> for this repository.</li>");
                    azureDevOpsFooter.AppendLine();
                    azureDevOpsFooter.Append(@"<li>Load the attached log file in the <a href=""https://marketplace.visualstudio.com/_apis/public/gallery/publishers/WDGIS/vsextensions/MicrosoftSarifViewer/2.1.7/vspackage"">CG Scan</a> Visual Studio SARIF add-in.</li>");
                    azureDevOpsFooter.AppendLine();
                    azureDevOpsFooter.Append(@"<li>Load the attached log file in the <a href=""https://marketplace.visualstudio.com/items?itemName=MS-SarifVSCode.sarif-viewer"">CG Scan</a> VS Code SARIF extension.</li>");
                    footer = azureDevOpsFooter.ToString();
                }
                else
                {
                    footer = @"Details for the above issues can be found in the attachment filed with this issue.";
                }

                SarifWorkItemModel model = (SarifWorkItemModel)input.WorkItemContextsToProcess[i].Model;
                model.BodyOrDescription += Environment.NewLine + footer;
            }

            return input;
        }
    }
}
