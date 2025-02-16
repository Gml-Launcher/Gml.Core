using System.Collections.Generic;
using System.Threading.Tasks;
using GmlCore.Interfaces.News;

namespace GmlCore.Interfaces.Integrations;

public interface INewsProvider
{
    Task<IReadOnlyCollection<INews>> GetNews(int count = 20);
}
