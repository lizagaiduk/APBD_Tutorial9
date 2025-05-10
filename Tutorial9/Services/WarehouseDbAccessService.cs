using Tutorial9.Model;

namespace Tutorial9.Services;

using Microsoft.Data.SqlClient;

public class WarehouseDbAccessService
{
    private readonly string _connectionString;
    
    public WarehouseDbAccessService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }
    public async Task<SqlConnection> OpenConnectionAsync()
    {
        var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }
    public async Task<bool> ProductExistsAsync(int id, SqlConnection conn, SqlTransaction tran)
    {
        var cmd = new SqlCommand("SELECT 1 FROM Product WHERE IdProduct = @id", conn, tran);
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteScalarAsync() is not null;
    }
    public async Task<bool> WarehouseExistsAsync(int id, SqlConnection conn, SqlTransaction tran)
    {
        var cmd = new SqlCommand("SELECT 1 FROM Warehouse WHERE IdWarehouse = @id", conn, tran);
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteScalarAsync() is not null;
    }
    public async Task<int?> GetOrderIdAsync(int productId, int amount, DateTime createdAt, SqlConnection conn, SqlTransaction tran)
    {
        var cmd = new SqlCommand("""
            SELECT IdOrder FROM [Order]
            WHERE IdProduct = @p AND Amount = @a AND CreatedAt < @c
        """, conn, tran);
        cmd.Parameters.AddWithValue("@p", productId);
        cmd.Parameters.AddWithValue("@a", amount);
        cmd.Parameters.AddWithValue("@c", createdAt);
        var result = await cmd.ExecuteScalarAsync();
        return result is null ? null : (int)result;
    }
    public async Task<bool> IsOrderFulfilledAsync(int orderId, SqlConnection conn, SqlTransaction tran)
    {
        var cmd = new SqlCommand("SELECT 1 FROM Product_Warehouse WHERE IdOrder = @id", conn, tran);
        cmd.Parameters.AddWithValue("@id", orderId);
        return await cmd.ExecuteScalarAsync() is not null;
    }
    public async Task UpdateOrderAsFulfilledAsync(int orderId, SqlConnection conn, SqlTransaction tran)
    {
        var cmd = new SqlCommand("UPDATE [Order] SET FulfilledAt = GETDATE() WHERE IdOrder = @id", conn, tran);
        cmd.Parameters.AddWithValue("@id", orderId);
        await cmd.ExecuteNonQueryAsync();
    }
    public async Task<decimal> GetProductPriceAsync(int productId, SqlConnection conn, SqlTransaction tran)
    {
        var cmd = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @id", conn, tran);
        cmd.Parameters.AddWithValue("@id", productId);
        return (decimal)(await cmd.ExecuteScalarAsync()!);
    }
    public async Task<int> InsertProductWarehouseAsync(int productId, int warehouseId, int orderId, int amount, decimal price, SqlConnection conn, SqlTransaction tran)
    {
        var cmd = new SqlCommand("""
            INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
            VALUES (@w, @p, @o, @a, @price, GETDATE());
            SELECT SCOPE_IDENTITY();
        """, conn, tran);
        cmd.Parameters.AddWithValue("@w", warehouseId);
        cmd.Parameters.AddWithValue("@p", productId);
        cmd.Parameters.AddWithValue("@o", orderId);
        cmd.Parameters.AddWithValue("@a", amount);
        cmd.Parameters.AddWithValue("@price", price);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
    public async Task<int> CallProcedureAsync(ProductWarehouseRequestDto dto, SqlConnection conn)
    {
        var cmd = new SqlCommand("AddProductToWarehouse", conn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
        cmd.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
        cmd.Parameters.AddWithValue("@Amount", dto.Amount);
        cmd.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
}
  