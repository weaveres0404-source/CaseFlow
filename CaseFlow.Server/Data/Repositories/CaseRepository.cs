using CaseFlow.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CaseFlow.Server.Data.Repositories;

public class CaseRepository : ICaseRepository
{
    private readonly CaseFlowDbContext _db;

    public CaseRepository(CaseFlowDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<Case?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Cases
            .Include(c => c.Project)
            .Include(c => c.CreatedByNavigation)
            .FirstOrDefaultAsync(c => c.CaseId == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Case> AddAsync(Case caseEntity, CancellationToken cancellationToken = default)
    {
        _db.Cases.Add(caseEntity);
        await _db.SaveChangesAsync(cancellationToken);
        return caseEntity;
    }
}
