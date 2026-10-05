# Audit và tích hợp thư viện ASSET — 2026-10-05

Phạm vi: hoàn thiện hình ảnh Công đoạn 14 trong project hiện có. Không đổi recipe, giá, progression, stable ID, owner, transaction, Input System hoặc save schema.

## Rà soát toàn bộ

- Đã đọc/hash **7,553 file**, 409.5 MiB; **3,668 file model** gồm nhiều định dạng của cùng model, không phải số asset độc lập.
- 177 nhóm file giống hệt SHA-256. Không xóa hay gom lại nguồn của người dùng.
- glTF/GLB: header/version, mesh/triangle metadata, buffer/texture phụ thuộc; OBJ: vertex/face; FBX: header. Không phát hiện lỗi ở các kiểm tra này. Đây không phải kiểm tra deformation/animation của mọi FBX trong thư viện.
- Đã xem các preview tổng bộ Kenney và render toàn bộ 48 model được chọn trong Windows Player. Các asset không được chọn chỉ được rà soát file/metadata và phân loại, không được tuyên bố đã nghiệm thu từng rig.
- Inventory đầy đủ, hash, dependency metadata và duplicate groups: `work/art-refresh/library-audit.json`.

| Bộ nguồn | File | File model (kể cả format khác) | Model chọn mới | Xử lý |
|---|---:|---:|---:|---|
| Fonts | 2 | 0 | 0 | Giữ font tiếng Việt Noto Sans. |
| Generated | 1 | 0 | 0 | Giữ brand hiện có. |
| UserProvided | 7 | 6 | 0 | Giữ Character1/2/3, Cow/Chicken có animation; Machine2 giữ nguồn/prefab, quầy dùng POS mới. |
| animals | 2 | 1 | 0 | Giữ model bò nguồn và bản Cow của người dùng đang có rig. |
| buildings | 7 | 6 | 1 | Dùng OpenBarn; ChickenCoop đã import được giữ và đưa vào chuồng. |
| characters | 56 | 56 | 0 | Giữ rig/clip hiện tại. Bộ modular chưa có License.txt cục bộ; không thay rig bằng mesh rời. |
| crops | 4 | 3 | 0 | Giữ carrot_crop; không đưa corn vào gameplay chưa có route. |
| food | 3 | 2 | 0 | Giữ nguồn cũ, chuyển model hàng đang dùng sang bộ Kenney đồng bộ. |
| kaykit_adventures_characters | 72 | 32 | 0 | Fantasy/weapon không hợp bối cảnh tycoon; không import. |
| kaykit_city_builder | 208 | 102 | 3 | Dùng đèn, ghế, thùng rác; không đưa xe/nhà thành phố vào layout. |
| kaykit_furniture | 216 | 106 | 1 | Dùng ghế gỗ; các đồ phòng ngủ được giữ ở thư viện. |
| kaykit_restaurant_bits | 592 | 294 | 17 | Quầy, oven, bếp, bàn, chậu rửa, tủ lạnh và dụng cụ. |
| kenney_food_kit | 1009 | 600 | 12 | Hình ảnh SKU thật và dụng cụ bếp; không mở thêm loại hàng. |
| kenney_furniture_kit | 1546 | 700 | 5 | Kệ, POS, chậu cây, máy cà phê; các đồ còn lại giữ ở thư viện. |
| kenney_industrial_kit | 201 | 111 | 0 | Model chủ yếu là nhà/prop công nghiệp; không dùng thay processing machine sai chức năng. |
| kenney_nature_kit | 3618 | 1645 | 9 | Luống, lúa, cây, bụi, đá và hàng rào; không thêm terrain/cliff vào lối đi. |
| sushi_restaurant | 5 | 4 | 0 | Bốn character động vật, thiếu license cục bộ, khác style nhân vật hiện tại; không import. |

## Mapping đang dùng

- Bản import nằm tại `Assets/_Game/Art/Library/`, có prefab, FBX, texture URP và license CC0. Không chỉnh trực tiếp `ASSET`. Các source hash trước/sau phải khớp.
- 48 model, 12372 triangles tổng thư viện, tối đa 1612 triangles/model. Static meshes không có collider; footprint/interaction vẫn do gameplay hiện tại xác định.
- Model item thay qua GameCatalog cùng key cũ; ItemPool và InventoryStack tiếp tục thể hiện lượng/type thật. Không dùng crate có đồ ăn sẵn để giả lập stock.
- Bàn Restaurant hiển thị món khi customer basket thật có meal; dirty plate theo Cleaning, empty plate khi gọi món. Đây là view, không cập nhật order/payment/inventory.
- Quy tắc palette: cây/lá xanh, lúa vàng, đất nâu; sàn bếp cream/sage giảm tương phản. Chỉ bản import/material đổi màu, texture nguồn giữ nguyên.
- Nhân vật/vật nuôi đã có rig và custom processing machine được giữ; quầy POS, oven/kitchen, kệ, luống và scenery dùng asset mới.
- Mapping/path/SHA/license/bounds/material đầy đủ: `Docs/asset-library-import.json`.

