using System.Net;
using System.Net.Http;
using ForestFireWebApp.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;

public class WeatherServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IFireRiskOnnxPredictor> _mockPredictor;

    public WeatherServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockPredictor = new Mock<IFireRiskOnnxPredictor>();

        _mockConfig.Setup(c => c["OpenWeather:ApiKey"]).Returns("fake-api-key");
    }

    private WeatherService CreateService(HttpClient httpClient)
    {
        return new WeatherService(_mockConfig.Object, _mockPredictor.Object, httpClient)
        {
            // Inject custom HttpClient if needed
        };
    }

    [Fact]
    public async Task GetStatesAsync_ReturnsListOfStates()
    {
        var service = CreateService(null);
        var states = await service.GetStatesAsync();

        Assert.Contains("Acre", states);
        Assert.Contains("Sao Paulo", states);
        Assert.Equal(25, states.Count); // Only 25 in your coordinates list
    }

    [Fact]
    public async Task GetWeatherAndFirePredictionAsync_InvalidState_ReturnsNull()
    {
        var service = CreateService(null);
        var result = await service.GetWeatherAndFirePredictionAsync("InvalidState");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetWeatherAndFirePredictionAsync_ValidState_ReturnsPredictions()
    {
        // Arrange
        var state = "Acre";
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        string mockJson = @"{
            ""list"": [
                {
                    ""dt"": 1714857600,
                    ""main"": { ""temp_max"": 33, ""temp_min"": 22, ""humidity"": 70 },
                    ""wind"": { ""speed"": 5 }
                },
                {
                    ""dt"": 1714868400,
                    ""main"": { ""temp_max"": 35, ""temp_min"": 21, ""humidity"": 60 },
                    ""wind"": { ""speed"": 7 }
                }
            ]
        }";

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(mockJson)
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);

        _mockPredictor.Setup(p => p.Predict(It.IsAny<float[]>())).Returns(120); // Medium Risk

        var service = new WeatherService(_mockConfig.Object, _mockPredictor.Object, httpClient);

        // Act
        var result = await service.GetWeatherAndFirePredictionAsync(state);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        foreach (var prediction in result)
        {
            Assert.Equal("Medium Risk", prediction.RiskLevel);
            Assert.InRange(prediction.Probability, 0, 100);
        }
    }

    [Fact]
    public async Task GetWeatherAndFirePredictionAsync_ApiFails_ReturnsNull()
    {
        var state = "Acre";
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        var client = new HttpClient(mockHandler.Object);
        var service = new WeatherService(_mockConfig.Object, _mockPredictor.Object, client);

        var result = await service.GetWeatherAndFirePredictionAsync(state);

        Assert.Null(result);
    }
}
