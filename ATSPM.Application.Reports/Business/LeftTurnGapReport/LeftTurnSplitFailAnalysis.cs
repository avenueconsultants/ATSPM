﻿using ATSPM.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATSPM.Application.Reports.Business.LeftTurnGapReport
{

    public class LeftTurnSplitFailAnalysis
    {
        private readonly IApproachRepository _approachRepository;
        private readonly IApproachSplitFailAggregationRepository _approachSplitFailAggregationRepository;

        public LeftTurnSplitFailAnalysis(IApproachRepository approachRepository,
                                         IApproachSplitFailAggregationRepository approachSplitFailAggregationRepository)
        {
            _approachRepository = approachRepository;
            _approachSplitFailAggregationRepository = approachSplitFailAggregationRepository;
        }
        public SplitFailResult GetSplitFailPercent(int approachId,
                                                   DateTime start,
                                                   DateTime end,
                                                   TimeSpan startTime,
                                                   TimeSpan endTime,
                                                   int[] daysOfWeek)
        {
            List<Models.ApproachSplitFailAggregation> splitFailsAggregates = GetSplitFailAggregates(approachId, start, end, startTime, endTime, daysOfWeek);
            Dictionary<DateTime, double> percentCyclesWithSplitFail = GetPercentCyclesWithSplitFails(start, end, startTime, endTime, daysOfWeek, splitFailsAggregates);
            int cycles = splitFailsAggregates.Sum(s => s.Cycles);
            int splitFails = splitFailsAggregates.Sum(s => s.SplitFailures);
            if (cycles == 0)
                throw new ArithmeticException("Cycles cannot be zero");
            var approach = _approachRepository.GetApproachByApproachID(approachId);
            return new SplitFailResult
            {
                CyclesWithSplitFails = splitFails,
                SplitFailPercent = splitFails / cycles,
                PercentCyclesWithSplitFailList = percentCyclesWithSplitFail,
                Direction = approach.DirectionType.Description,
        };

        }

        private List<Models.ApproachSplitFailAggregation> GetSplitFailAggregates(int approachId,
                                                                                 DateTime start,
                                                                                 DateTime end,
                                                                                 TimeSpan startTime,
                                                                                 TimeSpan endTime,
                                                                                 int[] daysOfWeek)
        {
            var approach = _approachRepository.GetApproachByApproachID(approachId);
            List<Models.ApproachSplitFailAggregation> splitFailsAggregates = new List<Models.ApproachSplitFailAggregation>();
            for (var tempDate = start.Date; tempDate <= end; tempDate = tempDate.AddDays(1))
            {
                if (daysOfWeek.Contains((int)start.DayOfWeek))
                {
                    if (approach.ProtectedPhaseNumber != 0)
                    {
                        splitFailsAggregates.AddRange(_approachSplitFailAggregationRepository.GetApproachSplitFailsAggregationBySignalIdPhaseDateRange(
                            approach.SignalId, approach.ProtectedPhaseNumber, tempDate.Date.Add(startTime), tempDate.Date.Add(endTime)));
                    }
                    else if (approach.PermissivePhaseNumber.HasValue)
                    {
                        splitFailsAggregates.AddRange(_approachSplitFailAggregationRepository.GetApproachSplitFailsAggregationBySignalIdPhaseDateRange(
                            approach.SignalId, approach.PermissivePhaseNumber.Value, tempDate.Date.Add(startTime), tempDate.Date.Add(endTime)));
                    }
                }
            }

            return splitFailsAggregates;
        }

        public static Dictionary<DateTime, double> GetPercentCyclesWithSplitFails(DateTime start,
                                                                                   DateTime end,
                                                                                   TimeSpan startTime,
                                                                                   TimeSpan endTime,
                                                                                   int[] daysOfWeek,
                                                                                   List<Models.ApproachSplitFailAggregation> splitFailsAggregates)
        {
            Dictionary<DateTime, double> percentCyclesWithSplitFail = new Dictionary<DateTime, double>();
            for (var tempDate = start.Date; tempDate <= end; tempDate = tempDate.AddDays(1))
            {
                if (daysOfWeek.Contains((int)tempDate.DayOfWeek))
                {
                    for (var tempstart = tempDate.Date.Add(startTime); tempstart < tempDate.Add(endTime); tempstart = tempstart.AddMinutes(30))
                    {
                        var tempEndTime = tempstart.AddMinutes(30);
                        var tempSplitFails = splitFailsAggregates.Where(s => s.BinStartTime >= tempstart && s.BinStartTime < tempEndTime).Sum(s => s.SplitFailures);
                        var tempCycles = splitFailsAggregates.Where(s => s.BinStartTime >= tempstart && s.BinStartTime < tempEndTime).Sum(s => s.Cycles);
                        double tempPercentFails = 0;
                        if (tempCycles != 0)
                            tempPercentFails = tempSplitFails / tempCycles;
                        percentCyclesWithSplitFail.Add(tempstart, tempPercentFails);
                    }
                }
            }

            return percentCyclesWithSplitFail;
        }
    }
}
