using BbfFirmasPoderes.Domain.Audit;
using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Domain.Enums;
using BbfFirmasPoderes.Infrastructure.Persistence;
using BbfFirmasPoderes.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace BbfFirmasPoderes.Tests;

public class AppendOnlyAuditInterceptorTests
{
    [Fact]
    public async Task SaveChanges_CommandInsert_AppendsAuditEvent()
    {
        await using var db = CreateContext(new StubAuditContext("corr_cmd", "operador-1"));

        db.Documents.Add(NewDocument("doc_wf05"));
        await db.SaveChangesAsync();

        var audit = Assert.Single(db.AuditEvents.Local, e => e.Type == AuditEventTypes.CommandPersisted);
        Assert.Equal("corr_cmd", audit.CorrelationId);
        Assert.Equal("operador-1", audit.Actor);
        Assert.Equal("doc_wf05", audit.DocumentId);
        Assert.Contains("Added Document", audit.Details);
    }

    [Fact]
    public async Task SaveChanges_AuditEventUpdate_Throws()
    {
        await using var db = CreateContext(new StubAuditContext("corr_ro", "auditor"));

        var evt = new AuditEvent
        {
            EventId = "evt_locked",
            CorrelationId = "corr_ro",
            Type = "seed",
            Actor = "system",
            OccurredAt = DateTimeOffset.UtcNow,
            Details = "ok"
        };
        db.AuditEvents.Add(evt);
        await db.SaveChangesAsync();

        evt.Details = "tamper";

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Contains("append-only", ex.Message);
    }

    [Fact]
    public async Task SaveChanges_AuditEventDelete_Throws()
    {
        await using var db = CreateContext(new StubAuditContext("corr_del", "auditor"));

        var evt = new AuditEvent
        {
            EventId = "evt_delete_me",
            CorrelationId = "corr_del",
            Type = "seed",
            Actor = "system",
            OccurredAt = DateTimeOffset.UtcNow,
            Details = "ok"
        };
        db.AuditEvents.Add(evt);
        await db.SaveChangesAsync();

        db.AuditEvents.Remove(evt);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Contains("append-only", ex.Message);
    }

    [Fact]
    public async Task SaveChanges_TypedAuditEvent_DoesNotAppendCommandPersisted()
    {
        await using var db = CreateContext(new StubAuditContext("corr_typed", "operador-1"));

        db.Documents.Add(NewDocument("doc_typed"));
        db.AuditEvents.Add(new AuditEvent
        {
            EventId = "ev_typed",
            CorrelationId = "corr_typed",
            DocumentId = "doc_typed",
            Type = AuditEventTypes.DocumentUploaded,
            Actor = "operador-1",
            OccurredAt = DateTimeOffset.UtcNow,
            Details = "Upload"
        });
        await db.SaveChangesAsync();

        Assert.DoesNotContain(db.AuditEvents, e => e.Type == AuditEventTypes.CommandPersisted);
        Assert.Single(db.AuditEvents, e => e.Type == AuditEventTypes.DocumentUploaded);
    }

    private static AppDbContext CreateContext(IAuditContext auditContext)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"audit-{Guid.NewGuid():N}")
            .AddInterceptors(new AppendOnlyAuditInterceptor(auditContext))
            .Options;

        return new AppDbContext(options);
    }

    private static Document NewDocument(string id) => new()
    {
        DocumentId = id,
        FileName = "contrato.pdf",
        Cnpj = "12.***.***/****-90",
        RazaoSocial = "ACME",
        TipoSocietario = "LTDA",
        UploadedAt = DateTimeOffset.UtcNow,
        UploadedBy = "test",
        Status = DocStatus.pendente,
        FileHash = "abc",
        CorrelationId = "corr_cmd"
    };

    private sealed class StubAuditContext(string correlationId, string actor) : IAuditContext
    {
        public string CorrelationId { get; } = correlationId;
        public string Actor { get; } = actor;
    }
}
