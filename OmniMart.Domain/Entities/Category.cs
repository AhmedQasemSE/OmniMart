namespace OmniMart.Domain.Entities;

public class Category
{
    public Guid Id { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;
    public virtual Category? ParentCategory { get; private set; }
    
    public virtual ICollection<Category> SubCategories { get; private set; } = new List<Category>();
    public virtual ICollection<CategoryAttribute> CategoryAttributes { get; private set; } = new List<CategoryAttribute>();
    public virtual ICollection<Product> Products { get; private set; } = new List<Product>();
   
    public Category(string name,Guid? parentCategoryId, string description, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be null or empty.", nameof(description));
        

        Id = Guid.NewGuid();
        ParentCategoryId = parentCategoryId;
        Name = name;
        Description = description;
        IsActive = isActive;
    }
#pragma warning disable CS8618
    protected Category() { }
#pragma warning restore CS8618

    public void UpdateDetails(string name, string description, Guid? parentCategoryId) 
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be null or empty.", nameof(description));
        Name = name;
        Description = description;
        ParentCategoryId = parentCategoryId;
    }

    public void AddAttribute(Guid attributeId, bool isRequired)
    {
        if (CategoryAttributes.Any(ca => ca.ProductAttributeId == attributeId))
            throw new InvalidOperationException("This attribute is already assigned to the category.");

        CategoryAttributes.Add(new CategoryAttribute(this.Id, attributeId, isRequired));
    }

    public void RemoveAttribute(Guid attributeId)
    {
        var attribute = CategoryAttributes.FirstOrDefault(ca => ca.ProductAttributeId == attributeId);
        if (attribute == null)
            throw new InvalidOperationException("Attribute not found in this category.");

        CategoryAttributes.Remove(attribute);
    }

    public void ToggleActiveStatus() {
        if (IsDeleted) return;
        IsActive = !IsActive;
    }
    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsDeleted = false;
        IsActive = true;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        IsActive = false; 
    }

    public void Restore()
    {
        IsDeleted = false;
        IsActive = false;
    }
}