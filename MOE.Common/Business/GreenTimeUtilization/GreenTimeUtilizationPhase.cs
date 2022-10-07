using System;
using System.Collections.Generic;
using System.Linq;
using MOE.Common.Business.WCFServiceLibrary;
using MOE.Common.Models;
using MOE.Common.Models.Repositories;

namespace MOE.Common.Business.GreenTimeUtilization
{
    public class GreenTimeUtilizationPhase : GreenTimeUtilizationOptions
    {
        //define ECs
        public const int PHASE_BEGIN_GREEN = 1;
        public const int PHASE_BEGIN_YELLOW = 8;
        public const int PHASE_END_RED_CLEAR = 11;
        public const int DETECTOR_ON = 82;

        //define lists to be used
        public List<double> BinAvgList { get; } = new List<double>(new double[99]);
        public List<int> BinValueList { get; } = new List<int>(new int[99]);
        public List<int> BinMinList { get; } = new List<int>(new int[99]);
        public List<int> BinMaxList { get; } = new List<int>(new int[99]);
        public List<double> GreenDurationList { get; } = new List<double>();

        //define other variables to be transferred
        public double AvgGreenDuration { get; set; }
        public double ProgrammedGreenDuration { get; set;}
        public Approach Approach { get; }  //?? not sure if I need this one
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int PlanName { get; set; }
        public int PlanSort { get; set; }
        public int PhaseNumber { get; set; }
        public string PhaseSort { get; set; }    
        public string SignalID { get; set; }

        //define private variables
        private int splitLengthEventCode { get; set; }
        private double splitLength { get; set; }
        private double durYellowRed { get; set; }

        public GreenTimeUtilizationPhase(Approach approach, GreenTimeUtilizationOptions options, Plan plan, int plansort)
        {
            Approach = approach;
            //get list of split pattern plans

            StartTime = plan.StartTime;
            EndTime = plan.EndTime;
            PlanName = plan.PlanNumber;
            PlanSort = plansort;
            PhaseSort = approach.ProtectedPhaseNumber + "-1";
            SignalID = approach.SignalID;
            //PhaseNumberSort = getPermissivePhase ? approach.PermissivePhaseNumber.Value.ToString() + "-1" : approach.ProtectedPhaseNumber.ToString() + "-2";   <-- from Split fail, might be useful if we use getPermissivePhase bool        
            GetProgrammedSplitTime(approach.ProtectedPhaseNumber, options.StartDate, options.EndDate);
            GetYellowRedTime(approach, options);
            ProgrammedGreenDuration = splitLength - durYellowRed;
            int cycleCount = 0;

            //populate BinMinList with high values
            for (int i = 0; i < 99; i++)
            {
                BinMinList[i] = 999;
            }

            //get a list of cycle events
            SPM db = new SPM();
            var cel = ControllerEventLogRepositoryFactory.Create(db);
            var phaseEventNumbers = new List<int> { PHASE_BEGIN_GREEN, PHASE_BEGIN_YELLOW };
            var phaseEvents = cel.GetEventsByEventCodesParam(options.SignalID, StartTime, EndTime, phaseEventNumbers, approach.ProtectedPhaseNumber);
                
            //get a list of detections for that phase
            var detectorsToUse = approach.GetAllDetectorsOfDetectionType(4);
            var allDetectionEvents = cel.GetSignalEventsByEventCode(options.SignalID, StartTime, EndTime, DETECTOR_ON);
            var detectionEvents = new List<Controller_Event_Log>();
            foreach (var detector in detectorsToUse)
            {
                detectionEvents.AddRange(allDetectionEvents.Where(x =>
                    x.EventCode == DETECTOR_ON && x.EventParam == detector.DetChannel));
            }
                
            //pair a yellow with each green
            var greenList = phaseEvents.Where(x => x.EventCode == PHASE_BEGIN_GREEN)
                .OrderBy(x => x.Timestamp);
            var yellowList = phaseEvents.Where(x => x.EventCode == PHASE_BEGIN_YELLOW)
                .OrderBy(x => x.Timestamp);
            var detectionsList = detectionEvents.Where(x => x.EventCode == DETECTOR_ON)
                .OrderBy(x => x.Timestamp);
            foreach (var green in greenList)
            {
                //Find the corresponding yellow
                var yellow = yellowList.Where(x => x.Timestamp > green.Timestamp).OrderBy(x => x.Timestamp)
                    .FirstOrDefault();
                if (yellow == null)
                    continue;

                //get the green duration
                TimeSpan greenDuration = yellow.Timestamp - green.Timestamp;
                GreenDurationList.Add(greenDuration.TotalSeconds);

                //count the number of cycles
                cycleCount++;

                //Find all events between the green and yellow
                var greenDetectionsList = detectionsList
                    .Where(x => x.Timestamp >= green.Timestamp && x.Timestamp < yellow.Timestamp)
                    .OrderBy(x => x.Timestamp).ToList();
                if (!greenDetectionsList.Any())
                    continue;

                //create the cycle-only list
                List<int> cycleBinValueList = new List<int>(new int[99]);


                //add 1 to the bin value for each detection occuring during green
                foreach (var detection in greenDetectionsList)
                {
                    TimeSpan timeSinceGreenStart = detection.Timestamp - green.Timestamp;
                    var binnumber = (int)(timeSinceGreenStart.TotalSeconds / options.SelectedBinSize);
                    BinValueList[binnumber] = BinValueList[binnumber] + 1;
                    cycleBinValueList[binnumber]++;
                }

                //assign new plan min counts and max counts as needed
                //foreach (var entry in cycleBinValueList)
                for (int i = 0; i < 99; i++)
                {
                    if (cycleBinValueList[i] < BinMinList[i])
                        BinMinList[i] = cycleBinValueList[i];

                    if (cycleBinValueList[i] > BinMaxList[i])
                        BinMaxList[i] = cycleBinValueList[i];
                }
            //end of cycle loop; move on to next cycle
            }

            //foreach (var entry in binAvgList)
            for (int i = 0; i < 99; i++)
            {
                BinAvgList[i] = (double)BinValueList[i]/cycleCount;
            }

            //get average green duration
            AvgGreenDuration = GreenDurationList.Average();

        //end of function; phase-plan finished
        }


