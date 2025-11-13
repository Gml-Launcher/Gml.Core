using Gml.Common.TextureService;
using Newtonsoft.Json;

namespace GmlCore.Tests;

[TestFixture]
public class TextureParseTests
{
    [Test]
    public void DeserializeTextureReadDto_ValidJson_ReturnsCorrectObject()
    {
        // Arrange
        var json = @"{
            ""userName"": ""TestUser"",
            ""hasCloak"": true,
            ""hasSkin"": true,
            ""skinUrl"": ""http://example.com/skin.png"",
            ""clockUrl"": ""http://example.com/clock.png"",
            ""skinFormat"": 1,
            ""texture"": {
                ""head"": ""head_texture"",
                ""front"": ""front_texture"",
                ""back"": ""back_texture"",
                ""cloakBack"": ""cloak_back_texture"",
                ""cloak"": ""cloak_texture""
            }
        }";

        // Act
        var result = JsonConvert.DeserializeObject<TextureReadDto>(json);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.UserName, Is.EqualTo("TestUser"));
            Assert.That(result.HasCloak, Is.True);
            Assert.That(result.HasSkin, Is.True);
            Assert.That(result.SkinUrl, Is.EqualTo("http://example.com/skin.png"));
            Assert.That(result.ClockUrl, Is.EqualTo("http://example.com/clock.png"));
            Assert.That(result.SkinFormat, Is.EqualTo(1));
            Assert.That(result.Texture.Head, Is.EqualTo("head_texture"));
            Assert.That(result.Texture.Front, Is.EqualTo("front_texture"));
            Assert.That(result.Texture.Back, Is.EqualTo("back_texture"));
            Assert.That(result.Texture.CloakBack, Is.EqualTo("cloak_back_texture"));
            Assert.That(result.Texture.Cloak, Is.EqualTo("cloak_texture"));
        });
    }

    [Test]
    public void DeserializeTextureReadDto_EmptyJson_ReturnsObjectWithNullValues()
    {
        // Arrange
        var json = "{}";

        // Act
        var result = JsonConvert.DeserializeObject<TextureReadDto>(json);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.UserName, Is.Null);
            Assert.That(result.HasCloak, Is.False);
            Assert.That(result.HasSkin, Is.False);
            Assert.That(result.SkinUrl, Is.Null);
            Assert.That(result.ClockUrl, Is.Null);
            Assert.That(result.SkinFormat, Is.EqualTo(0));
            Assert.That(result.Texture, Is.Null);
        });
    }
}
