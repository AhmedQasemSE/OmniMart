namespace OmniMart.Domain.Entities
{
    public class VariantAttributeValue
    {
        public Guid Id { get; private set; }
        public Guid ProductVariantId { get; private set; }
        public Guid ProductAttributeId { get; private set; }
        public string Value { get; private set; }

        public virtual ProductVariant? ProductVariant { get; private set; }
        public virtual ProductAttribute? ProductAttribute { get; private set; }

        public VariantAttributeValue(Guid productVariantId, Guid productAttributeId, string value)
        {
            if (productVariantId == Guid.Empty) throw new ArgumentException("ProductVariantId cannot be empty.", nameof(productVariantId));
            if (productAttributeId == Guid.Empty) throw new ArgumentException("ProductAttributeId cannot be empty.", nameof(productAttributeId));
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
            Id = Guid.NewGuid();
            ProductVariantId = productVariantId;
            ProductAttributeId = productAttributeId;
            Value = value;
        }
#pragma warning disable CS8618
        protected VariantAttributeValue() { }
#pragma warning restore CS8618
        public void UpdateValue(string newValue)
        {
            if (string.IsNullOrWhiteSpace(newValue))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(newValue));

            Value = newValue;
        }
    }
}


