using System;
using System.Collections.Generic;
using System.Text;

namespace GraphWorkflow.Net
{
    public class TransitionDiff
    {
        public int TransitionIndex { get; private set; }
        public TransitionState PreviousState { get; private set; }
        public TransitionState NewState { get; private set; }
        public TransitionDiff(int transitionIndex, TransitionState previousState, TransitionState newState)
        {
            TransitionIndex = transitionIndex;
            PreviousState = previousState;
            NewState = newState;
        }

        public override string ToString()
        {
            return $"Transition at index {TransitionIndex} changed from {PreviousState} to {NewState}";
        }
    }
}
