namespace FamilyAccountApi.Domain.Entities;

public sealed class Product
{
    public int     IdProduct       { get; set; }
    public string  CodeProduct     { get; set; } = null!;
    public string  NameProduct     { get; set; } = null!;
    public int     IdProductType   { get; set; }
    public int     IdUnit          { get; set; }
    public int?    IdProductParent { get; set; }
    public decimal AverageCost     { get; set; }
    public byte[]  RowVersion      { get; set; } = null!;

    public bool    HasOptions      { get; set; } = false;
    public bool    IsCombo         { get; set; } = false;
    public bool    IsVariantParent { get; set; } = false;

    public decimal? ReorderPoint    { get; set; }
    public decimal? SafetyStock     { get; set; }
    public decimal? ReorderQuantity { get; set; }

    public string?  ClassificationAbc { get; set; }

    public ProductType  IdProductTypeNavigation  { get; set; } = null!;
    public UnitOfMeasure IdUnitNavigation        { get; set; } = null!;
    public Product?      IdProductParentNavigation { get; set; }

    public ICollection<Product>              Variants                  { get; set; } = [];
    public ICollection<ProductUnit>          ProductUnits              { get; set; } = [];
    public ICollection<ProductProductCategory> ProductProductCategories { get; set; } = [];
    public ICollection<ProductAccount>       ProductAccounts           { get; set; } = [];
    public ICollection<ProductOptionGroup>   ProductOptionGroups       { get; set; } = [];
    public ICollection<ProductComboSlot>     ProductComboSlots         { get; set; } = [];
    public ICollection<ProductAttribute>     ProductAttributes         { get; set; } = [];
    public ICollection<ProductVariantAttribute> ProductVariantAttributes { get; set; } = [];
}
