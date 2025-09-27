My Payoneer Projects on Azure 

Microservices demo project with .NET 8: 

• BooksService → deployed to Azure App Service 

• LoansService → runs locally and consumes BooksService

Features:

• ASP.NET Core Minimal APIs
• EF Core (SQL Server / SQLite)
• Azure App Service deployment
• Secure config via User Secrets & Azure App Settings 

Run locally:
dotnet run 

Endpoints:
• /api/books
• /api/loans

API Examples:

BooksService
- Get all books
  GET /api/books
- Get book by id
  GET /api/books/{id}
- Add new book
  POST /api/books
{
  "title": "Harry Potter",
  "isAvailable": true
}

LoansService
- Loan a book
  POST /api/loans
{
  "bookId": 3
}
- Return a book
  PUT /api/loans/{id}/return
