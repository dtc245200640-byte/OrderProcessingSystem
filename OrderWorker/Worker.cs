using Microsoft.Data.SqlClient;

namespace OrderWorker;

public class Worker(
    ILogger<Worker> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation("OrderWorker da khoi dong!");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var orderId = await GetNextOrderAsync(stoppingToken);

                if (orderId == null)
                {
                    await Task.Delay(3000, stoppingToken);
                    continue;
                }

                logger.LogInformation(
                    "Dang xu ly don hang: {OrderId}", orderId);

                await Task.Delay(2000, stoppingToken);

                await UpdateOrderStatusAsync(
                    orderId.Value,
                    "Completed",
                    stoppingToken);

                logger.LogInformation(
                    "Don hang {OrderId} da xu ly thanh cong!",
                    orderId);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Loi xu ly don hang!");

                await Task.Delay(3000, stoppingToken);
            }
        }
    }

    private async Task<int?> GetNextOrderAsync(
        CancellationToken cancellationToken)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        await using var connection =
            new SqlConnection(connectionString);

        await connection.OpenAsync(cancellationToken);

        var sql = @"
            UPDATE TOP (1) dbo.Orders
            SET Status = 'Processing',
                UpdatedAt = SYSUTCDATETIME()
            OUTPUT INSERTED.OrderId
            WHERE OrderId = (
                SELECT TOP (1) OrderId
                FROM dbo.Orders WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE Status = 'Pending'
                ORDER BY CreatedAt
            );";

        await using var command =
            new SqlCommand(sql, connection);

        var result = await command.ExecuteScalarAsync(
            cancellationToken);

        return result == null || result == DBNull.Value
            ? null
            : Convert.ToInt32(result);
    }

    private async Task UpdateOrderStatusAsync(
        int orderId,
        string status,
        CancellationToken cancellationToken)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        await using var connection =
            new SqlConnection(connectionString);

        await connection.OpenAsync(cancellationToken);

        var sql = @"
            UPDATE dbo.Orders
            SET Status = @Status,
                UpdatedAt = SYSUTCDATETIME()
            WHERE OrderId = @OrderId;";

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@OrderId", orderId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}