using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOE.Common.Models.Repositories
{
    public class AreaRepository : IAreaRepository
    {
        private readonly SPM db = new SPM();

        public List<Area> GetAllAreas()
        {
            var Areas = (from r in db.Areas
                         orderby r.AreaName
                         select r).ToList();
            return Areas;
        }

        public Area GetAreaByID(int areaId)
        {
            var Area = (from r in db.Areas
                        where r.Id == areaId
                        select r).FirstOrDefault();
            if (Area != null)
                return Area;
            {
                var repository =
                    ApplicationEventRepositoryFactory.Create();
                var error = new ApplicationEvent
                {
                    ApplicationName = "MOE.Common",
                    Class = "Models.Repository.AreaRepository",
                    Function = "GetApproachByApproachID",
                    Description = "No Area for ID.  Attempted ID# = " + areaId,
                    SeverityLevel = ApplicationEvent.SeverityLevels.High,
                    Timestamp = DateTime.Now
                };
                repository.Add(error);
                throw new Exception("There is no Area for this ID");
            }
        }

        public Area GetAreaByName(string AreaName)
        {
            var Area = (from r in db.Areas
                        where r.AreaName == AreaName
                        select r).FirstOrDefault();
            return Area;
        }

        public void DeleteByID(int areaId)
        {
            var Area = (from r in db.Areas
                        where r.Id == areaId
                        select r).FirstOrDefault();

            db.Areas.Remove(Area);
            db.SaveChanges();
        }

        public void Remove(Area Area)
        {
            var g = (from r in db.Areas
                     where r.Id == Area.Id
                     select r).FirstOrDefault();
            if (g != null)
                try
                {
                    db.Areas.Remove(g);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    {
                        var repository =
                            ApplicationEventRepositoryFactory.Create();
                        var error = new ApplicationEvent
                        {
                            ApplicationName = "MOE.Common",
                            Class = "Models.Repository.AreaRepository",
                            Function = "Remove",
                            Description = ex.Message,
                            SeverityLevel = ApplicationEvent.SeverityLevels.High,
                            Timestamp = DateTime.Now
                        };
                        repository.Add(error);
                        throw ex;
                    }
                }
        }

        public void Update(Area newArea)
        {
            var Area = GetAreaByID(newArea.Id);
            if (Area != null)
            {
                db.Entry(Area).CurrentValues.SetValues(newArea);
                db.SaveChanges();
            }
            else
            {
                throw new Exception("Area Not Found");
            }
        }

        public void Add(Area newArea)
        {
            try
            {
                db.Areas.Add(newArea);
                db.SaveChanges();
            }

            catch (Exception ex)
            {
                var repository =
                    ApplicationEventRepositoryFactory.Create();
                var error = new ApplicationEvent();
                error.ApplicationName = "MOE.Common";
                error.Class = "Models.Repository.ApproachAreaRepository";
                error.Function = "Add";
                error.Description = ex.Message;
                error.SeverityLevel = ApplicationEvent.SeverityLevels.High;
                error.Timestamp = DateTime.Now;
                repository.Add(error);
                throw;
            }
        }
    }
}
