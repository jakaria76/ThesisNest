using System.Collections.Generic;

namespace ThesisNest.Models.ViewModels
{
    public class ThesisIndexVm
    {
        public List<ThesisIndexItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; } = 0;
    }
}
