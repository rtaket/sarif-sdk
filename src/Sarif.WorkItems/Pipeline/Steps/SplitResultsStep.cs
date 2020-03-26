using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Sarif.Visitors;
using Microsoft.WorkItems.Logging;
using Microsoft.WorkItems.Pipeline.Steps;

namespace Microsoft.CodeAnalysis.Sarif.WorkItems.Pipeline.Steps
{
    public class SplitResultsStep : Step<SarifWorkItemContextEx, SarifWorkItemContextEx>, ISplitResultsStep
    {
        public override SarifWorkItemContextEx Process(SarifWorkItemContextEx input)
        {
            Stopwatch splittingStopwatch = Stopwatch.StartNew();

            if (input.SplittingStrategy == SplittingStrategy.None)
            {
                input.SarifLogsToProcess.Add(input.SarifLog);
            }
            else
            {
                PartitionFunction<string> partitionFunction = null;

                switch (input.SplittingStrategy)
                {
                    case SplittingStrategy.PerRun:
                    {
                        partitionFunction = (result) => result.ShouldBeFiled() ? "Include" : null;
                        break;
                    }
                    case SplittingStrategy.PerResult:
                    {
                        partitionFunction = (result) => result.ShouldBeFiled() ? Guid.NewGuid().ToString() : null;
                        break;
                    }
                    default:
                    {
                        throw new ArgumentOutOfRangeException($"SplittingStrategy: {input.SplittingStrategy}");
                    }
                }

                var partitioningVisitor = new PartitioningVisitor<string>(partitionFunction, deepClone: false);
                partitioningVisitor.VisitSarifLog(input.SarifLog);

                ((List<SarifLog>)input.SarifLogsToProcess).AddRange(partitioningVisitor.GetPartitionLogs().Values);
            }

            var logsToProcessMetrics = new Dictionary<string, object>
            {
                { "splittingStrategy", input.SplittingStrategy },
                { "logsToProcessCount", input.SarifLogsToProcess.Count },
                { "splittingDurationInMilliseconds", splittingStopwatch.ElapsedMilliseconds },
            };

            this.Logger.LogMetrics(EventIds.LogsToProcessMetrics, logsToProcessMetrics);
            splittingStopwatch.Stop();

            return input;
        }
    }
}
