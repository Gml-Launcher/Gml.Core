namespace Modrinth.Api.Core.Filter
{
    public class ProjectModFilter : ProjectFilter
    {
        public ProjectModFilter()
        {
            AddFacet(ProjectFilterTypes.ProjectType, "mod");
        }
    }
}
