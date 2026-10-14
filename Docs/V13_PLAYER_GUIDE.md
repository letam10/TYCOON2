# Bản Windows V13

## Chạy game

- Bản mới: `work/city-model-v13-20261010/FinalRelease/TYCOON2.exe`.
- Khi giải nén Windows.zip, giữ EXE, UnityPlayer.dll, GameAssembly.dll và TYCOON2_Data cùng nhau.
- Mở Cài đặt trong game để chọn chất lượng, ngôn ngữ Việt/Anh và bật/tắt FPS.
- 165 Hz là tần số màn hình đang nhận diện; độ mượt trình bày thực tế vẫn cần kiểm khi mở khóa Windows.
- Di chuyển bằng WASD hoặc cần trái; đứng ổn định cạnh trạm để tương tác tự động.
- Tay chỉ giữ một loại hàng hoặc tiền; gửi tiền vào két hoặc dùng hết trước khi lấy hàng.
- Mod tiền thêm999.999 vào két; Mod xây dựng tối đa áp dụng và lưu cấp công trình hiện hành.
- Mỗi lần vào vùng Gửi/Rút của két chuyển toàn bộ tiền tương ứng; đứng lâu không lặp.

## Model và cảnh

- Giữ27loại hàng, chồng2x3 mỗi tầng; vật dễ vỡ dùng vỉ/khay/hộp/két đúng số lượng.
- Két, thùng, kệ/quầy, kho, máy và13landmark dùng model nâng cấp từ V10/V11.
- Café, ngân hàng, khách sạn, nhà hát, công viên, bến thuyền, cảng và khu công cộng có collider/lối tiếp cận.
- Model riêng có tám hướng quanh vật và góc trên; công trình đặt có năm góc trong gallery V11.
- Công trình công cộng mới có hình ảnh và đường tiếp cận; chưa thêm nghiệp vụ kinh doanh riêng cho từng tòa nhà.
- V13 sửa mặt biển xa, màu nối cảng/marina và các khoảng nền trống ở góc bên ngoài thành phố.
- Nước dùng gợn sóng, độ bóng và bọt ven bờ theo shader; không thêm phản xạ màn hình hoặc ray tracing.
- Cảnh nền ngoài rìa chỉ để nhìn; WorldBounds, ba collider biển và đường NPC giữ nguyên.

## Kiểm chứng

- Native:887/887PASS,0skip; `work/city-model-v13-20261010/full-tests-final.xml`.
- Lượt Player biển V13 và nghiệm thu: `QA/city-model-v13-acceptance-20261010.json`.
- Lượt V12 toàn thành phố sau sửa bộ đo:194.505frame/880,977s,706tương tác,39nhân viên,0runtimeerror.
- FPS engine của lượt đó220,783; phần thành phố đầy207,242; P956,468ms/P998,641ms.
- Desktop khóa và cảnh báo DXGI/VSync khiến số này không chứng minh trình bày165Hz ổn định.
- Mục tiêu165FPS ổn định chưa nghiệm thu; không suy ra từ trung bình hoặc FPS của gallery đóng băng.
- Mọi QA dùng thư mục save riêng; không ghi đè tiến trình chơi cá nhân.

## Lặp lại kiểm tra

```powershell
./Tools/run_unity.ps1 -Task Tests
./Tools/run_unity.ps1 -Task Build -Il2Cpp -BuildPath work/my-v13/TYCOON2.exe
./Tools/run_player.ps1 -Task OceanV13 -BuildPath D:/GAME/TYCOON2/work/my-v13/TYCOON2.exe -Visible
```

- OceanV13 chụp15góc Harbor/Beach/Marina và hai ảnh fog bật/tắt.
- ABBA: chân trời tắt/bật/bật/tắt; mỗi lượt ổn định2s rồi đo15s, không chụp ảnh trong khoảng đo.
- Số GPU là toàn cảnh Marina đóng băng, không phải FPS gameplay hay phép đo riêng một draw call.
