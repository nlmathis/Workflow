﻿using System;
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
        public object WorkflowData;
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
            WorkflowData = inputData;
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
                    foreach (var transition in CurrentMarking.GetEnabledTransitions().Where(tr => tr.TransitionTrigger(WorkflowData)))
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
            OnWorkflowCompletion(WorkflowData);
        }
        private void PersistMarking()
        {
            _logger.Log("Start persisting marking");
            var placeDiffs = CurrentMarking.GetPlaceDiffs(PersistedMarking);
            var transitionDiffs = CurrentMarking.GetTransitionDiffs(PersistedMarking);
            string persistDetails = string.Join("\n", placeDiffs.Select(diff => $" -- {diff.ToString()}")
                .Concat(transitionDiffs.Select(diff => $" -- {diff.ToString()}")));
            _logger.Log(persistDetails);
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

            PersistMarking();
        }

        private void StartTransition(Transition transitionNodeToStart)
        {
            StartTransition(transitionNodeToStart, WorkflowData);
        }

        private void EndTransition(object transitionResult, Transition transitionNodeToEnd)
        {
            lock (_workflowLock)
            {
                transitionNodeToEnd.PostTransitionAction(WorkflowData, transitionResult);
                transitionNodeToEnd.State = TransitionState.Completed;
                _logger.Log($"Producing output tokens for transition: {transitionNodeToEnd.Name}");
                foreach (var outputPlaceIndes in transitionNodeToEnd.OutputPlaceIndicies)
                {
                    CurrentMarking.Places[outputPlaceIndes].State = PlaceState.HasToken;
                }

                PersistMarking();
            }
            GenerateNextMarking();
        }

    }
}