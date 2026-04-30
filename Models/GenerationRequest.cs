namespace AINovel.Models;

public record GenerationRequest(
    NovelCore Core,
    int GenerateType
);