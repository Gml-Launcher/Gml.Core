using Modrinth.Api.Core.Filter;
using Modrinth.Api.Core.Projects;

namespace Modrinth.Api.Core.System
{
    public static class ProjectHelper
    {
        internal static ProjectType GetProjectType(string projectTypeString)
        {
            return projectTypeString switch
            {
                ProjectFilterTypesStrings.Mod => ProjectType.Mod,
                ProjectFilterTypesStrings.Shader => ProjectType.Shader,
                ProjectFilterTypesStrings.ResourcePack => ProjectType.ResourcePack,
                ProjectFilterTypesStrings.ModPack => ProjectType.ModPack,
                _ => ProjectType.Undefined
            };
        }
    }
}
