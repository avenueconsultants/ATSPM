﻿using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace AtspmApi.Models
{
    public class DirectionTypeRepository : IDirectionTypeRepository
    {
        private readonly Models.AtspmApi db = new Models.AtspmApi();

        public List<SelectListItem> GetSelectList()
        {
            var list =
                new List<SelectListItem>();
            foreach (var d in db.DirectionTypes.OrderBy(d => d.DisplayOrder))
                list.Add(new SelectListItem {Value = d.DirectionTypeID.ToString(), Text = d.Description});
            return list;
        }

        public List<DirectionType> GetDirectionsByIDs(List<int> includedDirections)
        {
            return db.DirectionTypes.Where(d => includedDirections.Contains(d.DirectionTypeID)).ToList();
        }

        public DirectionType GetByDescription(string directionDescription)
        {
            return db.DirectionTypes.FirstOrDefault(d => d.Description == directionDescription);
        }

        public List<DirectionType> GetAllDirections()
        {
            var results = (from r in db.DirectionTypes
                orderby r.DisplayOrder
                select r).ToList();
            return results;
        }

        public DirectionType GetDirectionByID(int DirectionID)
        {
            return db.DirectionTypes.Where(x => x.DirectionTypeID == DirectionID).FirstOrDefault();
        }
    }
}