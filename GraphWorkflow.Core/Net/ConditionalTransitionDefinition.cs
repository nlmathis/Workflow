using System;
using System.Collections.Generic;
using System.Text;

namespace GraphWorkflow.Net
{
    public class ConditionalTransitionDefinition
    {
        public ConditionalTransitionDefinition(Func<object, bool> triggerCondition, Func<Transition> transitionContext)
        {

        }
    }
}
