# VFX Texture Library Intake Notes

Date: 2026-05-28

This note records how the local `VFX_Texture_Library_v1.0.0` download should be used with `ToioDistanceUnityLab`.

## Source

- BOOTH item: https://vfxstudio.booth.pm/items/8196708
- Game Makers article: https://gamemakers.jp/article/2026_05_27_138499/
- C&R Creative Studios blog: https://vfx.crdg.jp/tech/2026/05/26/5972/
- Local download source: `C:\Users\unocy\Downloads\VFX_Texture_Library_v1.0.0`
- Unity-local copy: `Assets/External/VFX_Texture_Library_v1.0.0`

## What The Library Contains

- Generic game-effect textures.
- Substance 3D Designer source data (`.sbs`).
- Custom Substance nodes for texture creation support.
- Local documentation entry: `Assets/External/VFX_Texture_Library_v1.0.0/README.html`

The `.sbs` files require Substance 3D Designer 15.0.1 or later. Unity can still use the exported texture files directly.

## License Handling

Use the library locally, but do not commit the downloaded payload.

Reasons:

- The BOOTH license permits use and modification, but prohibits redistribution or public copying.
- The texture payload is large and should not enter this SDK repository history.
- Commercial use is described as allowed by the article and source pages, with credit recommended rather than required.

The project `.gitignore` excludes `Assets/External/VFX_Texture_Library*/` for this reason.

## First ToioDistanceUnityLab Use Cases

Start small. The best fit is not a full VFX system yet, but one visible polish pass for the existing distance-cube hook:

1. Use `Line_*`, `Glow_*`, or `Gradient_*` textures on the Unity distance bar material.
2. Use `Aura_*` or `Hit_*` textures for short-lived endpoint feedback when Cube A or Cube B is captured.
3. Keep the Blender rich-distance object as the main visual; Unity should stay readable for recording.

## Operational Guardrail

This is a weekend/device-track enhancement, not a new main project. If it starts taking more than one short test pass, park it and return to the existing `toio x Unity x Blender` distance-cube flow.
