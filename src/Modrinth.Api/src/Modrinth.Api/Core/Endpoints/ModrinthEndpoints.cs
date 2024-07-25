namespace Modrinth.Api.Core.Endpoints
{
    internal static class ModrinthEndpoints
    {
        internal static string SearchProjects = "/v2/search";
        internal static string Project = "/v2/project/";
        internal static string ProjectVersions = "/v2/project/{id}/version";
        internal static string Version = "/v2/version/{id}";
    }
}
