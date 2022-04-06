﻿using ATSPM.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATSPM.Application.Reports.Business.LeftTurnGapReport
{
    public class LeftTurnGapOutAnalysis
    {
        private readonly IApproachRepository _approachRepository;
        private readonly IDetectorRepository _detectorRepository;
        private readonly IDetectorEventCountAggregationRepository _detectorEventCountAggregationRepository;
        private readonly IPhaseLeftTurnGapAggregationRepository _phaseLeftTurnGapAggregationRepository;
        private readonly ISignalsRepository _signalsRepository;
        public LeftTurnGapOutAnalysis(
            IApproachRepository approachRepository,
            IDetectorRepository detectorRepository,
            IDetectorEventCountAggregationRepository detectorEventCountAggregationRepository,
            IPhaseLeftTurnGapAggregationRepository phaseLeftTurnGapAggregationRepository, 
            ISignalsRepository signalsRepository)
        {
            _approachRepository = approachRepository;
            _detectorRepository = detectorRepository;
            _detectorEventCountAggregationRepository = detectorEventCountAggregationRepository;
            _phaseLeftTurnGapAggregationRepository = phaseLeftTurnGapAggregationRepository;
            _signalsRepository = signalsRepository;
        }
        public GapOutResult GetPercentOfGapDuration(string signalId, int approachId, DateTime start, DateTime end, TimeSpan startTime, TimeSpan endTime, int[] daysOfWeek)
        {
            var approach = _approachRepository.GetApproachByApproachID(approachId); 
            int opposingPhase = LeftTurnReportPreCheck.GetOpposingPhase(approach);
            int numberOfOposingLanes = GetNumberOfOpposingLanes(signalId, start, opposingPhase);
            double criticalGap = GetCriticalGap(numberOfOposingLanes);
            var gapOutResult = new GapOutResult();
            gapOutResult.Capacity = GetGapSummedTotal(signalId, opposingPhase, start, end, startTime, endTime, criticalGap, daysOfWeek);
            gapOutResult.AcceptableGaps = GetGapsList(signalId, opposingPhase, start, end, startTime, endTime, criticalGap, daysOfWeek);
            gapOutResult.DetectorCount = GetGapsList(signalId, opposingPhase, start, end, startTime, endTime, criticalGap, daysOfWeek);
            gapOutResult.Demand = GetGapDemand(approachId, start, end, startTime, endTime, criticalGap);
            if (gapOutResult.Capacity == 0)
                throw new ArithmeticException("Gap Count cannot be zero");
            gapOutResult.GapOutPercent = gapOutResult.Demand / gapOutResult.Capacity;
            return gapOutResult;
        }

        private Dictionary<DateTime, double> GetGapsList(string signalId, int phaseNumber, DateTime start, DateTime end, TimeSpan startTime, TimeSpan endTime, double criticalGap, int[] daysOfWeek)
        {
            List<Models.PhaseLeftTurnGapAggregation> amAggregations = new List<Models.PhaseLeftTurnGapAggregation>();
            int gapColumn = 1;
            if (criticalGap == 4.1)
                gapColumn = 12;
            else if (criticalGap == 5.3)
                gapColumn = 13;
            double gapTotal = 0;
            Dictionary<DateTime, double> acceptableGaps = new Dictionary<DateTime, double>();
            for (var tempDate = start.Date; tempDate <= end; tempDate = tempDate.AddDays(1))
            {
                for (var tempStart = tempDate.Date.Add(startTime); tempStart <= tempDate.Date.Add(endTime); tempStart = tempStart.AddMinutes(15))
                {
                    if (daysOfWeek.Contains((int)start.DayOfWeek))
                    {
                        var leftTurnGaps = _phaseLeftTurnGapAggregationRepository.GetPhaseLeftTurnGapAggregationBySignalIdPhaseNumberAndDateRange(
                                 signalId, phaseNumber, tempStart, tempStart.Add(startTime).AddMinutes(15));
                        int count = 0;
                        if(gapColumn ==12)
                            count = leftTurnGaps.Sum(l => l.GapCount6 + l.GapCount7 + l.GapCount8 + l.GapCount9);
                        else
                            count = leftTurnGaps.Sum(l => l.GapCount7 + l.GapCount8 + l.GapCount9);
                        acceptableGaps.Add(tempStart, count);
                    }
                }
            }
            return acceptableGaps;
        }

        private double GetGapDemand( int approachId, DateTime start, DateTime end, TimeSpan startTime, TimeSpan endTime, double criticalGap)
        {
            var detectors = LeftTurnReportPreCheck.GetLeftTurnDetectors(approachId, _approachRepository);
            int totalActivations = 0;
            for (var tempDate = start.Date; tempDate <= end; tempDate = tempDate.AddDays(1))
            {
                foreach (var detector in detectors)
                {
                    totalActivations += _detectorEventCountAggregationRepository.GetDetectorEventCountSumAggregationByDetectorIdAndDateRange(detector.Id, tempDate.Date.Add(startTime), tempDate.Date.Add(endTime));
                }
            }
            return totalActivations * criticalGap;
        }

        private double GetGapSummedTotal(string signalId, int phaseNumber, DateTime start, DateTime end, TimeSpan startTime, TimeSpan endTime, double criticalGap, int[] daysOfWeek)
        {
            List<Models.PhaseLeftTurnGapAggregation> amAggregations = new List<Models.PhaseLeftTurnGapAggregation>();
            int gapColumn = 1;
            if (criticalGap == 4.1)
                gapColumn = 12;
            else if (criticalGap == 5.3)
                gapColumn = 13;
            double gapTotal = 0;
            for (var tempDate = start.Date; tempDate <= end; tempDate = tempDate.AddDays(1))
            {
                if(daysOfWeek.Contains((int)start.DayOfWeek))
                    gapTotal += _phaseLeftTurnGapAggregationRepository.GetSummedGapsBySignalIdPhaseNumberAndDateRange(
                         signalId, phaseNumber, tempDate.Date.Add(startTime), tempDate.Date.Add(endTime), gapColumn);
            }
            return gapTotal;
        }

        private static double GetCriticalGap(int numberOfOposingLanes)
        {
            if (numberOfOposingLanes <= 2)
            {
                return 4.1;
            }
            else
            {
                return 5.3;
            }
        }

        public int GetNumberOfOpposingLanes(string signalId, DateTime startDate, int opposingPhase)
        {
            return _signalsRepository
                .GetVersionOfSignalByDate(signalId, startDate)
                .Approaches
                .SelectMany(a => a.Detectors)
                .Count(d => d.Approach.ProtectedPhaseNumber == opposingPhase);            
        }
    }
}
