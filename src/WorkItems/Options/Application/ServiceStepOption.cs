using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.WorkItems.Options.Application
{
    /// <summary>
    /// These are settings that are hydrated from the appsettings.json file.
    /// These describe a service that is available to be used in a pipeline.
    /// </summary>
    public class ServiceStepOption
    {
        public string Name { get; set; }
        public string ServiceType { get; set; }
        public string ImplementationType { get; set; }
    }
}
