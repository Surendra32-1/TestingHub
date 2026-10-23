using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.EmployeeTermination;
using TeamHub.Infrastructure.Database;
using TeamHub.Shared.Responses;

namespace TeamHub.Infrastructure.EmployeeTerminations;

public sealed class EmployeeTerminationRepository(IDbConnectionFactory connectionFactory) : IEmployeeTerminationRepository
{
    private const string BaseSelect = """
        SELECT t.*, CONCAT_WS(' ', e.FirstName, e.MiddleName, e.LastName) AS EmployeeName,
                   e.ENumber AS EmployeeCode, d.DepartmentName AS Department,
                   ds.DesignationName AS Designation,
                   CONCAT_WS(' ', manager.FirstName, manager.MiddleName, manager.LastName) AS ManagerName,
                   e.JoinedDate
        FROM EmployeeTerminates t
        INNER JOIN Employees e ON e.EmployeeId = t.EmployeeId
        LEFT JOIN Departments d ON d.DepartmentId = e.DepartmentId
        LEFT JOIN Designations ds ON ds.DesignationId = e.DesignationId
        LEFT JOIN Employees manager ON manager.EmployeeId = e.ReportsToEmployeeId
        """;

    public async Task<IReadOnlyList<EmployeeTerminationResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = BaseSelect + " ORDER BY t.CreatedDate DESC, t.TerminationId DESC;";
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<EmployeeTerminationRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.Select(Map).ToList();
    }

    public async Task<PagedResult<EmployeeTerminationResponse>> GetPagedAsync(
        int page, int pageSize, string? search, string? department, string? status, CancellationToken cancellationToken)
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
            where.Add("t.Status = @Status");
        }
        var whereSql = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";
        var sql = BaseSelect + whereSql + " ORDER BY t.CreatedDate DESC, t.TerminationId DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
        var filterParams = new
        {
            Search = $"%{search?.Trim()}%",
            Department = department,
            Status = status,
            Offset = (Math.Max(page, 1) - 1) * pageSize,
            PageSize = pageSize
        };
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<EmployeeTerminationRow>(new CommandDefinition(sql,
            filterParams, cancellationToken: cancellationToken));
        var items = rows.Select(Map).ToList();
        var totalItems = items.Count;
        if (items.Count == pageSize)
        {
            totalItems = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COUNT(1) FROM EmployeeTerminates t INNER JOIN Employees e ON e.EmployeeId = t.EmployeeId" +
                " LEFT JOIN Departments d ON d.DepartmentId = e.DepartmentId" + whereSql,
                new { Search = filterParams.Search, Department = department, Status = status },
                cancellationToken: cancellationToken));
        }
        return new PagedResult<EmployeeTerminationResponse>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }

    public async Task<IReadOnlyList<EmployeeTerminationResponse>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken)
    {
        var sql = BaseSelect + " WHERE t.EmployeeId = @EmployeeId ORDER BY t.CreatedDate DESC;";
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<EmployeeTerminationRow>(new CommandDefinition(sql,
            new { EmployeeId = employeeId }, cancellationToken: cancellationToken));
        return rows.Select(Map).ToList();
    }

    public async Task<EmployeeTerminationResponse?> GetByIdAsync(int terminationId, CancellationToken cancellationToken)
    {
        var sql = BaseSelect + " WHERE t.TerminationId = @TerminationId;";
        using var connection = connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<EmployeeTerminationRow>(new CommandDefinition(sql,
            new { TerminationId = terminationId }, cancellationToken: cancellationToken));
        return row is null ? null : Map(row);
    }

    public async Task<EmployeeTerminationResponse?> CreateAsync(int employeeId, EmployeeTerminationRequest request, string actorName, CancellationToken cancellationToken)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM Employees WHERE EmployeeId = @EmployeeId) RETURN;
            INSERT INTO EmployeeTerminates
                (EmployeeId, TerminationType, EffectiveDate, InvestigationRequired,
                 InvestigationSummary, WarningCount, LastWarningDate, Reason, Remarks, AssetRecovered,
                 SystemAccessRevoked, ExitInterviewCompleted, SettlementAmount, EligibleForRehire,
                 TerminationLetterIssued, TerminationLetterDate, Status, ApprovedBy, ApprovedDate,
                 CreatedBy, CreatedDate)
            OUTPUT INSERTED.TerminationId
            VALUES
                (@EmployeeId, @TerminationType, @EffectiveDate, @InvestigationRequired,
                 @InvestigationSummary, @WarningCount, @LastWarningDate, @Reason, @Remarks, @AssetRecovered,
                 @SystemAccessRevoked, @ExitInterviewCompleted, @SettlementAmount, @EligibleForRehire,
                 @TerminationLetterIssued, @TerminationLetterDate, @Status, @ApprovedBy, @ApprovedDate,
                 @CreatedBy, @CreatedDate);
            IF @Status IN ('Completed', 'Terminated')
                UPDATE Employees SET IsActive = 0 WHERE EmployeeId = @EmployeeId;
            """;
        using var connection = connectionFactory.CreateConnection();
        var id = await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(sql,
            Parameters(employeeId, request, actorName, created: true), cancellationToken: cancellationToken));
        return id.HasValue ? await GetByIdAsync(id.Value, cancellationToken) : null;
    }

    public async Task<bool> UpdateAsync(int terminationId, EmployeeTerminationRequest request, string actorName, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE EmployeeTerminates
            SET TerminationType = @TerminationType, EffectiveDate = @EffectiveDate,
                InvestigationRequired = @InvestigationRequired, InvestigationSummary = @InvestigationSummary,
                WarningCount = @WarningCount, LastWarningDate = @LastWarningDate, Reason = @Reason,
                Remarks = @Remarks, AssetRecovered = @AssetRecovered, SystemAccessRevoked = @SystemAccessRevoked,
                ExitInterviewCompleted = @ExitInterviewCompleted, SettlementAmount = @SettlementAmount,
                EligibleForRehire = @EligibleForRehire, TerminationLetterIssued = @TerminationLetterIssued,
                TerminationLetterDate = @TerminationLetterDate, Status = @Status,
                ApprovedBy = CASE WHEN @IsApproved = 1 THEN @ApprovedBy ELSE ApprovedBy END,
                ApprovedDate = CASE WHEN @IsApproved = 1 THEN @ApprovedDate ELSE ApprovedDate END,
                LastModifiedBy = @LastModifiedBy, LastModifiedDate = @LastModifiedDate
            WHERE TerminationId = @TerminationId;
            IF @Status IN ('Completed', 'Terminated')
                UPDATE Employees
                SET IsActive = 0
                WHERE EmployeeId = (SELECT EmployeeId FROM EmployeeTerminates WHERE TerminationId = @TerminationId);
            """;
        var parameters = Parameters(0, request, actorName, created: false, terminationId);
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)) > 0;
    }

    public async Task<EmployeeTerminationResponse?> TransitionStatusAsync(
        int terminationId, string status, string actorName, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE EmployeeTerminates
            SET Status = @Status,
                ApprovedBy = CASE WHEN @Status IN ('Approved', 'Terminated') THEN @Actor ELSE ApprovedBy END,
                ApprovedDate = CASE WHEN @Status IN ('Approved', 'Terminated') THEN @Now ELSE ApprovedDate END,
                LastModifiedBy = @Actor, LastModifiedDate = @Now
            WHERE TerminationId = @TerminationId;
            IF @Status = 'Terminated'
                UPDATE Employees SET IsActive = 0
                WHERE EmployeeId = (SELECT EmployeeId FROM EmployeeTerminates WHERE TerminationId = @TerminationId);
            """;
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql,
            new { TerminationId = terminationId, Status = status, Actor = actorName, Now = DateTime.UtcNow },
            cancellationToken: cancellationToken));
        return affected > 0 ? await GetByIdAsync(terminationId, cancellationToken) : null;
    }

    public async Task<EmployeeTerminationResponse?> UpdateClearanceAsync(
        int terminationId, UpdateTerminationClearanceRequest request, string actorName, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE EmployeeTerminates
            SET AssetRecovered = @AssetRecovered,
                SystemAccessRevoked = @SystemAccessRevoked,
                ExitInterviewCompleted = @ExitInterviewCompleted,
                TerminationLetterIssued = @TerminationLetterIssued,
                SettlementAmount = COALESCE(@SettlementAmount, SettlementAmount),
                LastModifiedBy = @Actor, LastModifiedDate = @Now
            WHERE TerminationId = @TerminationId;
            """;
        using var connection = connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql,
            new
            {
                TerminationId = terminationId,
                request.AssetRecovered,
                request.SystemAccessRevoked,
                request.ExitInterviewCompleted,
                request.TerminationLetterIssued,
                request.SettlementAmount,
                Actor = actorName,
                Now = DateTime.UtcNow
            }, cancellationToken: cancellationToken));
        return affected > 0 ? await GetByIdAsync(terminationId, cancellationToken) : null;
    }

    private static object Parameters(int employeeId, EmployeeTerminationRequest request, string actorName, bool created, int terminationId = 0)
    {
        var now = DateTime.UtcNow;
        return new
        {
            TerminationId = terminationId,
            EmployeeId = employeeId,
            request.TerminationType,
            EffectiveDate = request.EffectiveDate?.ToDateTime(TimeOnly.MinValue),
            request.InvestigationRequired,
            request.InvestigationSummary,
            request.WarningCount,
            LastWarningDate = request.LastWarningDate?.ToDateTime(TimeOnly.MinValue),
            request.Reason,
            request.Remarks,
            request.AssetRecovered,
            request.SystemAccessRevoked,
            request.ExitInterviewCompleted,
            request.SettlementAmount,
            request.EligibleForRehire,
            request.TerminationLetterIssued,
            TerminationLetterDate = request.TerminationLetterDate?.ToDateTime(TimeOnly.MinValue),
            request.Status,
            IsApproved = request.Status is "Approved" or "Completed" or "Terminated",
            ApprovedBy = request.Status is "Approved" or "Completed" or "Terminated" ? actorName : null,
            ApprovedDate = request.Status is "Approved" or "Completed" or "Terminated" ? now : (DateTime?)null,
            CreatedBy = created ? actorName : null,
            CreatedDate = created ? now : (DateTime?)null,
            LastModifiedBy = created ? null : actorName,
            LastModifiedDate = created ? (DateTime?)null : now
        };
    }

    private static EmployeeTerminationResponse Map(EmployeeTerminationRow row) => new(
        row.TerminationId, row.EmployeeId, row.TerminationType, ToDateOnly(row.EffectiveDate),
        row.InvestigationRequired, row.InvestigationSummary, row.WarningCount,
        ToDateOnly(row.LastWarningDate), row.Reason, row.Remarks, row.AssetRecovered,
        row.SystemAccessRevoked, row.ExitInterviewCompleted, row.SettlementAmount,
        row.EligibleForRehire, row.TerminationLetterIssued, ToDateOnly(row.TerminationLetterDate),
        row.Status, row.ApprovedBy, ToOffset(row.ApprovedDate), row.CreatedBy, ToOffset(row.CreatedDate),
        row.LastModifiedBy, ToOffset(row.LastModifiedDate))
    {
        EmployeeName = row.EmployeeName ?? string.Empty,
        EmployeeCode = row.EmployeeCode,
        Department = row.Department,
        Designation = row.Designation,
        ManagerName = row.ManagerName,
        JoinedDate = ToDateOnly(row.JoinedDate)
    };

    private static DateOnly? ToDateOnly(DateTime? value) => value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
    private static DateTimeOffset? ToOffset(DateTime? value) =>
        value.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)) : null;

    private sealed class EmployeeTerminationRow
    {
        public int TerminationId { get; set; }
        public int EmployeeId { get; set; }
        public string TerminationType { get; set; } = string.Empty;
        public DateTime? EffectiveDate { get; set; }
        public bool? InvestigationRequired { get; set; }
        public string? InvestigationSummary { get; set; }
        public int? WarningCount { get; set; }
        public DateTime? LastWarningDate { get; set; }
        public string? Reason { get; set; }
        public string? Remarks { get; set; }
        public bool? AssetRecovered { get; set; }
        public bool? SystemAccessRevoked { get; set; }
        public bool? ExitInterviewCompleted { get; set; }
        public decimal? SettlementAmount { get; set; }
        public bool? EligibleForRehire { get; set; }
        public bool? TerminationLetterIssued { get; set; }
        public DateTime? TerminationLetterDate { get; set; }
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
        public DateTime? JoinedDate { get; set; }
    }
}
