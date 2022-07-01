using Xunit;
using MOE.Common.Business.SplitFail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOE.Common.Business.Bins;
using MOE.Common.Business.FilterExtensions;
using MOE.Common.Business.WCFServiceLibrary;
using MOE.Common.Models.Repositories;
using MOE.CommonTests.Helpers;
using MOE.CommonTests.Models;


namespace MOE.Common.Business.SplitFail.Tests
{
    public class SplitFailPhaseTests
    {
        //private InMemoryMOEDatabase inMemoryMoeDatabase = new InMemoryMOEDatabase();
        //public SplitFailPhaseTests()
        //{
            //inMemoryMoeDatabase.ClearTables();
            //XmlToListImporter.LoadControllerEventLog("ControllerEventLogs7185201710175-6pm.xml", inMemoryMoeDatabase);
            //XmlToListImporter.LoadSignals("signals.xml", inMemoryMoeDatabase);
            //XmlToListImporter.LoadApproaches("approachesfor7185.xml", inMemoryMoeDatabase);
            //XmlToListImporter.LoadDetectors("detectorsFor7185.xml", inMemoryMoeDatabase);
            //XmlToListImporter.AddDetectionTypesToDetectors
            //    ("DetectorTypesforDetectorsFor7185.xml", inMemoryMoeDatabase);
            //XmlToListImporter.AddDetectionTypesToMetricTypes("mtdt.xml", inMemoryMoeDatabase);
            //MOE.Common.Models.Repositories.SignalsRepositoryFactory.SetSignalsRepository(
            //    new InMemorySignalsRepository(inMemoryMoeDatabase));
            //MetricTypeRepositoryFactory.SetMetricsRepository(new InMemoryMetricTypeRepository(inMemoryMoeDatabase));
            //ApplicationEventRepositoryFactory.SetApplicationEventRepository(
            //    new InMemoryApplicationEventRepository(inMemoryMoeDatabase));
            //Common.Models.Repositories.DirectionTypeRepositoryFactory.SetDirectionsRepository(
            //    new InMemoryDirectionTypeRepository());
            //SpeedEventRepositoryFactory.SetSignalsRepository(new InMemorySpeedEventRepository(inMemoryMoeDatabase));
            //ApproachRepositoryFactory.SetApproachRepository(new InMemoryApproachRepository(inMemoryMoeDatabase));
            //ControllerEventLogRepositoryFactory.SetRepository(new InMemoryControllerEventLogRepository(inMemoryMoeDatabase));
            //DetectorRepositoryFactory.SetDetectorRepository(new InMemoryDetectorRepository(inMemoryMoeDatabase));
            ////XmlToListImporter.LoadSpeedEvents("7185speed.xml", inMemoryMoeDatabase);
            //[TestMethod()]
            //public void SplitFailPhaseTest()
            //{
            //    SplitFailOptions splitFailOptions = new SplitFailOptions{StartDate = new DateTime(2017, 10, 17, 17, 0, 0), EndDate = new DateTime(2017, 10,17, 17, 11, 1 ),  FirstSecondsOfRed = 5, SignalID = "7185", MetricTypeID = 12, ShowAvgLines = true, ShowPercentFailLines = true, ShowFailLines = true, Y2AxisMax = null, YAxisMin = 0, Y2AxisMin = 0, YAxisMax = null};
            //    var signalRepository = SignalsRepositoryFactory.Create();
            //    var signal = signalRepository.GetLatestVersionOfSignalBySignalID("7185");
            //    var approach = signal.Approaches.Where(a => a.ApproachID == 5593).FirstOrDefault();
            //    SplitFailPhase splitFailPhase = new SplitFailPhase(approach, splitFailOptions, true);
            //    Assert.IsTrue(splitFailPhase.Cycles[4].StartTime == new DateTime(2017,10, 17, 17, 9, 33));
            //    Assert.IsTrue(splitFailPhase.Cycles[4].YellowEvent == new DateTime(2017, 10, 17, 17, 10, 09));
            //    Assert.IsTrue(splitFailPhase.Cycles[4].RedEvent == new DateTime(2017, 10, 17, 17, 10, 13));
            //    Assert.IsTrue(splitFailPhase.Cycles[4].ActivationsDuringGreen.Count == 2);
            //    DateTime date1 = new DateTime(2017, 10, 17, 17, 1, 59, 500);
            //    Assert.IsTrue(splitFailPhase.Cycles[4].ActivationsDuringGreen[0].DetectorOn == date1);
            //    Assert.IsTrue(splitFailPhase.Cycles[4].ActivationsDuringGreen[0].DetectorOff == new DateTime(2017, 10, 17, 17, 9, 43, 800));
            //    Assert.IsTrue(splitFailPhase.Cycles[4].ActivationsDuringGreen[0].DurationInMilliseconds == 464300);
            //    Assert.IsTrue(splitFailPhase.Cycles[4].ActivationsDuringGreen[1].DetectorOn == new DateTime(2017, 10, 17, 17, 9, 55, 400));
            //    Assert.IsTrue(splitFailPhase.Cycles[4].ActivationsDuringGreen[1].DetectorOff == new DateTime(2017, 10, 17, 17, 10, 6, 300));
            //    Assert.IsTrue(splitFailPhase.Cycles[4].ActivationsDuringGreen[1].DurationInMilliseconds == 10900);
            //    Assert.IsTrue(splitFailPhase.Cycles[4].ActivationsDuringRed.Count == 1);
            //    Assert.IsTrue(splitFailPhase.Cycles[4].ActivationsDuringRed[0].DetectorOn == new DateTime(2017, 10, 17, 17, 10, 9, 200));
            //    Assert.IsTrue(splitFailPhase.Cycles[4].ActivationsDuringRed[0].DetectorOff == new DateTime(2017, 10, 17, 17, 10, 13, 300));
            //    Assert.IsTrue(splitFailPhase.Cycles[4].ActivationsDuringRed[0].DurationInMilliseconds == 4100);
            //    Assert.IsTrue(splitFailPhase.Cycles[4].GreenOccupancyTimeInMilliseconds == 21700.0);
            //    Assert.IsTrue(splitFailPhase.Cycles[4].TotalGreenTimeMilliseconds == 36000.0);
            //    Assert.IsTrue(splitFailPhase.Cycles[4].RedOccupancyTimeInMilliseconds == 300.0);
            //    Assert.IsTrue(splitFailPhase.Cycles[4].RedOccupancyPercent == 6.0);
            //    Assert.IsTrue(splitFailPhase.Cycles[4].FirstSecondsOfRed == 5); 
            //    Assert.IsTrue(Math.Round(splitFailPhase.Cycles[4].GreenOccupancyPercent) == 60.0);
            //}
            //[TestMethod()]
            //public void SplitFailDataAggregationTest()
            //{
            //    var startTime = new DateTime(2014, 1, 1);
            //    var endTime = new DateTime(2014, 1, 1, 0, 15, 0);
            //    var splitFailAggregateRepository = MOE.Common.Models.Repositories.ApproachSplitFailAggregationRepositoryFactory.Create();
            //    var splitFails = splitFailAggregateRepository.GetApproachSplitFailsAggregationByApproachIdAndDateRange(4971,
            //        startTime, endTime, true);

