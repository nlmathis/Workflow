using System;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GraphWorkflow.Core;

namespace GraphWorkflow.Console
{
    public class SimpleWorkflowData
    {
        public bool WasApproved = false;
    }

    public class ConsoleLogger : ILogger
    {
        public void Log(string logMessage)
        {
            System.Console.WriteLine(logMessage);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            bool isWorkflowCompleted = false;
            var wfData = new SimpleWorkflowData { WasApproved = false };
            var logger = new ConsoleLogger();
            var workflow = new Workflow(wfData, 4, wfObj => isWorkflowCompleted = true,
                new List<PlaceNode> { new PlaceNode(true), new PlaceNode(false), new PlaceNode(false), new PlaceNode(false), new PlaceNode(false) },
                new List<TransitionNode>
                {
                    new TransitionNode("SendApprovalRequest", wfObj => { System.Console.WriteLine("Send approval request"); return null; }, (wfObj, resObj) => { }, wfObj => true, new List<int> {0}, new List<int> {1}),
                    new TransitionNode("ApproveReject", wfObj => { System.Console.WriteLine("Approve Reject"); return wfObj; }, (wfObj, resObj) => ((SimpleWorkflowData)wfObj).WasApproved = ((bool)resObj), wfObj => true, new List<int> {1}, new List<int> {2 }, true),
                    new TransitionNode("SendRequestApproval", wfObj => { System.Console.WriteLine("Request Approved"); return null; }, (wfObj, resObj) => { }, wfObj => ((SimpleWorkflowData)wfObj).WasApproved, new List<int> {2}, new List<int> {3}),
                    new TransitionNode("SendRequestRejection", wfObj => { System.Console.WriteLine("Request Rejected"); return null; }, (wfObj, resObj) => { }, wfObj => !((SimpleWorkflowData)wfObj).WasApproved, new List<int> {2}, new List<int> {3}),
                    new TransitionNode("PersistToDb", wfObj => { System.Console.WriteLine("Persisting to DB"); return null; }, (wfObj, resObj) => { }, wfObj => true, new List<int>{3}, new List<int>{4})
                },
                logger);

            string caseId = workflow.StartWorkflow();
            Thread.Sleep(5000);
            while (!isWorkflowCompleted)
            {
                var waitingTransitionId = workflow.GetWaitingTransitionIds();
                if (waitingTransitionId.Any())
                {
                    workflow.ExecuteTransition(waitingTransitionId.First(), true);
                }
                Thread.Sleep(200);
            }
        }
    }
}
