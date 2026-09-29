namespace OmniMart.Domain.Entities;

public class ProductAttribute
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;
    public virtual ICollection<VariantAttributeValue> VariantAttributeValues { get; private set; } = new List<VariantAttributeValue>();
    public virtual ICollection<CategoryAttribute> CategoryAttributes { get; private set; } = new List<CategoryAttribute>();


    public ProductAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
        Id = Guid.NewGuid();
        Name = name;
    }
#pragma warning disable CS8618
    protected ProductAttribute() { }
#pragma warning restore CS8618

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be null or whitespace.", nameof(newName));

        Name = newName;
    }
}
