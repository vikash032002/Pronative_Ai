using Microsoft.AspNetCore.Mvc;
using OuterloopLabApi.Models;
using OuterloopLabApi.Services;
using OuterloopLabApi.Services.Exceptions;

namespace OuterloopLabApi.Controllers;

[ApiController]
[Route("api/[controller]", Name = "Conversions")]
public sealed class ConversionsController : ControllerBase
{
    private readonly ConversionService _service;
    private readonly ConversionAuditRepository _repository;

    public ConversionsController(ConversionService service, ConversionAuditRepository repository)
    {
        _service = service;
        _repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> Convert([FromBody] ConversionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.ConvertAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (CurrencyRateProviderUnavailableException)
        {
            return Problem(
                title: "Currency rate provider unavailable",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid conversion request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound();
        return Ok(item);
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "sourceCurrency")] string? sourceCurrency,
        [FromQuery(Name = "targetCurrency")] string? targetCurrency,
        [FromQuery(Name = "startUtc")] DateTime? startUtc,
        [FromQuery(Name = "endUtc")] DateTime? endUtc,
        [FromQuery(Name = "limit")] int? limit,
        CancellationToken cancellationToken)
    {
        var effectiveLimit = limit is null ? 50 : Math.Clamp(limit.Value, 1, 200);
        var items = await _repository.SearchAsync(sourceCurrency, targetCurrency, startUtc, endUtc, effectiveLimit, cancellationToken);
        return Ok(items);
    }
}
