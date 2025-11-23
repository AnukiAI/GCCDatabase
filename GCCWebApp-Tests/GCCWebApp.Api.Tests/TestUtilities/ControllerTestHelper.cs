using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace GCCWebApp.Api.Tests.TestUtilities
{
    /// <summary>
    /// Helper class for setting up API controller tests with proper HttpContext mocking
    /// </summary>
    public static class ControllerTestHelper
    {
        /// <summary>
        /// Sets up HttpContext for a controller with all required services mocked
        /// </summary>
        /// <typeparam name="T">Controller type</typeparam>
        /// <param name="controller">The controller instance to configure</param>
        /// <param name="isDevelopment">Whether to simulate Development environment (default: true)</param>
        public static void SetupHttpContext<T>(T controller, bool isDevelopment = true) where T : ControllerBase
        {
            // Create mock HttpContext
            var mockHttpContext = new Mock<HttpContext>();

            // Setup TraceIdentifier
            mockHttpContext.Setup(c => c.TraceIdentifier)
                .Returns(Guid.NewGuid().ToString());

            // Setup RequestServices with required services
            var serviceCollection = new ServiceCollection();

            // Add logger factory
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            var mockLogger = new Mock<ILogger>();
            mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
                .Returns(mockLogger.Object);
            serviceCollection.AddSingleton(mockLoggerFactory.Object);

            // Add specific logger for the controller
            serviceCollection.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            // Add environment
            var mockEnvironment = new Mock<IHostEnvironment>();
            mockEnvironment.Setup(e => e.EnvironmentName)
                .Returns(isDevelopment ? "Development" : "Production");
            serviceCollection.AddSingleton(mockEnvironment.Object);

            // Add IWebHostEnvironment for legacy compatibility
            var mockWebHostEnvironment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            mockWebHostEnvironment.Setup(e => e.EnvironmentName)
                .Returns(isDevelopment ? "Development" : "Production");
            serviceCollection.AddSingleton(mockWebHostEnvironment.Object);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            mockHttpContext.Setup(c => c.RequestServices)
                .Returns(serviceProvider);

            // Setup Request
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Scheme).Returns("https");
            mockRequest.Setup(r => r.Host).Returns(new HostString("localhost", 5101));
            mockRequest.Setup(r => r.Path).Returns("/api/v1/certificates");
            mockRequest.Setup(r => r.PathBase).Returns(PathString.Empty);
            mockRequest.Setup(r => r.Method).Returns("GET");
            mockRequest.Setup(r => r.Headers).Returns(new HeaderDictionary());

            mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);

            // Setup Response
            var mockResponse = new Mock<HttpResponse>();
            mockResponse.Setup(r => r.Headers).Returns(new HeaderDictionary());
            mockHttpContext.Setup(c => c.Response).Returns(mockResponse.Object);

            // Setup User (anonymous by default)
            var mockUser = new System.Security.Claims.ClaimsPrincipal();
            mockHttpContext.Setup(c => c.User).Returns(mockUser);

            // Create ControllerContext and assign to controller
            var controllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            controller.ControllerContext = controllerContext;
        }

        /// <summary>
        /// Sets up HttpContext with authenticated user
        /// </summary>
        /// <typeparam name="T">Controller type</typeparam>
        /// <param name="controller">The controller instance to configure</param>
        /// <param name="username">Username for the authenticated user</param>
        /// <param name="roles">Roles for the authenticated user</param>
        /// <param name="isDevelopment">Whether to simulate Development environment (default: true)</param>
        public static void SetupHttpContextWithUser<T>(
            T controller,
            string username,
            string[] roles = null,
            bool isDevelopment = true) where T : ControllerBase
        {
            SetupHttpContext(controller, isDevelopment);

            // Create claims for the user
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };

            // Add role claims if provided
            if (roles != null)
            {
                foreach (var role in roles)
                {
                    claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
                }
            }

            var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuthentication");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            // Update the User in HttpContext
            var httpContext = controller.ControllerContext.HttpContext;
            var mockHttpContext = Mock.Get(httpContext);
            mockHttpContext.Setup(c => c.User).Returns(principal);
        }
    }

    /// <summary>
    /// Null logger implementation for testing
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class NullLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
    }
}
