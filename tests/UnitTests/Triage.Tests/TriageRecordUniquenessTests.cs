using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Triage.Domain;
using Triage.Infrastructure;
using Xunit;

namespace Triage.Tests;

/// <summary>
/// Proves the database-level constraint that actually closes the redelivered-TicketCreated race
/// (see docs/concurrency/001-redelivered-ticket-created-race.md) is real, not just declared — a
/// filtered EF Core index configuration is easy to get subtly wrong (wrong column name in the SQL
/// filter string, wrong provider syntax) and this uses SQLite's own enforcement to catch that
/// class of mistake, since the InMemory provider used elsewhere in this repo doesn't enforce
/// unique indexes at all.
/// </summary>
public sealed class TriageRecordUniquenessTests
{
    private static (TriageDbContext DbContext, SqliteConnection Connection) CreateSqliteContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var dbContext = new TriageDbContext(new DbContextOptionsBuilder<TriageDbContext>()
            .UseSqlite(connection)
            .Options);
        dbContext.Database.EnsureCreated();
        return (dbContext, connection);
    }

    [Fact]
    public async Task A_second_succeeded_triage_record_for_the_same_ticket_violates_the_unique_index()
    {
        var (dbContext, connection) = CreateSqliteContext();
        await using var _ = connection;
        await using var __ = dbContext;

        var ticketId = Guid.NewGuid();
        var first = TriageRecord.CreateSucceeded(
            ticketId, "billing", "high", "summary one", "draft one", "local", false, "customer@example.com");
        dbContext.TriageRecords.Add(first);
        await dbContext.SaveChangesAsync();

        var second = TriageRecord.CreateSucceeded(
            ticketId, "billing", "high", "summary two", "draft two", "local", false, "customer@example.com");
        dbContext.TriageRecords.Add(second);

        var act = () => dbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>("the unique index on (TicketId, Succeeded) must reject a second succeeded record for the same ticket");
    }

    [Fact]
    public async Task A_failed_record_for_a_ticket_does_not_block_a_later_succeeded_record()
    {
        var (dbContext, connection) = CreateSqliteContext();
        await using var _ = connection;
        await using var __ = dbContext;

        var ticketId = Guid.NewGuid();
        dbContext.TriageRecords.Add(TriageRecord.CreateFailed(ticketId, "ollama unreachable"));
        await dbContext.SaveChangesAsync();

        dbContext.TriageRecords.Add(TriageRecord.CreateSucceeded(
            ticketId, "billing", "high", "summary", "draft", "local", false, "customer@example.com"));

        var act = () => dbContext.SaveChangesAsync();

        await act.Should().NotThrowAsync("a failed attempt must not block a later successful retry for the same ticket");
    }

    [Fact]
    public async Task Two_different_tickets_can_each_have_their_own_succeeded_record()
    {
        var (dbContext, connection) = CreateSqliteContext();
        await using var _ = connection;
        await using var __ = dbContext;

        dbContext.TriageRecords.Add(TriageRecord.CreateSucceeded(
            Guid.NewGuid(), "billing", "high", "summary", "draft", "local", false, "customer@example.com"));
        dbContext.TriageRecords.Add(TriageRecord.CreateSucceeded(
            Guid.NewGuid(), "technical", "low", "summary", "draft", "local", false, "customer@example.com"));

        var act = () => dbContext.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }
}
