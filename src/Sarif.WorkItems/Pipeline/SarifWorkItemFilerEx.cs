// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.CodeAnalysis.Sarif.Visitors;
using Microsoft.CodeAnalysis.WorkItems.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.WorkItems;
using Microsoft.WorkItems.Logging;
using Microsoft.WorkItems.Pipeline;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Sarif.WorkItems.Pipeline
{
    public class SarifWorkItemFilerEx : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SarifWorkItemFilerEx"> class.</see>
        /// </summary>
        public SarifWorkItemFilerEx()
        {
            this.Logger = ServiceProviderFactory.ServiceProvider.GetService<ILogger<ILogger>>();
        }

        internal ILogger Logger { get; }

        public virtual void FileWorkItems(SarifWorkItemContextEx context)
        {
            using (TimingLog timing = new TimingLog(EventIds.SarifWorkItemFilerEx_FileWorkItems))
            {
                using (Logger.BeginScope(nameof(FileWorkItems)))
                {
                    SarifWorkItemFilerPipeline pipeline = new SarifWorkItemFilerPipeline(CreateWorkItems);
                    pipeline.StartBlock.Post(context);
                    pipeline.StartBlock.Complete();
                    pipeline.EndBlock.Completion.Wait();
                }
            }
        }

        internal void CreateWorkItems(SarifWorkItemContextEx context)
        {
            WorkItemFilerPipeline pipeline = new WorkItemFilerPipeline(Complete);

            foreach (WorkItemContext workItemContext in context.WorkItemContextsToProcess)
            {
                pipeline.StartBlock.Post(new[] { workItemContext });
            }

            pipeline.StartBlock.Complete();
            pipeline.EndBlock.Completion.Wait();
        }

        internal void Complete(IEnumerable<WorkItemContext> completedContexts)
        {
            Logger.LogInformation($"Created {completedContexts.Count()} work items");

            foreach (WorkItemContext context in completedContexts)
            {
                Logger.LogInformation($"{context.Model.HtmlUri}");
            }
        }

        public void Dispose()
        {
            ITelemetryChannel channel = ServiceProviderFactory.ServiceProvider.GetService<ITelemetryChannel>();
            channel?.Flush();
            channel?.Dispose();
        }
    }
}