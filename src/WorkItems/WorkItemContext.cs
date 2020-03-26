﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.WorkItems
{
    public class WorkItemContext
    {
        public WorkItemContext(WorkItemModel model, Uri filingHostUri)
        {
            this.Model = model;
            this.FilingHostUri = filingHostUri;
        }

        public WorkItemModel Model { get; }

        public Uri FilingHostUri { get; }
    }
}
