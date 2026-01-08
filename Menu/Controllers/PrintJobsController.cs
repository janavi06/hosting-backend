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

    [HttpGet]
    public async Task<IActionResult> GetJobs()
    {
        var jobs = await _context.PrintJobs
            .Where(j => j.Status == "PENDING")
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


    [HttpPut("{id}/done")]
    public async Task<IActionResult> MarkDone(int id)
    {
        var job = await _context.PrintJobs.FindAsync(id);
        if (job == null) return NotFound();

        job.Status = "PRINTED";
        job.PrintedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok();
    }
}