| Key | Nguồn | Triangles | Kích thước Unity x/y/z (m) |
|---|---|---:|---|
| carrot | `ASSET/Downloaded/kenney_food_kit/Models/GLB format/carrot.glb` | 148 | 0.201/0.420/0.201 |
| tomato | `ASSET/Downloaded/kenney_food_kit/Models/GLB format/tomato.glb` | 132 | 0.360/0.303/0.360 |
| milk | `ASSET/Downloaded/kenney_food_kit/Models/GLB format/carton.glb` | 94 | 0.156/0.400/0.156 |
| egg | `ASSET/Downloaded/kenney_food_kit/Models/GLB format/egg.glb` | 44 | 0.197/0.280/0.227 |
| beef | `ASSET/Downloaded/kenney_food_kit/Models/GLB format/meat-raw.glb` | 110 | 0.349/0.047/0.420 |
| flour | `ASSET/Downloaded/kenney_food_kit/Models/GLB format/bag.glb` | 46 | 0.132/0.360/0.247 |
| cheese | `ASSET/Downloaded/kenney_food_kit/Models/GLB format/cheese-cut.glb` | 160 | 0.400/0.021/0.275 |
| sauce | `ASSET/Downloaded/kenney_food_kit/Models/GLB format/bottle-ketchup.glb` | 96 | 0.143/0.390/0.165 |
| bread | `ASSET/Downloaded/kenney_food_kit/Models/GLB format/loaf.glb` | 116 | 0.430/0.328/0.271 |
| cake | `ASSET/Downloaded/kenney_food_kit/Models/GLB format/cake.glb` | 750 | 0.440/0.188/0.440 |
| meal | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/food_dinner.gltf` | 1196 | 0.450/0.394/0.412 |
| wheat_crop | `ASSET/Downloaded/kenney_nature_kit/Models/GLTF format/crops_wheatStageB.glb` | 360 | 0.706/0.700/0.585 |
| crop_soil | `ASSET/Downloaded/kenney_nature_kit/Models/GLTF format/crops_dirtDoubleRow.glb` | 88 | 3.900/0.140/3.400 |
| crop_leaves | `ASSET/Downloaded/kenney_nature_kit/Models/GLTF format/crops_leafsStageB.glb` | 84 | 0.288/0.500/0.288 |
| floor_tile | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/floor_kitchen.gltf` | 48 | 4.000/0.028/4.000 |
| display_counter | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/kitchentable_B_large.gltf` | 236 | 1.450/1.000/4.700 |
| checkout_counter | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/kitchencounter_straight_A.gltf` | 168 | 2.100/1.000/1.100 |
| storage_rack | `ASSET/Downloaded/kenney_furniture_kit/Models/GLTF format/bookcaseOpen.glb` | 320 | 3.000/1.650/0.850 |
| supply_crate | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/crate.gltf` | 132 | 1.150/0.700/1.150 |
| feed_trough | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/crate.gltf` | 132 | 1.900/0.800/1.800 |
| farm_shelter | `ASSET/Downloaded/buildings/FBX/OpenBarn.fbx` | 1612 | 4.000/3.200/2.200 |
| oven_asset | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/oven.gltf` | 542 | 2.250/1.650/1.800 |
| cooking_range | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/stove_multi.gltf` | 1084 | 2.400/1.050/1.800 |
| prep_table | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/kitchentable_A_large.gltf` | 236 | 2.200/0.950/1.200 |
| kitchen_sink | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/kitchencounter_sink.gltf` | 674 | 1.600/1.000/1.100 |
| cold_cabinet | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/fridge_A.gltf` | 614 | 1.300/1.800/1.100 |
| extractor | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/extractorhood.gltf` | 204 | 2.200/0.500/1.400 |
| dining_table | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/table_round_B.gltf` | 300 | 1.500/0.850/1.500 |
| dining_chair | `ASSET/Downloaded/kaykit_furniture/KayKit-Furniture-Bits-1.0-main/addons/kaykit_furniture_bits/Assets/gltf/chair_A_wood.gltf` | 308 | 0.536/0.900/0.604 |
| clean_plate | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/plate.gltf` | 160 | 0.330/0.035/0.330 |
| dirty_plate | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/plate_dirty.gltf` | 262 | 0.330/0.035/0.330 |
| menu_board | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/menu.gltf` | 108 | 0.344/0.550/0.206 |
| rolling_pin | `ASSET/Downloaded/kenney_food_kit/Models/GLB format/rollingPin.glb` | 140 | 0.400/0.069/0.080 |
| cutting_board | `ASSET/Downloaded/kenney_food_kit/Models/GLB format/cutting-board.glb` | 120 | 0.460/0.048/0.650 |
| cooking_pot | `ASSET/Downloaded/kaykit_restaurant_bits/KayKit-Restaurant-Bits-1.0-main/addons/kaykit_restaurant_bits/Assets/gltf/pot_A.gltf` | 324 | 0.480/0.171/0.343 |
| pos_screen | `ASSET/Downloaded/kenney_furniture_kit/Models/GLTF format/computerScreen.glb` | 72 | 0.560/0.420/0.148 |
| pos_keyboard | `ASSET/Downloaded/kenney_furniture_kit/Models/GLTF format/computerKeyboard.glb` | 32 | 0.380/0.037/0.159 |
| coffee_machine | `ASSET/Downloaded/kenney_furniture_kit/Models/GLTF format/kitchenCoffeeMachine.glb` | 166 | 0.694/0.650/0.880 |
| planter | `ASSET/Downloaded/kenney_furniture_kit/Models/GLTF format/pottedPlant.glb` | 60 | 0.292/0.900/0.332 |
| tree_oak | `ASSET/Downloaded/kenney_nature_kit/Models/GLTF format/tree_oak.glb` | 196 | 1.985/3.800/2.292 |
| tree_pine | `ASSET/Downloaded/kenney_nature_kit/Models/GLTF format/tree_pineRoundD.glb` | 170 | 1.865/4.200/2.154 |
| tree_small | `ASSET/Downloaded/kenney_nature_kit/Models/GLTF format/tree_small.glb` | 62 | 0.895/2.800/1.033 |
| garden_bush | `ASSET/Downloaded/kenney_nature_kit/Models/GLTF format/plant_bushLarge.glb` | 60 | 1.156/0.750/1.036 |
| garden_rock | `ASSET/Downloaded/kenney_nature_kit/Models/GLTF format/rock_smallA.glb` | 16 | 0.700/0.371/0.700 |
| farm_fence | `ASSET/Downloaded/kenney_nature_kit/Models/GLTF format/fence_planksDouble.glb` | 152 | 2.200/0.850/0.220 |
| street_lamp | `ASSET/Downloaded/kaykit_city_builder/KayKit-City-Builder-Bits-1.0-main/addons/kaykit_city_builder_bits/Assets/gltf/streetlight.gltf` | 176 | 0.926/3.300/0.237 |
| park_bench | `ASSET/Downloaded/kaykit_city_builder/KayKit-City-Builder-Bits-1.0-main/addons/kaykit_city_builder_bits/Assets/gltf/bench.gltf` | 44 | 1.700/0.850/0.700 |
| trash_bin | `ASSET/Downloaded/kaykit_city_builder/KayKit-City-Builder-Bits-1.0-main/addons/kaykit_city_builder_bits/Assets/gltf/trash_A.gltf` | 18 | 1.694/0.700/1.781 |

