# Nwn.Toolset.Avalonia

Reusable Avalonia 11 controls for Neverwinter Nights authoring tools, built on Nwn.Authoring and Nwn.Preview. Targets `net10.0`. Rendering uses OpenGL through Silk.NET.

- Area and model viewports, area editor surface, creation, property and instance forms.
- Palettes, module explorer, appearance gallery.
- Field, variable, behavior, sound, door, waypoint and trigger editors.
- Dock-based tool layout and shared theme.

The controls are presentation only. The host application supplies state through interfaces such as `IAreaCreationFormState` and `IAreaViewportMaterialProvider`.

Include the shared styles in `App.axaml`:

```xml
<Application.Styles>
  <FluentTheme />
  <StyleInclude Source="avares://Nwn.Toolset.Avalonia/Styles/ToolsetStyles.axaml" />
</Application.Styles>
```

Use a viewport in a view:

```xml
<UserControl xmlns:nwn="using:Nwn.Toolset.Avalonia.Areas">
  <nwn:AreaViewportControl />
</UserControl>
```

Part of the Nwn.* toolset libraries (Nwn.Formats, Nwn.Authoring, Nwn.Preview, Nwn.Toolset.Avalonia). Released as previews; APIs may change before 1.0.

Licensed under MIT. Some source was extracted from SWLOR (MIT, copyright 2019 Zunath); its notice is in `licenses/SWLOR-MIT.txt` inside the package.
