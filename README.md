# NTexturePreview
Enhanced Texture Previewer for Unity

**Currently Unity 2018.3+**

### Advancements

- RGB Toggles (CTRL+CLICK for Single-Channel)
- Zooming & Panning
- Zoom Resets
- Matching Filter Mode (default preview is always Point-filtered)
- Normal Map Diffuse Preview (Right-Mouse, Right-Mouse+Scroll)

![gif](http://vertx.xyz/wp-content/uploads/2018/04/2dTexturePreview.gif)

![gif](http://vertx.xyz/wp-content/uploads/2018/04/NormalMapPreview2.gif)

### 3D Materials

#### Default Material
- Cube preview (default was sphere)
- XYZ Axis Preview Sliders

![gif](http://vertx.xyz/wp-content/uploads/2018/04/3dTexturePreview4.gif)

#### Overrides
You can override the Material used for the 3D Texture Preview. This is done by inheriting from N3DTexturePreview.I3DMaterialOverride.

An example is provided at NTexturePreview/Examples/Custom 3D/Editor/N3DTexturePreviewExample.cs. The file specifically operates on Texture3D assets named "3DTexturePreviewExample", but a method of your own might perform any logic to provide a custom preview material.

![gif](http://vertx.xyz/wp-content/uploads/2018/04/3dTexturePreview2.gif)


## Installation
Ensure your project is on .NET 4.x by navigating to Edit>Project Settings>Player>Configuration>Scripting Runtime Version and switching to .NET 4.x Equivalent.

Pull the project locally and use the Package Manager (Window>Package Manager), adding the package.json file present in the root of the folder with the `+` button.

Or edit your Package.json file to contain `"com.vertx.ntexturepreview": "https://github.com/vertxxyz/NTexturePreview.git",`