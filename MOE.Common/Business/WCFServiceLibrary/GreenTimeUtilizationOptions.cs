using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Web.UI.DataVisualization.Charting;
using MOE.Common.Business.GreenTimeUtilization;
using MOE.Common.Models;
using MOE.Common.Models.Repositories;
using Newtonsoft.Json;

namespace MOE.Common.Business.WCFServiceLibrary
{
    [DataContract]
    public class GreenTimeUtilizationOptions : MetricOptions
    {


        public GreenTimeUtilizationOptions(string signalID, DateTime startDate, DateTime endDate, 
            int binSize, int aggSize, bool showAverageSplit, bool showProgrammedSplit)
        {

            SignalID = signalID;
            SelectedBinSize = binSize;
            SelectedAggSize = aggSize;
            ShowAverageSplit = showAverageSplit;
            ShowProgrammedSplit = showProgrammedSplit;
            StartDate = startDate;
            EndDate = endDate;
        }

        public GreenTimeUtilizationOptions()
        {
            SetDefaults();
        }

        [Required]
        [Display(Name = "Green Time Bin Size (seconds; range is 2 to 10)")]
        [DataMember]
        public int SelectedBinSize { get; set; }

        [Display(Name = "Time of Day Aggregation (minutes; range is 5 to 30)")]
        [DataMember]
        public int SelectedAggSize { get; set; }

        [DataMember]
        [Display(Name = "Show Average Split")]
        public bool ShowAverageSplit { get; set; }

        [DataMember]
        [Display(Name = "Show Programmed Split")]
        public bool ShowProgrammedSplit { get; set; }

        public Models.Signal Signal { get; set; }

        public string JsonText { get; set; }

public override List<string> CreateMetric()
        {
            base.CreateMetric();
            var returnString = new List<string>();
            var signalRepository = SignalsRepositoryFactory.Create();
            var signal = signalRepository.GetVersionOfSignalByDate(SignalID, StartDate);
            //int MetricTypeID = 36; //measure ID number
            //var chart = new Chart();
            var metricApproaches = signal.GetApproachesForSignalThatSupportMetric(MetricTypeID);
            if (metricApproaches.Count > 0)
            {
                List<GreenTimeUtilizationPhase> greenTimeUtilizationPhases = new List<GreenTimeUtilizationPhase>();
                foreach (Approach approach in metricApproaches)
                {
                    if (approach.PermissivePhaseNumber != null && approach.PermissivePhaseNumber > 0)
                    {
                        greenTimeUtilizationPhases.Add(new GreenTimeUtilizationPhase(approach, this, true));
                    }
                    if (approach.ProtectedPhaseNumber > 0)
                    {
                        greenTimeUtilizationPhases.Add(new GreenTimeUtilizationPhase(approach, this, false));
                    }
                }
                greenTimeUtilizationPhases = greenTimeUtilizationPhases.OrderBy(s => s.PhaseNumberSort).ToList();
                foreach (var greenTimeUtilizationPhase in greenTimeUtilizationPhases)
                {
                    //JsonSerializer.Serialize(greenTimeUtilizationPhase);
                    JsonText = JsonConvert.SerializeObject(greenTimeUtilizationPhase);
                    System.Diagnostics.Debug.WriteLine(JsonText);
                    //JsonText = JsonConvert.SerializeObject(greenTimeUtilizationPhase.BinAvgList);
                    //System.Diagnostics.Debug.WriteLine(JsonText);
                    ReturnList.Add(JsonText);
                    //GetChart(greenTimeUtilizationPhase, returnString);
                }
            }

            //return ReturnList;
            return ReturnList;
        }

       


    }
}
