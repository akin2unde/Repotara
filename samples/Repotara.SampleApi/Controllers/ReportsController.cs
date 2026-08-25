using Microsoft.AspNetCore.Mvc;
using Repotara;
using Repotara.Definition;
using Repotara.Output;
using Repotara.SampleApi.Examples;
using Repotara.SampleApi.Models;

namespace Repotara.SampleApi.Controllers;

/// <summary>
/// Demonstrates every key Repotara capability: run an arbitrary
/// ReportDefinition posted by a frontend, or fetch one of the named examples
/// to see its exact JSON shape.
/// </summary>
[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ReportEngine _reportEngine;

    public ReportsController(ReportEngine reportEngine)
    {
        _reportEngine = reportEngine;
    }

    /// <summary>
    /// Runs any ReportDefinition against Order/Customer/Region and returns the
    /// result in the requested format. This is the endpoint a real frontend
    /// report designer would call. Invalid definitions come back as a
    /// structured 400 instead of an unhandled exception.
    /// </summary>
    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] ReportDefinition definition, [FromQuery] OutputFormat format = OutputFormat.Json)
    {
        try
        {
            var result = await _reportEngine.ExecuteAsync(definition, format);
            return Content(result.Content, result.ContentType);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "Invalid report definition.", detail = ex.Message });
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { error = "Unsupported operation in report definition.", detail = ex.Message });
        }
    }

    /// <summary>Lists the names of every available example, for discovery.</summary>
    [HttpGet("examples")]
    public IActionResult ListExamples()
    {
        return Ok(new[]
        {
            "basic", "joined", "left-join", "grouped-aggregate", "aggregate-showcase",
            "filter-and-or", "filter-in", "filter-contains", "having",
            "sort-pagination", "concat", "relative-date", "column-to-column",
            "html-template", "chart", "invalid"
        });
    }

    /// <summary>Returns the raw JSON shape of a named example, without executing it.</summary>
    [HttpGet("examples/{name}")]
    public IActionResult GetExample(string name)
    {
        var definition = ResolveExample(name);
        return definition == null ? NotFound() : Ok(definition);
    }

    /// <summary>
    /// Executes a named example directly against the configured database.
    /// Try "invalid" with any format to see the structured validation error response.
    /// </summary>
    [HttpPost("examples/{name}/run")]
    public async Task<IActionResult> RunExample(string name, [FromQuery] OutputFormat format = OutputFormat.Json)
    {
        var definition = ResolveExample(name);
        if (definition == null)
        {
            return NotFound();
        }

        try
        {
            var result = await _reportEngine.ExecuteAsync(definition, format);
            return Content(result.Content, result.ContentType);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "Invalid report definition.", detail = ex.Message });
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { error = "Unsupported operation in report definition.", detail = ex.Message });
        }
    }

    private static ReportDefinition? ResolveExample(string name)
    {
        return name switch
        {
            "basic" => ExampleReportDefinitions.Basic(),
            "joined" => ExampleReportDefinitions.Joined(),
            "left-join" => ExampleReportDefinitions.LeftJoinExample(),
            "grouped-aggregate" => ExampleReportDefinitions.GroupedAggregate(),
            "aggregate-showcase" => ExampleReportDefinitions.AggregateShowcase(),
            "filter-and-or" => ExampleReportDefinitions.FilterAndOr(),
            "filter-in" => ExampleReportDefinitions.FilterInExample(),
            "filter-contains" => ExampleReportDefinitions.FilterContainsExample(),
            "having" => ExampleReportDefinitions.HavingExample(),
            "sort-pagination" => ExampleReportDefinitions.SortAndPagination(),
            "concat" => ExampleReportDefinitions.ConcatExample(),
            "relative-date" => ExampleReportDefinitions.RelativeDateExample(),
            "column-to-column" => ExampleReportDefinitions.ColumnToColumnExample(),
            "html-template" => ExampleReportDefinitions.HtmlTemplateExample(),
            "chart" => ExampleReportDefinitions.ChartExample(),
            "invalid" => ExampleReportDefinitions.InvalidExample(),
            _ => null
        };
    }
}
