using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Sarif.Visitors;
using Microsoft.WorkItems.Pipeline.Steps;

namespace Microsoft.CodeAnalysis.Sarif.WorkItems.Pipeline.Steps
{
    public class RemoveOptionalDataStep : Step<SarifWorkItemContextEx, SarifWorkItemContextEx>, IRemoveOptionalDataStep
    {
        public override SarifWorkItemContextEx Process(SarifWorkItemContextEx input)
        {
            OptionallyEmittedData optionallyEmittedData = input.DataToRemove;
            if (optionallyEmittedData != OptionallyEmittedData.None)
            {
                var dataRemovingVisitor = new RemoveOptionalDataVisitor(optionallyEmittedData);
                dataRemovingVisitor.Visit(input.SarifLog);
            }

            return input;
        }
    }
}
