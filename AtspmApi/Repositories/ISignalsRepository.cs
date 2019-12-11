﻿using System;
using System.Collections.Generic;
using AtspmApi.Models;

namespace AtspmApi.Repositories
{
    public interface ISignalsRepository
    {
        //List<Signal> GetAllSignals();
        string GetSignalDescription(string signalId);
        //List<Signal> GetAllEnabledSignals();
        List<Signal> EagerLoadAllSignals();
        Signal GetSignalWithApproachesBySignalID(string signalID);
        Signal GetLatestVersionOfSignalBySignalID(string signalID);
        //SignalFTPInfo GetSignalFTPInfoByID(string signalID);
        //List<SignalFTPInfo> GetSignalFTPInfoForAllFTPSignals();
        void AddOrUpdate(Signal signal);
        //List<Pin> GetPinInfo();
        string GetSignalLocation(string signalID);
        void AddList(List<Signal> signals);
        //Signal CopySignalToNewVersion(Signal originalVersion);
        List<Signal> GetAllVersionsOfSignalBySignalID(string signalID);
        //List<Signal> GetLatestVersionOfAllSignals();
        //List<Signal> GetLatestVersionOfAllSignalsForFtp();
        int CheckVersionWithFirstDate(string signalId);
        //List<Signal> GetLatestVerionOfAllSignalsByControllerType(int controllerTypeId);
        Signal GetVersionOfSignalByDate(string signalId, DateTime startDate);
        Signal GetSignalVersionByVersionId(int versionId);
        void SetVersionToDeleted(int versionId);
        void SetAllVersionsOfASignalToDeleted(string id);
        List<Signal> GetSignalsBetweenDates(string signalId, DateTime startDate, DateTime endDate);
        //bool Exists(string signalId);
        Signal GetVersionOfSignalByDateWithDetectionTypes(string signalId, DateTime startDate);
    }
}