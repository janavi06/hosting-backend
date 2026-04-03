using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/print-jobs")]
public class PrintJobsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PrintJobsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/print-jobs
    [HttpGet]
    public async Task<IActionResult> GetJobs()
    {
        var jobs = await _context.PrintJobs
            .AsNoTracking() // ✅ performance optimization
            .Where(j => EF.Functions.ILike(j.Status, "pending")) // ✅ FIXED
            .OrderBy(j => j.CreatedAt)
            .Select(j => new
            {
                j.PrintJobID,
                j.RestaurantID,
                j.PayloadJson
            })
            .ToListAsync();

        return Ok(jobs);
    }

    // PUT: api/print-jobs/{id}/done
    [HttpPut("{id}/done")]
    public async Task<IActionResult> MarkDone(int id)
    {
        var job = await _context.PrintJobs.FindAsync(id);
        if (job == null)
            return NotFound();

        job.Status = "printed"; // ✅ normalized lowercase
        job.PrintedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Print job marked as done" });
    }
}