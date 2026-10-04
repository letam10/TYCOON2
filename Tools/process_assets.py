"""Blender CLI asset processing. Source FBX files are never modified."""
from pathlib import Path
import bpy
import hashlib
import json
import math
import sys
from mathutils import Vector, Quaternion

ROOT = Path(__file__).resolve().parents[1]
PROCESSED = ROOT / "ArtSource/Processed"
IMPORTED = ROOT / "Assets/_Game/Art/Imported/Models"
REPORT = []
MATERIALS = {}
OBJECTS = []

def reset():
    global MATERIALS, OBJECTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.context.scene.render.fps = 24
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1
    MATERIALS = {}
    OBJECTS = []

def material(name, color, roughness=.65, metallic=0):
    if name in MATERIALS:
        return MATERIALS[name]
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1)
    shader.inputs["Roughness"].default_value = roughness
    shader.inputs["Metallic"].default_value = metallic
    MATERIALS[name] = mat
    return mat

def palette():
    return {
        "cream": material("Cream", (.91, .83, .64)),
        "mint": material("MintPaint", (.31, .66, .56), .5),
        "teal": material("TealPaint", (.075, .25, .26), .5),
        "coral": material("CoralPaint", (.79, .30, .24), .55),
        "wood": material("WarmWood", (.47, .29, .13), .8),
        "lightwood": material("LightWood", (.69, .49, .27), .8),
        "metal": material("BrushedMetal", (.31, .40, .41), .45, .55),
        "dark": material("Charcoal", (.035, .065, .068), .55),
        "leaf": material("Leaf", (.25, .48, .17), .85),
        "gold": material("HarvestGold", (.90, .63, .20), .7),
        "white": material("WarmWhite", (.94, .92, .83), .65),
        "red": material("TomatoRed", (.83, .16, .10), .45),
    }

def finish(obj, name, mat, smooth=False):
    obj.name = name
    if mat:
        obj.data.materials.append(mat)
    if obj.type == "MESH":
        for poly in obj.data.polygons:
            poly.use_smooth = smooth
    OBJECTS.append(obj)
    return obj

def box(name, location, size, mat, bevel=.05):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = finish(bpy.context.object, name, mat)
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        modifier = obj.modifiers.new("SoftEdges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 3
        modifier.profile = .5
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        modifier = obj.modifiers.new("WeightedNormals", "WEIGHTED_NORMAL")
        modifier.keep_sharp = True
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    return obj

def sphere(name, location, scale, mat, segments=16, rings=10):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=1, location=location)
    obj = finish(bpy.context.object, name, mat, True)
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return obj

def cylinder(name, location, radius, depth, mat, rotation=(0, 0, 0), vertices=20):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = finish(bpy.context.object, name, mat, True)
    bevel = obj.modifiers.new("Rim", "BEVEL")
    bevel.width = min(.018, radius * .12)
    bevel.segments = 2
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    return obj

def cone(name, location, radius1, radius2, depth, mat, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(vertices=12, radius1=radius1, radius2=radius2, depth=depth, location=location, rotation=rotation)
    return finish(bpy.context.object, name, mat, True)

def rod(name, start, end, radius, mat):
    a, b = Vector(start), Vector(end)
    obj = cylinder(name, (a + b) / 2, radius, (b - a).length, mat)
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0, 0, 1)).rotation_difference((b - a).normalized())
    return obj

def ensure_uv(mesh):
    if mesh.data.uv_layers:
        return
    bpy.ops.object.select_all(action="DESELECT")
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = mesh
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=.02)
    bpy.ops.object.mode_set(mode="OBJECT")

def mesh_bounds():
    points = [obj.matrix_world @ Vector(point) for obj in bpy.context.scene.objects if obj.type == "MESH" for point in obj.bound_box]
    minimum = Vector(tuple(min(p[i] for p in points) for i in range(3)))
    maximum = Vector(tuple(max(p[i] for p in points) for i in range(3)))
    return minimum, maximum

