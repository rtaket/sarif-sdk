// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WorkItems.Logging;
using Microsoft.WorkItems.Options.Application;
using Microsoft.WorkItems.Options.Pipeline;

namespace Microsoft.WorkItems
{
    public static class ServiceProviderFactory
    {
        internal const string AppSettingsEnvironmentVariableName = "SarifWorkItemAppSettingsFile";
        private const string LoggingSection = "Sarif-WorkItems:Logging";
        private const string LoggingApplicationInsightsSection = "Sarif-WorkItems:Logging:ApplicationInsights";
        private const string LoggingApplicationInsightsInstrumentationKey = "Sarif-WorkItems:Logging:ApplicationInsights:InstrumentationKey";
        private const string LoggingConsoleSection = "Sarif-WorkItems:Logging:Console";

        public static IServiceProvider ServiceProvider { get; private set; }

        static ServiceProviderFactory()
        {
            Initialize(new Dictionary<string, string>());
        }

        internal static void Initialize(IDictionary<string, string> additionalServices)
        {
            IServiceCollection services = new ServiceCollection();

            // Read the settings files and register the service.
            IConfiguration config = GetConfiguration();
            services.AddSingleton(typeof(IConfiguration), config);

            // Register the TelemetryChannel. This is so we can retrieve and flush the
            // channel in the application.
            ITelemetryChannel channel = new InMemoryChannel();
            services.AddSingleton(typeof(ITelemetryChannel), channel);

            // Create and register options (classes to access the config settings in the application)
            services.Configure<ServiceOption>(config.GetSection("Sarif-WorkItems:Service"));
            services.Configure<PreprocessPipelineOption>(config.GetSection("Sarif-WorkItems:PreprocessPipeline"));
            services.Configure<WorkItemFilerPipelineOption>(config.GetSection("Sarif-WorkItems:WorkItemFilerPipeline"));

            // Register injected services. Any services registered here will
            // take precedence over services defined in the appsettings.json file.
            // Services registered here will typically be test services to overide the
            // behavior of a production service.
            foreach (KeyValuePair<string, string> additionalService in additionalServices)
            {
                Type serviceInteface = Type.GetType(additionalService.Key);
                Type serviceImplementation = Type.GetType(additionalService.Value);

                services.AddTransient(serviceInteface, serviceImplementation);
            }

            // Register any loggers
            services.AddLogging(builder =>
            {
                if (config.GetSection(LoggingConsoleSection).Exists())
                {
                    builder.AddConsole();
                }

                if (config.GetSection(LoggingApplicationInsightsSection).Exists())
                {
                    if (!string.IsNullOrEmpty(config[LoggingApplicationInsightsInstrumentationKey]))
                    {
                        builder.AddApplicationInsights(config[LoggingApplicationInsightsInstrumentationKey], options => options.FlushOnDispose = true);
                    }
                }

                builder.AddConfiguration(config.GetSection(LoggingSection));
            });

            services.Configure<TelemetryConfiguration>((o) =>
            {
                o.TelemetryChannel = channel;
                o.TelemetryInitializers.Add(new ApplicationInsightsTelemetryInitializer());
                o.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            });

            // We need to build the service provider once to hydrate the options services.
            ServiceProvider = services.BuildServiceProvider();

            ILogger logger = ServiceProviderFactory.ServiceProvider.GetService<ILogger<ILogger>>();
            IOptions<ServiceOption> workItemOptions = ServiceProvider.GetService<IOptions<ServiceOption>>();

            // Register the services defined in the appsettings.json file.
            foreach (ServiceStepOption stepServiceOption in workItemOptions.Value.ServiceSteps)
            {
                logger.LogDebug($"Registering StepService {stepServiceOption.Name}");
                Type serviceType = Type.GetType(stepServiceOption.ServiceType);
                Type implementationType = Type.GetType(stepServiceOption.ImplementationType);

                // Use the Try overload to ensure we do not override any services that have already been registered.
                services.TryAddTransient(serviceType, implementationType);
            }

            // Rebuild the service provider that now inclues the services from the appsettings.json file.
            ServiceProvider = services.BuildServiceProvider();
        }

        internal static string GetAppSettingsFilePath()
        {
            string appSettingsFile = Environment.GetEnvironmentVariable(AppSettingsEnvironmentVariableName);

            if (!string.IsNullOrEmpty(appSettingsFile))
            {
                return appSettingsFile;
            }

            return "appSettings.json";
        }

        internal static IConfiguration GetConfiguration()
        {
            string appSettingsFile = GetAppSettingsFilePath();
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile(appSettingsFile, optional: true, reloadOnChange: false)
                .AddJsonFile("pipelinesettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
                .Build();

            return config;
        }
    }
}
