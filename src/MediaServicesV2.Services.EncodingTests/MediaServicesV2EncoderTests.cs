using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaServicesV2.Services.Encoding.Media;
using MediaServicesV2.Services.Encoding.Presets;
using MediaServicesV2.Services.Encoding.Services.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace MediaServicesV2.Services.EncodingTests
{
    /// <summary>
    /// It is expected that the user will develop appropriate tests.
    /// These are simply to satisfy the sample yml pipelines.
    /// </summary>
    public class MediaServicesV2EncoderTests
    {
        private const string ExpectedPresetName = "SpriteOnlySetting";
        private static readonly ILogger<MediaServicesV2Encoder> Logger = Mock.Of<ILogger<MediaServicesV2Encoder>>();
        private static readonly List<Uri> ExpectedSourceUris = new List<Uri> { new Uri("https://yourinbox00sa.blob/somepath/somefile.mp4") };
        private static readonly Uri ExpectedAmsV2CallbackEndpoint = new Uri("https://my.service.com/amsv2statusendpoint");
        private static readonly JObject ExpectedOperationContext = new JObject()
            {
                new JProperty("someKey1", "That you need during job status or after completion."),
                new JProperty("someKey2", "It will be encoded into the first Task name, so it can be easily parsed later."),
            };

        [Fact]
        public void NoAmsV2CallbackEndpointShouldConstructWithoutException()
        {
            // Arrange
            var mediaServicesV2EncodeOperations = Mock.Of<IMediaServicesV2EncodeOperations>();
            var mediaServicesPreset = Mock.Of<IMediaServicesPreset>();
            var configuration = Mock.Of<IConfiguration>();
            AddMockSetting("AmsV2CallbackEndpoint", null, configuration);

            // Act
            // Assert
            IMediaServicesV2Encoder mediaServicesV2Encoder = new MediaServicesV2Encoder(Logger, mediaServicesV2EncodeOperations, mediaServicesPreset, configuration);
            mediaServicesV2Encoder.ShouldNotBeNull();
        }

        [Fact]
        public void BadAmsV2CallbackEndpointShouldConstructWithoutException()
        {
            // Arrange
            var mediaServicesV2EncodeOperations = Mock.Of<IMediaServicesV2EncodeOperations>();
            var mediaServicesPreset = Mock.Of<IMediaServicesPreset>();
            var configuration = Mock.Of<IConfiguration>();
            AddMockSetting("AmsV2CallbackEndpoint", "This is not a uri.", configuration);

            // Act
            // Assert
            IMediaServicesV2Encoder mediaServicesV2Encoder = new MediaServicesV2Encoder(Logger, mediaServicesV2EncodeOperations, mediaServicesPreset, configuration);
            mediaServicesV2Encoder.ShouldNotBeNull();
        }

        [Fact]
        public void ValidAmsV2CallbackEndpointShouldConstructWithoutException()
        {
            // Arrange
            var mediaServicesV2EncodeOperations = Mock.Of<IMediaServicesV2EncodeOperations>();
            var mediaServicesPreset = Mock.Of<IMediaServicesPreset>();
            var configuration = Mock.Of<IConfiguration>();
            AddMockSetting("AmsV2CallbackEndpoint", ExpectedAmsV2CallbackEndpoint.ToString(), configuration);

            // Act
            // Assert
            IMediaServicesV2Encoder mediaServicesV2Encoder = new MediaServicesV2Encoder(Logger, mediaServicesV2EncodeOperations, mediaServicesPreset, configuration);
            mediaServicesV2Encoder.ShouldNotBeNull();
        }

        [Fact]
        public async Task NullPresetNameShouldThrowArgumentException()
        {
            // Arrange
            var mediaServicesV2EncodeOperations = Mock.Of<IMediaServicesV2EncodeOperations>();
            var mediaServicesPreset = Mock.Of<IMediaServicesPreset>();
            var configuration = Mock.Of<IConfiguration>();
            AddMockSetting("AmsV2CallbackEndpoint", ExpectedAmsV2CallbackEndpoint.ToString(), configuration);
            var mediaServicesV2Encoder = new MediaServicesV2Encoder(Logger, mediaServicesV2EncodeOperations, mediaServicesPreset, configuration);

            // Act
            // Assert
            _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await mediaServicesV2Encoder.EncodeCreateAsync(
                    null,
                    ExpectedSourceUris,
                    null,
                    ExpectedOperationContext)
                .ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task NullSourceUrisShouldThrowArgumentException()
        {
            // Arrange
            var mediaServicesV2EncodeOperations = Mock.Of<IMediaServicesV2EncodeOperations>();
            var mediaServicesPreset = Mock.Of<IMediaServicesPreset>();
            var configuration = Mock.Of<IConfiguration>();
            AddMockSetting("AmsV2CallbackEndpoint", ExpectedAmsV2CallbackEndpoint.ToString(), configuration);
            var mediaServicesV2Encoder = new MediaServicesV2Encoder(Logger, mediaServicesV2EncodeOperations, mediaServicesPreset, configuration);

            // Act
            // Assert
            _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await mediaServicesV2Encoder.EncodeCreateAsync(
                    ExpectedPresetName,
                    null,
                    null,
                    ExpectedOperationContext)
                .ConfigureAwait(false)).ConfigureAwait(false);
        }

        private static void AddMockSetting(string settingName, string settingValue, IConfiguration configuration)
        {
            var configurationSection = new Mock<IConfigurationSection>();
            configurationSection.Setup(a => a.Value).Returns(settingValue);
            Mock.Get(configuration).Setup(a => a.GetSection(settingName)).Returns(configurationSection.Object);
        }
    }
}
