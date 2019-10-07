using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using GraphWorkflow.Core;
using NUnit.Framework;

namespace GraphWorkflow.Tests.Core
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

            WorkflowDefinition wfDef = WorkflowDefinition
                .Start(new TransitionDefinition("FirstStep", wfObj => { concurrentQueue.Enqueue(0); return null; }, (wfObj, resObj) => { ((SimpleDualStepWorkflowData)wfObj).WasFirstActioned = true; }), wfObj => isWorkflowCompleted = true)
                .Then(new TransitionDefinition("SecondStep", wfObj => { concurrentQueue.Enqueue(1); return null; }, (wfObj, resObj) => { ((SimpleDualStepWorkflowData)wfObj).WasSecondActioned = true; }));

            var workflow = new Workflow(wfDef, logger);
            workflow.StartWorkflow(wfData);

            while(!isWorkflowCompleted)
            {
                Thread.Sleep(100);
            }

            Assert.AreEqual(2, concurrentQueue.Count);
            Assert.IsTrue(wfData.WasFirstActioned);
            Assert.IsTrue(wfData.WasSecondActioned);
        }

        [Test]
        public void SimpleOrTest()
        {
            bool isWorkflowCompleted = false;
            var wfData = new SimpleDualStepWorkflowData { WasFirstActioned = false, WasSecondActioned = false };
            var logger = new ConsoleLogger();
            var concurrentQueue = new ConcurrentQueue<int>();

            WorkflowDefinition wfDef = WorkflowDefinition
                .Start(new TransitionDefinition("StartStep", wfObj => { concurrentQueue.Enqueue(0); return null; }, (wfObj, resObj) => { }), wfObj => isWorkflowCompleted = true)
                //.ThenOneOf(
                //    new ConditionalTransitionDefinition(wfObj => true, new TransitionDefinition("FirstStep", wfObj => { concurrentQueue.Enqueue(1); return null; }, (wfObj, resObj) => { ((SimpleDualStepWorkflowData)wfObj).WasFirstActioned = true; })),
                //    new ConditionalTransitionDefinition(wfObj => false, new TransitionDefinition("SecondStep", wfObj => { concurrentQueue.Enqueue(2); return null; }, (wfObj, resObj) => { ((SimpleDualStepWorkflowData)wfObj).WasSecondActioned = true; }))
                //)
                .Then(new TransitionDefinition("EndStep", wfObj => { concurrentQueue.Enqueue(3); return null; }, (wfObj, resObj) => { }));

            var workflow = new Workflow(wfDef, logger);
            workflow.StartWorkflow(wfData);
        }

        [Test]
        public void SimpleAndTest()
        {
            bool isWorkflowCompleted = false;
            var wfData = new SimpleDualStepWorkflowData { WasFirstActioned = false, WasSecondActioned = false };
            var logger = new ConsoleLogger();
            var concurrentQueue = new ConcurrentQueue<int>();

            WorkflowDefinition wfDef = WorkflowDefinition
                .Start(new TransitionDefinition("StartStep", wfObj => { concurrentQueue.Enqueue(0); return null; }, (wfObj, resObj) => { }), wfObj => isWorkflowCompleted = true)
                .ThenAllOf(
                    new TransitionDefinition("FirstParallelAction", wfObj => { concurrentQueue.Enqueue(1); return null; }, (wfObj, resObj) => { ((SimpleDualStepWorkflowData)wfObj).WasFirstActioned = true; }),
                    new TransitionDefinition("SecondParallelAction", wfObj => { concurrentQueue.Enqueue(2); return null; }, (wfObj, resObj) => { ((SimpleDualStepWorkflowData)wfObj).WasSecondActioned = true; })
                )
                .Then(new TransitionDefinition("EndStep", wfObj => { concurrentQueue.Enqueue(3); return null; }, (wfObj, resObj) => { }));
            var workflow = new Workflow(wfDef, logger);
            workflow.StartWorkflow(wfData);
        }
    }
}
