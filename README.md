# NTexturePreview
Enhanced Texture Previewer for Unity

**Currently Unity 2017.3+**

### Advancements

- RGB Toggles (CTRL+CLICK for Single-Channel)
- Zooming & Panning
- Zoom Resets
- Matching Filter Mode (default preview is always Point-filtered)
- Normal Map Diffuse Preview (Right-Mouse, Right-Mouse+Scroll)

![gif](http://vertx.xyz/Images/NTexturePreview/2dTexturePreview2.gif)

![gif](http://vertx.xyz/Images/NTexturePreview/NormalMapPreview2.gif)

### 3D Materials

#### Default Material
- Cube preview (default was sphere)
- XYZ Axis Preview Sliders

![gif](http://vertx.xyz/Images/NTexturePreview/3dTexturePreview4.gif)

#### Overrides
You can override the Material used for the 3D Texture Preview. This is done by inheriting from N3DTexturePreview.I3DMaterialOverride.

An example is provided at NTexturePreview/Examples/Custom 3D/Editor/N3DTexturePreviewExample.cs. The file specifically operates on Texture3D assets named "3DTexturePreviewExample", but a method of your own might perform any logic to provide a custom preview material.

![gif](http://vertx.xyz/Images/NTexturePreview/3dTexturePreview2.gif)
