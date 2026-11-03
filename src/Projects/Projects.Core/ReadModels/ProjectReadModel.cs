using Projects.Core.Entities;

namespace Projects.Core.ReadModels;

internal record ProjectReadModel(Guid Id, string? Name, string? Description, Guid MainSceneFileId,DateTime LastUpdate)
{
	public static ProjectReadModel From(Project project) =>
		new(project.Id, project.Name, project.Description, project.MainSceneFileId, project.LastUpdate);
}