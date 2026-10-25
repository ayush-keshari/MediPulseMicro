namespace LogisticsService.Models;

// No foreign key: deletion evidence must outlive the source record.
public class ConsumptionChange
{
    public long ChangeId { get; set; }
    public int ConsumptionId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
    public int? BeforeFacilityId { get; set; }
    public int? BeforeItemId { get; set; }
    public DateTime? BeforeConsumedDate { get; set; }
    public int? BeforeQuantity { get; set; }
    public int? AfterFacilityId { get; set; }
    public int? AfterItemId { get; set; }
    public DateTime? AfterConsumedDate { get; set; }
    public int? AfterQuantity { get; set; }
}
