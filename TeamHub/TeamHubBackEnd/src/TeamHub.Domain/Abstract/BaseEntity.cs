namespace TeamHub.Domain.Entities;

public abstract class BaseEntity<TKey>
{
    public abstract TKey Id { get; set; }
}