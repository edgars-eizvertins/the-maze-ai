using Labyrinth.Application.Content;

namespace Labyrinth.Application.Abstractions;

/// <summary>Read-only access to the static game content (the 387 sections).</summary>
public interface ISectionRepository
{
    SectionContent Get(int id);
    bool Exists(int id);
    int Count { get; }
}
