using System;
using System.Collections.Generic;
using System.Text;

namespace GraphWorkflow.Net
{
    public class TransitionDefinition
    {
        public string Name;
        public Func<object, object> TransitionAction;
        public Action<object, object> PostTransitionAction;
        public bool ShouldWait;
        public TransitionDefinition(string name, Func<object, object> transitionAction, Action<object, object> postTransitionAction, bool shouldWait = false)
        {
            Name = name;
            TransitionAction = transitionAction;
            PostTransitionAction = postTransitionAction;
            ShouldWait = shouldWait;
        }
    }
}