def normalize(size, by_height=True):
    bpy.context.view_layer.update()
    low, high = mesh_bounds()
    extent = high - low
    factor = size / (extent.z if by_height else max(extent))
    center = Vector(((low.x + high.x) / 2, (low.y + high.y) / 2, low.z))
    roots = [obj for obj in bpy.context.scene.objects if obj.parent is None]
    parent = bpy.data.objects.new("AssetRoot", None)
    bpy.context.collection.objects.link(parent)
    for obj in roots:
        world = obj.matrix_world.copy()
        obj.parent = parent
        obj.matrix_world = world
    parent.location = -center * factor
    parent.scale = (factor,) * 3
    bpy.context.view_layer.update()
    return parent, factor

def world_direction(armature, bone_name, direction):
    """Đổi hướng xương theo rest pose, giữ nguyên skin và độ dài xương."""
    pose = armature.pose.bones.get(bone_name)
    if not pose:
        return
    rest = pose.bone.matrix_local.to_quaternion()
    initial = (pose.bone.tail_local - pose.bone.head_local).normalized()
    delta = initial.rotation_difference(Vector(direction).normalized())
    parent_delta = Quaternion()
    if pose.parent:
        parent_delta = pose.parent.matrix.to_quaternion() @ pose.parent.bone.matrix_local.to_quaternion().inverted()
    pose.rotation_mode = "QUATERNION"
    pose.rotation_quaternion = rest.inverted() @ parent_delta.inverted() @ delta @ rest
    bpy.context.view_layer.update()

def humanoid_actions(armature):
    bones = armature.data.bones
    up = (bones["Head"].head_local - bones["Hips"].head_local).normalized()
    side = (bones["UpperArm.L"].head_local - bones["UpperArm.R"].head_local).normalized()
    forward = side.cross(up).normalized()
    armature.animation_data_create()
    armature.animation_data.action = None
    for track in list(armature.animation_data.nla_tracks):
        armature.animation_data.nla_tracks.remove(track)
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)
    for name in ["Idle", "Walk", "Run", "Carry", "CarryWalk", "Pickup", "Drop"]:
        action = bpy.data.actions.new(name)
        action.use_fake_user = True
        armature.animation_data.action = action
        frames = 37 if name in ("Idle", "Carry") else 25
        for frame in range(1, frames + 1):
            bpy.context.scene.frame_set(frame)
            phase = (frame - 1) / (frames - 1) * math.tau
            for pose in armature.pose.bones:
                pose.rotation_mode = "QUATERNION"
                pose.rotation_quaternion = Quaternion()
                pose.location = (0, 0, 0)
                pose.scale = (1, 1, 1)
            bpy.context.view_layer.update()
            moving = name in ("Walk", "Run", "CarryWalk")
            carrying = name in ("Carry", "CarryWalk")
            interaction = name in ("Pickup", "Drop")
            reach = math.sin((frame - 1) / (frames - 1) * math.pi) if interaction else 0
            stride = .54 if name == "Run" else .31
            for suffix, sign in [("L", 1), ("R", -1)]:
                swing = math.sin(phase) * sign if moving else 0
                upper = side * (sign * .065) - up + forward * (-swing * .32)
                lower = upper
                if carrying:
                    upper = side * (sign * .2) - up * .78 + forward * .55
                    lower = forward * .96 + up * .09 - side * (sign * .10)
                if interaction:
                    upper = side * (sign * .09) - up * .90 + forward * (.40 * reach)
                    lower = forward * (.7 * reach + .08) - up * .55
                world_direction(armature, "UpperArm." + suffix, upper)
                world_direction(armature, "LowerArm." + suffix, lower)
                if moving:
                    world_direction(armature, "UpperLeg." + suffix, -up + forward * (swing * stride))
                    world_direction(armature, "LowerLeg." + suffix, -up - forward * (max(0, -swing) * .34))
                    world_direction(armature, "Foot." + suffix, forward)
            hips = armature.pose.bones["Hips"]
            hips.location = up * ((.025 if moving else .003) * (1 - math.cos(phase * 2)))
            if interaction:
                hips.location -= up * (.035 * reach)
            for pose in armature.pose.bones:
                pose.keyframe_insert(data_path="rotation_quaternion", frame=frame, group=pose.name)
                pose.keyframe_insert(data_path="location", frame=frame, group=pose.name)
            bpy.context.view_layer.update()
        track = armature.animation_data.nla_tracks.new()
        track.name = name
        strip = track.strips.new(name, 1, action)
        strip.action_frame_start = 1
        strip.action_frame_end = frames
        track.mute = True
        armature.animation_data.action = None
    armature.animation_data.action = bpy.data.actions["Idle"]
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()

