using Npgsql;

namespace SupportAgent.Api.Services;

public class CustomerDataService
{
    private readonly string _connectionString;

    public CustomerDataService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not configured.");
    }

    public async Task<object?> GetOrderStatusAsync(int orderId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"SELECT o.order_id, o.status, o.order_date, o.total_amount, o.shipping_address,
                     c.first_name, c.last_name, c.email
              FROM orders o
              JOIN customers c ON c.customer_id = o.customer_id
              WHERE o.order_id = @id", conn);
        cmd.Parameters.AddWithValue("id", orderId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new
        {
            OrderId = reader.GetInt32(0),
            Status = reader.GetString(1),
            OrderDate = reader.GetDateTime(2),
            TotalAmount = reader.GetDecimal(3),
            ShippingAddress = reader.IsDBNull(4) ? null : reader.GetString(4),
            CustomerName = $"{reader.GetString(5)} {reader.GetString(6)}",
            Email = reader.GetString(7)
        };
    }

    public async Task<object?> GetCustomerOrdersAsync(int customerId)
    {
        var results = new List<object>();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"SELECT order_id, order_date, status, total_amount
              FROM orders WHERE customer_id = @id
              ORDER BY order_date DESC LIMIT 10", conn);
        cmd.Parameters.AddWithValue("id", customerId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new
            {
                OrderId = reader.GetInt32(0),
                OrderDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                TotalAmount = reader.GetDecimal(3)
            });
        }
        return results;
    }

    public async Task<object?> GetProductAsync(string productName)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"SELECT product_id, name, description, price, stock_quantity
              FROM products
              WHERE name ILIKE @n LIMIT 1", conn);
        cmd.Parameters.AddWithValue("n", $"%{productName}%");

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new
        {
            ProductId = reader.GetInt32(0),
            Name = reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            Price = reader.GetDecimal(3),
            StockQuantity = reader.GetInt32(4)
        };
    }
}