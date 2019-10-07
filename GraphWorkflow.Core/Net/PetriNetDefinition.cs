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

        public PetriNetDefinition(int endPlaceIndex, Marking initialMarking, Action<object> onWorkflowCompletion)
        {
            EndPlaceIndex = endPlaceIndex;
            InitialMarking = initialMarking;
            OnWorkflowCompletion = onWorkflowCompletion;
        }
    }

    
}
