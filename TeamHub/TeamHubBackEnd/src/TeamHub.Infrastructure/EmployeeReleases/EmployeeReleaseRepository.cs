using System.Data;
using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.EmployeeRelease;
using TeamHub.Infrastructure.Database;
using TeamHub.Shared.Responses;

namespace TeamHub.Infrastructure.EmployeeReleases;

public sealed class EmployeeReleaseRepository(IDbConnectionFactory connectionFactory) : IEmployeeReleaseRepository
{
    private const string BaseSelect = """
        SELECT r.*, CONCAT_WS(' ', e.FirstName, e.MiddleName, e.LastName) AS EmployeeName,
                   e.ENumber AS EmployeeCode, d.DepartmentName AS Department,
                   ds.DesignationName AS Designation,
                   CONCAT_WS(' ', manager.FirstName, manager.MiddleName, manager.LastName) AS ManagerName
        FROM EmployeeReleases r
        INNER JOIN Employees e ON e.EmployeeId = r.EmployeeId
        LEFT JOIN Departments d ON d.DepartmentId = e.DepartmentId
        LEFT JOIN Designations ds ON ds.DesignationId = e.DesignationId
        LEFT JOIN Employees manager ON manager.EmployeeId = e.ReportsToEmployeeId
        """;

    public async Task<IReadOnlyList<EmployeeReleaseResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = BaseSelect + " ORDER BY r.CreatedDate DESC, r.ReleasedId DESC;";
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<EmployeeReleaseRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.Select(Map).ToList();
    }

    public async Task<PagedResult<EmployeeReleaseResponse>> GetPagedAsync(
        int page, int pageSize, string? search, string? department, string? status, string? releaseType, CancellationToken cancellationToken)
    {
        var where = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(CONCAT_WS(' ', e.FirstName, e.MiddleName, e.LastName) LIKE @Search OR CAST(e.ENumber AS CHAR(20)) LIKE @Search)");
        }
        if (!string.IsNullOrWhiteSpace(department))
        {
            where.Add("d.DepartmentName = @Department");
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            where.Add("r.Status = @Status");
        }
        if (!string.IsNullOrWhiteSpace(releaseType))
        {
            where.Add("r.ReleaseType = @ReleaseType");
        }
        var whereSql = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";
        var sql = BaseSelect + whereSql + " ORDER BY r.CreatedDate DESC, r.ReleasedId DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<EmployeeReleaseRow>(new CommandDefinition(sql,
            new
            {
                Search = $"%{search?.Trim()}%",
                Department = department,
                Status = status,
                ReleaseType = releaseType,
                Offset = (Math.Max(page, 1) - 1) * pageSize,
                PageSize = pageSize
            }, cancellationToken: cancellationToken));
        var items = rows.Select(Map).ToList();
        var totalItems = items.Count;
        if (items.Count == pageSize)
        {
            totalItems = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COUNT(1) FROM EmployeeReleases r INNER JOIN Employees e ON e.EmployeeId = r.EmployeeId" +
                " LEFT JOIN Departments d ON d.DepartmentId = e.DepartmentId" + whereSql,
                new { Search = $"%{search?.Trim()}%", Department = department, Status = status, ReleaseType = releaseType },
                cancellationToken: cancellationToken));
        }
        return new PagedResult<EmployeeReleaseResponse>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }

    public async Task<IReadOnlyList<EmployeeReleaseResponse>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken)
    {
        var sql = BaseSelect + " WHERE r.EmployeeId = @EmployeeId ORDER BY r.CreatedDate DESC;";
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<EmployeeReleaseRow>(new CommandDefinition(sql,
            new { EmployeeId = employeeId }, cancellationToken: cancellationToken));
        return rows.Select(Map).ToList();
    }

    public async Task<EmployeeReleaseResponse?> GetByIdAsync(int releasedId, CancellationToken cancellationToken)
    {
        var sql = BaseSelect + " WHERE r.ReleasedId = @ReleasedId;";
        using var connection = connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<EmployeeReleaseRow>(new CommandDefinition(sql,
            new { ReleasedId = releasedId }, cancellationToken: cancellationToken));
        return row is null ? null : Map(row);
    }

    public async Task<EmployeeReleaseResponse?> CreateAsync(int employeeId, EmployeeReleaseRequest request, string actorName, CancellationToken cancellationToken)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM Employees WHERE EmployeeId = @EmployeeId) RETURN;
            INSERT INTO EmployeeReleases
                (EmployeeId, ReleaseType, ResignationDate, NoticeDate, LastWorkingDate,
                 NoticePeriodDays, ServedNoticeDays, Reason, EarlyReleaseReason, Remarks, KnowledgeTransferCompleted,
                 AssetsReturned, ExitInterviewCompleted, SettlementAmount, RelievingLetterIssued,
                 ExperienceLetterIssued, Status, ApprovedBy, ApprovedDate, CreatedBy, CreatedDate)
            OUTPUT INSERTED.ReleasedId
            VALUES
                (@EmployeeId, @ReleaseType, @ResignationDate, @NoticeDate, @LastWorkingDate,
                 @NoticePeriodDays, @ServedNoticeDays, @Reason, @EarlyReleaseReason, @Remarks, @KnowledgeTransferCompleted,
                 @AssetsReturned, @ExitInterviewCompleted, @SettlementAmount, @RelievingLetterIssued,
                 @ExperienceLetterIssued, @Status, @ApprovedBy, @ApprovedDate, @CreatedBy, @CreatedDate);
            IF @Status = 'Released'
                UPDATE Employees SET IsActive = 0 WHERE EmployeeId = @EmployeeId;
            """;
        using var connection = connectionFactory.CreateConnection();
        var id = await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(sql,
            Parameters(employeeId, request, actorName, created: true), cancellationToken: cancellationToken));
        return id.HasValue ? await GetByIdAsync(id.Value, cancellationToken) : null;
    }

    public async Task<bool> UpdateAsync(int releasedId, EmployeeReleaseRequest request, string actorName, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE EmployeeReleases
            SET ReleaseType = @ReleaseType, ResignationDate = @ResignationDate,
                NoticeDate = @NoticeDate, LastWorkingDate = @LastWorkingDate,
                NoticePeriodDays = @NoticePeriodDays, ServedNoticeDays = @ServedNoticeDays,
                Reason = @Reason, EarlyReleaseReason = @EarlyReleaseReason, Remarks = @Remarks, KnowledgeTransferCompleted = @KnowledgeTransferCompleted,
                AssetsReturned = @AssetsReturned, ExitInterviewCompleted = @ExitInterviewCompleted,
                SettlementAmount = @SettlementAmount, RelievingLetterIssued = @RelievingLetterIssued,
                ExperienceLetterIssued = @ExperienceLetterIssued, Status = @Status,
                ApprovedBy = CASE WHEN @IsApproved = 1 THEN @ApprovedBy ELSE ApprovedBy END,
                ApprovedDate = CASE WHEN @IsApproved = 1 THEN @ApprovedDate ELSE ApprovedDate END,
                LastModifiedBy = @LastModifiedBy, LastModifiedDate = @LastModifiedDate
            WHERE ReleasedId = @ReleasedId;
            IF @Status = 'Released'
                UPDATE Employees
                SET IsActive = 0
                WHERE EmployeeId = (SELECT EmployeeId FROM EmployeeReleases WHERE ReleasedId = @ReleasedId);
            """;
        var parameters = Parameters(0, request, actorName, created: false, releasedId);
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)) > 0;
    }

    public async Task<EmployeeReleaseResponse?> TransitionStatusAsync(
        int releasedId, UpdateReleaseStatusRequest request, string actorName, CancellationToken cancellationToken)
    {
        const string releasedStatus = "Released";
        var now = DateTime.UtcNow;

        using var connection = connectionFactory.CreateConnection();
        var departingEmployeeId = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT e.EmployeeId FROM EmployeeReleases r INNER JOIN Employees e ON e.EmployeeId = r.EmployeeId WHERE r.ReleasedId = @ReleasedId",
            new { ReleasedId = releasedId }, cancellationToken: cancellationToken));

        if (departingEmployeeId is null)
        {
            return null;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE EmployeeReleases
            SET Status = @Status,
                ApprovedBy = CASE WHEN @Status IN ('Approved', 'Released') THEN @Actor ELSE ApprovedBy END,
                ApprovedDate = CASE WHEN @Status IN ('Approved', 'Released') THEN @Now ELSE ApprovedDate END,
                LastModifiedBy = @Actor, LastModifiedDate = @Now
            WHERE ReleasedId = @ReleasedId
            """,
            new { Status = request.Status, Actor = actorName, Now = now, ReleasedId = releasedId },
            cancellationToken: cancellationToken));

        if (request.Status == releasedStatus)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE Employees SET IsActive = 0 WHERE EmployeeId = @EmpId",
                new { EmpId = departingEmployeeId.Value }, cancellationToken: cancellationToken));
        }

        return await GetByIdAsync(releasedId, cancellationToken);
    }

    public async Task<EmployeeReleaseResponse?> UpdateClearanceAsync(
        int releasedId, UpdateReleaseClearanceRequest request, string actorName, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE EmployeeReleases
            SET KnowledgeTransferCompleted = @KnowledgeTransferCompleted,
                AssetsReturned = @AssetsReturned,
                ExitInterviewCompleted = @ExitInterviewCompleted,
                RelievingLetterIssued = @RelievingLetterIssued,
                ExperienceLetterIssued = @ExperienceLetterIssued,
                SettlementAmount = COALESCE(@SettlementAmount, SettlementAmount),
                LastModifiedBy = @Actor, LastModifiedDate = @Now
            WHERE ReleasedId = @ReleasedId;
            """;
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql,
            new
            {
                ReleasedId = releasedId,
                request.KnowledgeTransferCompleted,
                request.AssetsReturned,
                request.ExitInterviewCompleted,
                request.RelievingLetterIssued,
                request.ExperienceLetterIssued,
                request.SettlementAmount,
                Actor = actorName,
                Now = DateTime.UtcNow
            }, cancellationToken: cancellationToken));
        return affected > 0 ? await GetByIdAsync(releasedId, cancellationToken) : null;
    }

    private static object Parameters(
    int employeeId,
    EmployeeReleaseRequest request,
    string actorName,
    bool created,
    int releasedId = 0)
    {
        var now = DateTime.UtcNow;

        var servedNoticeDays =request.ResignationDate is { } resignationDate && request.LastWorkingDate is { } lastWorkingDate
        ? (int?)(lastWorkingDate.DayNumber - resignationDate.DayNumber)
        : null;
        return new
        {
            ReleasedId = releasedId,
            EmployeeId = employeeId,

            request.ReleaseType,

            ResignationDate = request.ResignationDate?.ToDateTime(TimeOnly.MinValue),
            NoticeDate = request.NoticeDate?.ToDateTime(TimeOnly.MinValue),
            LastWorkingDate = request.LastWorkingDate?.ToDateTime(TimeOnly.MinValue),

            request.NoticePeriodDays,
            ServedNoticeDays = servedNoticeDays,

            request.Reason,
            request.EarlyReleaseReason,
            request.Remarks,

            request.KnowledgeTransferCompleted,
            request.AssetsReturned,
            request.ExitInterviewCompleted,

            request.SettlementAmount,

            request.RelievingLetterIssued,
            request.ExperienceLetterIssued,

            request.Status,

            IsApproved = request.Status is "Approved" or "Released",

            ApprovedBy = request.Status is "Approved" or "Released"
                ? actorName
                : null,

            ApprovedDate = request.Status is "Approved" or "Released"
                ? now
                : (DateTime?)null,

            CreatedBy = created ? actorName : null,
            CreatedDate = created ? now : (DateTime?)null,

            LastModifiedBy = created ? null : actorName,
            LastModifiedDate = created ? (DateTime?)null : now
        };
    }
    private static EmployeeReleaseResponse Map(EmployeeReleaseRow row) => new(
        row.ReleasedId, row.EmployeeId, row.ReleaseType,
        ToDateOnly(row.ResignationDate), ToDateOnly(row.NoticeDate), ToDateOnly(row.LastWorkingDate),
        row.NoticePeriodDays, row.Reason, row.Remarks,
        row.KnowledgeTransferCompleted, row.AssetsReturned, row.ExitInterviewCompleted,
        row.SettlementAmount, row.RelievingLetterIssued, row.ExperienceLetterIssued,
        row.Status, row.ApprovedBy, ToOffset(row.ApprovedDate), row.CreatedBy, ToOffset(row.CreatedDate),
        row.LastModifiedBy, ToOffset(row.LastModifiedDate))
    {
        EmployeeName = row.EmployeeName ?? string.Empty,
        EmployeeCode = row.EmployeeCode,
        Department = row.Department,
        Designation = row.Designation,
        ManagerName = row.ManagerName,
        ServedNoticeDays = row.ServedNoticeDays,
        EarlyReleaseReason = row.EarlyReleaseReason
    };

    private static DateOnly? ToDateOnly(DateTime? value) => value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
    private static DateTimeOffset? ToOffset(DateTime? value) =>
        value.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)) : null;

    private sealed class EmployeeReleaseRow
    {
        public int ReleasedId { get; set; }
        public int EmployeeId { get; set; }
        public string ReleaseType { get; set; } = string.Empty;
        public DateTime? ResignationDate { get; set; }
        public DateTime? NoticeDate { get; set; }
        public DateTime? LastWorkingDate { get; set; }
        public int? NoticePeriodDays { get; set; }
        public int? ServedNoticeDays { get; set; }
        public string? Reason { get; set; }
        public string? EarlyReleaseReason { get; set; }
        public string? Remarks { get; set; }
        public bool? KnowledgeTransferCompleted { get; set; }
        public bool? AssetsReturned { get; set; }
        public bool? ExitInterviewCompleted { get; set; }
        public decimal? SettlementAmount { get; set; }
        public bool? RelievingLetterIssued { get; set; }
        public bool? ExperienceLetterIssued { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string? EmployeeName { get; set; }
        public int EmployeeCode { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public string? ManagerName { get; set; }
    }
}
