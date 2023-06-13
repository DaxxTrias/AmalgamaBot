using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AmalgamaBot.Models;

public class Database
{
    public class DougBotContext : DbContext
    {
        public DbSet<Guild>? Guilds { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var accountEndpoint = Environment.GetEnvironmentVariable("ACCOUNT_ENDPOINT");
            var accountKey = Environment.GetEnvironmentVariable("ACCOUNT_KEY");
            var databaseName = Environment.GetEnvironmentVariable("DATABASE_NAME");

            if (string.IsNullOrEmpty(accountEndpoint) || string.IsNullOrEmpty(accountKey) || string.IsNullOrEmpty(databaseName))
            {
                throw new InvalidOperationException("Environment variables for the Azure Cosmos DB connection are not set correctly.");
            }

            optionsBuilder.UseCosmos(accountEndpoint, accountKey, databaseName);
            //optionsBuilder
            //.UseCosmos(accountEndpoint, accountKey, databaseName)
            //.EnableSensitiveDataLogging()
            //.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Guild>()
                .ToContainer("Guilds")
                .HasPartitionKey(e => e.Id);
            modelBuilder.Entity<Guild>().OwnsMany(p => p.YoutubeSettings);

            //output results to console
            Console.WriteLine(modelBuilder.Model.ToString());
        }
    }
}