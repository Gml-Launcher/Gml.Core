namespace Gml.Dto.Texture;

public class UrlServiceDto
{
    public UrlServiceDto()
    {
    }
    
    public UrlServiceDto(string url)
    {
        Url = url;
    }

    public string Url { get; set; }
}
