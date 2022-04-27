using MOE.Common.Models.Repositories;
using MOE.Common.Models.ViewModel.Chart;

namespace MOE.Common.Models.ViewModel.WebConfigTool
{
    public class WebConfigToolViewModel
    {
        public WebConfigToolViewModel()
        {
            SignalSearch = new SignalSearchViewModel();
        }

        public WebConfigToolViewModel(IAreaRepository areaRepository, IRegionsRepository regionRepositry, IMetricTypeRepository metricRepository)
        {
            SignalSearch = new SignalSearchViewModel(areaRepository, regionRepositry, metricRepository);
        }

        public SignalSearchViewModel SignalSearch { get; set; }
    }
}