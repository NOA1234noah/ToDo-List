using Microsoft.EntityFrameworkCore;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

// הוספת CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();;
    });
});

// הוספת שירותי Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// הזרקת הקשר למסד הנתונים
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("ToDoDB"),
    new MySqlServerVersion(new Version(8, 0, 21))));

var app = builder.Build();

// הגדרת Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API V1");
        c.RoutePrefix = string.Empty;
    });
}

// שימוש ב-CORS
app.UseCors("AllowAll");

// שליפת כל המשימות
app.MapGet("/todos", async (ToDoDbContext db) =>
    await db.Items.ToListAsync());

// הוספת משימה חדשה
app.MapPost("/todos", async (ToDoDbContext db, Item newItem) =>
{
    if (string.IsNullOrEmpty(newItem.Name))
        return Results.BadRequest("Task name is required.");
    newItem.IsComplete=false;
    db.Items.Add(newItem);
    await db.SaveChangesAsync();
    return Results.Created($"/todos/{newItem.IdItems}", newItem);
});

// עדכון משימה
app.MapPut("/todos/{IdItems}", async (ToDoDbContext db, int IdItems, Item updatedItem) =>
{
    var existingItem = await db.Items.FindAsync(IdItems);
    if (existingItem is null) return Results.NotFound();

    if (string.IsNullOrEmpty(updatedItem.Name))
        return Results.BadRequest("Task name is required.");

    existingItem.Name = updatedItem.Name;
    existingItem.IsComplete = updatedItem.IsComplete;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

// מחיקת משימה
app.MapDelete("/todos/{IdItems}", async (ToDoDbContext db, int IdItems) =>
{
    var item = await db.Items.FindAsync(IdItems);
    if (item is null) return Results.NotFound();

    db.Items.Remove(item);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
