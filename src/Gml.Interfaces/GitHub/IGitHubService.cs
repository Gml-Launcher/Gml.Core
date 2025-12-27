using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GmlCore.Interfaces.GitHub;

public interface IGitHubService
{
    IObservable<string> Logs { get; }
    Task<IEnumerable<string>> GetRepositoryBranches(string user, string repository);
    Task<IReadOnlyCollection<string>> GetRepositoryTags(string user, string repository);
    Task<string> DownloadProject(string projectPath, string branchName, string repoUrl);
    Task EditLauncherFiles(string projectPath, string host, string folder);
}
