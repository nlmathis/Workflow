using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using GraphWorkflow.Net;

//Allow unit test project to access internal
[assembly: InternalsVisibleToAttribute("GraphWorkflow.Tests")]

namespace GraphWorkflow.Core
{
    public class WorkflowDefinition<TWfData>
    {
        private int EndPlaceIndex;
        private Action<TWfData> OnWorkflowCompletion = obj => { };
        private Marking InitialMarking = new Marking();

        public static implicit operator PetriNetDefinition(WorkflowDefinition<TWfData> self) 
        {
            return new PetriNetDefinition(self.EndPlaceIndex, self.InitialMarking, obj => self.OnWorkflowCompletion((TWfData)obj));
        }

        public static WorkflowDefinition<TWfData> Start<TInput, TResult>(StepDefinition<TWfData, TInput, TResult> stepDefinition)
        {
            int startPlaceIndex = 0;

            var workflowDefinition = new WorkflowDefinition<TWfData>();
            workflowDefinition.EndPlaceIndex = 1;
            workflowDefinition.InitialMarking.Places.Add(new Place(PlaceState.HasToken));
            workflowDefinition.InitialMarking.Places.Add(new Place());
            workflowDefinition.InitialMarking.Transitions.Add(new Transition(
                stepDefinition,
                wfObj => true,
                new List<int> { startPlaceIndex },
                new List<int> { workflowDefinition.EndPlaceIndex }));

            return workflowDefinition;
        }

        public WorkflowDefinition<TWfData> Then<TInput, TResult>(StepDefinition<TWfData, TInput, TResult> stepDefinition)
        {
            int inputPlaceIndex = EndPlaceIndex++;
            InitialMarking.Places.Add(new Place());
            InitialMarking.Transitions.Add(new Transition(
                stepDefinition,
                wfObj => true,
                new List<int> { inputPlaceIndex },
                new List<int> { EndPlaceIndex }
                ));

            return this;
        }

        public WorkflowDefinition<TWfData> ThenOneOf(params ConditionalTransitionDefinition[] conditionalTransitionDefinitions)
        {
            int inputPlaceIndex = EndPlaceIndex;
            InitialMarking.Places.Add(new Place());
            int outputPlaceIndex = InitialMarking.Places.Count - 1;
            //Give all branches the same input place and the same output place
            foreach (var conditionalTransitionDefinition in conditionalTransitionDefinitions)
            {

            }

            throw new NotImplementedException();
        }

        public WorkflowDefinition<TWfData> ThenAllOf(params TransitionDefinition[] transitionDefinitions)
        {
            //Before: Take current end Place add a Transition(empty) that has output Places to use as a separate input Place for each branch
            //After: Tie each output Place to shared Transition(empty) and add a single output place from that transition
            throw new NotImplementedException();
        }

        public WorkflowDefinition<TWfData> End()
        {
            throw new NotImplementedException();
        }
    }
}
