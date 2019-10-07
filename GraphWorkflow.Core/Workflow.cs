using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GraphWorkflow.Core
{
    public class Workflow
    {
        public IList<PlaceNode> PlaceNodes;
        public int EndPlaceNodeIndex;
        public IList<TransitionNode> TransitionNodes;
        public object WorkflowData;
        public Action<object> OnWorkflowCompletion;
        private object _workflowLock = new object();
        private ILogger _logger;

        public Workflow(int endPlaceNodeIndex, Action<object> onWorkflowCompletion, IList<PlaceNode> placeNodes, IList<TransitionNode> transitionNodes, ILogger logger)
        {
            EndPlaceNodeIndex = endPlaceNodeIndex;
            OnWorkflowCompletion = onWorkflowCompletion;
            PlaceNodes = placeNodes;
            TransitionNodes = transitionNodes;
            _logger = logger;
        }

        public Workflow(WorkflowDefinition workflowDefinition, ILogger logger)
        {
            EndPlaceNodeIndex = workflowDefinition.EndPlaceIndex;
            OnWorkflowCompletion = workflowDefinition.OnWorkflowCompletion;
            PlaceNodes = workflowDefinition.Places;
            TransitionNodes = workflowDefinition.Transitions;
            _logger = logger;
        }

        public string StartWorkflow(object inputData)
        {
            WorkflowData = inputData;
            _logger.Log($"Starting workflow");
            UpdateWorkflowState();

            return Guid.NewGuid().ToString();
        }

        private void UpdateWorkflowState()
        {
            lock(_workflowLock)
            {
                if (PlaceNodes[EndPlaceNodeIndex].HasToken)
                {
                    EndWorkflow();
                }
                else
                {
                    foreach (var transitionNode in TransitionNodes.Where(tn => tn.TransitionTrigger(WorkflowData) && tn.InputPlaceIndicies.All(inputPlaceIndex => PlaceNodes[inputPlaceIndex].HasToken)))
                    {
                        StartTransition(transitionNode);
                    }
                }
            }
        }

        public IEnumerable<int> GetWaitingTransitionIds()
        {
            lock(_workflowLock)
            {
                return TransitionNodes.Select((tn, id) => new { tn.State, id })
                    .Where(tn => tn.State == TransitionState.Waiting)
                    .Select(tn => tn.id);
            }
        }

        public void ExecuteTransition(int transitionId, object inputData)
        {
            lock (_workflowLock)
            {
                var nodeToExecute = TransitionNodes[transitionId];
                _logger.Log($"External initiation of transition {nodeToExecute.Name}");
                StartTransition(nodeToExecute, inputData);
            }
        }

        private void EndWorkflow()
        {
            _logger.Log("Ending workflow");
            OnWorkflowCompletion(WorkflowData);
        }
        private void PersistWorkflowState()
        {
            _logger.Log("Persisting workflow");
            //Consider including IPersister here
        }

        private void StartTransition(TransitionNode transitionNodeToStart, object inputData)
        {
            _logger.Log($"Consuming input tokens for: transition {transitionNodeToStart.Name}");
            foreach (var inputPlaceIndex in transitionNodeToStart.InputPlaceIndicies)
            {
                PlaceNodes[inputPlaceIndex].HasToken = false;
            }

            if (transitionNodeToStart.ShouldWait && transitionNodeToStart.State != TransitionState.Waiting)
            {
                _logger.Log($"Updating transition: {transitionNodeToStart.Name} to Waiting");
                transitionNodeToStart.State = TransitionState.Waiting;
            }
            else
            {
                _logger.Log($"Executing transition: {transitionNodeToStart.Name}");
                transitionNodeToStart.State = TransitionState.Executing;
                var thread = Task.Run(() => transitionNodeToStart.TransitionAction(inputData))//Consider including an IScheduler and using that here
                    .ContinueWith(task => EndTransition(task.Result, transitionNodeToStart));
            }

            PersistWorkflowState();
        }

        private void StartTransition(TransitionNode transitionNodeToStart)
        {
            StartTransition(transitionNodeToStart, WorkflowData);
        }

        private void EndTransition(object transitionResult, TransitionNode transitionNodeToEnd)
        {
            lock (_workflowLock)
            {
                transitionNodeToEnd.PostTransitionAction(WorkflowData, transitionResult);
                transitionNodeToEnd.State = TransitionState.Completed;
                _logger.Log($"Producing output tokens for transition: {transitionNodeToEnd.Name}");
                foreach (var outputPlaceIndes in transitionNodeToEnd.OutputPlaceIndicies)
                {
                    PlaceNodes[outputPlaceIndes].HasToken = true;
                }

                PersistWorkflowState();
            }
            UpdateWorkflowState();
        }

    }
}
