using Microsoft.VisualStudio.TestTools.UnitTesting;
using MOE.Common.Business.PEDDelay;
using MOE.Common.Models.Repositories;
using MOE.Common.Models;
using System.Linq;
using System;

namespace MOE.CommonTests.Business.PEDDelay
{
    [TestClass]
    public class PEDDelayTest
    {
        private Signal Signal { get; set; }
        private DateTime StartTime { get; set; }
        private DateTime EndTime { get; set; }
        private PedDelaySignal PedDelaySignal { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            var signalRepository = SignalsRepositoryFactory.Create();
            StartTime = DateTime.Parse("10/26/2021 12:00 AM");
            EndTime = StartTime.AddDays(1);
            Signal = signalRepository.GetVersionOfSignalByDate("7115", StartTime);
            PedDelaySignal = new PedDelaySignal(Signal, 15, StartTime, EndTime);
        }

        [TestMethod]
        public void PedPlan_PedRecallWhenEventsCountIsNull_ReturnsFalse()
        {
            var pedPlan = new PedPlan(0, StartTime, EndTime, 0);
            Assert.IsFalse(pedPlan.PedRecallOn);
        }

        [TestMethod]
        public void PedPlan_PedCallsRegisteredCount_Equals2()
        {
            var pedPhase = PedDelaySignal.PedPhases.Where(p => p.PhaseNumber == 2).FirstOrDefault();
            var plan = pedPhase.Plans.FirstOrDefault();

            Assert.AreEqual(2, plan.PedCallsRegisteredCount);
        }

        [TestMethod]
        public void PedPlan_PedCallsBeginWalkCount_Equals2()
        {
            var pedPhase = PedDelaySignal.PedPhases.Where(p => p.PhaseNumber == 2).FirstOrDefault();
            var plan = pedPhase.Plans.FirstOrDefault();

            Assert.AreEqual(2, plan.PedBeginWalkCount);
        }

        [TestMethod]
        public void PedPlan_PedRecall_EqualsFalse()
        {
            var pedPhase = PedDelaySignal.PedPhases.Where(p => p.PhaseNumber == 2).FirstOrDefault();
            var plan = pedPhase.Plans.FirstOrDefault();

            Assert.IsFalse(plan.PedRecallOn);
        }

        [TestMethod]
        public void PedPlan_PlanType_EqualsFree()
        {
            var pedPhase = PedDelaySignal.PedPhases.Where(p => p.PhaseNumber == 2).FirstOrDefault();
            var plan = pedPhase.Plans.FirstOrDefault();

            Assert.AreEqual(254, plan.PlanNumber);
        }

        [TestMethod]
        public void PedDelayChart_PedPhaseAndPedPlan_CyclesWithPedDelayMatch()
        {
            var pedPhase = PedDelaySignal.PedPhases.Where(p => p.PhaseNumber == 2).FirstOrDefault();
            var pedPlans = pedPhase.Plans;

            Assert.AreEqual(pedPhase.Cycles.Count, pedPlans.Sum(p => p.Cycles.Count));
        }

        [TestMethod]
        public void PedPhase_PedPhaseCount_PedPressesEquals7()
        {
            var pedPhase = PedDelaySignal.PedPhases.Where(p => p.PhaseNumber == 2).FirstOrDefault();
            var plan = pedPhase.Plans.FirstOrDefault();

            Assert.AreEqual(205, pedPhase.PedPresses);
        }

        [TestMethod]
        public void PedPlan_EventCount_Equals13()
        {
            var pedPhase = PedDelaySignal.PedPhases.Where(p => p.PhaseNumber == 2).FirstOrDefault();
            var plan = pedPhase.Plans.FirstOrDefault();

            Assert.AreEqual(13, plan.Events.Count);
        }
    }
}
