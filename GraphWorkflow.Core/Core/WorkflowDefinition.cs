using System;
using System.Collections.Generic;
using System.Linq;
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

        public static WorkflowDefinition<TWfData> Start()
        {
            var workflowDefinition = new WorkflowDefinition<TWfData>();
            workflowDefinition.AddPlace(PlaceState.HasToken);//Add start place

            return workflowDefinition;
        }

        public static WorkflowDefinition<TWfData> Start<TInput, TResult>(StepDefinition<TWfData, TInput, TResult> stepDefinition)
        {
            var workflowDefinition = Start();
            int inputPlaceIndex = workflowDefinition.EndPlaceIndex;
            workflowDefinition.AddPlace();//Add end Place is new input Place
            workflowDefinition.InitialMarking.Transitions.Add(new Transition(
                stepDefinition,
                wfObj => true,
                new List<int> { inputPlaceIndex },
                new List<int> { workflowDefinition.EndPlaceIndex }));

            return workflowDefinition;
        }

        public WorkflowDefinition<TWfData> Then<TInput, TResult>(StepDefinition<TWfData, TInput, TResult> stepDefinition)
        {
            int inputPlaceIndex = EndPlaceIndex;//Previous end Place is new input Place
            AddPlace();
            InitialMarking.Transitions.Add(new Transition(
                stepDefinition,
                wfObj => true,
                new List<int> { inputPlaceIndex },
                new List<int> { EndPlaceIndex }
                ));

            return this;
        }

        public WorkflowDefinition<TWfData> ThenOneOf(params ConditionalStepDefinition<TWfData>[] conditionalStepDefinitions)
        {
            int inputPlaceIndex = EndPlaceIndex;
            int orJoinOutputPlaceIndex = AddPlace();

            //Give all branches the same input place and the same output place
            foreach (var conditionalStepDefinition in conditionalStepDefinitions)
            {
                int orOutputPlaceIndex = AddPlace();

                var orBranchTransition = new Transition($"NoOp({InitialMarking.Transitions.Count})",
                    new List<int> { inputPlaceIndex },
                    new List<int> { orOutputPlaceIndex },
                    (wfData) => conditionalStepDefinition.TriggerCondition((TWfData)wfData));
                InitialMarking.Transitions.Add(orBranchTransition);

                conditionalStepDefinition.BranchContext(this);

                var orJoinTransition = new Transition($"NoOp({InitialMarking.Transitions.Count})",
                new List<int> { EndPlaceIndex },
               new List<int> { orJoinOutputPlaceIndex });
                InitialMarking.Transitions.Add(orJoinTransition);
            }

            EndPlaceIndex = orJoinOutputPlaceIndex;

            return this;
        }

        public WorkflowDefinition<TWfData> ThenAllOf(params Func<WorkflowDefinition<TWfData>, WorkflowDefinition<TWfData>>[] branches)
        {
            //Before: Take current end Place add a Transition(empty) that has output Places to use as a separate input Place for each branch
            //After: Tie each output Place to shared Transition(empty) and add a single output place from that transition
            int andSplitInputPlaceIndex = EndPlaceIndex;//Previous end Place is new Input Place
            int andJoinOutputPlaceIndex = AddPlace();//Add andJoin Place

            int branchCount = branches.Length;
            var andSplitOutputPlaceIndices = new List<int>();
            var andJoinInputPlaceIndices = new List<int>();
            foreach (var branch in branches)
            {
                int splitOutputPlaceIndex = AddPlace();
                andSplitOutputPlaceIndices.Add(splitOutputPlaceIndex);
                branch(this);
                andJoinInputPlaceIndices.Add(EndPlaceIndex);
            }

            var andSplitTransition = new Transition($"NoOp({InitialMarking.Transitions.Count})",
                new List<int> { andSplitInputPlaceIndex },
                andSplitOutputPlaceIndices);

            InitialMarking.Transitions.Add(andSplitTransition);

            var andJoinTransition = new Transition($"NoOp({InitialMarking.Transitions.Count})",
                andJoinInputPlaceIndices,
                new List<int> { andJoinOutputPlaceIndex });

            InitialMarking.Transitions.Add(andJoinTransition);

            EndPlaceIndex = andJoinOutputPlaceIndex;

            return this;
        }

        private int AddPlace(PlaceState state = PlaceState.Unused)
        {
            var newPlace = new Place(state);
            InitialMarking.Places.Add(newPlace);
            EndPlaceIndex = InitialMarking.Places.Count - 1;

            return EndPlaceIndex;
        }

        public WorkflowDefinition<TWfData> End()
        {
            throw new NotImplementedException();
        }
    }
}
