using System.Collections.Generic;
using System.Threading.Tasks;
using GmlCore.Interfaces.Integrations;
using GmlCore.Interfaces.News;

namespace Gml.Core.Integrations;

public class TelegramNewsProvider : INewsProvider
{
    public Task<IReadOnlyCollection<INews>> GetNews(int count = 20)
    {
        throw new System.NotImplementedException();
    }
}
