namespace Domain.ValueObjects.DummyJson;

public class DummyProducts
{
    public List<Product> products { get; set; }
    public int total { get; set; }
    public int skip { get; set; }
    public int limit { get; set; }

    public class Product
    {
        public int id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string category { get; set; }
        public double price { get; set; }
        public double discountPercentage { get; set; }
        public double rating { get; set; }
        public int stock { get; set; }
        public List<string> tags { get; set; }
        public string brand { get; set; }
        public string sku { get; set; }
        public int weight { get; set; }
        public Dimensions dimensions { get; set; }
        public string warrantyInformation { get; set; }
        public string shippingInformation { get; set; }
        public string availabilityStatus { get; set; }
        public List<Review> reviews { get; set; }
        public string returnPolicy { get; set; }
        public int minimumOrderQuantity { get; set; }
        public Meta meta { get; set; }
        public List<string> images { get; set; }
        public string thumbnail { get; set; }
    }

    public class Dimensions
    {
        public double width { get; set; }
        public double height { get; set; }
        public double depth { get; set; }
    }

    public class Review
    {
        public int rating { get; set; }
        public string comment { get; set; }
        public DateTime date { get; set; }
        public string reviewerName { get; set; }
        public string reviewerEmail { get; set; }
    }

    public class Meta
    {
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public string barcode { get; set; }
        public string qrCode { get; set; }
    }
}