def sanitize_materials():
    for mat in bpy.data.materials:
        mat.use_nodes = True
        shader = mat.node_tree.nodes.get("Principled BSDF")
        if not shader:
            continue
        color = shader.inputs["Base Color"].default_value
        mat.diffuse_color = color
        lower = mat.name.lower()
        shader.inputs["Roughness"].default_value = .78 if any(x in lower for x in ("wood", "fabric", "soil", "leaf")) else .55
        shader.inputs["Metallic"].default_value = .45 if "metal" in lower else 0

def audit(key, source=None):
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    triangles = 0
    for mesh in meshes:
        mesh.data.calc_loop_triangles()
        triangles += len(mesh.data.loop_triangles)
    low, high = mesh_bounds()
    result = {
        "key": key, "source": str(source.relative_to(ROOT)) if source else "authored:Tools/process_assets.py",
        "source_sha256": hashlib.sha256(source.read_bytes()).hexdigest() if source else None,
        "triangles": triangles, "meshes": len(meshes), "dimensions": list(high - low),
        "uv_layers": [len(mesh.data.uv_layers) for mesh in meshes],
        "materials": [{"name": mat.name, "color": list(mat.diffuse_color)} for mat in bpy.data.materials if mat.users],
        "bones": sum(len(obj.data.bones) for obj in bpy.context.scene.objects if obj.type == "ARMATURE"),
        "actions": [action.name for action in bpy.data.actions],
        "processed_blend": str((PROCESSED / (key + ".blend")).relative_to(ROOT)),
        "unity_fbx": str((IMPORTED / (key + ".fbx")).relative_to(ROOT)),
    }
    if triangles <= 0 or any(count == 0 for count in result["uv_layers"]):
        raise RuntimeError("Invalid processed mesh/UV: " + key)
    REPORT.append(result)
    return result

def combine_static_surfaces():
    """Gộp các chi tiết cùng material; giữ riêng Rotor cho chuyển động máy."""
    groups = {}
    for obj in list(bpy.context.scene.objects):
        if obj.type != "MESH" or len(obj.data.materials) != 1 or obj.name.startswith("Rotor"):
            continue
        groups.setdefault(obj.data.materials[0], []).append(obj)
    for mat, objects in groups.items():
        if len(objects) < 2:
            continue
        bpy.ops.object.select_all(action="DESELECT")
        for obj in objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = objects[0]
        bpy.ops.object.join()
        bpy.context.object.name = "Surface_" + mat.name

def export(key, source=None, animated=False):
    if not animated:
        combine_static_surfaces()
    for obj in bpy.context.scene.objects:
        if obj.type == "MESH":
            ensure_uv(obj)
    sanitize_materials()
    PROCESSED.mkdir(parents=True, exist_ok=True)
    IMPORTED.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.wm.save_as_mainfile(filepath=str(PROCESSED / (key + ".blend")))
    bpy.ops.export_scene.fbx(
        filepath=str(IMPORTED / (key + ".fbx")), use_selection=True,
        object_types={"EMPTY", "MESH", "ARMATURE"}, add_leaf_bones=False,
        axis_forward="-Z", axis_up="Y", apply_unit_scale=True,
        mesh_smooth_type="FACE", use_mesh_modifiers=True,
        bake_anim=animated, bake_anim_use_all_actions=animated,
        bake_anim_use_nla_strips=False, bake_anim_simplify_factor=.3,
        path_mode="COPY", embed_textures=True,
    )
    result = audit(key, source)
    result["fbx_sha256"] = hashlib.sha256((IMPORTED / (key + ".fbx")).read_bytes()).hexdigest()
    print(json.dumps({"asset": key, "triangles": result["triangles"], "bones": result["bones"], "actions": result["actions"]}), flush=True)

def imported_asset(key, relative, size, animated=False, human=False, by_height=True):
    source = ROOT / "ASSET/Downloaded" / relative
    if not source.is_file():
        print("Missing optional source", source, flush=True)
        return False
    reset()
    bpy.ops.import_scene.fbx(filepath=str(source))
    normalize(size, by_height)
    if human:
        rig = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
        humanoid_actions(rig)
    export(key, source, animated)
    return True

