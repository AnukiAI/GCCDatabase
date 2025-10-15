using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GCCWebApp.Data
{
    /// <summary>
    /// Design-time factory for Entity Framework tooling (migrations, database commands)
    /// This forces SQL Server provider for script generation
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Use SQL Server for design-time operations (migrations, database commands)
            optionsBuilder.UseSqlServer("Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
