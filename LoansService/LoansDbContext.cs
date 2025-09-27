using Microsoft.EntityFrameworkCore;

public class LoansDbContext : DbContext
{
    public LoansDbContext(DbContextOptions<LoansDbContext> options) : base(options)
    {

    }
   public DbSet<Loan> Loans { get; set; }
}
