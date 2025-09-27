global using Microsoft.EntityFrameworkCore;
using LoansService;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add the DbContext service
builder.Services.AddDbContext<LoansDbContext>(options =>
{
    options.UseSqlite($"Data Source = loans.db");
});
builder.Services.AddHttpClient();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Handles GET all loans
app.MapGet("/api/loans", (LoansDbContext db) =>
{
    return db.Loans.ToList();
});

// Handles GET for a specific loan by ID
app.MapGet("/api/loans/{id}", async (int id, LoansDbContext db) =>
{
    var loan = await db.Loans.FindAsync(id);
    return loan is null ? Results.NotFound() : Results.Ok(loan);
});

//Handles POST 
app.MapPost("/api/loans", async (Loan loan, LoansDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration) =>
{
    if (loan.LoanDate == default)
        loan.LoanDate = DateTime.Now;
    //Validate the book's existence and availability using BooksService
    var httpClient = httpClientFactory.CreateClient();
    var booksServiceUrl = configuration["BooksService:BaseUrl"];
    var BookResponse = await httpClient.GetAsync($"{booksServiceUrl}/{loan.BookId}");
    if (!BookResponse.IsSuccessStatusCode)
    {
        return Results.BadRequest("Invalid Book ID");
    }
    var book = await BookResponse.Content.ReadFromJsonAsync<Book>();
    if (book.IsAvailable == false)
    {
        return Results.BadRequest("Book is not available for loan");
    }

    // Save the loan to database
    db.Loans.Add(loan);
    await db.SaveChangesAsync();

    //Update the book's status to unavailable in the BookService
    book.IsAvailable = false;
    await httpClient.PutAsJsonAsync($"{booksServiceUrl}/{book.Id}", book);

    return Results.Created($"/api/loans/{loan.Id}", loan);
});

// Handles PUT to return a book
app.MapPut("/api/loans/{id}/return", async (int id, LoansDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration) =>
{
    var loan = await db.Loans.FindAsync(id);
    if (loan is null)
    {
        return Results.NotFound();
    }

    // Update the book's status to available in the BooksService
    var httpClient = httpClientFactory.CreateClient();
    var booksServiceUrl = configuration["BooksService:BaseUrl"];
    var bookResponse = await httpClient.GetAsync($"{booksServiceUrl}/{loan.BookId}");
    
    if (!bookResponse.IsSuccessStatusCode)
    {
        return Results.Problem("Failed to retrieve book information from BooksService.", statusCode: 500);
    }

    var book = await bookResponse.Content.ReadFromJsonAsync<Book>();
    book.IsAvailable = true;
    await httpClient.PutAsJsonAsync($"{booksServiceUrl}/{book.Id}", book);   
    
    // Update the loan in the LoansService
    loan.ReturnDate = DateTime.Now;
    await db.SaveChangesAsync();

    return Results.Ok(loan);
});

// Handles DELETE 
app.MapDelete("/api/loans/{id}", async (int id, LoansDbContext db) =>
{
    var loan = await db.Loans.FindAsync(id);
    if (loan is null)
    {
        return Results.NotFound();
    }
    db.Loans.Remove(loan);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
