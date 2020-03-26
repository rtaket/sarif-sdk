using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.WorkItems;

namespace Microsoft.CodeAnalysis.Sarif.WorkItems.Pipeline
{
    public class SarifWorkItemContextEx : SarifWorkItemContext
    {
        public SarifWorkItemContextEx(SarifLog sarifLog, Uri filingUri) 
        {
            this.SarifLog = sarifLog;
            this.SarifLogsToProcess = new List<SarifLog>();
            this.WorkItemContextsToProcess = new List<WorkItemContext>();
            this.HostUri = filingUri;
        }

        public SarifLog SarifLog
        {
            get;
        }

        public IList<SarifLog> SarifLogsToProcess
        {
            get;
        }

        public IList<WorkItemContext> WorkItemContextsToProcess
        {
            get;
        }
    }
}
