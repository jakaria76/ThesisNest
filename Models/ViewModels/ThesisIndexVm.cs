using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;


namespace ThesisNest.Models.ViewModels
{
    public class ThesisIndexVm
    {
        public int? Year { get; set; }
        public int? DepartmentId { get; set; }
        public ProposalStatus? ProposalStatus { get; set; }
        public string? Q { get; set; }
        public IEnumerable<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
        public List<ThesisListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1; public int PageSize { get; set; } = 10; public int TotalCount { get; set; }
    }


    public class ThesisListItemVm
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Department { get; set; } = "";
        public ProposalStatus ProposalStatus { get; set; }
        public int CurrentVersionNo { get; set; }
        public string CreatedAtStr { get; set; } = "";
    }
}