## Chạy lại pipeline

1. `python Tools/audit_asset_library.py` — rà soát thư viện local.
2. Blender 5.2 background/factory-startup/python-exit-code 1 chạy `Tools/prepare_library_assets.py` — FBX/texture derivative; không tạo .blend/.bak.
3. `Tools/run_unity.ps1 -Task ArtLibrary` — URP material remap, prefab/catalog merge, bounds/pivot/collider checks.
4. Build/Player bằng các CLI hiện có. `run_player.ps1 -Task Assets -BuildPath <exe>` dùng `mod/test/asset-preview.json`; `-LoadPath <save-QA>` có thể xem save QA trước đó qua fixture ở output riêng; chỉ dùng xem hình ảnh toàn khu, không chứng minh balance/progression.

Nguồn raw ASSET có sẵn chưa được track tiếp tục nằm ngoài commit; binary derivative/prefab/license/mapping cần cho build được commit. Muốn tái xử lý art từ source trên máy khác phải có cùng ASSET local. Build/game không phụ thuộc vào thư mục raw này.

## Kiểm chứng và phần chưa nghiệm thu

Kết quả cuối, commit/push và phần chưa nghiệm thu được cập nhật trong `Docs/CODEX_HANDOFF.md`. Asset refresh không xác nhận thay mục tiêu 0 → Restaurant/180–240 phút, mọi rig mới, hoặc FPS 165.
