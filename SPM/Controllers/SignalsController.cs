﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using MOE.Common.Models;
using SPM.Filters;

namespace SPM.Controllers
{

    [Authorize(Roles = "Technician")]
    public class SignalsController : Controller
    {
        private MOE.Common.Models.Repositories.IControllerTypeRepository _controllerTypeRepository; 
        private MOE.Common.Models.Repositories.IRegionsRepository _regionRepository;
        private MOE.Common.Models.Repositories.IDirectionTypeRepository _directionTypeRepository;
        private MOE.Common.Models.Repositories.IMovementTypeRepository _movementTypeRepository;
        private MOE.Common.Models.Repositories.ILaneTypeRepository _laneTypeRepository;
        private MOE.Common.Models.Repositories.IDetectionHardwareRepository _detectionHardwareRepository;
        private MOE.Common.Models.Repositories.ISignalsRepository _signalsRepository;
        private MOE.Common.Models.Repositories.IDetectorRepository _detectorRepository; 
        private MOE.Common.Models.Repositories.IDetectionTypeRepository _detectionTypeRepository; 
        private MOE.Common.Models.Repositories.IApproachRepository _approachRepository; 
        private MOE.Common.Models.Repositories.IMetricTypeRepository _metricTypeRepository; 

        public SignalsController()
        {

            _signalsRepository = MOE.Common.Models.Repositories.SignalsRepositoryFactory.Create();
            _detectorRepository = MOE.Common.Models.Repositories.DetectorRepositoryFactory.Create();
            _detectionTypeRepository = MOE.Common.Models.Repositories.DetectionTypeRepositoryFactory.Create();
            _approachRepository = MOE.Common.Models.Repositories.ApproachRepositoryFactory.Create();
            _metricTypeRepository = MOE.Common.Models.Repositories.MetricTypeRepositoryFactory.Create();
            _controllerTypeRepository = MOE.Common.Models.Repositories.ControllerTypeRepositoryFactory.Create();
            _regionRepository = MOE.Common.Models.Repositories.RegionsRepositoryFactory.Create();
            _directionTypeRepository = MOE.Common.Models.Repositories.DirectionTypeRepositoryFactory.Create();
            _movementTypeRepository = MOE.Common.Models.Repositories.MovementTypeRepositoryFactory.Create();
            _laneTypeRepository = MOE.Common.Models.Repositories.LaneTypeRepositoryFactory.Create();
            _detectionHardwareRepository = MOE.Common.Models.Repositories.DetectionHardwareRepositoryFactory.Create();
        }

        public SignalsController(
         MOE.Common.Models.Repositories.IControllerTypeRepository controllerTypeRepository,
         MOE.Common.Models.Repositories.IRegionsRepository regionRepository,
         MOE.Common.Models.Repositories.IDirectionTypeRepository directionTypeRepository,
         MOE.Common.Models.Repositories.IMovementTypeRepository movementTypeRepository,
         MOE.Common.Models.Repositories.ILaneTypeRepository laneTypeRepository,
         MOE.Common.Models.Repositories.IDetectionHardwareRepository detectionHardwareRepository,
         MOE.Common.Models.Repositories.ISignalsRepository signalsRepository,
         MOE.Common.Models.Repositories.IDetectorRepository detectorRepository,
         MOE.Common.Models.Repositories.IDetectionTypeRepository detectionTypeRepository,
         MOE.Common.Models.Repositories.IApproachRepository approachRepository,
         MOE.Common.Models.Repositories.IMetricTypeRepository metricTypeRepository)
        {
            _signalsRepository = signalsRepository;
            _detectorRepository = detectorRepository;
            _detectionTypeRepository = detectionTypeRepository;
            _approachRepository = approachRepository;
            _controllerTypeRepository = controllerTypeRepository;
            _regionRepository = regionRepository;
            _directionTypeRepository = directionTypeRepository;
            _movementTypeRepository = movementTypeRepository;
            _laneTypeRepository = laneTypeRepository;
            _detectionHardwareRepository = detectionHardwareRepository;
            _metricTypeRepository = metricTypeRepository;
        }

        public ActionResult Index()
        {
            MOE.Common.Models.ViewModel.WebConfigTool.WebConfigToolViewModel wctv =
                new MOE.Common.Models.ViewModel.WebConfigTool.WebConfigToolViewModel(_regionRepository, _metricTypeRepository);

            return View(wctv);
        }

        // GET: Signals
        [AllowAnonymous]
        public ActionResult SignalDetail()
        {
            MOE.Common.Models.ViewModel.WebConfigTool.WebConfigToolViewModel wctv =
                new MOE.Common.Models.ViewModel.WebConfigTool.WebConfigToolViewModel(_regionRepository, _metricTypeRepository);
            return View(wctv);
        }