        void GetProgrammedSplitTime(int phaseNumber, DateTime startDate, DateTime endDate)
        {
            SPM db = new SPM();
            var cel = ControllerEventLogRepositoryFactory.Create(db);
            GetEventCodeForPhase(phaseNumber);
            var tempSplitTimes = cel.GetSignalEventsByEventCode(SignalID, startDate.Date, endDate, splitLengthEventCode)
                .OrderByDescending(e => e.Timestamp).ToList();
            foreach (var tempSplitTime in tempSplitTimes)
            {
                if (tempSplitTime.Timestamp <= StartTime)
                {
                    splitLength = tempSplitTime.EventParam;
                    break;
                }
            }
        }


        void GetEventCodeForPhase(int PhaseNumber)
        {
            switch (PhaseNumber)
            {
                case 1:
                    splitLengthEventCode = 134;
                    break;
                case 2:
                    splitLengthEventCode = 135;
                    break;
                case 3:
                    splitLengthEventCode = 136;
                    break;
                case 4:
                    splitLengthEventCode = 137;
                    break;
                case 5:
                    splitLengthEventCode = 138;
                    break;
                case 6:
                    splitLengthEventCode = 139;
                    break;
                case 7:
                    splitLengthEventCode = 140;
                    break;
                case 8:
                    splitLengthEventCode = 141;
                    break;
                case 17:
                    splitLengthEventCode = 203;
                    break;
                case 18:
                    splitLengthEventCode = 204;
                    break;
                case 19:
                    splitLengthEventCode = 205;
                    break;
                case 20:
                    splitLengthEventCode = 206;
                    break;
                case 21:
                    splitLengthEventCode = 207;
                    break;
                case 22:
                    splitLengthEventCode = 208;
                    break;
                case 23:
                    splitLengthEventCode = 209;
                    break;
                case 24:
                    splitLengthEventCode = 210;
                    break;
                case 25:
                    splitLengthEventCode = 211;
                    break;
                case 26:
                    splitLengthEventCode = 212;
                    break;
                case 27:
                    splitLengthEventCode = 213;
                    break;
                case 28:
                    splitLengthEventCode = 214;
                    break;
                case 29:
                    splitLengthEventCode = 215;
                    break;
                case 30:
                    splitLengthEventCode = 216;
                    break;
                case 31:
                    splitLengthEventCode = 217;
                    break;
                case 32:
                    splitLengthEventCode = 218;
                    break;
                default:
                    splitLengthEventCode = 219;
                    break;
            }
        }

        void GetYellowRedTime(Approach approach, GreenTimeUtilizationOptions options)
        {
            SPM db = new SPM();
            var cel = ControllerEventLogRepositoryFactory.Create(db);
            var yrEventNumbers = new List<int> { PHASE_BEGIN_YELLOW, PHASE_END_RED_CLEAR };
            var yrEvents = cel.GetEventsByEventCodesParam(options.SignalID, StartTime, EndTime, yrEventNumbers, approach.ProtectedPhaseNumber);
            var yellowList = yrEvents.Where(x => x.EventCode == PHASE_BEGIN_YELLOW)
                .OrderBy(x => x.Timestamp);
            var redList = yrEvents.Where(x => x.EventCode == PHASE_END_RED_CLEAR)
                .OrderBy(x => x.Timestamp);
            var startyellow = yellowList.FirstOrDefault();
            var endRedClear = redList.Where(x => x.Timestamp > startyellow.Timestamp).OrderBy(x => x.Timestamp)
                    .FirstOrDefault();
            TimeSpan spanYellowRed = endRedClear.Timestamp - startyellow.Timestamp;
            durYellowRed = spanYellowRed.TotalSeconds;
        }


    }

}