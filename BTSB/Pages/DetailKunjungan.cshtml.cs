using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BTSB.Data;
using BTSB.Models;

namespace BTSB.Pages
{
    public class DetailKunjunganModel : AuthPageModel
    {
        private readonly AppDbContext _context;

        public DetailKunjunganModel(AppDbContext context)
        {
            _context = context;
        }

        public Kunjungan? Kunjungan { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToPage("/Login");
            }

            Kunjungan = await _context.Kunjungan
                .FirstOrDefaultAsync(k => k.Id == id);

            if (Kunjungan == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}