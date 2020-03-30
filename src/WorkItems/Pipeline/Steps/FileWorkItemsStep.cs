using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.WorkItems.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WorkItems.Logging;

namespace Microsoft.WorkItems.Pipeline.Steps
{
    public class FileWorkItemsStep : Step<IEnumerable<WorkItemContext>, IEnumerable<WorkItemContext>>, IFileWorkItemsStep
    {
        public override int MaxDegreeOfParallelism => 3;

        public override IEnumerable<WorkItemContext> ProcessInternal(IEnumerable<WorkItemContext> input, IDictionary<string, object> customDimensions)
        {
            customDimensions.Add("InputCount", input.Count());

            ParallelOptions parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 2,
            };

            List<WorkItemModel> updatedModels = new List<WorkItemModel>();

            // Hmm, this doesn't make sense in the current pipeline. We queue one item at a time,
            // so there's never anything to parallelize. Perhaps we should change this to a regular foreach.
            Parallel.ForEach(input, parallelOptions, (context) =>
            {
                IConfiguration config = ServiceProviderFactory.ServiceProvider.GetService<IConfiguration>();

                FilingClient filingClient = FilingClientFactory.Create(context.FilingHostUri);
                filingClient.Connect(config["SarifWorkItemFilingPat"]).Wait();
                IEnumerable<WorkItemModel> retModels = filingClient.FileWorkItems(new[] { context.Model }).Result;

                lock (updatedModels)
                {
                    updatedModels.AddRange(retModels);
                }
            });

            customDimensions.Add("UpdatedCount", updatedModels.Count);

            return updatedModels.Select(model => new WorkItemContext(model, input.First().FilingHostUri));
        }
    }
}
