using System.Text.Json.Serialization;

namespace Shop.Core.Entities.Catalog;

[JsonConverter(typeof(JsonStringEnumConverter<GameStates>))]
public enum GameStates
{
	Draft, // gra w fazie projektu
	Published, // gra opublikowana
	Archived,
	Suspended
}