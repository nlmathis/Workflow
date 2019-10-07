using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using GraphWorkflow.Core;
using GraphWorkflow.Net;
using NUnit.Framework;

namespace GraphWorkflow.Tests.Net
{
    [TestFixture]
    public class WorkflowDefinitionTests
    {
        [Test]
        public void SimpleSequenceTest()
        {
            bool isWorkflowCompleted = false;
            var wfData = new SimpleDualStepWorkflowData { WasFirstActioned = false, WasSecondActioned = false };
            var logger = new ConsoleLogger();
            var concurrentQueue = new ConcurrentQueue<int>();

            var firstStep = new StepDefinition<SimpleDualStepWorkflowData, SimpleDualStepWorkflowData, SimpleDualStepWorkflowData>
                ("FirstStep", input => { concurrentQueue.Enqueue(0); return null; })
                .AfterExecution((wfObj, result) => { wfObj.WasFirstActioned = true; });


            var secondStep = new StepDefinition<SimpleDualStepWorkflowData, SimpleDualStepWorkflowData, SimpleDualStepWorkflowData>
                ("SecondStep", input => { concurrentQueue.Enqueue(1); return null; })
                .AfterExecution((wfData, result) => { wfData.WasSecondActioned = true; });

            PetriNetDefinition wfDef = WorkflowDefinition<SimpleDualStepWorkflowData>
                .Start(firstStep)
                .Then(secondStep);

            wfDef.OnWorkflowCompletion = wfObj => isWorkflowCompleted = true;

            var workflow = new PetriNet(wfDef, logger);
            workflow.StartWorkflow(wfData);

            while(!isWorkflowCompleted)
            {
                Thread.Sleep(100);
            }

            Assert.AreEqual(2, concurrentQueue.Count, "2 transitions should be executed");
            Assert.IsTrue(wfData.WasFirstActioned, $"FirstStep should have set {nameof(wfData.WasFirstActioned)}");
            Assert.IsTrue(wfData.WasSecondActioned, $"SecondStep should have set {nameof(wfData.WasSecondActioned)}");
            Assert.AreEqual(1, concurrentQueue.Last());
        }

        [Test]
        public void SimpleOrTest()
        {
            bool isWorkflowCompleted = false;
            var wfData = new SimpleDualStepWorkflowData { WasFirstActioned = false, WasSecondActioned = false };
            var logger = new ConsoleLogger();
            var concurrentQueue = new ConcurrentQueue<int>();

//            var workflow = new PetriNet(wfDef, logger);
            //workflow.StartWorkflow(wfData);

            Assert.AreEqual(3, concurrentQueue.Count, "3 transitions should be executed");
        }

        //[Test]
        public void SimpleAndTest()
        {
            bool isWorkflowCompleted = false;
            var wfData = new SimpleDualStepWorkflowData { WasFirstActioned = false, WasSecondActioned = false };
            var logger = new ConsoleLogger();
            var concurrentQueue = new ConcurrentQueue<int>();

            //PetriNetDefinition wfDef = PetriNetDefinition
            //    .Start(new TransitionDefinition("StartStep", wfObj => { concurrentQueue.Enqueue(0); return null; }, (wfObj, resObj) => { }), wfObj => isWorkflowCompleted = true)
            //    .ThenAllOf(
            //        new TransitionDefinition("FirstParallelAction", wfObj => { concurrentQueue.Enqueue(1); return null; }, (wfObj, resObj) => { ((SimpleDualStepWorkflowData)wfObj).WasFirstActioned = true; }),
            //        new TransitionDefinition("SecondParallelAction", wfObj => { concurrentQueue.Enqueue(2); return null; }, (wfObj, resObj) => { ((SimpleDualStepWorkflowData)wfObj).WasSecondActioned = true; })
            //    )
            //    .Then(new TransitionDefinition("EndStep", wfObj => { concurrentQueue.Enqueue(3); return null; }, (wfObj, resObj) => { }));
            //var workflow = new PetriNet(wfDef, logger);
            //workflow.StartWorkflow(wfData);
        }
    }
}
