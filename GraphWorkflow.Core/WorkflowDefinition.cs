using System;
using System.Collections.Generic;
using System.Text;

namespace GraphWorkflow.Core
{
    public class WorkflowDefinition
    {
        public int StartPlaceIndex;
        public int EndPlaceIndex;
        public Action<object> OnWorkflowCompletion;
        public IList<PlaceNode> Places = new List<PlaceNode>();
        public IList<TransitionNode> Transitions = new List<TransitionNode>();

        public static WorkflowDefinition Start(TransitionDefinition transitionDefinition, Action<object> onWorkflowCompletion)
        {
            var workflowDefinition = new WorkflowDefinition();
            workflowDefinition.OnWorkflowCompletion = onWorkflowCompletion;
            workflowDefinition.StartPlaceIndex = 0;
            workflowDefinition.EndPlaceIndex = 1;
            workflowDefinition.Places.Add(new PlaceNode(true));
            workflowDefinition.Places.Add(new PlaceNode(false));
            workflowDefinition.Transitions.Add(new TransitionNode(
                transitionDefinition.Name,
                transitionDefinition.TransitionAction,
                transitionDefinition.PostTransitionAction,
                wfObj => true,
                new List<int> { workflowDefinition.StartPlaceIndex },
                new List<int> { workflowDefinition.EndPlaceIndex }));

            return workflowDefinition;
        }

        public WorkflowDefinition Then(TransitionDefinition transitionDefinition)
        {
            int inputPlaceIndex = EndPlaceIndex++;
            Places.Add(new PlaceNode(false));
            Transitions.Add(new TransitionNode(
                transitionDefinition.Name,
                transitionDefinition.TransitionAction,
                transitionDefinition.PostTransitionAction,
                wfObj => true,
                new List<int> { inputPlaceIndex },
                new List<int> { EndPlaceIndex }
                ));

            return this;
        }

        public WorkflowDefinition ThenOneOf(params ConditionalTransitionDefinition[] conditionalTransitionDefinitions)
        {
            int inputPlaceIndex = EndPlaceIndex;
            Places.Add(new PlaceNode(false));
            int outputPlaceIndex = Places.Count - 1;
            //Give all branches the same input node
            foreach (var conditionalTransitionDefinition in conditionalTransitionDefinitions)
            {

            }

        }

        public WorkflowDefinition ThenAllOf(params TransitionDefinition[] transitionDefinitions)
        {
            throw new NotImplementedException();
        }

        public WorkflowDefinition End()
        {
            throw new NotImplementedException();
        }
    }
}
