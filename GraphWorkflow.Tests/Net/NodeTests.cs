using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using GraphWorkflow.Net;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;

namespace GraphWorkflow.Tests.Net
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

            var workflow = new PetriNet(3, wfObj => isWorkflowCompleted = true, 
                new Marking
                (
                    new List<Place> { new Place(PlaceState.HasToken), new Place(), new Place(), new Place() },
                    new List<Transition>
                    {
                        new Transition("FirstAction", wfObj => { concurrentQueue.Enqueue(0); return null; }, (wfObj, resObj) => { ((SimpleDualStepWorkflowData)wfObj).WasFirstActioned = true; }, wfObj => true, new List<int> {0}, new List<int> {1}),
                        new Transition("NoOp", wfObj => {  return null; }, (wfObj, resObj) => { }, wfObj => true, new List<int> {1}, new List<int> {2}, TransitionStartType.NoOp),
                        new Transition("SecondAction", wfObj => { concurrentQueue.Enqueue(1); return null; }, (wfObj, resObj) => { ((SimpleDualStepWorkflowData)wfObj).WasSecondActioned = true; }, wfObj => true, new List<int> {2}, new List<int> {3}),
                    }
                ),
                logger);

            string caseId = workflow.StartWorkflow(wfData);

            while (!isWorkflowCompleted)
            {
                Thread.Sleep(100);
            }

            Assert.IsTrue(wfData.WasFirstActioned, $"FirstAction should have set {nameof(wfData.WasFirstActioned)} to true");
            Assert.IsTrue(wfData.WasSecondActioned, $"SecondAction should have set {nameof(wfData.WasSecondActioned)} to true");
            Assert.AreEqual(2, concurrentQueue.Count, "2 transitions should have been executed");
            Assert.Contains(0, concurrentQueue, "FirstAction should have executed");
            Assert.Contains(1, concurrentQueue, "SecondAction should have executed");
            Assert.AreEqual(1, concurrentQueue.Last(), "SecondAction should be executed last");
        }


        [Test]
        public void SimpleOrTest()
        {
            bool isWorkflowCompleted = false;
            var wfData = new SimpleDualStepWorkflowData { WasApproved = false, WasFirstActioned = false, WasSecondActioned = false };
            var logger = new ConsoleLogger();
            var concurrentQueue = new ConcurrentQueue<int>();

            var workflow = new PetriNet(4, wfObj => isWorkflowCompleted = true, 
                new Marking
                (
                    new List<Place> { new Place(PlaceState.HasToken), new Place(), new Place(), new Place(), new Place() },
                    new List<Transition>
                    { 
                        new Transition("SendApprovalRequest", wfObj => { concurrentQueue.Enqueue(0); return null; }, (wfObj, resObj) => { }, wfObj => true, new List<int> {0}, new List<int> {1}),
                        new Transition("ApproveReject", wfObj => { concurrentQueue.Enqueue(1); return wfObj; }, (wfObj, resObj) => ((SimpleDualStepWorkflowData)wfObj).WasApproved = ((bool)resObj), wfObj => true, new List<int> {1}, new List<int> {2}, TransitionStartType.Wait),
                        new Transition("SendRequestApproval", wfObj => { concurrentQueue.Enqueue(2); return null; }, (wfObj, resObj) => { ((SimpleDualStepWorkflowData)wfObj).WasFirstActioned = true; }, wfObj => ((SimpleDualStepWorkflowData)wfObj).WasApproved, new List<int> {2}, new List<int> {3}),
                        new Transition("SendRequestRejection", wfObj => { concurrentQueue.Enqueue(3); return null; }, (wfObj, resObj) => { ((SimpleDualStepWorkflowData)wfObj).WasSecondActioned = true; }, wfObj => !((SimpleDualStepWorkflowData)wfObj).WasApproved, new List<int> {2}, new List<int> {3}),
                        new Transition("PersistToDb", wfObj => { concurrentQueue.Enqueue(4); return null; }, (wfObj, resObj) => { }, wfObj => true, new List<int>{3}, new List<int>{4})
                    }
                ),
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

            Assert.IsTrue(wfData.WasApproved, $"ApproveReject should have set {nameof(wfData.WasApproved)} to true");
            Assert.IsTrue(wfData.WasFirstActioned, $"SendRequestApproval should have set {nameof(wfData.WasFirstActioned)} to true");
            Assert.IsFalse(wfData.WasSecondActioned, $"SendRequestRejection should not have run so {nameof(wfData.WasSecondActioned)} should be false");
            Assert.AreEqual(4, concurrentQueue.Count, "4 transitions should have executed");
            Assert.Contains(0, concurrentQueue, "SendApprovalRequest should have executed");
            Assert.Contains(1, concurrentQueue, "ApproveReject should have executed");
            Assert.Contains(2, concurrentQueue, "SendRequestApproval should have executed");
            Assert.Contains(4, concurrentQueue, "PersistToDb should have executed");
            Assert.That(concurrentQueue, Has.No.Member(3), "SendRequestRejection should not have executed");
            Assert.AreEqual(4, concurrentQueue.Last(), "PersistToDb should have executed last");
        }




        [Test]
        public void SimpleAndTest()
        {
            bool isWorkflowCompleted = false;
            var wfData = new SimpleDualStepWorkflowData { WasFirstActioned = false, WasSecondActioned = false};
            var logger = new ConsoleLogger();
            var concurrentQueue = new ConcurrentQueue<int>();
            var workflow = new PetriNet(5, wfObj => isWorkflowCompleted = true, 
                new Marking
                (
                    new List<Place> { new Place(PlaceState.HasToken), new Place(), new Place(), new Place(), new Place(), new Place() },
                    new List<Transition>
                    {
                        new Transition("StartStep", wfObj => { concurrentQueue.Enqueue(0); return null; }, (wfObj, resObj) => { }, wfObj => true, new List<int> {0}, new List<int> {1, 2}),
                        new Transition("FirstParallelAction", wfObj => { concurrentQueue.Enqueue(1); Thread.Sleep(200); return null; }, (wfObj, resObj) => ((SimpleDualStepWorkflowData)wfObj).WasFirstActioned = true, wfObj => true, new List<int> {1}, new List<int> {3}),
                        new Transition("SecondParallelAction", wfObj => { concurrentQueue.Enqueue(2); return null; }, (wfObj, resObj) => ((SimpleDualStepWorkflowData)wfObj).WasSecondActioned = true, wfObj => true, new List<int> {2}, new List<int> {4}),
                        new Transition("EndStep", wfObj => { concurrentQueue.Enqueue(3); return null; }, (wfObj, resObj) => { }, wfObj => true, new List<int>{3, 4}, new List<int>{5})
                    }
                ),
                logger);

            string caseId = workflow.StartWorkflow(wfData);

            while (!isWorkflowCompleted)
            {
                Thread.Sleep(100);
            }

            Assert.IsTrue(wfData.WasFirstActioned, $"FirstParallelAction should have set {nameof(wfData.WasFirstActioned)} to true");
            Assert.IsTrue(wfData.WasSecondActioned, $"SecondParallelAction should have set {nameof(wfData.WasSecondActioned)} to true");
            Assert.AreEqual(4, concurrentQueue.Count, "4 transitions should have executed");
            Assert.Contains(0, concurrentQueue, "StartStep should have executed");
            Assert.Contains(1, concurrentQueue, "FirstParallelAction should have executed");
            Assert.Contains(2, concurrentQueue, "SecondParallelAction should have executed");
            Assert.Contains(3, concurrentQueue, "EndStep should have executed");
            Assert.AreEqual(3, concurrentQueue.Last(), "EndStep should have executed last");
        }
    }
}
