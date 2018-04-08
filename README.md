# NTexturePreview
Enhanced Texture Previewer for Unity

### Advancements

- RGB Toggles
- Zooming & Panning
- Zoom Resets
- Matching Filter Mode

![gif](http://vertx.xyz/wp-content/uploads/2018/04/2dTexturePreview.gif)

### 3D Materials

You can override the Material used for the 3D Texture Preview. This is done by inheriting from N3DTexturePreview.I3DMaterialOverride.

An example is provided at NTexturePreview > Editor > N3DTexturePreviewExample.cs. Uncommenting this file will provide a simple raymarching material to all Texture3D previews.

![gif](http://vertx.xyz/wp-content/uploads/2018/04/3dTexturePreview.gif)