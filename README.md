# NTexturePreview
Enhanced Texture Previewer for Unity

### Advancements

- RGB Toggles
- Zooming & Panning
- Zoom Resets
- Matching Filter Mode
- Normal Map Diffuse Preview

![gif](http://vertx.xyz/wp-content/uploads/2018/04/2dTexturePreview.gif)

![gif](http://vertx.xyz/wp-content/uploads/2018/04/NormalMapPreview2.gif)

### 3D Materials

#### Default Material
- Cube preview (default was sphere)
- XUZ Axis Preview Sliders

#### Overrides
You can override the Material used for the 3D Texture Preview. This is done by inheriting from N3DTexturePreview.I3DMaterialOverride.

An example is provided at NTexturePreview > Editor > N3DTexturePreviewExample.cs. The file specifically operates on Texture3D assets named "3DTexturePreviewExample", but a method of your own might perform any logic to provide a custom preview material.

![gif](http://vertx.xyz/wp-content/uploads/2018/04/3dTexturePreview2.gif)