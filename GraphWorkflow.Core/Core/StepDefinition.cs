using System;
using System.Collections.Generic;
using System.Text;
using GraphWorkflow.Net;

namespace GraphWorkflow.Core
{
    public class StepDefinition<TWfData, TInput, TResult>
    {
        private string Name;
        private Func<TWfData, TInput> PreTransitionAction = wfData => default(TInput);
        private Func<TInput, TResult> TransitionAction = input => default(TResult);
        private Action<TWfData, TResult> PostTransitionAction = (wfData, result) => { };
        private TransitionStartType TransitionStartType = TransitionStartType.Immediate;
        public StepDefinition(string name, Func<TInput, TResult> transitionAction)
        {
            Name = name;
            TransitionAction = transitionAction;
        }

        public StepDefinition<TWfData, TInput, TResult> ShouldWait()
        {
            TransitionStartType = TransitionStartType.Wait;
            return this;
        }

        public StepDefinition<TWfData, TInput, TResult> MapInput(Func<TWfData, TInput> inputMapper)
        {
            PreTransitionAction = inputMapper;
            return this;
        }

        internal StepDefinition<TWfData, TInput, TResult> AfterExecution(Action<TWfData, TResult> outputAction)
        {
            PostTransitionAction = outputAction;
            return this;
        }

        public static implicit operator TransitionDefinition(StepDefinition<TWfData, TInput, TResult> self)
        {
            Func<object, object> transitionAction = (obj) => (object)self.TransitionAction(self.PreTransitionAction((TWfData)obj));
            Action<object, object> postTransitionAction = (wfObj, resObj) => self.PostTransitionAction((TWfData)wfObj, (TResult)resObj);
            return new TransitionDefinition(self.Name, transitionAction, postTransitionAction, self.TransitionStartType);
        }
    }
}
