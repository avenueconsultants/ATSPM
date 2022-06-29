using Xunit;
using MOE.Common.Business.PEDDelay;
using MOE.Common.Models.Repositories;
using MOE.Common.Models;
using System.Linq;
using System;

namespace MOE.CommonTests.Business.PEDDelay
{
    public class PEDDelayTest
    {
        private Signal Signal { get; set; }
        private DateTime StartTime { get; set; }
        private DateTime EndTime { get; set; }
        private PedDelaySignal PedDelaySignal { get; set; }

        [Fact]
        public void Initialize()
        {
            var signalRepository = SignalsRepositoryFactory.Create();
            StartTime = DateTime.Parse("10/26/2021 12:00 AM");
            EndTime = StartTime.AddDays(1);
            Signal = signalRepository.GetVersionOfSignalByDate("7115", StartTime);
            PedDelaySignal = new PedDelaySignal(Signal, 15, StartTime, EndTime);
        }

        [Fact]
        public void PedPlan_PedRecallWhenEventsCountIsNull_ReturnsFalse()
        {
            var pedPlan = new PedPlan(0, StartTime, EndTime, 0);          
            Assert.False(pedPlan.PedRecallOn);
        }

        [Fact]
        public void PedPlan_PedCallsRegisteredCount_Equals2()
        {
            Initialize();
            var pedPhase = PedDelaySignal.PedPhases.Where(p => p.PhaseNumber == 2).FirstOrDefault();
            var plan = pedPhase.Plans.FirstOrDefault();

            Assert.Equal(2, plan.PedCallsRegisteredCount);
        }

        [Fact]
        public void PedPlan_PedCallsBeginWalkCount_Equals2()
        {
            Initialize();
            var pedPhase = PedDelaySignal.PedPhases.Where(p => p.PhaseNumber == 2).FirstOrDefault();
            var plan = pedPhase.Plans.FirstOrDefault();

            Assert.Equal(2, plan.PedBeginWalkCount);
        }

        [Fact]
        public void PedPlan_PedRecall_EqualsFalse()
        {
            Initialize();
            var pedPhase = PedDelaySignal.PedPhases.Where(p => p.PhaseNumber == 2).FirstOrDefault();
            var plan = pedPhase.Plans.FirstOrDefault();

            Assert.False(plan.PedRecallOn);
        }

        [Fact]
        public void PedPlan_PlanType_EqualsFree()
        {
            Initialize();
            var pedPhase = PedDelaySignal.PedPhases.Where(p => p.PhaseNumber == 2).FirstOrDefault();
            var plan = pedPhase.Plans.FirstOrDefault();

            Assert.Equal(254, plan.PlanNumber);
        }

        [Fact]
        public void PedDelayChart_PedPhaseAndPedPlan_CyclesWithPedDelayMatch()
        {
            Initialize();
            var pedPhase = PedDelaySignal.PedPhases.Where(p => p.PhaseNumber == 2).FirstOrDefault();
            var pedPlans = pedPhase.Plans;

            Assert.Equal(pedPhase.Cycles.Count, pedPlans.Sum(p => p.Cycles.Count));
        }

        [Fact]
        public void PedPhase_PedPhaseCount_PedPressesEquals7()
        {
            Initialize();
            var pedPhase = PedDelaySignal.PedPhases.Where(p => p.PhaseNumber == 2).FirstOrDefault();
            var plan = pedPhase.Plans.FirstOrDefault();

            Assert.Equal(205, pedPhase.PedPresses);
        }

        [Fact]
        public void PedPlan_EventCount_Equals13()
        {
            Initialize();
            var pedPhase = PedDelaySignal.PedPhases.Where(p => p.PhaseNumber == 2).FirstOrDefault();
            var plan = pedPhase.Plans.FirstOrDefault();

            Assert.Equal(13, plan.Events.Count);
        }
    }
}
