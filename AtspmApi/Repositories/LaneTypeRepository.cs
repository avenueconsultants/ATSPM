﻿using System.Collections.Generic;
using System.Linq;
using AtspmApi.Models;

namespace AtspmApi.Repositories
{
    public class LaneTypeRepository : ILaneTypeRepository
    {
        private readonly Models.AtspmApi db = new Models.AtspmApi();


        public List<LaneType> GetAllLaneTypes()
        {
            db.Configuration.LazyLoadingEnabled = false;
            var laneTypes = (from r in db.LaneTypes
                select r).ToList();
            return laneTypes;
        }

        public LaneType GetLaneTypeByLaneTypeID(int laneTypeID)
        {
            var laneType = from r in db.LaneTypes
                where r.LaneTypeID == laneTypeID
                select r;
            return laneType.FirstOrDefault();
        }

        public void Update(LaneType laneType)
        {
            var g = (from r in db.LaneTypes
                where r.LaneTypeID == laneType.LaneTypeID
                select r).FirstOrDefault();
            if (g != null)
            {
                db.Entry(g).CurrentValues.SetValues(laneType);
                db.SaveChanges();
            }
            else
            {
                db.LaneTypes.Add(laneType);
                db.SaveChanges();
            }
        }

        public void Remove(LaneType laneType)
        {
            var g = (from r in db.LaneTypes
                where r.LaneTypeID == laneType.LaneTypeID
                select r).FirstOrDefault();
            if (g != null)
            {
                db.LaneTypes.Remove(g);
                db.SaveChanges();
            }
        }

        public void Add(LaneType laneType)
        {
            var g = (from r in db.LaneTypes
                where r.LaneTypeID == laneType.LaneTypeID
                select r).FirstOrDefault();
            if (g == null)
            {
                db.LaneTypes.Add(g);
                db.SaveChanges();
            }
        }
    }
}