using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MOE.Common.Business.WCFServiceLibrary;
using MOE.Common.Models;
using MOE.Common.Models.Repositories;

namespace MOE.Common.Business.GreenTimeUtilization
{
    [DataContract]
    public class GreenTimeUtilizationPhase : GreenTimeUtilizationOptions
    {
        //define ECs
        public const int PHASE_BEGIN_GREEN = 1;
        public const int PHASE_BEGIN_YELLOW = 8;
        public const int PHASE_END_RED_CLEAR = 11;
        public const int DETECTOR_ON = 82;

        //define lists to be used
        [DataMember]
        public List<BarStack> Stacks { get; } = new List<BarStack>();
        [DataMember]
        public List<AverageSplit> AvgSplits { get; } = new List<AverageSplit>();
        [DataMember]
        public List<ProgrammedSplit> ProgSplits { get; } = new List<ProgrammedSplit>();
        [DataMember]
        public DateTime StartDate { get; set; }
        [DataMember]
        public DateTime EndDate { get; set; }
        [DataMember]
        public int PhaseNumber { get; set; }
        [DataMember]
        public string SignalID { get; set; }
        [DataMember]
        public string MeasureName { get; set; }
        [DataMember]
        public string SignalLocation { get; set; }
        [DataMember]
        public string PhaseName { get; set; }

        //define private variables
        private int splitLengthEventCode { get; set; }
        private double splitLength { get; set; }
        private double durYellowRed { get; set; }

        private static readonly ISignalsRepository signalsRepository =
            SignalsRepositoryFactory.Create();

        private static readonly IMetricTypeRepository metricTypesRepository =
            MetricTypeRepositoryFactory.Create();

