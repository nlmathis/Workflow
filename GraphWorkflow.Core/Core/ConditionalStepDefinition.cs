using System;
using System.Collections.Generic;
using System.Text;

namespace GraphWorkflow.Core
{
    public class ConditionalStepDefinition<TWfData>
    {
        public Func<TWfData, bool> TriggerCondition { get; private set; }
        public Func<WorkflowDefinition<TWfData>, WorkflowDefinition<TWfData>> BranchContext { get; private set; }
        public ConditionalStepDefinition(Func<TWfData, bool> triggerCondition, Func<WorkflowDefinition<TWfData>, WorkflowDefinition<TWfData>> branchContext)
        {
            TriggerCondition = triggerCondition;
            BranchContext = branchContext;
        }
    }
}
