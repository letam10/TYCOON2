from pathlib import Path
import bpy
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]
SOURCES = {
    "player": ROOT / "ASSET/Downloaded/characters/Humanoid Rig/Individual Characters/FBX/Casual.fbx",
    "farmer": ROOT / "ASSET/Downloaded/characters/Humanoid Rig/Individual Characters/FBX/Farmer.fbx",
    "cow": ROOT / "ASSET/Downloaded/animals/FBX/Cow.fbx",
}
results = []
for key, path in SOURCES.items():
    if not path.is_file():
        results.append({"key": key, "error": "missing", "path": str(path)})
        continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.import_scene.fbx(filepath=str(path))
    report = {"key": key, "path": str(path.relative_to(ROOT)), "sha256": hashlib.sha256(path.read_bytes()).hexdigest(), "meshes": [], "armatures": [], "actions": []}
    for obj in bpy.context.scene.objects:
        if obj.type == "MESH":
            obj.data.calc_loop_triangles()
            report["meshes"].append({
                "name": obj.name, "verts": len(obj.data.vertices), "faces": len(obj.data.polygons),
                "triangles": len(obj.data.loop_triangles), "uv_layers": len(obj.data.uv_layers),
                "dimensions": list(obj.dimensions), "groups": [g.name for g in obj.vertex_groups],
                "materials": [{"name": material.name, "color": list(material.diffuse_color)} for material in obj.data.materials if material],
                "modifiers": [modifier.type for modifier in obj.modifiers],
            })
        if obj.type == "ARMATURE":
            report["armatures"].append({
                "name": obj.name,
                "bones": [{"name": bone.name, "head": list(bone.head_local), "tail": list(bone.tail_local),
                           "parent": bone.parent.name if bone.parent else None} for bone in obj.data.bones],
                "nla": [strip.name for track in obj.animation_data.nla_tracks for strip in track.strips] if obj.animation_data else [],
            })
    for action in bpy.data.actions:
        report["actions"].append({"name": action.name, "range": list(action.frame_range)})
    results.append(report)
    print(json.dumps({
        "key": key, "mesh_count": len(report["meshes"]),
        "triangles": sum(mesh["triangles"] for mesh in report["meshes"]),
        "armatures": [{"name": armature["name"], "bones": [bone["name"] for bone in armature["bones"]]} for armature in report["armatures"]],
        "actions": report["actions"][:20],
    }), flush=True)
(ROOT / "QA/source-rig-audit.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
