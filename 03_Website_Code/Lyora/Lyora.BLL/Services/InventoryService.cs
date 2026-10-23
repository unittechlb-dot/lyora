using Lyora.DAL.Entities;
using Lyora.DAL.Repositories;

namespace Lyora.BLL.Services;

/// <summary>
/// Administrator: Manage Raw Materials and Manage Inventory.
/// (Deducting by BOM and restoring after a cancellation run inside sp_PlaceOrder and sp_CancelOrder.)
/// </summary>
public class InventoryService
{
    public static readonly string[] Units = { "ml", "g", "pcs" };
    private const decimal MaxQuantity = 1_000_000;

    private readonly RawMaterialRepository _materials;

    public InventoryService(RawMaterialRepository materials)
    {
        _materials = materials;
    }

    public List<RawMaterial> GetMaterials() => _materials.GetAll();

    public RawMaterial? GetMaterial(int materialId) => _materials.GetById(materialId);

    public List<RawMaterial> CheckThresholds() => _materials.GetAll().Where(m => m.IsBelowThreshold).ToList();

    public List<InventoryTransaction> GetTransactions(int materialId) => _materials.GetTransactions(materialId);

    public ServiceResult SaveMaterial(RawMaterial material)
    {
        material.Name = material.Name.Trim();

        if (!Units.Contains(material.Unit))
        {
            return ServiceResult.Fail("Please choose a unit: ml, g or pcs.");
        }
        if (material.Threshold < 0 || material.Threshold > MaxQuantity)
        {
            return ServiceResult.Fail("The threshold must be between 0 and 1,000,000.");
        }
        if (material.CostPerUnit < 0 || material.CostPerUnit > 10_000)
        {
            return ServiceResult.Fail("The cost per unit must be between 0 and 10,000.");
        }
        var exceptId = material.MaterialId == 0 ? (int?)null : material.MaterialId;
        if (_materials.NameExists(material.Name, exceptId))
        {
            return ServiceResult.Fail("Another raw material already has this name.");
        }

        if (material.MaterialId == 0)
        {
            _materials.Create(material);
        }
        else
        {
            _materials.Update(material);
        }
        return ServiceResult.Ok();
    }

    /// <summary>Adds a quantity received from the supplier (logged as a Restock transaction).</summary>
    public ServiceResult Restock(int materialId, decimal quantity)
    {
        var material = _materials.GetById(materialId);
        if (material == null)
        {
            return ServiceResult.Fail("Raw material not found.");
        }
        if (quantity <= 0 || quantity > MaxQuantity)
        {
            return ServiceResult.Fail("The received quantity must be between 0.01 and 1,000,000.");
        }
        if (material.Unit == "pcs" && quantity != Math.Floor(quantity))
        {
            return ServiceResult.Fail($"{material.Name} is counted in pieces: use a whole number.");
        }

        _materials.Restock(materialId, quantity);
        return ServiceResult.Ok();
    }
}