        public GreenTimeUtilizationPhase(Approach approach, GreenTimeUtilizationOptions options) // the plans/splits input is still TBD
        {
            bool getPermissivePhase = false; // might need to move the setting of this to the Options page instead of here; for now it is set to false because I haven'tfully incorporated getPermissivePhase yet

            //define properties
            PhaseNumber = approach.ProtectedPhaseNumber;
            StartDate = options.StartDate;
            EndDate = options.EndDate;
            SignalID = options.SignalID;
            SelectedBinSize = options.SelectedBinSize;
            SelectedAggSize = options.SelectedAggSize;
            ShowAverageSplit = options.ShowAverageSplit;
            ShowProgrammedSplit = options.ShowProgrammedSplit;
            MetricTypeID = options.MetricTypeID;
            MeasureName = metricTypesRepository.GetMetricsByID(MetricTypeID).ChartName;//ChartTitleFactory.GetChartName(MetricTypeID);
            SignalLocation = signalsRepository.GetSignalLocation(SignalID); //ChartTitleFactory.GetSignalLocationAndDateRange(SignalID, StartDate, EndDate); 
            string phaseNumberDescription;
            int phaseNum = getPermissivePhase ? approach.PermissivePhaseNumber.Value : approach.ProtectedPhaseNumber; //this and the if statement below were taken from ChartTitleFactory.GetPhaseAndPhaseDescriptions
            if ((approach.IsProtectedPhaseOverlap && !getPermissivePhase) ||
                (approach.IsPermissivePhaseOverlap && getPermissivePhase))
            {
                phaseNumberDescription = "Overlap " + phaseNum + ": " + approach.Description;
            }
            else
            {
                phaseNumberDescription = "Phase " + phaseNum + ": " + approach.Description;
            }
            PhaseName = phaseNumberDescription;

            //get a list of cycle events
            SPM db = new SPM();
            var cel = ControllerEventLogRepositoryFactory.Create(db);
            var phaseEventNumbers = new List<int> { PHASE_BEGIN_GREEN, PHASE_BEGIN_YELLOW };
            var phaseEvents = cel.GetEventsByEventCodesParam(options.SignalID, options.StartDate, options.EndDate.AddMinutes(options.SelectedAggSize), phaseEventNumbers, approach.ProtectedPhaseNumber); //goes until a bin after to make sure we get the whole green time of the last cycle within the anlaysis period

            //get a list of detections for that phase
            var detectorsToUse = approach.GetAllDetectorsOfDetectionType(4);  //should this really be approach-based adn not phase-based? 
            var allDetectionEvents = cel.GetSignalEventsByEventCode(options.SignalID, options.StartDate, options.EndDate.AddMinutes(options.SelectedAggSize), DETECTOR_ON);
            var detectionEvents = new List<Controller_Event_Log>();
            foreach (var detector in detectorsToUse)
            {
                detectionEvents.AddRange(allDetectionEvents.Where(x =>
                    x.EventCode == DETECTOR_ON && x.EventParam == detector.DetChannel));
            }

            //loop for each Agg bin
            for (DateTime StartAggTime = options.StartDate; StartAggTime < options.EndDate; StartAggTime = StartAggTime.AddMinutes(options.SelectedAggSize))
            {
                DateTime endAggTime = StartAggTime.AddMinutes(options.SelectedAggSize);
                List<double> greenDurationList = new List<double>();
                List<int> BinValueList = new List<int>(new int[99]);
                int cycleCount = 0;

                //determine timestamps of the first green and last yellow
                var firstGreen = phaseEvents.Where(x => x.Timestamp > StartAggTime && x.EventCode == PHASE_BEGIN_GREEN).OrderBy(x => x.Timestamp).FirstOrDefault();
                var lastGreen = phaseEvents.Where(x => x.Timestamp < endAggTime && x.EventCode == PHASE_BEGIN_GREEN).OrderByDescending(x => x.Timestamp).FirstOrDefault();
                var lastYellow = phaseEvents.Where(x => x.Timestamp > lastGreen.Timestamp && x.EventCode == PHASE_BEGIN_YELLOW).OrderBy(x => x.Timestamp).FirstOrDefault();

                //get the event lists for the agg bin
                var aggDetections = detectionEvents
                    .Where(x => x.Timestamp >= firstGreen.Timestamp &&
                                x.Timestamp <= lastYellow.Timestamp)
                    .OrderBy(x => x.Timestamp);
                var greenList = phaseEvents
                    .Where(x => x.EventCode == PHASE_BEGIN_GREEN &&
                                x.Timestamp >= firstGreen.Timestamp &&
                                x.Timestamp <= lastGreen.Timestamp)
                    .OrderBy(x => x.Timestamp);
                var yellowList = phaseEvents
                    .Where(x => x.EventCode == PHASE_BEGIN_YELLOW &&
                                x.Timestamp >= firstGreen.Timestamp &&
                                x.Timestamp <= lastYellow.Timestamp)
                    .OrderBy(x => x.Timestamp);

                //pair each green with a yellow
                foreach (var green in greenList)
                {
                    //Find the corresponding yellow
                    var yellow = yellowList.Where(x => x.Timestamp > green.Timestamp).OrderBy(x => x.Timestamp)
                        .FirstOrDefault();
                    if (yellow == null)
                        continue;

                    //get the green duration
                    TimeSpan greenDuration = yellow.Timestamp - green.Timestamp;
                    greenDurationList.Add(greenDuration.TotalSeconds);

                    //count the number of cycles
                    cycleCount++;

                    //Find all events between the green and yellow
                    var greenDetectionsList = aggDetections
                        .Where(x => x.Timestamp >= green.Timestamp && x.Timestamp < yellow.Timestamp)
                        .OrderBy(x => x.Timestamp).ToList();
                    if (!greenDetectionsList.Any())
                        continue;

                    //add 1 to the bin value for each detection occuring during green
                    foreach (var detection in greenDetectionsList)
                    {
                        TimeSpan timeSinceGreenStart = detection.Timestamp - green.Timestamp;
                        var binnumber = (int)(timeSinceGreenStart.TotalSeconds / options.SelectedBinSize);
                        BinValueList[binnumber] = BinValueList[binnumber] + 1;
                        //might want to add a calculation for the max bin used so it can be sent as a charting value
                    }

                }

                //create new classes
                Stacks.Add(new BarStack(StartAggTime, BinValueList, cycleCount, options.SelectedBinSize));
                AvgSplits.Add(new AverageSplit(StartAggTime, greenDurationList));
            }

            //get plans
            var plans = PlanFactory.GetSplitMonitorPlans(options.StartDate, options.EndDate, SignalID);
            GetYellowRedTime(approach, options);
            foreach (Plan analysisplan in plans)
            {
                //GetProgrammedSplitTimesInAnalysisPeriod(approach.ProtectedPhaseNumber, analysisplan, options.EndDate);
                GetProgrammedSplitTime(approach.ProtectedPhaseNumber, analysisplan.StartTime, analysisplan.EndTime.AddMinutes(-1));                
                ProgSplits.Add(new ProgrammedSplit(analysisplan, options.StartDate, splitLength, durYellowRed));
            }


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
                if (tempSplitTime.Timestamp <= startDate)
                {
                    splitLength = tempSplitTime.EventParam;
                    break;
                }
            }
        }



