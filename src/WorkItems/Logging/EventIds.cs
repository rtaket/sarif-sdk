// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Build.WebApi;

namespace Microsoft.WorkItems.Logging
{
    public static class EventIds
    {
        public static EventId LogsToProcessMetrics => new EventId(9001, nameof(LogsToProcessMetrics));
        public static EventId ConvertSarifLogToWorkItemContextStep => new EventId(9002, nameof(ConvertSarifLogToWorkItemContextStep));
        public static EventId FileWorkItemsStep => new EventId(9003, nameof(FileWorkItemsStep));
        public static EventId SarifWorkItemFilerEx_FileWorkItems => new EventId(9004, nameof(SarifWorkItemFilerEx_FileWorkItems));
    }
}