def food(key):
    reset()
    p = palette()
    if key == "tomato":
        sphere("TomatoBody", (0, 0, .17), (.18, .18, .16), p["red"])
        for index in range(5):
            angle = index * math.tau / 5
            leaf = cone("Leaf", (.05 * math.cos(angle), .05 * math.sin(angle), .32), .012, .035, .12, p["leaf"])
            leaf.rotation_euler = (math.radians(70), 0, angle)
        cylinder("Stem", (0, 0, .345), .02, .07, p["leaf"])
    elif key == "wheat":
        for stalk in range(3):
            offset = (stalk - 1) * .045
            rod("WheatStem", (offset, 0, .02), (offset, 0, .42), .01, p["gold"])
            for index in range(5):
                for sign in (-1, 1):
                    grain = sphere("Grain", (offset + sign * .03, 0, .23 + index * .04), (.025, .022, .046), p["gold"], 8, 6)
                    grain.rotation_euler.y = sign * .4
    elif key == "milk":
        cylinder("BottleBody", (0, 0, .16), .11, .29, p["white"])
        sphere("BottleShoulder", (0, 0, .31), (.105, .105, .065), p["white"])
        cylinder("BottleNeck", (0, 0, .36), .057, .09, p["white"])
        cylinder("MintCap", (0, 0, .407), .062, .028, p["mint"])
        cylinder("LabelBand", (0, 0, .17), .113, .095, p["mint"])
    elif key == "egg":
        sphere("Egg", (0, 0, .17), (.11, .11, .17), p["cream"])
        for vertex in bpy.context.object.data.vertices:
            vertex.co.x *= 1 - max(0, vertex.co.z) * .7
            vertex.co.y *= 1 - max(0, vertex.co.z) * .7
    elif key == "flour":
        body = sphere("FlourSack", (0, 0, .17), (.15, .12, .17), p["cream"])
        for vertex in body.data.vertices:
            vertex.co.x += .007 * math.sin(vertex.co.z * 50 + vertex.co.y * 20)
        cone("GatheredTop", (0, 0, .32), .07, .035, .12, p["cream"])
        cylinder("Twine", (0, 0, .34), .042, .025, p["wood"])
        box("Label", (0, -.119, .18), (.16, .009, .13), p["mint"], .005)
    elif key == "cheese":
        vertices = [(-.17, -.15, 0), (.17, -.15, 0), (0, .17, 0), (-.17, -.15, .15), (.17, -.15, .15), (0, .17, .15)]
        faces = [(0, 2, 1), (3, 4, 5), (0, 1, 4, 3), (1, 2, 5, 4), (2, 0, 3, 5)]
        mesh = bpy.data.meshes.new("CheeseWedge")
        mesh.from_pydata(vertices, [], faces)
        obj = bpy.data.objects.new("CheeseWedge", mesh)
        bpy.context.collection.objects.link(obj)
        finish(obj, "CheeseWedge", p["gold"])
        bpy.context.view_layer.objects.active = obj
        for location in [(-.075, -.08, .145), (.06, -.055, .15), (0, .065, .15)]:
            hole = sphere("HoleCutter", location, (.024, .024, .025), None, 12, 8)
            modifier = obj.modifiers.new("CheeseHole", "BOOLEAN")
            modifier.operation = "DIFFERENCE"
            modifier.object = hole
            bpy.ops.object.modifier_apply(modifier=modifier.name)
            bpy.data.objects.remove(hole, do_unlink=True)
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
        bevel = obj.modifiers.new("SoftCut", "BEVEL")
        bevel.width = .008; bevel.segments = 2
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=bevel.name)
    elif key == "sauce":
        cylinder("RedJar", (0, 0, .15), .115, .28, p["red"])
        cylinder("CreamLabel", (0, 0, .14), .117, .11, p["cream"])
        cylinder("MintLid", (0, 0, .30), .123, .035, p["mint"])
    elif key == "cake":
        cylinder("Cake", (0, 0, .11), .20, .18, p["cream"])
        cylinder("Frosting", (0, 0, .205), .205, .035, p["white"])
        cylinder("Plate", (0, 0, .015), .225, .025, p["mint"])
        for index in range(6):
            angle = index * math.tau / 6
            sphere("Strawberry", (.13 * math.cos(angle), .13 * math.sin(angle), .26), (.035, .035, .045), p["red"], 10, 8)
    elif key == "meal":
        cylinder("Plate", (0, 0, .025), .22, .05, p["cream"])
        sphere("Vegetables", (-.08, .01, .10), (.075, .06, .07), p["leaf"])
        sphere("Vegetables2", (-.02, -.06, .085), (.045, .05, .05), p["red"])
        box("Bread", (.08, .02, .10), (.12, .13, .12), p["gold"], .025)
    elif key == "carrot":
        cone("Carrot", (0, 0, .20), .025, .08, .37, p["coral"])
        for angle in (-.6, 0, .6):
            cone("CarrotLeaf", (.04 * math.sin(angle), 0, .43), .022, .003, .16, p["leaf"], (0, angle, 0))
    normalize(.42, False)
    export(key)

