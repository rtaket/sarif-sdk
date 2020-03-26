using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.WorkItems.Pipeline.Steps
{
    public class FileWorkItemsStep : Step<IEnumerable<WorkItemContext>, IEnumerable<WorkItemContext>>, IFileWorkItemsStep
    {
        public override IEnumerable<WorkItemContext> Process(IEnumerable<WorkItemContext> input)
        {
            ParallelOptions parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 2,
            };

            List<WorkItemModel> updatedModels = new List<WorkItemModel>();

            Parallel.ForEach(input, parallelOptions, (context) =>
            {
                IConfiguration config = ServiceProviderFactory.ServiceProvider.GetService<IConfiguration>();

                FilingClient filingClient = FilingClientFactory.Create(context.FilingHostUri);
                filingClient.Connect(config["SarifWorkItemFilingPat"]).Wait();
                var retModels = filingClient.FileWorkItems(new[] { context.Model }).Result;

                lock (updatedModels)
                {
                    updatedModels.AddRange(retModels);
                }
            });

            return updatedModels.Select(model => new WorkItemContext(model, input.First().FilingHostUri));
        }
    }
}
