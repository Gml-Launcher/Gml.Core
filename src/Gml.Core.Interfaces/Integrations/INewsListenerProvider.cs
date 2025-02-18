using System.Collections.Generic;
using System.Threading.Tasks;
using GmlCore.Interfaces.News;

namespace GmlCore.Interfaces.Integrations;

public interface INewsListenerProvider
{
    Task<ICollection<INewsData>> GetNews(int count = 20);
    void RefreshAsync(long nubmer = 0);
    Task AddListener(INewsProvider newsProvider);
    Task RemoveListener(INewsProvider newsProvider);
}
