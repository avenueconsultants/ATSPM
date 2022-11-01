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

        //define sorting variables
        //public int PlanSort { get; set; }
        //public string PhaseSort { get; set; }

        //get plan starttime, endtime, and number
        //public DateTime StartTime { get; set; }
        //public DateTime EndTime { get; set; }
        //public int PlanName { get; set; }

        //other variables
        //public int splitLengthEventCode { get; set; }
        //public int SplitLength { get; set; }

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
                var Plans = PlanFactory.GetSplitMonitorPlans(StartDate, EndDate, SignalID);
                List<GreenTimeUtilizationPhase> greenTimeUtilizationPhases = new List<GreenTimeUtilizationPhase>();
                int planSort; 
                foreach (Approach approach in metricApproaches)
                {
                    planSort = 0;
                    foreach (Plan plan in Plans)
                    {
                        planSort++;

                        greenTimeUtilizationPhases.Add(new GreenTimeUtilizationPhase(approach, this, plan, planSort));
                        var chart = 2;
                        //chart = GetNewChart();
                        //var chartName = CreateFileName();
                        //chart.ImageLocation = MetricFileLocation + chartName;
                        //chart.SaveImage(MetricFileLocation + chartName, ChartImageFormat.Jpeg);
                        //ReturnList.Add(MetricWebPath + chartName);
                    }
                }
                
                greenTimeUtilizationPhases = greenTimeUtilizationPhases.OrderBy(s => s.PlanSort).ThenBy(s => s.PhaseSort).ToList();
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

       


        Chart GetChart(GreenTimeUtilizationPhase greenTimeUtilizationPhase, List<string> returnString)
            {
                var chart = ChartFactory.CreateDefaultChartNoX2Axis(this);
                chart.ChartAreas[0].AxisY2.Title = "Volume Per Hour";
                CreateChartLegend(chart);
                return chart;
            }

           
            void CreateChartLegend(Chart chart)
            {
                var chartLegend = new Legend();
                chartLegend.Name = "MainLegend";
                chartLegend.Docking = Docking.Left;
                chartLegend.CustomItems.Add(Color.Blue, "AoG - Arrival On Green");
                chartLegend.CustomItems.Add(Color.Blue, "GT - Green Time");
                chartLegend.CustomItems.Add(Color.Maroon, "PR - Platoon Ratio");
                chart.Legends.Add(chartLegend);
            }

            void SetChartTitle(Chart chart, SignalPhase signalPhase, Dictionary<string, string> statistics)
            {
                var detectorsForMetric = signalPhase.Approach.GetDetectorsForMetricType(MetricTypeID);
                var message = "\n Advanced detector located " +
                              detectorsForMetric.FirstOrDefault().DistanceFromStopBar +
                              " ft. upstream of stop bar";
                chart.Titles.Add(ChartTitleFactory.GetChartName(MetricTypeID));
                chart.Titles.Add(
                    ChartTitleFactory.GetSignalLocationAndDateRangeAndMessage(signalPhase.Approach.SignalID, StartDate,
                        EndDate,
                        message));
                chart.Titles.Add(ChartTitleFactory.GetPhaseAndPhaseDescriptions(signalPhase.Approach,
                    signalPhase.GetPermissivePhase));
                chart.Titles.Add(ChartTitleFactory.GetStatistics(statistics));
            }


            /// <summary>
            ///     Adds plan strips to the chart
            /// </summary>
            /// <param name="plans"></param>
            /// <param name="chart"></param>
            /// <param name="StartDate"></param>
            void SetPlanStrips(List<PlanPcd> plans, Chart chart)
            {
                var backGroundColor = 1;
                foreach (var plan in plans)
                {
                    var stripline = new StripLine();
                    //Creates alternating backcolor to distinguish the plans
                    if (backGroundColor % 2 == 0)
                        stripline.BackColor = Color.FromArgb(120, Color.LightGray);
                    else
                        stripline.BackColor = Color.FromArgb(120, Color.LightBlue);

                    //Set the stripline properties
                    stripline.IntervalOffset = (plan.StartTime - StartDate).TotalHours;
                    stripline.IntervalOffsetType = DateTimeIntervalType.Hours;
                    stripline.Interval = 1;
                    stripline.IntervalType = DateTimeIntervalType.Days;
                    stripline.StripWidth = (plan.EndTime - plan.StartTime).TotalHours;
                    stripline.StripWidthType = DateTimeIntervalType.Hours;

                    chart.ChartAreas["ChartArea1"].AxisX.StripLines.Add(stripline);

                    //Add a corrisponding custom label for each strip
                    var Plannumberlabel = new CustomLabel();
                    Plannumberlabel.FromPosition = plan.StartTime.ToOADate();
                    Plannumberlabel.ToPosition = plan.EndTime.ToOADate();
                    switch (plan.PlanNumber)
                    {
                        case 254:
                            Plannumberlabel.Text = "Free";
                            break;
                        case 255:
                            Plannumberlabel.Text = "Flash";
                            break;
                        case 0:
                            Plannumberlabel.Text = "Unknown";
                            break;
                        default:
                            Plannumberlabel.Text = "Plan " + plan.PlanNumber;

                            break;
                    }

                    Plannumberlabel.ForeColor = Color.Black;
                    Plannumberlabel.RowIndex = 3;

                    chart.ChartAreas["ChartArea1"].AxisX2.CustomLabels.Add(Plannumberlabel);

                    var aogLabel = new CustomLabel();
                    aogLabel.FromPosition = plan.StartTime.ToOADate();
                    aogLabel.ToPosition = plan.EndTime.ToOADate();
                    aogLabel.Text = plan.PercentArrivalOnGreen + "% AoG\n" +
                                    plan.PercentGreenTime + "% GT";

                    aogLabel.LabelMark = LabelMarkStyle.LineSideMark;
                    aogLabel.ForeColor = Color.Blue;
                    aogLabel.RowIndex = 2;
                    chart.ChartAreas["ChartArea1"].AxisX2.CustomLabels.Add(aogLabel);

                    var statisticlabel = new CustomLabel();
                    statisticlabel.FromPosition = plan.StartTime.ToOADate();
                    statisticlabel.ToPosition = plan.EndTime.ToOADate();
                    statisticlabel.Text =
                        plan.PlatoonRatio + " PR";
                    statisticlabel.ForeColor = Color.Maroon;
                    statisticlabel.RowIndex = 1;
                    chart.ChartAreas["ChartArea1"].AxisX2.CustomLabels.Add(statisticlabel);


                    //Change the background color counter for alternating color
                    backGroundColor++;
                }
            }
        }
    }
