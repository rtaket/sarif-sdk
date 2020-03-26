using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Sarif.Visitors;
using Microsoft.WorkItems.Pipeline.Steps;

namespace Microsoft.CodeAnalysis.Sarif.WorkItems.Pipeline.Steps
{
    public class InsertOptionalDataStep : Step<SarifWorkItemContextEx, SarifWorkItemContextEx>, IInsertOptionalDataStep
    {
        public override SarifWorkItemContextEx Process(SarifWorkItemContextEx input)
        {
            OptionallyEmittedData optionallyEmittedData = input.DataToInsert;
            if (optionallyEmittedData != OptionallyEmittedData.None)
            {
                var dataInsertingVisitor = new InsertOptionalDataVisitor(optionallyEmittedData);
                dataInsertingVisitor.Visit(input.SarifLog);
            }

            return input;
        }
    }
}
