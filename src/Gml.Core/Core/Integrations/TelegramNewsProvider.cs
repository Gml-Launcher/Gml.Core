using System.Collections.Generic;
using System.Threading.Tasks;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Integrations;
using GmlCore.Interfaces.News;

namespace Gml.Core.Integrations;

public class TelegramNewsProvider : BaseNewsProvider
{
    public NewsListenerType Type { get; }
    public override Task<IReadOnlyCollection<INewsData>> GetNews(int count = 20)
    {
        throw new System.NotImplementedException();
    }
}
