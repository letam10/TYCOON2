"""Convert selected local assets to game FBX. No source edits or .blend backups.

Run with Blender --background --factory-startup --python-exit-code 1 --python.
This is a CPU import/export job; visual acceptance happens in the Unity Player.
"""
from pathlib import Path
import hashlib
import json
import math
import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "Assets/_Game/Art/Library"
REPORT = ROOT / "Docs/asset-library-import.json"
VERSION = 4
# key, pack, exact source filename, target Unity size (or uniform height/max), yaw.
SELECTION = [
    ("carrot", "kenney_food_kit", "carrot.glb", .42, "max", 0),
    ("tomato", "kenney_food_kit", "tomato.glb", .36, "max", 0),
    ("milk", "kenney_food_kit", "carton.glb", .40, "height", 0),
    ("egg", "kenney_food_kit", "egg.glb", .28, "height", 0),
    ("beef", "kenney_food_kit", "meat-raw.glb", .42, "max", 0),
    ("flour", "kenney_food_kit", "bag.glb", .36, "height", 0),
    ("cheese", "kenney_food_kit", "cheese-cut.glb", .40, "max", 0),
    ("sauce", "kenney_food_kit", "bottle-ketchup.glb", .39, "height", 0),
    ("bread", "kenney_food_kit", "loaf.glb", .43, "max", 0),
    ("cake", "kenney_food_kit", "cake.glb", .44, "max", 0),
    ("meal", "kaykit_restaurant_bits", "food_dinner.gltf", .45, "max", 0),
    ("wheat_crop", "kenney_nature_kit", "crops_wheatStageB.glb", .70, "height", 0),
    ("crop_soil", "kenney_nature_kit", "crops_dirtDoubleRow.glb", [3.9, .14, 3.4], "fit", 0),
    ("crop_leaves", "kenney_nature_kit", "crops_leafsStageB.glb", .50, "height", 0),
    ("floor_tile", "kaykit_restaurant_bits", "floor_kitchen.gltf", [4.0, .028, 4.0], "fit", 0),
    ("display_counter", "kaykit_restaurant_bits", "kitchentable_B_large.gltf", [1.45, 1.0, 4.7], "fit", 90),
    ("checkout_counter", "kaykit_restaurant_bits", "kitchencounter_straight_A.gltf", [2.1, 1.0, 1.1], "fit", 0),
    ("storage_rack", "kenney_furniture_kit", "bookcaseOpen.glb", [3.0, 1.65, .85], "fit", 0),
    ("supply_crate", "kaykit_restaurant_bits", "crate.gltf", [1.15, .70, 1.15], "fit", 0),
    ("feed_trough", "kaykit_restaurant_bits", "crate.gltf", [1.9, .8, 1.8], "fit", 0),
    ("farm_shelter", "buildings", "OpenBarn.fbx", [4.0, 3.2, 2.2], "fit", 0),
    ("oven_asset", "kaykit_restaurant_bits", "oven.gltf", [2.25, 1.65, 1.8], "fit", 180),
    ("cooking_range", "kaykit_restaurant_bits", "stove_multi.gltf", [2.4, 1.05, 1.8], "fit", 180),
    ("prep_table", "kaykit_restaurant_bits", "kitchentable_A_large.gltf", [2.2, .95, 1.2], "fit", 0),
    ("kitchen_sink", "kaykit_restaurant_bits", "kitchencounter_sink.gltf", [1.6, 1.0, 1.1], "fit", 0),
    ("cold_cabinet", "kaykit_restaurant_bits", "fridge_A.gltf", [1.3, 1.8, 1.1], "fit", 180),
    ("extractor", "kaykit_restaurant_bits", "extractorhood.gltf", [2.2, .50, 1.4], "fit", 0),
    ("dining_table", "kaykit_restaurant_bits", "table_round_B.gltf", [1.5, .85, 1.5], "fit", 0),
    ("dining_chair", "kaykit_furniture", "chair_A_wood.gltf", .9, "height", 0),
    ("clean_plate", "kaykit_restaurant_bits", "plate.gltf", .33, "max", 0),
    ("dirty_plate", "kaykit_restaurant_bits", "plate_dirty.gltf", .33, "max", 0),
    ("menu_board", "kaykit_restaurant_bits", "menu.gltf", .55, "height", 0),
    ("rolling_pin", "kenney_food_kit", "rollingPin.glb", .40, "max", 0),
    ("cutting_board", "kenney_food_kit", "cutting-board.glb", .65, "max", 0),
    ("cooking_pot", "kaykit_restaurant_bits", "pot_A.gltf", .48, "max", 0),
    ("pos_screen", "kenney_furniture_kit", "computerScreen.glb", .42, "height", 180),
    ("pos_keyboard", "kenney_furniture_kit", "computerKeyboard.glb", .38, "max", 0),
    ("coffee_machine", "kenney_furniture_kit", "kitchenCoffeeMachine.glb", .65, "height", 0),
    ("planter", "kenney_furniture_kit", "pottedPlant.glb", .9, "height", 0),
    ("tree_oak", "kenney_nature_kit", "tree_oak.glb", 3.8, "height", 0),
    ("tree_pine", "kenney_nature_kit", "tree_pineRoundD.glb", 4.2, "height", 0),
    ("tree_small", "kenney_nature_kit", "tree_small.glb", 2.8, "height", 0),
    ("garden_bush", "kenney_nature_kit", "plant_bushLarge.glb", .75, "height", 0),
    ("garden_rock", "kenney_nature_kit", "rock_smallA.glb", .70, "max", 0),
    ("farm_fence", "kenney_nature_kit", "fence_planksDouble.glb", [2.2, .85, .22], "fit", 0),
    ("street_lamp", "kaykit_city_builder", "streetlight.gltf", 3.3, "height", 0),
    ("park_bench", "kaykit_city_builder", "bench.gltf", [1.7, .85, .70], "fit", 0),
    ("trash_bin", "kaykit_city_builder", "trash_A.gltf", .70, "height", 0),
]


