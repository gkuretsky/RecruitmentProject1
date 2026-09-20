using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace RecruitmentProject.Pages
{
    public class SeatingChartGeneratorModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Please select a round.")]
        public string? SelectedRound { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Please select people.")]
        public string? SelectedPeopleCount { get; set; }

        public bool HasSubmitted { get; private set; }

        public List<SelectListItem> RoundOptions { get; } = new()
        {
            new SelectListItem("Philanthropy", "Philanthropy"),
            new SelectListItem("Sisterhood", "Sisterhood"),
            new SelectListItem("Preference", "Preference"),
        };

        public List<SelectListItem> PeopleCountOptions { get; } = new()
        {
            new SelectListItem("Actives", "Actives"),
            new SelectListItem("Celebrities", "Celebrities"),
        };

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            HasSubmitted = true;
            return Page();
        }
    }
}