            //    var signalRepository = SignalsRepositoryFactory.Create();
            //    var signal = signalRepository.GetLatestVersionOfSignalBySignalID("5078");
            //    var approach = signal.Approaches.Where(s => s.ApproachID == 4971).FirstOrDefault();

            //    var splitFailOptions = new SplitFailOptions
            //    {
            //        FirstSecondsOfRed = 5,
            //        StartDate = startTime,
            //        EndDate = endTime,
            //        MetricTypeID = 12
            //    };
            //    var splitFailPhase = new SplitFailPhase(approach, splitFailOptions, true);

            //    Assert.IsTrue(splitFails.FirstOrDefault().SplitFailures == splitFailPhase.TotalFails);

            //}

        [Theory]
        [InlineData("07:36:40.8", "7:36:47.7", "7:36:51.7", "7:37:46.0", 5800, 0, 84, 0, false)]
        [InlineData("08:36:40.8", "8:36:47.7", "8:36:51.7", "8:37:46.0", 5800, 4300, 84, 86, true)] // event sequence 82, 1, 81, 8
        [InlineData("9:33:57.2", "9:34:4.9", "9:34:8.9", "9:36:25.2", 5200, 0, 68, 0, false)] // event sequence 1, 82, 81, 8
        [InlineData("14:04:21.0", "14:04:28.4", "14:04:32.4", "14:05:59.0", 7400, 0, 100, 0, false)] // event sequence 82, 1, 8, 81
        [InlineData("15:04:21.0", "15:04:28.4", "15:04:32.4", "15:05:59.0", 6400, 0, 86, 0, false)] // event sequence 1, 82, 8, 81
        public void TestSetDetectors(string firstGreen, string yellow, string red, string secondGreen, int expectedGreenOccupancy, int expectedRedOccupancy, int expectedGreenOccupancyPercent, int expectedRedOccupancyPercent, bool expectedSplitFailResult)
        {
            var firstGreenEvent = DateTime.Parse(firstGreen);
            var yellowEvent = DateTime.Parse(yellow);
            var redEvent = DateTime.Parse(red);
            var lastGreenEvent = DateTime.Parse(secondGreen);
            var cycleDetectorActivations = new List<SplitFailDetectorActivation>()
            {
                //new SplitFailDetectorActivation(DateTime.Parse(""), DateTime.Parse("")),
                //new SplitFailDetectorActivation(DateTime.Parse("7:09:44.5"), DateTime.Parse("7:09:31.1")),
                //test 1
                new SplitFailDetectorActivation(DateTime.Parse("7:36:46.6"), DateTime.Parse("7:36:33.7")),
                //test 2
                new SplitFailDetectorActivation(DateTime.Parse("8:36:56.0"), DateTime.Parse("8:36:51.0")),
                new SplitFailDetectorActivation(DateTime.Parse("8:36:46.6"), DateTime.Parse("8:36:33.7")),
                //test 3
                new SplitFailDetectorActivation(DateTime.Parse("9:34:3.8"), DateTime.Parse("9:33:58.6")),
                new SplitFailDetectorActivation(DateTime.Parse("9:36:37.2"), DateTime.Parse("9:36:18.1")),
                //test 4
                new SplitFailDetectorActivation(DateTime.Parse("14:04:30.9"), DateTime.Parse("14:04:13.9")),
                new SplitFailDetectorActivation(DateTime.Parse("14:06:7.8"), DateTime.Parse("14:04:37.7")),
                //test 5
                new SplitFailDetectorActivation(DateTime.Parse("15:04:30.9"), DateTime.Parse("15:04:22.0")),
                new SplitFailDetectorActivation(DateTime.Parse("15:06:7.8"), DateTime.Parse("15:04:37.7")),
                //new SplitFailDetectorActivation(DateTime.Parse("7:40:57.3"), DateTime.Parse("7:40:45.0")),
                //new SplitFailDetectorActivation(DateTime.Parse("7:43:32.2"), DateTime.Parse("7:43:20.0")),
                //new SplitFailDetectorActivation(DateTime.Parse("7:50:18.1"), DateTime.Parse("7:50:5.4")),
                //new SplitFailDetectorActivation(DateTime.Parse("7:57:29.8"), DateTime.Parse("7:57:29.3")),
                //new SplitFailDetectorActivation(DateTime.Parse("8:02:54.6"), DateTime.Parse("8:02:41.5")),
                //new SplitFailDetectorActivation(DateTime.Parse("8:06:45.0"), DateTime.Parse("8:06:32.9")),
                //new SplitFailDetectorActivation(DateTime.Parse("8:07:41.9"), DateTime.Parse("8:07:28.9")),
                //new SplitFailDetectorActivation(DateTime.Parse("8:11:48.2"), DateTime.Parse("8:12:7.0")),
                //new SplitFailDetectorActivation(DateTime.Parse("8:12:22.3"), DateTime.Parse("8:21:21.6")),
                //new SplitFailDetectorActivation(DateTime.Parse("8:21:43.0"), DateTime.Parse("8:22:44.7")),
                //new SplitFailDetectorActivation(DateTime.Parse("8:22:57.5"), DateTime.Parse("8:27:3.8")),
                //new SplitFailDetectorActivation(DateTime.Parse("8:27:38.1"), DateTime.Parse("8:29:19.7")),
                //new SplitFailDetectorActivation(DateTime.Parse("8:29:43.7"), DateTime.Parse("8:36:10.2")),
            };

            var cycleSplitFail = new CycleSplitFail(firstGreenEvent, redEvent, yellowEvent, lastGreenEvent, CycleSplitFail.TerminationType.Unknown, 5);
            cycleSplitFail.SetDetectorActivations(cycleDetectorActivations);

            Assert.Equal(expectedGreenOccupancy, cycleSplitFail.GreenOccupancyTimeInMilliseconds);
            Assert.Equal(expectedRedOccupancy, cycleSplitFail.RedOccupancyTimeInMilliseconds);
            Assert.Equal(expectedGreenOccupancyPercent, Math.Round(cycleSplitFail.GreenOccupancyPercent));
            Assert.Equal(expectedRedOccupancyPercent, cycleSplitFail.RedOccupancyPercent);
            Assert.Equal(expectedSplitFailResult, cycleSplitFail.IsSplitFail);
        }
        //}
    }

    
}