# Generated environment texture set

These original raster assets are intended for the first GENSHIN prototype pass. They are material inputs, not finished house or terrain prefabs.

- `Ground_DirtRoad_Stylized.png`: dirt and worn road albedo.
- `Ground_StonePaving_Stylized.png`: pale stone paving albedo.
- `House_Timber_Stylized.png`: warm timber wall/roof albedo.
- `House_Plaster_Interior_Stylized.png`: plaster interior albedo.
- `Decals_DirtMossAtlas_Transparent.png`: dirt, moss and damp-stain decal atlas.

Suggested Unity import settings for the first PC/RTX 2050 pass: Texture Type `Default`, sRGB enabled for albedo/decal color, `Wrap Mode: Repeat` for the four tileable surfaces, `Filter Mode: Trilinear`, and compression `Normal Quality` or `High Quality` depending on the final memory budget. The decal atlas can use alpha transparency and should be placed on a transparent material.