def shelf():
    reset(); p = palette()
    for x in (-1.25, 1.25):
        for y in (-.45, .45):
            box("Upright", (x, y, 1.05), (.11, .11, 2.1), p["mint"], .025)
    for height in (.35, .95, 1.55):
        box("Shelf", (0, 0, height), (2.65, 1.03, .11), p["lightwood"], .025)
        box("FrontLip", (0, -.53, height + .065), (2.70, .07, .18), p["cream"], .018)
    box("Header", (0, .38, 2.05), (2.7, .18, .36), p["mint"], .06)
    box("BackPanel", (0, .48, 1.2), (2.5, .07, 1.45), p["cream"], .02)
    export("shelf")

def checkout():
    reset(); p = palette()
    box("Counter", (0, 0, .58), (2.4, 1.15, 1.16), p["mint"], .10)
    box("CounterTop", (0, 0, 1.19), (2.55, 1.25, .13), p["cream"], .06)
    box("Conveyor", (-.45, 0, 1.27), (1.25, .8, .035), p["dark"], .015)
    box("RegisterBase", (.7, .13, 1.32), (.5, .42, .15), p["teal"], .03)
    rod("ScreenStand", (.7, .18, 1.4), (.7, .18, 1.68), .055, p["metal"])
    screen = box("Screen", (.7, .15, 1.72), (.48, .08, .34), p["teal"], .025)
    screen.rotation_euler.x = -.14
    box("Display", (.7, .097, 1.72), (.40, .015, .25), p["mint"], .008)
    box("Drawer", (.6, -.59, .91), (.82, .04, .22), p["teal"], .02)
    export("checkout")

def crate():
    reset(); p = palette()
    box("Base", (0, 0, .07), (1, .78, .12), p["wood"], .025)
    for x in (-.46, .46):
        for y in (-.34, .34):
            box("Corner", (x, y, .28), (.08, .08, .46), p["lightwood"], .018)
    for height in (.19, .35):
        for x in (-.46, .46):
            box("Side", (x, 0, height), (.07, .70, .11), p["lightwood"], .018)
        for y in (-.35, .35):
            box("End", (0, y, height), (.98, .06, .11), p["lightwood"], .018)
    export("crate")

def machine(key, color):
    reset(); p = palette(); body = p[color]
    box("MachineBase", (0, 0, .19), (1.7, 1.35, .38), p["teal"], .10)
    box("MachineBody", (0, .13, .88), (1.50, 1.06, 1.15), body, .13)
    box("ControlPanel", (.43, -.43, 1.18), (.50, .08, .48), p["cream"], .035)
    for x, mat in [(.31, p["coral"]), (.52, p["mint"])]:
        cylinder("Button", (x, -.49, 1.23), .063, .06, mat, (math.pi / 2, 0, 0))
    if key == "mill":
        cone("Hopper", (-.32, .12, 1.75), .18, .52, .66, p["metal"])
        cylinder("Rotor", (-.32, .12, 1.96), .44, .04, p["gold"], vertices=12)
        box("Outlet", (-.38, -.45, .66), (.47, .6, .2), p["metal"], .04)
    elif key in ("cheesemaker", "saucemaker", "kitchen"):
        cylinder("Tank", (-.32, .04, 1.55), .43, .52, body)
        cylinder("TankRim", (-.32, .04, 1.82), .46, .065, p["metal"])
        rod("MixerPole", (-.32, .04, 1.72), (-.32, .04, 2.12), .05, p["metal"])
        rotor = box("Rotor", (-.32, .04, 2.13), (.62, .10, .06), p["wood"], .02)
        box("Outlet", (-.28, -.51, .66), (.52, .42, .24), p["metal"], .04)
        cylinder("Pot", (.10, .20, 1.50), .2, .16, p["teal"])
    else:
        box("OvenDoor", (-.22, -.43, .91), (.86, .09, .62), p["teal"], .07)
        box("OvenWindow", (-.22, -.49, .94), (.65, .035, .34), p["dark"], .035)
        rod("Handle", (-.55, -.55, .65), (.10, -.55, .65), .04, p["metal"])
        cylinder("Rotor", (0, .10, 1.49), .38, .05, p["metal"])
        box("Chimney", (.52, .34, 1.75), (.28, .28, .65), p["cream"], .045)
    box("OutputTray", (-.32, -.75, .46), (1.1, .8, .08), p["lightwood"], .025)
    export(key)

