namespace TeamHub.Domain.Entities;

public class CotivityEmployeeImage : AuditableEntity<int>
{
public int CotivityImageId { get; set; }
    public override int Id
    {
        get => CotivityImageId;
        set => CotivityImageId = value;
    }
    
     public int CotivityEmployeeId { get; set; }
 
     public string CotivityImagePath { get; set; }=string.Empty;
    
}