        void GetProgrammedSplitTimesInAnalysisPeriod(int phaseNumber, Plan analysisplan, DateTime analysisEnd)
        {
            SPM db = new SPM();
            var cel = ControllerEventLogRepositoryFactory.Create(db);
            GetEventCodeForPhase(phaseNumber);
            var tempSplitTimes = cel.GetSignalEventsByEventCode(SignalID, analysisplan.StartTime, analysisEnd, splitLengthEventCode)
                .OrderByDescending(e => e.Timestamp).ToList();
            int i = 0;
            for (i = 0; tempSplitTimes[i].Timestamp < analysisplan.StartTime; i++)
            {
                splitLength = tempSplitTimes[i].EventParam;
                break;

            }
            i++;

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
            var yrEvents = cel.GetEventsByEventCodesParam(options.SignalID, options.StartDate, options.EndDate, yrEventNumbers, approach.ProtectedPhaseNumber);
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

    public class BarStack
    {
        [DataMember]
        public List<Layer> Layers { get; }
        [DataMember]
        public DateTime StartTime { get; set; }


        public BarStack(DateTime startAggTime, List<int> binValueList, int cycleCount, int binSize)
        {
            StartTime = startAggTime;
            //find the max layers number that is used
            int maxI = 0;
            for (int i = 0; i < 99; i++)
            {
                if (binValueList[i] != 0 && i > maxI)
                {
                    maxI = i;
                }
            }
            //create Layers
            Layers = new List<Layer>();
            int binStart = 0;
            for (int i = 0; i <= maxI; i++)
            {
                Layers.Add(new Layer(binValueList[i], cycleCount, binStart));
                binStart = binStart + binSize;
            }
        }
    }

    public class Layer
    {
        [DataMember]
        public double DataValue { get; set; }
        [DataMember]
        public int LowerEnd { get; set; }


        public Layer(double sumValue, int cycleCount, int binStart)
        {
            DataValue = (double)sumValue / cycleCount;
            LowerEnd = binStart;
        }
    }

    public class AverageSplit
    {
        [DataMember]
        public DateTime StartTime { get; set; }
        [DataMember]
        public double AvgValue { get; set; }


        public AverageSplit(DateTime startAggTime, List<double> greenDurationList)
        {
            AvgValue = greenDurationList.Average();
            StartTime = startAggTime;
        }

    }

    public class ProgrammedSplit 
    {
        [DataMember]
        public DateTime StartTime { get; set; }
        [DataMember]
        public double ProgValue { get; set; }


        public ProgrammedSplit(Plan analysisPlan, DateTime analysisStart, double splitLength, double durYR)
        {
            if (analysisStart < analysisPlan.StartTime)
            {
                StartTime = analysisPlan.StartTime;
            }
            else
            {
                StartTime = analysisStart;
            }
            ProgValue = splitLength - durYR;
        }

    }
}