using Users.Core.Entities;

namespace Users.Core.ReadModels;

internal record ProjectReadModel(Guid Id, string? Name, string? Description, DateTime LastUpdate)
{
	public static ProjectReadModel From(Project project) =>
		new(project.Id, project.Name, project.Description, project.LastUpdate);
}