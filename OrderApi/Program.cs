
using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Models;
using OrderApi.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Kết nối SQL Server
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Kiểm tra kết nối Database
app.MapGet("/db-check", async (OrderDbContext db) =>
{
    var connected = await db.Database.CanConnectAsync();

    return Results.Ok(new
    {
        Success = connected,
        Message = connected
            ? "Ket noi SQL Server thanh cong!"
            : "Khong the ket noi SQL Server!"
    });
});

// API lấy danh sách đơn hàng
app.MapGet("/orders", async (OrderDbContext db) =>
{
    var orders = await db.Orders
        .AsNoTracking()
        .Include(o => o.Customer)
        .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
        .OrderByDescending(o => o.CreatedAt)
        .Select(o => new
        {
            o.OrderId,
            o.CustomerId,
            CustomerName = o.Customer.FullName,
            o.Status,
            o.CreatedAt,
            o.UpdatedAt,
            Items = o.OrderItems.Select(i => new
            {
                i.ProductId,
                ProductName = i.Product.ProductName,
                i.Quantity,
                i.UnitPrice,
                Total = i.Quantity * i.UnitPrice
            })
        })
        .ToListAsync();

    return Results.Ok(orders);
});

// API lấy chi tiết một đơn hàng
app.MapGet("/orders/{id:int}", async (int id, OrderDbContext db) =>
{
    var order = await db.Orders
        .AsNoTracking()
        .Include(o => o.Customer)
        .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
        .Where(o => o.OrderId == id)
        .Select(o => new
        {
            o.OrderId,
            o.CustomerId,
            CustomerName = o.Customer.FullName,
            o.Status,
            o.CreatedAt,
            o.UpdatedAt,
            Items = o.OrderItems.Select(i => new
            {
                i.ProductId,
                ProductName = i.Product.ProductName,
                i.Quantity,
                i.UnitPrice,
                Total = i.Quantity * i.UnitPrice
            }),
            TotalAmount = o.OrderItems.Sum(i =>
                i.Quantity * i.UnitPrice)
        })
        .FirstOrDefaultAsync();

    return order == null
        ? Results.NotFound(new { Message = "Khong tim thay don hang!" })
        : Results.Ok(order);
})
.WithName("GetOrderById");

// API tạo đơn hàng
app.MapPost("/orders", async (
    CreateOrderRequest request,
    OrderDbContext db) =>
{
    // Kiểm tra dữ liệu đầu vào
    if (request.CustomerId <= 0)
    {
        return Results.BadRequest(new
        {
            Message = "CustomerId khong hop le!"
        });
    }

    if (request.Items == null || request.Items.Count == 0)
    {
        return Results.BadRequest(new
        {
            Message = "Don hang phai co it nhat mot san pham!"
        });
    }

    if (request.Items.Any(i =>
        i.ProductId <= 0 || i.Quantity <= 0 || i.Quantity > 10000))
    {
        return Results.BadRequest(new
        {
            Message = "Ma san pham hoac so luong khong hop le!"
        });
    }

    if (request.Items.Select(i => i.ProductId).Distinct().Count()
        != request.Items.Count)
    {
        return Results.BadRequest(new
        {
            Message = "Khong duoc lap lai san pham trong danh sach!"
        });
    }

    // Kiểm tra khách hàng
    var customer = await db.Customers
        .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId);

    if (customer == null)
    {
        return Results.NotFound(new
        {
            Message = "Khong tim thay khach hang!"
        });
    }

    // Lấy danh sách sản phẩm
    var productIds = request.Items.Select(i => i.ProductId).ToList();

    var products = await db.Products
        .Where(p => productIds.Contains(p.ProductId))
        .ToListAsync();

    if (products.Count != productIds.Count)
    {
        return Results.BadRequest(new
        {
            Message = "Co san pham khong ton tai!"
        });
    }

    // Kiểm tra tồn kho
    foreach (var item in request.Items)
    {
        var product = products.First(p =>
            p.ProductId == item.ProductId);

        if (product.Stock < item.Quantity)
        {
            return Results.BadRequest(new
            {
                Message = $"San pham {product.ProductName} khong du hang!"
            });
        }
    }

    // Tạo đơn hàng
    var order = new Order
    {
        CustomerId = request.CustomerId,
        Status = "Pending",
        CreatedAt = DateTime.UtcNow
    };

    foreach (var item in request.Items)
    {
        var product = products.First(p =>
            p.ProductId == item.ProductId);

        order.OrderItems.Add(new OrderItem
        {
            ProductId = product.ProductId,
            Quantity = item.Quantity,
            UnitPrice = product.UnitPrice
        });
    }

    // Lưu vào SQL Server
    db.Orders.Add(order);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/orders/{order.OrderId}",
        new
        {
            Message = "Tao don hang thanh cong!",
            order.OrderId,
            order.CustomerId,
            order.Status,
            order.CreatedAt,
            Items = order.OrderItems.Select(i => new
            {
                i.ProductId,
                i.Quantity,
                i.UnitPrice
            }),
            TotalAmount = order.OrderItems.Sum(i =>
                i.Quantity * i.UnitPrice)
        });
});

// OpenAPI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
