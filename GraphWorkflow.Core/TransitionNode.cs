using System;
using System.Collections.Generic;

namespace GraphWorkflow.Core
{
    public class TransitionNode
    {
        public IList<int> InputPlaceIndicies { get; private set; }
        public IList<int> OutputPlaceIndicies { get; private set; }
        public Func<object, object> TransitionAction { get; private set; }
        public Action<object, object> PostTransitionAction { get; private set; }
        public Func<object, bool> TransitionTrigger { get; private set; }

        public string Name { get; private set; }

        public TransitionState State { get; set; }

        public bool ShouldWait { get; private set; }
        public TransitionNode(string name, Func<object, object> transitionAction, Action<object, object> postTransitionAction, Func<object, bool> transitionTrigger, IList<int> inputPlaceIndicies, IList<int> outputPlaceIndicies, bool shouldWait = false)
        {
            Name = name;
            TransitionAction = transitionAction;
            PostTransitionAction = postTransitionAction;
            TransitionTrigger = transitionTrigger;
            InputPlaceIndicies = inputPlaceIndicies;
            OutputPlaceIndicies = outputPlaceIndicies;
            TransitionAction = transitionAction;
            State = TransitionState.Unused;
            ShouldWait = shouldWait;
        }
    }

    public enum TransitionState
    {
        Executing,
        Waiting,
        Completed,
        Failed,
        Unused
    }
}
