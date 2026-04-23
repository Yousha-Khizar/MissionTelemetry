using Microsoft.AspNetCore.Mvc;
using MissionTelemetry.Api.Dtos;
using MissionTelemetry.Api.Repositories;
using MissionTelemetry.Core.Models;




namespace MissionTelemetry.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

public sealed class TelemetryController : ControllerBase
{
    private readonly ITelemetryRepository _repo;

    public TelemetryController(ITelemetryRepository repo) => _repo = repo;

    [HttpGet("latest")]
    public ActionResult<IEnumerable<TelemetryFrameDto>> GetLatest([FromQuery] int take = 100)
    {
        take = Math.Clamp(take, 1, 1000);
        var frames =_repo.GetLatest(take);
        var dtos = frames.Select(f => new TelemetryFrameDto
        {
            TimeStamp = f.TimeStamp,
            Values = f.Values
        });
        return Ok(dtos);
    }

    [HttpGet("range")]
    public ActionResult<TelemetryPageDto> GetRange([FromQuery] int skip=0, [FromQuery] int take = 20)
    {
        take = Math.Clamp(take, 1, 500);
        skip = Math.Max(skip, 0);

        long total = _repo.Count;
        var frames = _repo.GetRange(skip, take);

        var items = frames.Select(MapToDto).ToList();

        var page = new TelemetryPageDto
        {
            Total = total,
            Skip = skip,
            Take = take,
            Items = items
        };
        return Ok(page);
    }

    [HttpGet("count")]
    public ActionResult<long> GetCount() => Ok(_repo.Count);


    private static TelemetryFrameDto MapToDto(TelemetryFrame f)
    => new TelemetryFrameDto
    {
        Sequence = f.Sequence,
        TimeStamp = f.TimeStamp,
        ValueCount = f.Values?.Count ?? 0,
        Values = f.Values is null ? new Dictionary<string,double>()
                                  : new Dictionary<string,double>(f.Values)
    };

    [HttpDelete("clear")]
    public IActionResult Clear()
    {
        _repo.Clear();
        return NoContent();
    }

}

