using System;
using System.Collections.Generic;
using System.Text;

namespace GraphWorkflow.Core
{
    public class ConditionalTransitionDefinition
    {
        public ConditionalTransitionDefinition(Func<object, bool> triggerCondition, Func<WorkflowDefinition, WorkflowDefinition> transitionContext)
        {

        }
    }
}
