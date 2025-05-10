using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[ApiController]
[Route("api/warehouse")]
public class WarehouseController : ControllerBase
{
    private readonly WarehouseAssignmentService _service;

    public WarehouseController(WarehouseAssignmentService service)
    {
        _service = service;
    }
    [HttpPost]
    public async Task<IActionResult> AddManually([FromBody] ProductWarehouseRequestDto request)
    {
        try
        {
            var id = await _service.HandleManualAsync(request);
            return Ok(id);
        }
        catch (Exception ex)
        {
            return ex.Message switch
            {
                "Invalid amount" => BadRequest(ex.Message),
                "Product not found" => NotFound(ex.Message),
                "Warehouse not found" => NotFound(ex.Message),
                "Order not found" => BadRequest(ex.Message),
                "Order already fulfilled" => Conflict(ex.Message),
                _ => StatusCode(500, ex.Message)
            };
        }
    }
    [HttpPost("procedure")]
    public async Task<IActionResult> AddWithProcedure([FromBody] ProductWarehouseRequestDto request)
    {
        try
        {
            var id = await _service.HandleWithProcedureAsync(request);
            return Ok(id);
        }
        catch (SqlException ex) when (ex.Number == 50001)
        {
            return BadRequest("Order not found");
        }
        catch (SqlException ex) when (ex.Number == 50002)
        {
            return Conflict("Order already fulfilled");
        }
        catch (Exception ex)
        {
            return ex.Message switch
            {
                "Invalid amount" => BadRequest(ex.Message),
                "Product not found" => NotFound(ex.Message),
                "Warehouse not found" => NotFound(ex.Message),
                _ => StatusCode(500, ex.Message)
            };
        }
    }
}