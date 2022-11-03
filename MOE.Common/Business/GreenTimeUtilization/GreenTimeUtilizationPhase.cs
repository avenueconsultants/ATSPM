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
        public List<BarStack> Stacks { get; }
        [DataMember]
        public List<AverageSplit> AvgSplits { get; }
        [DataMember]
        public List<ProgrammedSplit> ProgSplits { get; }
        //[DataMember]
        //public List<double> BinAvgList { get; } = new List<double>(new double[99]);
        
        //[DataMember]
        //public List<int> BinMaxList { get; } = new List<int>(new int[99]);
        //public List<double> GreenDurationList { get; } = new List<double>();

        //define other variables to be transferred
        [DataMember]
        public double AvgGreenDuration { get; set; }
        [DataMember]
        public double ProgrammedGreenDuration { get; set;}
        public Approach Approach { get; }  //?? not sure if I need this one
        [DataMember]
        public DateTime StartTime { get; set; }
        [DataMember]
        public DateTime EndTime { get; set; }
        //[DataMember]
        //public int PlanName { get; set; }

        public int PlanSort { get; set; }
        [DataMember]
        public int PhaseNumber { get; set; }

        public string PhaseSort { get; set; }
        [DataMember]
        public string SignalID { get; set; }

        //define private variables
        private int splitLengthEventCode { get; set; }
        private double splitLength { get; set; }
        private double durYellowRed { get; set; }

        public GreenTimeUtilizationPhase(Approach approach, GreenTimeUtilizationOptions options, List<PlanSplitMonitor> Plans) // the plans/splits input is still TBD
        {
            //define lists
            List<BarStack> Stacks = new List<BarStack>();
            List<AverageSplit> AvgSplits = new List<AverageSplit>();
            List<ProgrammedSplit> ProgSplits = new List<ProgrammedSplit>();

            //get a list of cycle events
            SPM db = new SPM();
            var cel = ControllerEventLogRepositoryFactory.Create(db);
            var phaseEventNumbers = new List<int> { PHASE_BEGIN_GREEN, PHASE_BEGIN_YELLOW };
            var phaseEvents = cel.GetEventsByEventCodesParam(options.SignalID, StartTime, EndTime.AddMinutes(options.SelectedAggSize), phaseEventNumbers, approach.ProtectedPhaseNumber); //goes until a bin after to make sure we get the whole green time of the last cycle within the anlaysis period
            
            //get a list of detections for that phase
            var detectorsToUse = approach.GetAllDetectorsOfDetectionType(4);  //should this really be approach-based adn not phase-based? 
            var allDetectionEvents = cel.GetSignalEventsByEventCode(options.SignalID, StartTime, EndTime.AddMinutes(options.SelectedAggSize), DETECTOR_ON);
            var detectionEvents = new List<Controller_Event_Log>();
            foreach (var detector in detectorsToUse)
            {
                detectionEvents.AddRange(allDetectionEvents.Where(x =>
                    x.EventCode == DETECTOR_ON && x.EventParam == detector.DetChannel));
            }

            //loop for each Agg bin
            for (DateTime StartAggTime = options.StartDate; StartAggTime < options.EndDate; StartAggTime.AddMinutes(options.SelectedAggSize))
            {
                DateTime endAggTime = StartAggTime.AddMinutes(options.SelectedAggSize);
                List<double> greenDurationList = new List<double>();
                List<int> BinValueList = new List<int>(new int[99]);
                int cycleCount = 0;

                //determine timestamps of the first green and last yellow
                var firstGreen = phaseEvents.Where(x => x.Timestamp > StartAggTime).OrderBy(x => x.Timestamp).FirstOrDefault();
                var lastGreen = phaseEvents.Where(x => x.Timestamp < endAggTime).OrderByDescending(x => x.Timestamp).FirstOrDefault();
                var lastYellow = phaseEvents.Where(x => x.Timestamp > lastGreen.Timestamp).OrderBy(x => x.Timestamp).FirstOrDefault();

                //get the event lists for the agg bin
                var aggDetections = detectionEvents
                    .Where(x => x.Timestamp >= firstGreen.Timestamp &&
                                x.Timestamp <= lastYellow.Timestamp)
                    .OrderBy(x => x.Timestamp);
                var greenList = phaseEvents
                    .Where(x => x.EventCode == PHASE_BEGIN_GREEN &&
                                x.Timestamp >= firstGreen.Timestamp &&
                                x.Timestamp <= lastYellow.Timestamp)
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
                        //might want to add a claculation for the ax bin used so it can be sent as a charting value
                    }

                }

                //create new classes
                Stacks.Add(new BarStack(StartAggTime, BinValueList, cycleCount, options.SelectedBinSize));
                AvgSplits.Add(new AverageSplit(StartAggTime, greenDurationList));
            }

            new ProgrammedSplit(options.StartDate, options.EndDate, PhaseNumber);



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


        void GetEventCodeForPhase(int PhaseNumber)  // i think this might be better suited moved over to the options file now. so splits for a phase can be sent into this file
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

    public class ProgrammedSplit //:GreenTimeUtilizationPhase
    {
        // data memberes are: starttime and progsplit



        private int splitLengthEventCode { get; set; }
        private double splitLength { get; set; }
        private double durYellowRed { get; set; }

        public ProgrammedSplit(Approach approach, GreenTimeUtilizationOptions options, List<PlanSplitMonitor> plans)
        {
            GetYellowRedTime(approach, options);
            foreach (PlanSplitMonitor plan in plans)
            {
                //need a connector between the list of plans and using getprogrammedsplittime
                GetProgrammedSplitTime(approach.ProtectedPhaseNumber, Starttime, startDate) //might need something other than protected phase number for permissive phases
                if (splitLengthEventCode == plan.PlanNumber) //
                {

                }
                ProgrammedGreenDuration = splitLength - durYellowRed;
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

            void GetEventCodeForPhase(int PhaseNumber)  // i think this might be better suited moved over to the options file now. so splits for a phase can be sent into this file
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
        }

}