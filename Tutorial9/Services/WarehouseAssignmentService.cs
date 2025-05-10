using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class WarehouseAssignmentService
{
    private readonly WarehouseDbAccessService _db;

    public WarehouseAssignmentService(WarehouseDbAccessService db)
    {
        _db = db;
    }
    public async Task<int> HandleManualAsync(ProductWarehouseRequestDto dto)
    {
        if (dto.Amount <= 0)
            throw new Exception("Invalid amount");
        await using var conn = await _db.OpenConnectionAsync();
        await using var tran = await conn.BeginTransactionAsync();
        try
        {
            if (!await _db.ProductExistsAsync(dto.IdProduct, conn, (SqlTransaction)tran))
                throw new Exception("Product not found");
            if (!await _db.WarehouseExistsAsync(dto.IdWarehouse, conn, (SqlTransaction)tran))
                throw new Exception("Warehouse not found");
            var orderId = await _db.GetOrderIdAsync(dto.IdProduct, dto.Amount, dto.CreatedAt, conn, (SqlTransaction)tran);
            if (orderId == null)
                throw new Exception("Order not found");
            if (await _db.IsOrderFulfilledAsync(orderId.Value, conn, (SqlTransaction)tran))
                throw new Exception("Order already fulfilled");

            await _db.UpdateOrderAsFulfilledAsync(orderId.Value, conn, (SqlTransaction)tran);
            var price = await _db.GetProductPriceAsync(dto.IdProduct, conn, (SqlTransaction)tran);
            var id = await _db.InsertProductWarehouseAsync(dto.IdProduct, dto.IdWarehouse, orderId.Value, dto.Amount, price * dto.Amount, conn, (SqlTransaction)tran);
            await tran.CommitAsync();
            return id;
        }
        catch
        {
            await tran.RollbackAsync();
            throw;
        }
    }
    public async Task<int> HandleWithProcedureAsync(ProductWarehouseRequestDto dto)
    {
        if (dto.Amount <= 0)
            throw new Exception("Invalid amount");
        await using var conn = await _db.OpenConnectionAsync();
        var productExists = await _db.ProductExistsAsync(dto.IdProduct, conn, null);
        var warehouseExists = await _db.WarehouseExistsAsync(dto.IdWarehouse, conn, null);

        if (!productExists) throw new Exception("Product not found");
        if (!warehouseExists) throw new Exception("Warehouse not found");
        return await _db.CallProcedureAsync(dto, conn);
    }
}