using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using GraphWorkflow.Core;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;

namespace GraphWorkflow.Tests.Core
{
    [TestFixture]
    public class NodeTests
    {

        [Test]
        public void SimpleSequentialTest()
        {
            bool isWorkflowCompleted = false;
            var wfData = new SimpleDualStepWorkflowData { WasFirstActioned = false, WasSecondActioned = false };
            var logger = new ConsoleLogger();
            var concurrentQueue = new ConcurrentQueue<int>();

            var workflow = new Workflow(2, wfObj => isWorkflowCompleted = true,
                new List<PlaceNode> { new PlaceNode(true), new PlaceNode(false), new PlaceNode(false) },
                new List<TransitionNode>
                {
                    new TransitionNode("FirstAction", wfObj => { concurrentQueue.Enqueue(0); return null; }, (wfObj, resObj) => { ((SimpleDualStepWorkflowData)wfObj).WasFirstActioned = true; }, wfObj => true, new List<int> {0}, new List<int> {1}),
                    new TransitionNode("SecondAction", wfObj => { concurrentQueue.Enqueue(1); return null; }, (wfObj, resObj) => { ((SimpleDualStepWorkflowData)wfObj).WasSecondActioned = true; }, wfObj => true, new List<int> {1}, new List<int> {2}),
                },
                logger);

            string caseId = workflow.StartWorkflow(wfData);

            while (!isWorkflowCompleted)
            {
                Thread.Sleep(100);
            }

            Assert.That(wfData.WasFirstActioned, Is.True);
            Assert.That(wfData.WasSecondActioned, Is.True);
            Assert.AreEqual(2, concurrentQueue.Count);
            Assert.Contains(0, concurrentQueue);
            Assert.Contains(1, concurrentQueue);
            Assert.That(concurrentQueue.Last(), Is.EqualTo(1));
        }


        [Test]
        public void SimpleOrTest()
        {
            bool isWorkflowCompleted = false;
            var wfData = new SimpleDualStepWorkflowData { WasApproved = false, WasFirstActioned = false, WasSecondActioned = false };
            var logger = new ConsoleLogger();
            var concurrentQueue = new ConcurrentQueue<int>();

            var workflow = new Workflow(4, wfObj => isWorkflowCompleted = true,
                new List<PlaceNode> { new PlaceNode(true), new PlaceNode(false), new PlaceNode(false), new PlaceNode(false), new PlaceNode(false) },
                new List<TransitionNode>
                { 
                    new TransitionNode("SendApprovalRequest", wfObj => { concurrentQueue.Enqueue(0); return null; }, (wfObj, resObj) => { }, wfObj => true, new List<int> {0}, new List<int> {1}),
                    new TransitionNode("ApproveReject", wfObj => { concurrentQueue.Enqueue(1); return wfObj; }, (wfObj, resObj) => ((SimpleDualStepWorkflowData)wfObj).WasApproved = ((bool)resObj), wfObj => true, new List<int> {1}, new List<int> {2}, true),
                    new TransitionNode("SendRequestApproval", wfObj => { concurrentQueue.Enqueue(2); return null; }, (wfObj, resObj) => { ((SimpleDualStepWorkflowData)wfObj).WasFirstActioned = true; }, wfObj => ((SimpleDualStepWorkflowData)wfObj).WasApproved, new List<int> {2}, new List<int> {3}),
                    new TransitionNode("SendRequestRejection", wfObj => { concurrentQueue.Enqueue(3); return null; }, (wfObj, resObj) => { ((SimpleDualStepWorkflowData)wfObj).WasSecondActioned = true; }, wfObj => !((SimpleDualStepWorkflowData)wfObj).WasApproved, new List<int> {2}, new List<int> {3}),
                    new TransitionNode("PersistToDb", wfObj => { concurrentQueue.Enqueue(4); return null; }, (wfObj, resObj) => { }, wfObj => true, new List<int>{3}, new List<int>{4})
                },
                logger);

            string caseId = workflow.StartWorkflow(wfData);

            while(!isWorkflowCompleted)
            {
                var waitingTransitionId = workflow.GetWaitingTransitionIds();
                if(waitingTransitionId.Any())
                {
                    workflow.ExecuteTransition(waitingTransitionId.First(), true);
                }
                Thread.Sleep(100);
            }

            Assert.That(wfData.WasApproved, Is.True);
            Assert.That(wfData.WasFirstActioned, Is.True);
            Assert.That(wfData.WasSecondActioned, Is.False);
            Assert.AreEqual(4, concurrentQueue.Count);
            Assert.Contains(0, concurrentQueue);
            Assert.Contains(1, concurrentQueue);
            Assert.Contains(2, concurrentQueue);
            Assert.Contains(4, concurrentQueue);
            Assert.That(concurrentQueue, Has.No.Member(3));
            Assert.That(concurrentQueue.Last(), Is.EqualTo(4));
        }




        [Test]
        public void SimpleAndTest()
        {
            bool isWorkflowCompleted = false;
            var wfData = new SimpleDualStepWorkflowData { WasFirstActioned = false, WasSecondActioned = false};
            var logger = new ConsoleLogger();
            var concurrentQueue = new ConcurrentQueue<int>();
            var workflow = new Workflow(5, wfObj => isWorkflowCompleted = true,
                new List<PlaceNode> { new PlaceNode(true), new PlaceNode(false), new PlaceNode(false), new PlaceNode(false), new PlaceNode(false), new PlaceNode(false) },
                new List<TransitionNode>
                {
                    new TransitionNode("StartStep", wfObj => { concurrentQueue.Enqueue(0); return null; }, (wfObj, resObj) => { }, wfObj => true, new List<int> {0}, new List<int> {1, 2}),
                    new TransitionNode("FirstParallelAction", wfObj => { concurrentQueue.Enqueue(1); Thread.Sleep(200); return null; }, (wfObj, resObj) => ((SimpleDualStepWorkflowData)wfObj).WasFirstActioned = true, wfObj => true, new List<int> {1}, new List<int> {3}),
                    new TransitionNode("SecondParallelAction", wfObj => { concurrentQueue.Enqueue(2); return null; }, (wfObj, resObj) => ((SimpleDualStepWorkflowData)wfObj).WasSecondActioned = true, wfObj => true, new List<int> {2}, new List<int> {4}),
                    new TransitionNode("EndStep", wfObj => { concurrentQueue.Enqueue(3); return null; }, (wfObj, resObj) => { }, wfObj => true, new List<int>{3, 4}, new List<int>{5})
                },
                logger);

            string caseId = workflow.StartWorkflow(wfData);

            while (!isWorkflowCompleted)
            {
                Thread.Sleep(100);
            }

            Assert.That(wfData.WasFirstActioned, Is.True);
            Assert.That(wfData.WasSecondActioned, Is.True);
            Assert.AreEqual(4, concurrentQueue.Count);
            Assert.Contains(0, concurrentQueue);
            Assert.Contains(1, concurrentQueue);
            Assert.Contains(2, concurrentQueue);
            Assert.Contains(3, concurrentQueue);
            Assert.That(concurrentQueue.Last(), Is.EqualTo(3));
        }
    }
}
