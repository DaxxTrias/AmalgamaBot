using Microsoft.EntityFrameworkCore;

namespace AmalgamaBot.Models;

public class Database
{
    public class DougBotContext : DbContext
    {
        public DbSet<Guild> Guilds { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var accountEndpoint = Environment.GetEnvironmentVariable("ACCOUNT_ENDPOINT");
            var accountKey = Environment.GetEnvironmentVariable("ACCOUNT_KEY");
            var databaseName = Environment.GetEnvironmentVariable("DATABASE_NAME");

            if (string.IsNullOrEmpty(accountEndpoint) || string.IsNullOrEmpty(accountKey) || string.IsNullOrEmpty(databaseName))
            {
                throw new InvalidOperationException("Environment variables for the Azure Cosmos DB connection are not set correctly.");
            }

            //todo: check if database exists, if they dont create one or at least warn the user clearly
            optionsBuilder.UseCosmos(accountEndpoint, accountKey, databaseName);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Guild>()
                .ToContainer("Guilds")
                .HasPartitionKey(e => e.Id);
            modelBuilder.Entity<Guild>().OwnsMany(p => p.YoutubeSettings);
        }
    }
}