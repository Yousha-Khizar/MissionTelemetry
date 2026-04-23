using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissionTelemetry.Persistence;               // MissionDbContext
using MissionTelemetry.Persistence.Entities;      // TelemetrySample

namespace MissionTelemetry.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StorageController : ControllerBase
{
    // 
    private readonly MissionDbContext _db;

   
    public StorageController(MissionDbContext db) => _db = db;

    
    [HttpGet("samples")]
    public async Task<ActionResult<IEnumerable<TelemetrySample>>> GetSamples(
        [FromQuery] string key,
        [FromQuery] int take = 20)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest("Query parameter 'key' is required.");

        take = Math.Clamp(take, 1, 500);

        
        var data = await _db.TelemetrySamples
            .AsNoTracking()
            .Where(s => s.Key == key)
            .OrderByDescending(s => s.TimeStamp)
            .Take(take)
            .ToListAsync();

        return Ok(data);
    }

    // GET /api/storage/timestamps?take=10
    [HttpGet("timestamps")]
    public async Task<ActionResult<IEnumerable<DateTime>>> GetTimestamps([FromQuery] int take = 10)
    {
        take = Math.Clamp(take, 1, 1000);

        var stamps = await _db.TelemetrySamples
            .AsNoTracking()
            .OrderByDescending(s => s.TimeStamp)
            .Select(s => s.TimeStamp)
            .Distinct()
            .Take(take)
            .ToListAsync();

        return Ok(stamps);
    }

    // GET /api/storage/stats
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetStats()
    {
        var total = await _db.TelemetrySamples.AsNoTracking().LongCountAsync();
        var keys = await _db.TelemetrySamples.AsNoTracking()
                         .Select(s => s.Key)
                         .Distinct()
                         .CountAsync();

        return Ok(new { totalSamples = total, distinctKeys = keys });
    }
}
