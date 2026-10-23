namespace TeamHub.Domain.Entities;

public class EmployeeImage : AuditableEntity<int>
{
public int ImageId { get; set; }
    public override int Id
    {
        get => ImageId;
        set => ImageId = value;
    }
    
     public int EmployeeId { get; set; }
 
     public string ImagePath { get; set; }=string.Empty;
    
}