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
        public const int DETECTOR_ON = 82;

        //define sorting variables
        public int PlanSort { get; set; }
        public string PhaseSort { get; set; }

        //define lists to be used
        List<double> binAvgList { get; } = new List<double>(new double[99]);
        List<int> binValueList { get; } = new List<int>(new int[99]);
        List<int> binMinList { get; } = new List<int>(new int[99]);
        List<int> binMaxList { get; } = new List<int>(new int[99]);

        //get the approach???
        public Approach Approach { get; }

        public GreenTimeUtilizationPhase(Approach approach, GreenTimeUtilizationOptions options)
        {
            Approach = approach;
            //get list of split pattern plans
            var Plans = PlanFactory.GetSplitMonitorPlans(options.StartDate, options.EndDate, options.SignalID);
            PlanSort = 0;
            PhaseSort = approach.ProtectedPhaseNumber + "-1";
            //PhaseNumberSort = getPermissivePhase ? approach.PermissivePhaseNumber.Value.ToString() + "-1" : approach.ProtectedPhaseNumber.ToString() + "-2";   <-- from Split fail, might be useful if we use getPermissivePhase bool        
            
            foreach (var plan in Plans)
            {
                var cycleCount = 0;

                //get plan starttime, endtime, and number
                var startTime = plan.StartTime;
                var endTime = plan.EndTime;
                var planName = plan.PlanNumber;
                PlanSort++;

                //get a list of cycle events
                SPM db = new SPM();
                var cel = ControllerEventLogRepositoryFactory.Create(db);
                var phaseEventNumbers = new List<int> { PHASE_BEGIN_GREEN, PHASE_BEGIN_YELLOW };
                var phaseEvents = cel.GetEventsByEventCodesParam(options.SignalID, startTime, endTime, phaseEventNumbers, approach.ProtectedPhaseNumber);
                
                //get a list of detections for that phase
                var detectorsToUse = approach.GetAllDetectorsOfDetectionType(4);
                var allDetectionEvents = cel.GetSignalEventsByEventCode(options.SignalID, startTime, endTime, DETECTOR_ON);
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
                        binValueList[binnumber] = binValueList[binnumber] + 1;
                        cycleBinValueList[binnumber]++;
                    }

                    //assign new plan min counts and max counts as needed
                    //foreach (var entry in cycleBinValueList)
                    for (int i = 0; i < 99; i++)
                    {
                        if (cycleBinValueList[i] < binMinList[i])
                            binMinList[i] = cycleBinValueList[i];

                        if (cycleBinValueList[i] > binMaxList[i])
                            binMaxList[i] = cycleBinValueList[i];
                    }
                //end of cycle loop; move on to next cycle
                }

                //foreach (var entry in binAvgList)
                for (int i = 0; i < 99; i++)
                {
                    binAvgList[i] = (double)binValueList[i]/cycleCount;
                }
                

            //end of plan loop; move on to next plan
            }

        //end of function; phase finished
        }

    }

}