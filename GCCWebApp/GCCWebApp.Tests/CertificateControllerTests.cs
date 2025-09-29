using GCCWebApp.Models;

namespace GCCWebApp.Tests
{
    public class ControllerLogicTests
    {
        [Fact]
        public void CertificateEditViewModel_ShouldValidateNumber()
        {
            var viewModel = new CertificateEditViewModel
            {
                Number = "GCC001"
            };

            Assert.Equal("GCC001", viewModel.Number);
            Assert.True(viewModel.Number.Length <= 7);
        }

        [Fact]
        public void CertificateListRequest_ShouldHandlePagination()
        {
            var request = new CertificateListRequest
            {
                Page = 2,
                PageSize = 25
            };

            Assert.Equal(2, request.Page);
            Assert.Equal(25, request.PageSize);
        }

        [Theory]
        [InlineData(100, 10, 10)]
        [InlineData(95, 10, 10)]
        [InlineData(5, 10, 1)]
        public void PaginationLogic_ShouldCalculateCorrectPages(int totalCount, int pageSize, int expectedPages)
        {
            var response = new CertificateListResponse
            {
                TotalCount = totalCount,
                PageSize = pageSize
            };

            Assert.Equal(expectedPages, response.TotalPages);
        }
    }
}