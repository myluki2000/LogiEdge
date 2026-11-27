using System.ComponentModel.DataAnnotations;

namespace LogiEdge.WarehouseService.Data;

public class ItemsTableView
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(128)]
    public required string Name { get; set; }

    public required Guid UserId { get; set; }

    public bool IsDefault { get; set; }

    public required string ViewState { get; set; }
}
