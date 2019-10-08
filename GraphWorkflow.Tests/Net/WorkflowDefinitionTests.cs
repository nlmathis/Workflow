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
            var wfData = new SimpleDualStepWorkflowData { WasApproved = true, WasFirstActioned = false, WasSecondActioned = false };
            var logger = new ConsoleLogger();
            var concurrentQueue = new ConcurrentQueue<int>();

            var approvedStep = new StepDefinition<SimpleDualStepWorkflowData, SimpleDualStepWorkflowData, SimpleDualStepWorkflowData>
                ("ApprovedStep", input => { concurrentQueue.Enqueue(0); return null; })
                .AfterExecution((wfObj, result) => { wfObj.WasFirstActioned = true; });

            var rejectedStep = new StepDefinition<SimpleDualStepWorkflowData, SimpleDualStepWorkflowData, SimpleDualStepWorkflowData>
                ("RejectedStep", input => { concurrentQueue.Enqueue(1); return null; })
                .AfterExecution((wfObj, result) => { wfObj.WasSecondActioned = true; });

            var persistDbStep = new StepDefinition<SimpleDualStepWorkflowData, SimpleDualStepWorkflowData, SimpleDualStepWorkflowData>
                ("PersitToDb", input => { concurrentQueue.Enqueue(2); return null; });


            PetriNetDefinition wfDef = WorkflowDefinition<SimpleDualStepWorkflowData>
                .Start()
                .ThenOneOf(
                    new ConditionalStepDefinition<SimpleDualStepWorkflowData>(wfData => wfData.WasApproved,
                        approvedCtx => approvedCtx
                        .Then(approvedStep)
                    ),
                    new ConditionalStepDefinition<SimpleDualStepWorkflowData>(wfData => !wfData.WasApproved,
                        rejectedCtx => rejectedCtx
                        .Then(rejectedStep)
                    )
                )
                .Then(persistDbStep);

            wfDef.OnWorkflowCompletion = wfObj => isWorkflowCompleted = true;

            var workflow = new PetriNet(wfDef, logger);
            workflow.StartWorkflow(wfData);

            while (!isWorkflowCompleted)
            {
                Thread.Sleep(100);
            }

            Assert.AreEqual(2, concurrentQueue.Count, "2 transitions should be executed");
            Assert.IsTrue(wfData.WasFirstActioned);
            Assert.IsFalse(wfData.WasSecondActioned);
        }

        [Test]
        public void SimpleAndTest()
        {
            bool isWorkflowCompleted = false;
            var wfData = new SimpleDualStepWorkflowData { WasFirstActioned = false, WasSecondActioned = false };
            var logger = new ConsoleLogger();
            var concurrentQueue = new ConcurrentQueue<int>();

            var firstStepDefinition = new StepDefinition<SimpleDualStepWorkflowData, SimpleDualStepWorkflowData, SimpleDualStepWorkflowData>
                ("FirstAction", input => { concurrentQueue.Enqueue(0); return null; })
                .AfterExecution((wfData, result) => { wfData.WasFirstActioned = true; });

            var secondStepDefinition = new StepDefinition<SimpleDualStepWorkflowData, SimpleDualStepWorkflowData, SimpleDualStepWorkflowData>
                ("SecondAction", input => { concurrentQueue.Enqueue(1); return null; })
                .AfterExecution((wfData, result) => { wfData.WasSecondActioned = true; });

            var persistDbStep = new StepDefinition<SimpleDualStepWorkflowData, SimpleDualStepWorkflowData, SimpleDualStepWorkflowData>
                ("PersitToDb", input => { concurrentQueue.Enqueue(2); return null; });

            PetriNetDefinition wfDef = WorkflowDefinition<SimpleDualStepWorkflowData>
                .Start()
                .ThenAllOf(
                    firstCtx => firstCtx
                    .Then(firstStepDefinition),
                    secondCtx => secondCtx
                    .Then(secondStepDefinition)
                )
                .Then(persistDbStep);


            wfDef.OnWorkflowCompletion = wfObj => isWorkflowCompleted = true;

            var workflow = new PetriNet(wfDef, logger);
            workflow.StartWorkflow(wfData);

            while (!isWorkflowCompleted)
            {
                Thread.Sleep(100);
            }

            Assert.AreEqual(3, concurrentQueue.Count);
            Assert.IsTrue(wfData.WasFirstActioned);
            Assert.IsTrue(wfData.WasSecondActioned);
        }
    }
}