def bounds(objects):
    bpy.context.view_layer.update()
    points = [obj.matrix_world @ Vector(p) for obj in objects for p in obj.bound_box]
    return Vector(tuple(min(p[i] for p in points) for i in range(3))), Vector(tuple(max(p[i] for p in points) for i in range(3)))


def materials():
    result = []
    for mat in bpy.data.materials:
        if not mat.users:
            continue
        shader = next((node for node in mat.node_tree.nodes if node.type == "BSDF_PRINCIPLED"), None) if mat.node_tree else None
        color = list(shader.inputs["Base Color"].default_value if shader else mat.diffuse_color)
        record = {"name": mat.name, "color": color, "texture": ""}
        if shader and shader.inputs["Base Color"].is_linked:
            node = shader.inputs["Base Color"].links[0].from_node
            if node.type != "TEX_IMAGE" or node.image is None:
                raise ValueError("Unsupported base color nodes: " + mat.name)
            image = node.image
            if image.packed_file:
                data = bytes(image.packed_file.data)
            else:
                data = Path(bpy.path.abspath(image.filepath)).read_bytes()
            assert data[:8] == b"\x89PNG\r\n\x1a\n"
            name = "palette_" + hashlib.sha256(data).hexdigest()[:16] + ".png"
            texture_path = DEST / "Textures" / name
            if not texture_path.exists() or texture_path.read_bytes() != data:
                texture_path.write_bytes(data)
            image.filepath = str(texture_path)
            record["texture"] = texture_path.relative_to(ROOT).as_posix()
        result.append(record)
    return result


def soften_floor(objects):
    # Chỉ đổi material của bản tile import; giảm tương phản để zone/hàng vẫn nổi rõ.
    light = bpy.data.materials.new("FloorCream")
    light.diffuse_color = (.34, .38, .30, 1)
    light.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = light.diffuse_color
    dark = bpy.data.materials.new("FloorSage")
    dark.diffuse_color = (.10, .14, .11, 1)
    dark.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = dark.diffuse_color
    for obj in objects:
        colors = []
        uv = obj.data.uv_layers.active
        for poly in obj.data.polygons:
            mat = obj.data.materials[poly.material_index]
            image = next(node.image for node in mat.node_tree.nodes if node.type == "TEX_IMAGE")
            point = sum((uv.data[index].uv for index in poly.loop_indices), Vector((0, 0))) / len(poly.loop_indices)
            x = min(image.size[0] - 1, max(0, int(point.x * image.size[0])))
            y = min(image.size[1] - 1, max(0, int(point.y * image.size[1])))
            offset = (y * image.size[0] + x) * 4
            colors.append(sum(image.pixels[offset:offset + 3]) / 3 > .3)
        obj.data.materials.clear()
        obj.data.materials.append(light)
        obj.data.materials.append(dark)
        for poly, is_light in zip(obj.data.polygons, colors):
            poly.material_index = 0 if is_light else 1


