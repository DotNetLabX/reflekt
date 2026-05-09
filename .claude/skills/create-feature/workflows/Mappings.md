# Create Mappings (Mapster)

## Pattern

Mapster configs are auto-discovered via `IRegister` interface + assembly scan:

```csharp
public class {Domain}Mappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<{Source}, {Destination}>()
            .Map(dest => dest.{Prop}, src => src.{OtherProp});
    }
}
```

### Location by service variant

| Variant | Path |
|---------|------|
| MediatR (Application layer) | `{Svc}.Application/Mappings/{Domain}Mappings.cs` |
| FastEndpoints (API layer) | `{Svc}.API/Mappings/{Domain}Mappings.cs` |

## Registration

Auto-discovered — no manual registration beyond the assembly scan:
```csharp
TypeAdapterConfig.GlobalSettings.Scan(assembly);
// or: services.AddMapsterConfigsFromAssemblyContaining<GrpcMappings>();
```

## Inline Mapping (No Config Needed)

For simple cases where property names match:
```csharp
var response = entity.Adapt<{ResponseType}>();
var entity = command.Adapt<{Entity}>();
```

## Post-Mapping Actions

Use `AdaptWith<T>()` for mutations after mapping:
```csharp
var dto = entity.AdaptWith<EditorDto>(dest =>
{
    dest.Role = editorRole.EditorRole;
});
```

## Records

For immutable record types, use `MapToConstructor()`:
```csharp
config.NewConfig<Source, DestinationRecord>().MapToConstructor();
```

## Location

See "Location by service variant" table above. Check the service's CLAUDE.md for which variant applies.
