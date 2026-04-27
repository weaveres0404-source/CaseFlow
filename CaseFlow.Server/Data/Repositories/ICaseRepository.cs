using CaseFlow.Server.Models;

namespace CaseFlow.Server.Data.Repositories;

public interface ICaseRepository
{
    /// <summary>依 CaseId 載入案件，並 eager-load Project 與建立者 User。</summary>
    Task<Case?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>新增案件；若違反 DB 唯一約束（如 case_number 重複）會拋出 DbUpdateException。</summary>
    Task<Case> AddAsync(Case caseEntity, CancellationToken cancellationToken = default);
}
