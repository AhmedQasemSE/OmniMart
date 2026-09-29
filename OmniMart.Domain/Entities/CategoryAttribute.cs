namespace OmniMart.Domain.Entities
{
    public class CategoryAttribute
    {
        public Guid Id { get; private set; }
        public Guid CategoryId { get; private set; }
        public Guid ProductAttributeId { get; private set; }
        public bool IsRequired { get; private set; } = false;

        public virtual Category? Category { get; private set; }
        public virtual ProductAttribute? ProductAttribute { get; private set; }

        public CategoryAttribute(Guid categoryId, Guid productAttributeId, bool isRequired)
        {
            if (categoryId == Guid.Empty)
                throw new ArgumentException("CategoryId cannot be empty.", nameof(categoryId));
            if (productAttributeId == Guid.Empty) 
            throw new ArgumentException("ProductAttributeId cannot be empty.", nameof(productAttributeId));
            Id = Guid.NewGuid();
            CategoryId = categoryId;
            ProductAttributeId = productAttributeId;
            IsRequired = isRequired;
        }

       #pragma warning disable CS8618
        protected CategoryAttribute() { }
       #pragma warning restore CS8618



    }
}