def tree():
    reset(); p = palette()
    cone("Trunk", (0, 0, .85), .21, .13, 1.7, p["wood"])
    rod("BranchL", (0, 0, 1.1), (-.5, .1, 1.8), .09, p["wood"])
    rod("BranchR", (0, 0, 1.25), (.45, -.05, 1.95), .08, p["wood"])
    for index, (position, size) in enumerate([((0, 0, 2.2), (1, .85, 1)), ((-.55, .13, 1.94), (.65, .6, .7)), ((.5, -.1, 2.18), (.68, .63, .74))]):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1, location=position)
        obj = finish(bpy.context.object, "LeafCrown", p["leaf"])
        obj.scale = size
    export("tree")

def shop():
    reset(); p = palette()
    box("Floor", (0, 0, .12), (6.8, 5.5, .24), p["cream"], .06)
    for x in (-3, 3):
        for y in (-2.15, 2.15):
            box("Post", (x, y, 1.55), (.23, .23, 2.9), p["wood"], .04)
    box("BackWall", (0, 2.28, 1.05), (6.35, .19, 1.8), p["mint"], .045)
    for x in (-3.1, 3.1):
        box("LowWall", (x, .4, .57), (.17, 3.75, .9), p["cream"], .025)
    # Nửa mái sau để camera nhìn rõ hàng hóa và khách bên trong.
    for index in range(10):
        x = -3.15 + index * .7
        panel = box("Awning", (x, -.40, 3.0), (.72, 1.3, .10), p["mint"] if index % 2 == 0 else p["cream"], .025)
        panel.rotation_euler.x = .16
        box("AwningEdge", (x, -1.06, 2.81), (.72, .12, .31), p["mint"] if index % 2 == 0 else p["cream"], .025)
    for y in (1.35, 2.0):
        rod("BackRoofBeam", (-3.3, y, 3.05), (3.3, y, 3.05), .09, p["wood"])
    box("BrandBoard", (0, -1.16, 3.22), (3.85, .17, 1.02), p["wood"], .10)
    texture_path = ROOT / "ASSET/Generated/brand.png"
    if texture_path.exists():
        mat = bpy.data.materials.new("BrandDecal")
        mat.use_nodes = True
        shader = mat.node_tree.nodes.get("Principled BSDF")
        image = bpy.data.images.load(str(texture_path))
        texture = mat.node_tree.nodes.new("ShaderNodeTexImage")
        texture.image = image
        mat.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
        mat.node_tree.links.new(texture.outputs["Alpha"], shader.inputs["Alpha"])
        shader.inputs["Roughness"].default_value = .75
        bpy.ops.mesh.primitive_plane_add(size=1, location=(0, -1.26, 3.22), rotation=(math.pi / 2, 0, 0))
        obj = finish(bpy.context.object, "BrandDecal", mat)
        obj.scale = (3.55, .91, 1)
        image.pack()
    export("shop")

