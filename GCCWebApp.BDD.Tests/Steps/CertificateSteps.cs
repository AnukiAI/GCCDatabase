using TechTalk.SpecFlow;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using GCCWebApp.Models;
using Xunit;

namespace GCCWebApp.BDD.Tests.Steps
{
    [Binding]
    public class CertificateSteps
    {
        private readonly ScenarioContext _scenarioContext;
        private readonly WebApplicationFactory<Program> _factory;
        private HttpClient _client;
        private HttpResponseMessage _response;
        private Certificate _certificate;
        private List<Certificate> _certificates;

        public CertificateSteps(ScenarioContext scenarioContext, WebApplicationFactory<Program> factory)
        {
            _scenarioContext = scenarioContext;
            _factory = factory;
            _client = _factory.CreateClient();
            _certificates = new List<Certificate>();
        }

        [Given(@"I am logged in as a compliance officer")]
        public async Task GivenIAmLoggedInAsComplianceOfficer()
        {
            // Simulate authentication
            _client.DefaultRequestHeaders.Add("X-Test-User", "compliance.officer@test.com");
            _client.DefaultRequestHeaders.Add("X-Test-Role", "ComplianceOfficer");
        }

        [Given(@"the following companies exist:")]
        public async Task GivenTheFollowingCompaniesExist(Table table)
        {
            var companies = new List<Company>();
            foreach (var row in table.Rows)
            {
                var company = new Company
                {
                    Name = row["Name"],
                    Country = row["Country"],
                    Location = row["Location"],
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                companies.Add(company);
            }
            _scenarioContext["Companies"] = companies;
            
            // Send to API or save to test database
            foreach (var company in companies)
            {
                var json = JsonConvert.SerializeObject(company);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _client.PostAsync("/api/companies", content);
            }
        }

        [Given(@"the following products exist:")]
        public async Task GivenTheFollowingProductsExist(Table table)
        {
            var products = new List<Product>();
            var companies = _scenarioContext["Companies"] as List<Company>;
            
            foreach (var row in table.Rows)
            {
                var company = companies?.FirstOrDefault(c => c.Name == row["Company"]);
                var product = new Product
                {
                    Name = row["Name"],
                    CompanyId = company?.Id ?? 1,
                    Sku = row["SKU"],
                    CategoryId = GetCategoryId(row["Category"]),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                products.Add(product);
            }
            _scenarioContext["Products"] = products;
            
            // Send to API or save to test database
            foreach (var product in products)
            {
                var json = JsonConvert.SerializeObject(product);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _client.PostAsync("/api/products", content);
            }
        }

        [Given(@"I navigate to the certificate creation page")]
        public async Task GivenINavigateToTheCertificateCreationPage()
        {
            _response = await _client.GetAsync("/certificates/create");
            _response.EnsureSuccessStatusCode();
        }

        [When(@"I create a certificate with the following details:")]
        public async Task WhenICreateACertificateWithTheFollowingDetails(Table table)
        {
            _certificate = new Certificate();
            var products = _scenarioContext.Get<List<Product>>("Products");
            
            foreach (var row in table.Rows)
            {
                switch (row["Field"])
                {
                    case "Product":
                        var product = products.FirstOrDefault(p => p.Name == row["Value"]);
                        _certificate.ProductId = product?.Id ?? 1;
                        break;
                    case "Certificate Type":
                        _certificate.CertificationTypeId = GetCertificateTypeId(row["Value"]);
                        break;
                    case "Certificate Number":
                        _certificate.Number = row["Value"];
                        break;
                    case "Effective Date":
                        _certificate.EffectiveDate = DateTime.Parse(row["Value"]);
                        break;
                    case "Expiration Date":
                        _certificate.ExpirationDate = DateTime.Parse(row["Value"]);
                        break;
                    case "Status":
                        _certificate.Status = row["Value"];
                        break;
                }
            }
            
            var json = JsonConvert.SerializeObject(_certificate);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _response = await _client.PostAsync("/api/certificates", content);
        }

        [Then(@"the certificate should be created successfully")]
        public void ThenTheCertificateShouldBeCreatedSuccessfully()
        {
            _response.Should().NotBeNull();
            _response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        }

        [Then(@"I should see a success message ""(.*)""")]
        public async Task ThenIShouldSeeASuccessMessage(string expectedMessage)
        {
            var content = await _response.Content.ReadAsStringAsync();
            content.Should().Contain(expectedMessage);
        }

        [Then(@"the certificate should appear in the certificate list")]
        public async Task ThenTheCertificateShouldAppearInTheCertificateList()
        {
            var listResponse = await _client.GetAsync("/api/certificates");
            listResponse.EnsureSuccessStatusCode();
            
            var content = await listResponse.Content.ReadAsStringAsync();
            var certificates = JsonConvert.DeserializeObject<List<Certificate>>(content);
            
            certificates.Should().NotBeNull();
            certificates.Should().Contain(c => c.Number == _certificate.Number);
        }

        [When(@"I attempt to create a certificate without required fields")]
        public async Task WhenIAttemptToCreateACertificateWithoutRequiredFields()
        {
            _certificate = new Certificate(); // Empty certificate
            var json = JsonConvert.SerializeObject(_certificate);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _response = await _client.PostAsync("/api/certificates", content);
        }

        [Then(@"I should see validation errors for:")]
        public async Task ThenIShouldSeeValidationErrorsFor(Table table)
        {
            _response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
            var content = await _response.Content.ReadAsStringAsync();
            var errors = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(content);
            
            foreach (var row in table.Rows)
            {
                errors.Should().ContainKey(row["Field"]);
                errors[row["Field"]].Should().Contain(row["Error Message"]);
            }
        }

        [Given(@"the following certificates exist:")]
        public async Task GivenTheFollowingCertificatesExist(Table table)
        {
            var products = _scenarioContext.Get<List<Product>>("Products");
            
            foreach (var row in table.Rows)
            {
                var product = products.FirstOrDefault(p => p.Name == row["Product"]);
                var expirationDate = ParseRelativeDate(row["Expiration Date"]);
                
                var certificate = new Certificate
                {
                    ProductId = product?.Id ?? 1,
                    Number = row["Certificate Number"],
                    ExpirationDate = expirationDate,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };
                
                _certificates.Add(certificate);
                
                var json = JsonConvert.SerializeObject(certificate);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _client.PostAsync("/api/certificates", content);
            }
            
            _scenarioContext["Certificates"] = _certificates;
        }

        [When(@"I view the dashboard")]
        public async Task WhenIViewTheDashboard()
        {
            _response = await _client.GetAsync("/dashboard");
            _response.EnsureSuccessStatusCode();
        }

        [Then(@"I should see expiration warnings for:")]
        public async Task ThenIShouldSeeExpirationWarningsFor(Table table)
        {
            var content = await _response.Content.ReadAsStringAsync();
            
            foreach (var row in table.Rows)
            {
                content.Should().Contain(row["Product"]);
                content.Should().Contain(row["Warning Message"]);
            }
        }

        private int GetCategoryId(string categoryName)
        {
            // Mock implementation - would query actual database in real scenario
            return categoryName switch
            {
                "Electronics" => 1,
                "Safety" => 2,
                "Medical" => 3,
                _ => 1
            };
        }

        private int GetCertificateTypeId(string typeName)
        {
            // Mock implementation - would query actual database in real scenario
            return typeName switch
            {
                "ISO 9001" => 1,
                "CE Mark" => 2,
                "FDA Approval" => 3,
                _ => 1
            };
        }

        private DateTime ParseRelativeDate(string relativeDate)
        {
            if (relativeDate.Contains("days from now"))
            {
                var days = int.Parse(relativeDate.Split(' ')[0]);
                return DateTime.UtcNow.AddDays(days);
            }
            return DateTime.Parse(relativeDate);
        }
    }
}