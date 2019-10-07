using System;
using System.Collections.Generic;
using System.Text;

namespace GraphWorkflow.Net
{
    public class PetriNetDefinition
    {
        public int EndPlaceIndex;
        public Action<object> OnWorkflowCompletion;
        public Marking InitialMarking = new Marking();

        public static PetriNetDefinition Start(TransitionDefinition transitionDefinition, Action<object> onWorkflowCompletion)
        {
            int startPlaceIndex = 0;

            var workflowDefinition = new PetriNetDefinition();
            workflowDefinition.OnWorkflowCompletion = onWorkflowCompletion;
            workflowDefinition.EndPlaceIndex = 1;
            workflowDefinition.InitialMarking.Places.Add(new Place(PlaceState.HasToken));
            workflowDefinition.InitialMarking.Places.Add(new Place());
            workflowDefinition.InitialMarking.Transitions.Add(new Transition(
                transitionDefinition.Name,
                transitionDefinition.TransitionAction,
                transitionDefinition.PostTransitionAction,
                wfObj => true,
                new List<int> { startPlaceIndex },
                new List<int> { workflowDefinition.EndPlaceIndex }));

            return workflowDefinition;
        }

        public PetriNetDefinition Then(TransitionDefinition transitionDefinition)
        {
            int inputPlaceIndex = EndPlaceIndex++;
            InitialMarking.Places.Add(new Place());
            InitialMarking.Transitions.Add(new Transition(
                transitionDefinition.Name,
                transitionDefinition.TransitionAction,
                transitionDefinition.PostTransitionAction,
                wfObj => true,
                new List<int> { inputPlaceIndex },
                new List<int> { EndPlaceIndex }
                ));

            return this;
        }

        public PetriNetDefinition ThenOneOf(params ConditionalTransitionDefinition[] conditionalTransitionDefinitions)
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

        public PetriNetDefinition ThenAllOf(params TransitionDefinition[] transitionDefinitions)
        {
            //Before: Take current end Place add a Transition(empty) that has output Places to use as a separate input Place for each branch
            //After: Tie each output Place to shared Transition(empty) and add a single output place from that transition
            throw new NotImplementedException();
        }

        public PetriNetDefinition End()
        {
            throw new NotImplementedException();
        }
    }
}
