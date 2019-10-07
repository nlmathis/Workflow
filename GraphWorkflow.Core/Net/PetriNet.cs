using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GraphWorkflow.Net
{
    public class PetriNet
    {
        Marking PersistedMarking;
        Marking CurrentMarking;
        public int EndPlaceIndex;
        public object PreviousWorkflowData;
        public object CurrentWorkflowData;
        public Action<object> OnWorkflowCompletion;
        private object _workflowLock = new object();
        private ILogger _logger;

        public PetriNet(int endPlaceIndex, Action<object> onWorkflowCompletion, Marking initialMarking, ILogger logger)
        {
            EndPlaceIndex = endPlaceIndex;
            OnWorkflowCompletion = onWorkflowCompletion;
            CurrentMarking = initialMarking;
            PersistedMarking = CurrentMarking.Clone();
            _logger = logger;
        }

        public PetriNet(PetriNetDefinition workflowDefinition, ILogger logger)
        {
            EndPlaceIndex = workflowDefinition.EndPlaceIndex;
            OnWorkflowCompletion = workflowDefinition.OnWorkflowCompletion;
            CurrentMarking = workflowDefinition.InitialMarking;
            PersistedMarking = CurrentMarking.Clone();
            _logger = logger;
        }

        public string StartWorkflow(object inputData)
        {
            if(inputData == null)
            {
                throw new ArgumentException("Can't start a workflow with null input data");
            }
            CurrentWorkflowData = inputData;
            string copyInput = JsonConvert.SerializeObject(CurrentWorkflowData);
            PreviousWorkflowData = JsonConvert.DeserializeObject(copyInput, CurrentWorkflowData.GetType());
            _logger.Log($"Starting workflow");
            GenerateNextMarking();

            return Guid.NewGuid().ToString();
        }

        private void GenerateNextMarking()
        {
            lock(_workflowLock)
            {
                if (CurrentMarking.Places[EndPlaceIndex].HasToken)
                {
                    End();
                }
                else
                {
                    foreach (var transition in CurrentMarking.GetEnabledTransitions().Where(tr => tr.TransitionTrigger(CurrentWorkflowData)))
                    {
                        StartTransition(transition);
                    }
                }
            }
        }

        public IEnumerable<int> GetWaitingTransitionIds()
        {
            lock(_workflowLock)
            {
                return CurrentMarking.Transitions.Select((tn, id) => new { tn.State, id })
                    .Where(tn => tn.State == TransitionState.Waiting)
                    .Select(tn => tn.id);
            }
        }

        public void ExecuteTransition(int transitionId, object inputData)
        {
            lock (_workflowLock)
            {
                var nodeToExecute = CurrentMarking.Transitions[transitionId];
                _logger.Log($"External initiation of transition {nodeToExecute.Name}");
                StartTransition(nodeToExecute, inputData);
            }
        }

        private void End()
        {
            _logger.Log("Ending workflow");
            OnWorkflowCompletion(CurrentWorkflowData);
        }
        private void PersistMarking()
        {
            _logger.Log("Start persisting marking");
            var placeDiffs = CurrentMarking.GetPlaceDiffs(PersistedMarking);
            var transitionDiffs = CurrentMarking.GetTransitionDiffs(PersistedMarking);
            string persistDetails = string.Join("\n", placeDiffs.Select(diff => $" -- {diff.ToString()} skip persistence: {diff.PreviousState == PlaceState.Unused && diff.NewState == PlaceState.Empty}")
                .Concat(transitionDiffs.Select(diff => $" -- {diff.ToString()}, skip persistence: {CurrentMarking.Transitions[diff.TransitionIndex].StartType == TransitionStartType.NoOp}")));
            _logger.Log(persistDetails);

            string previousWorkflowDataString = JsonConvert.SerializeObject(PreviousWorkflowData);
            string currentWorkflowDataString = JsonConvert.SerializeObject(CurrentWorkflowData);
            if(previousWorkflowDataString != currentWorkflowDataString)
            {
                _logger.Log($" -- Workflow data needs to update from : {previousWorkflowDataString} to {currentWorkflowDataString}");
                PreviousWorkflowData = JsonConvert.DeserializeObject(currentWorkflowDataString, CurrentWorkflowData.GetType());
            }

            _logger.Log("End persisting marking");
            PersistedMarking = CurrentMarking.Clone();
            //Consider including IPersister here
        }

        private void StartTransition(Transition transitionNodeToStart, object inputData)
        {
            _logger.Log($"Consuming input tokens for: transition {transitionNodeToStart.Name}");
            foreach (var inputPlaceIndex in transitionNodeToStart.InputPlaceIndicies)
            {
                CurrentMarking.Places[inputPlaceIndex].State = PlaceState.Empty;
            }

            if (transitionNodeToStart.StartType == TransitionStartType.Wait && transitionNodeToStart.State != TransitionState.Waiting)
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

            //As an optimization only persist marking if the transition isn't a NoOp
            if(transitionNodeToStart.StartType == TransitionStartType.NoOp)
            {
                _logger.Log($"Skip persisting marking after start of {transitionNodeToStart.StartType} transition");
            }
            else
            {
                PersistMarking();
            }
        }

        private void StartTransition(Transition transitionNodeToStart)
        {
            StartTransition(transitionNodeToStart, CurrentWorkflowData);
        }

        private void EndTransition(object transitionResult, Transition transitionNodeToEnd)
        {
            lock (_workflowLock)
            {
                transitionNodeToEnd.PostTransitionAction(CurrentWorkflowData, transitionResult);
                transitionNodeToEnd.State = TransitionState.Completed;
                _logger.Log($"Producing output tokens for transition: {transitionNodeToEnd.Name}");
                foreach (var outputPlaceIndes in transitionNodeToEnd.OutputPlaceIndicies)
                {
                    CurrentMarking.Places[outputPlaceIndes].State = PlaceState.HasToken;
                }

                if(transitionNodeToEnd.StartType == TransitionStartType.NoOp)
                {
                    _logger.Log($"Skip persisting marking after completion of {transitionNodeToEnd.StartType} transition");
                }
                else
                {
                    PersistMarking();
                }
                
            }
            GenerateNextMarking();
        }

    }
}