def table():
    reset(); p = palette()
    cylinder("TableTop", (0, 0, 1.0), .63, .12, p["lightwood"], vertices=28)
    cylinder("TableLeg", (0, 0, .48), .075, .91, p["teal"])
    cylinder("TableFoot", (0, 0, .055), .30, .09, p["teal"])
    for sign in (-1, 1):
        box("ChairSeat", (sign * .92, 0, .53), (.56, .56, .10), p["mint"], .06)
        box("ChairBack", (sign * 1.16, 0, .92), (.09, .56, .76), p["mint"], .06)
        for x in (-.19, .19):
            for y in (-.19, .19):
                rod("ChairLeg", (sign * .92 + x, y, .04), (sign * .92 + x, y, .50), .04, p["wood"])
    sphere("Vase", (0, 0, 1.19), (.09, .09, .12), p["cream"])
    rod("FlowerStem", (0, 0, 1.2), (0, 0, 1.43), .012, p["leaf"])
    sphere("Flower", (0, 0, 1.45), (.075, .075, .045), p["coral"], 10, 8)
    export("table")

def chicken():
    reset(); p = palette()
    sphere("ChickenBody", (0, 0, .45), (.30, .37, .34), p["white"])
    sphere("ChickenHead", (0, -.30, .73), (.18, .20, .19), p["white"])
    cone("Beak", (0, -.51, .71), .065, .003, .17, p["gold"], (math.pi / 2, 0, 0))
    for x in (-.155, .155):
        sphere("Eye", (x, -.385, .79), (.025, .028, .027), p["dark"], 10, 8)
    for index in range(3):
        sphere("Comb", (0, -.30 + index * .06, .91), (.045, .055, .075), p["red"], 10, 8)
    sphere("Wattle", (0, -.435, .59), (.045, .035, .07), p["red"], 10, 8)
    for sign in (-1, 1):
        sphere("Wing" + str(sign), (sign * .25, .015, .49), (.07, .25, .18), p["cream"])
        rod("Leg" + str(sign), (sign * .13, -.02, .03), (sign * .13, -.02, .27), .035, p["gold"])
        for offset in (-.045, 0, .045):
            rod("Toe", (sign * .13, -.02, .04), (sign * .13 + offset, -.16, .035), .018, p["gold"])
    for index in range(3):
        feather = sphere("Tail", ((index - 1) * .07, .34, .58), (.065, .18, .20), p["cream"])
        feather.rotation_euler.x = -.6
    normalize(.95)
    export("chicken")

def main():
    selected = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    actors = [
        ("player", "Casual", 1.78), ("farmer", "Farmer", 1.78),
        ("worker", "Worker", 1.78), ("customer", "Casual2", 1.78),
        ("customer_suit", "Suit", 1.78), ("customer_beach", "Beach", 1.78),
    ]
    if not selected or "actors" in selected:
        for key, source, size in actors:
            imported_asset(key, f"characters/Humanoid Rig/Individual Characters/FBX/{source}.fbx", size, True, True)
        imported_asset("cow", "animals/FBX/Cow.fbx", 1.40, True)
    if not selected or "props" in selected:
        for key in ["carrot", "tomato", "wheat", "milk", "egg", "flour", "cheese", "sauce", "cake", "meal"]:
            food(key)
        if not imported_asset("bread", "food/FBX/Bread.fbx", .42, by_height=False):
            raise RuntimeError("Expected licensed bread source is missing")
        imported_asset("carrot_crop", "crops/FBX/Carrot_4.fbx", .65)
        imported_asset("coop", "buildings/FBX/ChickenCoop.fbx", 2.6)
        imported_asset("barn", "buildings/FBX/OpenBarn.fbx", 4.2)
        imported_asset("fence", "buildings/FBX/Fence.fbx", .85)
        imported_asset("well", "buildings/FBX/Well.fbx", 2.1)
        shelf(); checkout(); crate(); tree(); shop(); table(); chicken()
        for key, color in [("mill", "cream"), ("cheesemaker", "mint"), ("saucemaker", "coral"), ("oven", "cream"), ("cakeoven", "coral"), ("kitchen", "mint")]:
            machine(key, color)
    existing = ROOT / "QA/asset-audit.json"
    old = json.loads(existing.read_text(encoding="utf-8")) if existing.exists() else []
    keys = {entry["key"] for entry in REPORT}
    merged = [entry for entry in old if entry["key"] not in keys] + REPORT
    existing.write_text(json.dumps(merged, indent=2), encoding="utf-8")
    print("PROCESSED", len(REPORT), flush=True)

if __name__ == "__main__":
    main()
