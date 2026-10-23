namespace TeamHub.Domain.Entities;

public abstract class AuditableEntity<TKey> : BaseEntity<TKey>
{
    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? LastModifiedBy { get; set; }

    public DateTime? LastModifiedDate { get; set; }
}