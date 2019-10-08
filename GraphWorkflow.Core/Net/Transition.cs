using System;
using System.Collections.Generic;

namespace GraphWorkflow.Net
{
    public class Transition
    {
        public IList<int> InputPlaceIndicies { get; private set; }
        public IList<int> OutputPlaceIndicies { get; private set; }
        public Func<object, object> TransitionAction { get; private set; }
        public Action<object, object> PostTransitionAction { get; private set; }
        public Func<object, bool> TransitionTrigger { get; private set; }

        public string Name { get; private set; }

        public TransitionState State { get; set; }

        public TransitionStartType StartType { get; private set; }
        public Transition(string name, Func<object, object> transitionAction, Action<object, object> postTransitionAction, Func<object, bool> transitionTrigger, IList<int> inputPlaceIndicies, IList<int> outputPlaceIndicies, TransitionStartType startType = TransitionStartType.Immediate)
        {
            Name = name;
            TransitionAction = transitionAction;
            PostTransitionAction = postTransitionAction;
            TransitionTrigger = transitionTrigger;
            InputPlaceIndicies = inputPlaceIndicies;
            OutputPlaceIndicies = outputPlaceIndicies;
            TransitionAction = transitionAction;
            State = TransitionState.Unused;
            StartType = startType;
        }

        public Transition(TransitionDefinition transitionDefinition, Func<object, bool> transitionTrigger, IList<int> inputPlaceIndicies, IList<int> outputPlaceIndicies)
           : this(transitionDefinition.Name, transitionDefinition.TransitionAction, transitionDefinition.PostTransitionAction, 
                 transitionTrigger, inputPlaceIndicies, outputPlaceIndicies, transitionDefinition.TransitionStartType)
        {

        }

        public Transition(string name, IList<int> inputPlaceIndices, IList<int> outputPlaceIndices)
            : this(name, (object wfObj) => null, (wfObj, result) => { }, wfObj => true, 
                  inputPlaceIndices, outputPlaceIndices, TransitionStartType.NoOp)
        {
        }

        public Transition(string name, IList<int> inputPlaceIndices, IList<int> outputPlaceIndices, Func<object, bool> transitionTrigger)
            : this(name, (object wfObj) => null, (wfObj, result) => { }, transitionTrigger,
                  inputPlaceIndices, outputPlaceIndices, TransitionStartType.NoOp)
        {
        }

        public Transition Clone()
        {
            var transition = new Transition(Name, TransitionAction, PostTransitionAction, TransitionTrigger, InputPlaceIndicies, OutputPlaceIndicies, StartType);
            transition.State = State;

            return transition;
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

    public enum TransitionStartType
    {
        Immediate,
        Wait,
        NoOp
    }
}
