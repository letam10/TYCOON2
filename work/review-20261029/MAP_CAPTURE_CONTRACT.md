# Contract: map ảnh chụp thực tế — 29/10/2026

Yêu cầu mới thay thế quyết định map vector trước đó: chụp thế giới 3D từ trên xuống theo từng ô,
đặt mỗi ô đúng world bounds; nền map/minimap dùng ảnh này, icon/player/POI vẫn là lớp động.
Giữ camera gameplay, nhân vật và Farm logic. Không dùng ImageGen cho nền map.

## File và interface
- CityMapCaptureGrid.cs: sealed class, constructor (Bounds bounds, int columns, int rows, int tileWidth).
  Properties Bounds, Columns, Rows, TileWidth, TileHeight, AtlasWidth, AtlasHeight, TileCount.
  Methods TileBounds(int index): Bounds; TilePixels(int index): RectInt; TileUv(int index): Rect;
  WorldToUv(Vector3 point): Vector2; UvToWorld(Vector2 uv): Vector3.
- CityMapCapture.cs: MonoBehaviour service, static Ensure(GameSession): CityMapCapture;
  public Texture2D Atlas, CityMapCaptureGrid Grid, bool IsCapturing, int Generation,
  float MaximumProjectionErrorPixels; event Action Changed; RequestRefresh().
  Camera orthographic riêng, rotation (90,0,0), world X sang phải và Z lên trên; không sửa FollowCamera.
- CityMapCaptureVisibility.cs: IDisposable scope, lưu/khôi phục renderer/canvas/fog;
  loại người/xe chuyển động, nhãn và HUD; world/collider/transaction không đổi.
- CityMapGraphic.cs: mainTexture từ atlas, vẽ 12 quad tile từ cùng Position(world);
  overlay dùng pixel trắng riêng ở atlas. Không vẽ lại đường/nhà vector đè ảnh chụp.
  WorldAt(Vector2 normalized): Vector3 là inverse của Position; Pan chuyển delta viewport sang world.
- GameHudMap.cs: viewport mask chung cho nền/POI/labels; OpenMap yêu cầu refresh snapshot.
- CityMapPhotographyTests.cs: grid coverage, tile seams/UV, north orientation, map inverse và pan/rotate.
- QaMapPhotography.cs: partial QaDriver, MapPhotographyAcceptance(): IEnumerator;
  chờ capture, kiểm native render/projection, xuất atlas + manifest, chụp pan/zoom/rotate.

## Grid và ownership
- 4 cột × 3 hàng, tileWidth=1024. tileHeight=round(1024 × cellWorldDepth/cellWorldWidth).
- Mỗi ô dùng đúng bounds chia đều của CityDistricts.WorldBounds; không đo tọa độ bằng mắt.
- Atlas thêm viền 1 pixel; pixel (0,0) trắng dành cho mesh icon. TilePixels/TileUv cùng nguồn.
- Camera aspect = cellWorldWidth/cellWorldDepth; orthographicSize=cellWorldDepth/2.
- Capture từng ô rồi ghép đúng tọa độ pixel; atlas cũ chỉ được thay khi bộ mới hoàn chỉnh.
- Map projection: rotate world delta trước, sau đó chia ViewBounds size; unproject làm ngược.
- Pan: normalizedDelta × view size, rotate theo chiều ngược phép vẽ, sau đó cập nhật center.
- Cả nền, icon, click target và player dùng CityMapGraphic.Position. RectMask2D cắt ở viewport.
- Snapshot refresh sau khởi tạo world và mỗi lần mở map. Không render 12 camera mỗi frame.
- Gameplay owns dt. Map capture không nhân speed/dt và không thay timeScale.

## Gate
- Coverage toàn world, tile cạnh chung không gap, round-trip world/UV sai số <0,001m.
- Camera pixel projection sai số <0,5 pixel trên corners/center các tile.
- Ảnh chứa terrain/roof/road thật, không HUD/nhãn/nhân vật đóng băng, không blank/magenta.
- Pan/zoom/rotate ảnh và overlay khớp, không tràn minimap/full map viewport.
- Player mới, test focused mới, xem atlas và ảnh map ở zoom/pan/rotate; không dùng pass cũ.
