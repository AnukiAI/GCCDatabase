using Bogus;
using GCCWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GCCWebApp.BDD.Tests.Support
{
    public class ProductBuilder
    {
        private readonly Product _product;
        private readonly Faker _faker;

        public ProductBuilder()
        {
            _faker = new Faker();
            _product = new Product
            {
                Id = _faker.Random.Int(1, 10000),
                Name = _faker.Commerce.ProductName(),
                Description = _faker.Commerce.ProductDescription(),
                Sku = _faker.Commerce.Ean13(),
                Model = _faker.Commerce.Product(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public ProductBuilder WithId(int id)
        {
            _product.Id = id;
            return this;
        }

        public ProductBuilder WithName(string name)
        {
            _product.Name = name;
            return this;
        }

        public ProductBuilder WithSku(string sku)
        {
            _product.Sku = sku;
            return this;
        }

        public ProductBuilder WithCategory(int categoryId)
        {
            _product.CategoryId = categoryId;
            return this;
        }

        public ProductBuilder WithCompany(int companyId)
        {
            _product.CompanyId = companyId;
            return this;
        }

        public ProductBuilder WithIdentifiers(params ProductIdentifier[] identifiers)
        {
            _product.ProductIdentifiers = identifiers.ToList();
            return this;
        }

        public ProductBuilder WithCertificates(params Certificate[] certificates)
        {
            _product.Certificates = certificates.ToList();
            return this;
        }

        public ProductBuilder AsInactive()
        {
            _product.IsActive = false;
            return this;
        }

        public Product Build() => _product;

        public static Product Default() => new ProductBuilder().Build();

        public static List<Product> CreateMany(int count)
        {
            return Enumerable.Range(0, count)
                .Select(_ => new ProductBuilder().Build())
                .ToList();
        }
    }

    public class CertificateBuilder
    {
        private readonly Certificate _certificate;
        private readonly Faker _faker;

        public CertificateBuilder()
        {
            _faker = new Faker();
            _certificate = new Certificate
            {
                Id = _faker.Random.Int(1, 10000),
                Number = $"CERT-{_faker.Random.AlphaNumeric(10).ToUpper()}",
                Status = "Active",
                EffectiveDate = DateTime.UtcNow.AddDays(-30),
                ExpirationDate = DateTime.UtcNow.AddYears(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public CertificateBuilder WithNumber(string number)
        {
            _certificate.Number = number;
            return this;
        }

        public CertificateBuilder WithProduct(int productId)
        {
            _certificate.ProductId = productId;
            return this;
        }

        public CertificateBuilder WithCompany(int companyId)
        {
            _certificate.CompanyId = companyId;
            return this;
        }

        public CertificateBuilder WithCertificationType(int typeId)
        {
            _certificate.CertificationTypeId = typeId;
            return this;
        }

        public CertificateBuilder WithExpirationDate(DateTime date)
        {
            _certificate.ExpirationDate = date;
            return this;
        }

        public CertificateBuilder ExpiringIn(int days)
        {
            _certificate.ExpirationDate = DateTime.UtcNow.AddDays(days);
            return this;
        }

        public CertificateBuilder AsExpired()
        {
            _certificate.ExpirationDate = DateTime.UtcNow.AddDays(-1);
            _certificate.Status = "Expired";
            return this;
        }

        public CertificateBuilder AsSuspended()
        {
            _certificate.Status = "Suspended";
            return this;
        }

        public Certificate Build() => _certificate;

        public static Certificate Default() => new CertificateBuilder().Build();

        public static List<Certificate> CreateMany(int count)
        {
            return Enumerable.Range(0, count)
                .Select(_ => new CertificateBuilder().Build())
                .ToList();
        }
    }

    public class CompanyBuilder
    {
        private readonly Company _company;
        private readonly Faker _faker;

        public CompanyBuilder()
        {
            _faker = new Faker();
            _company = new Company
            {
                Id = _faker.Random.Int(1, 10000),
                Name = _faker.Company.CompanyName(),
                Location = _faker.Address.City(),
                Country = _faker.Address.Country(),
                ContactInfo = _faker.Phone.PhoneNumber(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public CompanyBuilder WithName(string name)
        {
            _company.Name = name;
            return this;
        }

        public CompanyBuilder WithLocation(string location, string country)
        {
            _company.Location = location;
            _company.Country = country;
            return this;
        }

        public CompanyBuilder WithProducts(params Product[] products)
        {
            _company.Products = products.ToList();
            return this;
        }

        public CompanyBuilder AsInactive()
        {
            _company.IsActive = false;
            return this;
        }

        public Company Build() => _company;

        public static Company Default() => new CompanyBuilder().Build();

        public static List<Company> CreateMany(int count)
        {
            return Enumerable.Range(0, count)
                .Select(_ => new CompanyBuilder().Build())
                .ToList();
        }
    }

    public class ProductIdentifierBuilder
    {
        private readonly ProductIdentifier _identifier;
        private readonly Faker _faker;

        public ProductIdentifierBuilder()
        {
            _faker = new Faker();
            _identifier = new ProductIdentifier
            {
                Id = _faker.Random.Int(1, 10000),
                IdentifierType = _faker.PickRandom("UPC", "ASIN", "EAN", "GTIN"),
                IdentifierValue = _faker.Random.AlphaNumeric(12),
                CreatedAt = DateTime.UtcNow
            };
        }

        public ProductIdentifierBuilder WithType(string type)
        {
            _identifier.IdentifierType = type;
            return this;
        }

        public ProductIdentifierBuilder WithValue(string value)
        {
            _identifier.IdentifierValue = value;
            return this;
        }

        public ProductIdentifierBuilder ForProduct(int productId)
        {
            _identifier.ProductId = productId;
            return this;
        }

        public ProductIdentifierBuilder AsUPC(string value = null)
        {
            _identifier.IdentifierType = "UPC";
            _identifier.IdentifierValue = value ?? _faker.Commerce.Ean13();
            return this;
        }

        public ProductIdentifierBuilder AsASIN(string value = null)
        {
            _identifier.IdentifierType = "ASIN";
            _identifier.IdentifierValue = value ?? $"B{_faker.Random.AlphaNumeric(9).ToUpper()}";
            return this;
        }

        public ProductIdentifier Build() => _identifier;

        public static ProductIdentifier Default() => new ProductIdentifierBuilder().Build();
    }

    public static class TestDataFactory
    {
        private static readonly Faker Faker = new Faker();

        public static List<Product> CreateProductsWithCertificates(int count)
        {
            var products = new List<Product>();
            
            for (int i = 0; i < count; i++)
            {
                var product = new ProductBuilder()
                    .WithCompany(Faker.Random.Int(1, 5))
                    .WithCategory(Faker.Random.Int(1, 3))
                    .WithIdentifiers(
                        new ProductIdentifierBuilder().AsUPC().Build(),
                        new ProductIdentifierBuilder().AsASIN().Build()
                    )
                    .WithCertificates(
                        new CertificateBuilder()
                            .WithCertificationType(Faker.Random.Int(1, 4))
                            .ExpiringIn(Faker.Random.Int(30, 365))
                            .Build()
                    )
                    .Build();
                    
                products.Add(product);
            }
            
            return products;
        }

        public static List<Certificate> CreateExpiringCertificates(int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => new CertificateBuilder()
                    .WithProduct(Faker.Random.Int(1, 10))
                    .WithCompany(Faker.Random.Int(1, 5))
                    .ExpiringIn(Faker.Random.Int(-30, 60)) // Some expired, some expiring soon
                    .Build())
                .ToList();
        }

        public static Dictionary<string, object> CreateCompleteTestDataSet()
        {
            var companies = CompanyBuilder.CreateMany(5);
            var products = ProductBuilder.CreateMany(20);
            var certificates = CertificateBuilder.CreateMany(30);
            
            // Link entities
            foreach (var product in products)
            {
                product.CompanyId = Faker.PickRandom(companies).Id;
            }
            
            foreach (var certificate in certificates)
            {
                certificate.ProductId = Faker.PickRandom(products).Id;
                certificate.CompanyId = products.First(p => p.Id == certificate.ProductId).CompanyId ?? 1;
            }
            
            return new Dictionary<string, object>
            {
                ["Companies"] = companies,
                ["Products"] = products,
                ["Certificates"] = certificates
            };
        }
    }
}