def convert(recipe):
    key, pack, filename, target, method, yaw = recipe
    candidates = list((ROOT / "ASSET/Downloaded" / pack).rglob(filename))
    if len(candidates) != 1:
        raise ValueError(f"Expected one source for {key}: {candidates}")
    source = candidates[0]
    source_digest = hashlib.sha256(source.read_bytes()).hexdigest()
    fingerprint = hashlib.sha256(json.dumps([VERSION, recipe, source_digest]).encode()).hexdigest()
    previous = OLD.get(key)
    output = DEST / "Models" / (key + ".fbx")
    if previous and previous["fingerprint"] == fingerprint and output.is_file():
        if hashlib.sha256(output.read_bytes()).hexdigest() == previous["fbx_sha256"]:
            return previous, False
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    if source.suffix.lower() == ".fbx":
        bpy.ops.import_scene.fbx(filepath=str(source))
    else:
        bpy.ops.import_scene.gltf(filepath=str(source))
    objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    assert objects and not any(obj.type == "ARMATURE" for obj in bpy.context.scene.objects), key
    if key == "floor_tile":
        soften_floor(objects)
    # Nướng transform vào mesh tĩnh để pivot và scale Unity luôn thống nhất.
    rotation = Matrix.Rotation(math.radians(yaw), 4, "Z")
    for obj in objects:
        transform = rotation @ obj.matrix_world.copy()
        obj.parent = None
        obj.matrix_world = Matrix.Identity(4)
        obj.data.transform(transform)
    low, high = bounds(objects)
    extent = high - low
    if method == "fit":
        desired = Vector((target[0], target[2], target[1]))
        factors = Vector(tuple(desired[i] / extent[i] for i in range(3)))
    else:
        factor = target / (extent.z if method == "height" else max(extent))
        factors = Vector((factor,) * 3)
    center = Vector(((low.x + high.x) * .5, (low.y + high.y) * .5, low.z))
    transform = Matrix.Diagonal((*factors, 1)) @ Matrix.Translation(-center)
    for obj in objects:
        obj.data.transform(transform)
        obj.data.update()
        if not obj.data.uv_layers:
            # FBX chuồng chỉ có màu phẳng; UV mới dành cho bản import, không sửa nguồn.
            uv = obj.data.uv_layers.new(name="UVMap")
            for loop in obj.data.loops:
                point = obj.data.vertices[loop.vertex_index].co
                uv.data[loop.index].uv = (point.x, point.y)
    low, high = bounds(objects)
    triangles = 0
    for obj in objects:
        obj.data.calc_loop_triangles()
        triangles += len(obj.data.loop_triangles)
    assert 0 < triangles <= 5000, (key, triangles)
    material_records = materials()
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(filepath=str(output), use_selection=True, object_types={"MESH"},
                             add_leaf_bones=False, axis_forward="-Z", axis_up="Y",
                             apply_unit_scale=True, mesh_smooth_type="FACE", bake_anim=False,
                             path_mode="RELATIVE", embed_textures=False)
    licenses = sorted(p for p in (ROOT / "ASSET/Downloaded" / pack).rglob("*") if p.is_file() and "license" in p.name.lower())
    assert licenses, pack
    record = {"key": key, "source": source.relative_to(ROOT).as_posix(), "source_sha256": source_digest,
              "license": licenses[0].relative_to(ROOT).as_posix(), "fingerprint": fingerprint,
              "triangles": triangles, "size": [high.x - low.x, high.z - low.z, high.y - low.y],
              "materials": material_records, "fbx": output.relative_to(ROOT).as_posix(),
              "fbx_sha256": hashlib.sha256(output.read_bytes()).hexdigest()}
    print(json.dumps({"converted": key, "triangles": triangles}), flush=True)
    return record, True


def main():
    assert bpy.app.version >= (5, 2, 0), bpy.app.version_string
    for key, pack, filename, *_ in SELECTION:
        candidates = list((ROOT / "ASSET/Downloaded" / pack).rglob(filename))
        if len(candidates) != 1:
            raise ValueError(f"Expected one source for {key}: {candidates}")
    for folder in ("Models", "Textures", "Licenses"):
        (DEST / folder).mkdir(parents=True, exist_ok=True)
    entries, converted = [], 0
    for recipe in SELECTION:
        record, changed = convert(recipe)
        entries.append(record)
        converted += int(changed)
    for entry in entries:
        pack = Path(entry["source"]).parts[2]
        destination = DEST / "Licenses" / (pack + ".txt")
        data = b"\n".join(line.rstrip() for line in (ROOT / entry["license"]).read_bytes().splitlines()).rstrip() + b"\n"
        if not destination.exists() or destination.read_bytes() != data:
            destination.write_bytes(data)
    report = {"pipelineVersion": VERSION, "entries": entries}
    text = json.dumps(report, indent=2, ensure_ascii=False) + "\n"
    if not REPORT.exists() or REPORT.read_text(encoding="utf-8") != text:
        REPORT.write_text(text, encoding="utf-8")
    print("RESULT " + json.dumps({"ok": True, "models": len(entries), "converted": converted,
                                  "blender": bpy.app.version_string, "triangles": sum(x["triangles"] for x in entries)}), flush=True)


OLD = {x["key"]: x for x in json.loads(REPORT.read_text(encoding="utf-8"))["entries"]} if REPORT.exists() else {}
if __name__ == "__main__":
    main()
