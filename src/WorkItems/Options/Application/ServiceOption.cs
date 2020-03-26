using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.WorkItems.Options.Application
{
    /// <summary>
    /// These are settings that are hydrated from the appsettings.json file.
    /// They are the list of services that are available to be used in a pipeline.
    /// </summary>
    public class ServiceOption
    {
        public ServiceStepOption[] ServiceSteps { get; set; }
    }
}