        public ActionResult AddNewVersion(string id)
        {
            var existingSignal = _signalsRepository.GetSignalBySignalID(id);
            if (existingSignal == null)
            {
                return Content("<h1>" +"No Signal Matches this SignalID" + "</h1>");
            }

            Signal signal = _signalsRepository.CopySignalToNewVersion(existingSignal);
                try
                {
                    _signalsRepository.AddOrUpdate(signal);
                }
                catch (Exception ex)
                {
                    return Content("<h1>" + ex.Message + "</h1>");
                }
                finally
                {
                    AddSelectListsToViewBag(signal);
                }
                return PartialView("Edit", signal);
            }
            
        

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult AddApproach(string id)
        {            
            var signal = _signalsRepository.GetSignalBySignalID(id);
            Approach approach = GetNewApproach(signal);           
            _approachRepository.AddOrUpdate(approach);
            AddSelectListsToViewBag(signal);
            return PartialView(approach);
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult CopyApproach(string id, int approachID)
        {
            var signal = _signalsRepository.GetSignalBySignalID(id);
            //Approach approachFromDatabase = signal.Approaches.Where(a => a.ApproachID == approachID).First();
            AddSelectListsToViewBag(signal);
            try
            {
                Approach newApproach = MOE.Common.Models.Approach.CopyApproach(approachID);
                _approachRepository.AddOrUpdate(newApproach);
                return Content("<h1>Copy Successful!</h1>");
            }
            catch (Exception ex)
            {
                return Content("<h1>" + ex.Message + "</h1>");
            }           
        }

        [Authorize(Roles = "Technician")]
        private string GetApproachIndex(Signal signal)
        {
            return "Approaches[" + signal.Approaches.Count.ToString() + "].";
        }

        private Approach GetNewApproach(Signal signal)
        {
            Approach approach = new Approach();
            approach.Detectors = new List<Detector>();
            approach.ApproachID = 0;
            approach.SignalID = signal.SignalID;
            approach.Description = "New Phase/Direction";
            approach.Index = GetApproachIndex(signal);
            approach.DirectionTypeID = 1;
            return approach;
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult AddDetector(string signalID, int approachID, string approachIndex)
        {
            Signal signal = _signalsRepository.GetSignalBySignalID(signalID);
            var approach = signal.Approaches.Where(s => s.ApproachID == approachID).First();
            Detector detector = CreateNewDetector(approach, approachIndex, signalID);            
            AddSelectListsToViewBag(signal);
            return PartialView(detector);
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult CopyDetector(int ID, string signalID, int approachID, string approachIndex)
        {
            Detector newDetector = MOE.Common.Models.Detector.CopyDetector(ID, true); //need to increase DetChannel if not copying the whole signal.
            Signal signal = _signalsRepository.GetSignalBySignalID(signalID);
            Approach approach = signal.Approaches.Where(s => s.ApproachID == approachID).First();
            newDetector.ApproachID = approach.ApproachID;
            newDetector.Index = approachIndex + "Detectors[" + approach.Detectors.Count.ToString() + "].";
            MOE.Common.Models.Repositories.IDetectorRepository detectorRepository =
                MOE.Common.Models.Repositories.DetectorRepositoryFactory.Create();
            detectorRepository.Add(newDetector);  //Do the Repository Add FOR detectors AT the detector leve.
            newDetector.Approach = approach;  //????do not associate up!!! Add from top down!
            //approach.Detectors.Add(newDetector);
            AddSelectListsToViewBag(signal);
            return PartialView("AddDetector", newDetector);
        }
        
        private Detector CreateNewDetector(Approach approach, string approachIndex, string signalID)
        {
            Detector detector = new Detector();
            detector.ApproachID = approach.ApproachID;
            detector.AllDetectionTypes = _detectionTypeRepository.GetAllDetectionTypesNoBasic();
            detector.DetectionTypeIDs = new List<int>();
            detector.DetectionTypes = new List<DetectionType>();
            detector.Index = approachIndex + "Detectors[" + approach.Detectors.Count.ToString() + "].";
            detector.DetectorComments = new List<DetectorComment>();
            detector.DateAdded = DateTime.Now;
            detector.DetChannel = _detectorRepository.GetMaximumDetectorChannel(signalID) + 1;
            detector.DetectorID = signalID + detector.DetChannel.ToString("D2");
            detector = _detectorRepository.Add(detector);
            detector.Approach = approach;
            return detector;
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create(string id)
        {
            var existingSignal = _signalsRepository.GetSignalBySignalID(id);
            if (existingSignal == null)
            {

                Signal signal = CreateNewSignal(id);
                try
                {
                    _signalsRepository.AddOrUpdate(signal);
                }
                catch (Exception ex)
                {
                    return Content("<h1>" + ex.Message + "</h1>");
                }
                finally
                {
                    AddSelectListsToViewBag(signal);
                }
                return PartialView("Edit", signal);
            }
            return Content("<h1>Signal Already Exists</h1>");
        }


        private Signal CreateNewSignal(string id)
        {
            Signal signal = new Signal();
            signal.SignalID = id;
            signal.PrimaryName = "ChangeMe";
            signal.SecondaryName = "ChangeMe";
            signal.IPAddress = "10.10.10.10";
            signal.Latitude = "0";
            signal.Longitude = "0";
            signal.RegionID = 2;
            signal.ControllerTypeID = 1;
            signal.Enabled = true;
            return signal;
        }
                
        // GET: Signals/Copy
        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Copy(string id, string newId)
        {           
            MOE.Common.Models.Signal newSignal = new MOE.Common.Models.Signal();           
            if (id == null)
            {
                return Content("<h1>A signal ID is required</h1>");
            }
            Signal signal = _signalsRepository.GetSignalBySignalID(id);
            if (signal != null)
            {
                newSignal = MOE.Common.Models.Signal.CopySignal(signal, newId);              
            }
            try
            {
                _signalsRepository.AddOrUpdate(newSignal);
            }
            catch(Exception ex)
            {
                return Content("<h1>"+ex.Message+"</h1>");
            }
            finally
            {
                AddSelectListsToViewBag(newSignal);
            }
            return PartialView("Edit", newSignal);
        }

        // GET: Signals/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return Content("<h1>A signal ID is required</h1>");
            }
            Signal signal = _signalsRepository.GetSignalBySignalID(id);
            signal.Approaches = signal.Approaches.OrderBy(a => a.ProtectedPhaseNumber).ThenBy(a => a.DirectionType.Description).ToList();
            foreach(Approach approach in signal.Approaches)
            {
                approach.Detectors = approach.Detectors.OrderBy(d => d.DetectorID).ToList();
            }
            if (signal != null)
            {
                List<MOE.Common.Models.DetectionType> allDetectionTypes = _detectionTypeRepository.GetAllDetectionTypesNoBasic();
                foreach (MOE.Common.Models.Approach a in signal.Approaches)
                {
                    foreach (MOE.Common.Models.Detector gd in a.Detectors)
                    {
                        gd.Index = a.Index + "Detector[" + a.Detectors.ToList().FindIndex(d => d.DetectorID == gd.DetectorID).ToString() + "].";
                        gd.AllDetectionTypes = allDetectionTypes;
                        gd.DetectionTypeIDs = new List<int>();
                        gd.DetectorComments = gd.DetectorComments.OrderByDescending(x => x.TimeStamp).ToList();
                        foreach (MOE.Common.Models.DetectionType dt in gd.DetectionTypes)
                        {
                            gd.DetectionTypeIDs.Add(dt.DetectionTypeID);
                        }
                    }
                    a.Index = "Approaches[" + signal.Approaches.ToList().FindIndex(app => app.ApproachID == a.ApproachID).ToString() +"].";
                }
                if (signal == null)
                {
                    return HttpNotFound();
                }
                
                signal.Comments = signal.Comments.OrderByDescending(s => s.TimeStamp).ToList();
                AddSelectListsToViewBag(signal);
                //foreach (MOE.Common.Models.MetricComment c in signal.Comments)
                //{
                //    c.MetricTypes = _metricTypeRepository.GetMetricTypesByMetricComment(c);
                //}
            }           
            return PartialView(signal);
        }

        public ActionResult _SignalPartial(Signal signal)
        {
            return PartialView(signal);
        }

        [AllowAnonymous]
        public ActionResult SignalDetailResult(string id)
        {
            if (id == null)
            {
                return Content("<h1>A signal ID is required</h1>");
            }
            Signal signal = _signalsRepository.GetSignalBySignalID(id);
            signal.Approaches = signal.Approaches.OrderBy(a => a.ProtectedPhaseNumber).ThenBy(a => a.DirectionType.Description).ToList();
            foreach (Approach approach in signal.Approaches)
            {
                approach.Detectors = approach.Detectors.OrderBy(d => d.DetectorID).ToList();
            }
            if (signal != null)
            {
                List<MOE.Common.Models.DetectionType> allDetectionTypes = _detectionTypeRepository.GetAllDetectionTypesNoBasic();
                foreach (MOE.Common.Models.Approach a in signal.Approaches)
                {
                    foreach (MOE.Common.Models.Detector gd in a.Detectors)
                    {
                        gd.Index = a.Index + "Detector[" + a.Detectors.ToList().FindIndex(d => d.DetectorID == gd.DetectorID).ToString() + "].";
                        gd.AllDetectionTypes = allDetectionTypes;
                        gd.DetectionTypeIDs = new List<int>();
                        gd.DetectorComments = gd.DetectorComments.OrderByDescending(x => x.TimeStamp).ToList();
                        foreach (MOE.Common.Models.DetectionType dt in gd.DetectionTypes)
                        {
                            gd.DetectionTypeIDs.Add(dt.DetectionTypeID);
                        }
                    }
                    a.Index = "Approaches[" + signal.Approaches.ToList().FindIndex(app => app.ApproachID == a.ApproachID).ToString() + "].";
                }
                if (signal == null)
                {
                    return HttpNotFound();
                }

                signal.Comments = signal.Comments.OrderByDescending(s => s.TimeStamp).ToList();
                AddSelectListsToViewBag(signal);
                //foreach (MOE.Common.Models.MetricComment c in signal.Comments)
                //{
                //    c.MetricTypes = _metricTypeRepository.GetMetricTypesByMetricComment(c);
                //}
            }
            return PartialView(signal);
        }

        // POST: Signals/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(Signal signal)
        {
            try
            {
                ModelState.Clear();
                signal = SetDetectionTypes(signal);
                if (TryValidateModel(signal))
                {
                    MOE.Common.Models.Repositories.ISignalsRepository repository =
                        MOE.Common.Models.Repositories.SignalsRepositoryFactory.Create();
                    repository.AddOrUpdate(signal);
                }
                AddSelectListsToViewBag(signal);
                return Content("Save Successful!" + DateTime.Now.ToString());
            }
            catch(Exception ex)
            {
                return Content(ex.Message);
            }
        }

        private Signal SetDetectionTypes(Signal signal)
        {
            if (signal.Approaches != null)
            {
                foreach (MOE.Common.Models.Approach a in signal.Approaches)
                {
                    if (a.Detectors != null)
                    {
                        foreach (MOE.Common.Models.Detector gd in a.Detectors)
                        {
                            gd.DetectorID = a.SignalID + gd.DetChannel.ToString("D2");
                            if (gd.DetectionTypeIDs == null)
                            {
                                gd.DetectionTypeIDs = new List<int>();
                            }
                            if (gd.DetectionIDs != null)
                            {
                                foreach (string detectionTypeID in gd.DetectionIDs)
                                {
                                    gd.DetectionTypeIDs.Add(Convert.ToInt32(detectionTypeID));
                                }
                            }
                        }
                    }
                }
            }
            return signal;
        }

        private void AddSelectListsToViewBag(MOE.Common.Models.Signal signal)
        {


            ViewBag.ControllerType = new SelectList(_controllerTypeRepository.GetControllerTypes(), "ControllerTypeID", "Description", signal.ControllerTypeID);
            ViewBag.Region = new SelectList(_regionRepository.GetAllRegions(), "ID", "Description", signal.RegionID);
            ViewBag.DirectionType = new SelectList(_directionTypeRepository.GetAllDirections(), "DirectionTypeID", "Abbreviation");
            ViewBag.MovementType = new SelectList(_movementTypeRepository.GetAllMovementTypes(), "MovementTypeID", "Description");
            ViewBag.LaneType = new SelectList(_laneTypeRepository.GetAllLaneTypes(), "LaneTypeID", "Description");
            ViewBag.DetectionHardware = new SelectList(_detectionHardwareRepository.GetAllDetectionHardwares(), "ID", "Name");  
        }

        // GET: Signals/Delete/5
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            _signalsRepository.SetAllVersionsOfASignalToDeleted(id);


            MOE.Common.Models.ViewModel.WebConfigTool.WebConfigToolViewModel wctv =
                new MOE.Common.Models.ViewModel.WebConfigTool.WebConfigToolViewModel(_regionRepository, _metricTypeRepository);

            return View(wctv);
        }

        // POST: Signals/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateJsonAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public string DeleteConfirmed(string id)
        {
            try
            {
                _signalsRepository.Remove(id);
                return id + " Removed";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }

        [HttpPost, ActionName("Delete Version")]
        [ValidateJsonAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteVersion(int versionId)
        {
            Signal signal = _signalsRepository.GetSignalVersionByVersionId(versionId);

            _signalsRepository.SetVersionToDeleted(versionId);

            var nextMostRecentVersion = _signalsRepository.GetLatestVersionOfSignalBySignalID(signal.SignalID);

            return PartialView("Edit", nextMostRecentVersion);
        }
    }
}
