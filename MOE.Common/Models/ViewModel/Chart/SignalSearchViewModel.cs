using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using MOE.Common.Models.Repositories;

namespace MOE.Common.Models.ViewModel.Chart
{
    public class SignalSearchViewModel
    {
        private readonly IMetricTypeRepository _metricRepository;

        private readonly IRegionsRepository _regionRepository;
        private readonly IAreaRepository _areaRepository;

        public SignalSearchViewModel()
        {
            _regionRepository = RegionsRepositoryFactory.Create();
            _metricRepository = MetricTypeRepositoryFactory.Create();
            _areaRepository = AreaRepositoryFactory.Create();
            GetRegions(_regionRepository);
            GetAreas(_areaRepository);
            GetMetrics(_metricRepository);
        }

        public SignalSearchViewModel(IAreaRepository areaRepository, IRegionsRepository regionRepositry, IMetricTypeRepository metricRepository)
        {
            GetRegions(regionRepositry);
            GetAreas(areaRepository);
            GetMetrics(metricRepository);
        }

        //public List<Models.Signal> Signals { get; set; }       
        [Required]
        [Display(Name = "Signal ID")]
        public string SignalID { get; set; }

        public List<Region> Regions { get; set; }
        public int? SelectedRegionID { get; set; }
        public List<Area> Areas { get; set; }
        public int? SelectedAreaID { get; set; }

        public List<SelectListItem> MapMetricsList { get; set; }
        public List<string> ImageLocation { get; set; }

        public void GetMetrics(IMetricTypeRepository metricRepository)
        {
            //MetricTypeRepositoryFactory.SetMetricsRepository(new TestMetricTypeRepository());

            var metricTypes = metricRepository.GetAllToDisplayMetrics().OrderBy( m=> m.DisplayOrder);
            MapMetricsList = new List<SelectListItem>();
            foreach (var m in metricTypes)
                MapMetricsList.Add(new SelectListItem {Value = m.MetricID.ToString(), Text = m.ChartName});
        }

        public void GetRegions(IRegionsRepository regionRepository)
        {
            Regions = regionRepository.GetAllRegions();
        }

        public void GetAreas(IAreaRepository areaRepository)
        {
            Areas = areaRepository.GetAllAreas();
        }
    }
}