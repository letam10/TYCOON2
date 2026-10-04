"""CPU-only preview; reads the user's source models without changing them."""
import bpy
from pathlib import Path
from mathutils import Vector
import json

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(r"D:\APP\TYCOON\ASSET")
OUT = ROOT / "work/reference-assets"
OUT.mkdir(parents=True, exist_ok=True)
records = []
for key in ("Character1", "Cow", "Chicken", "Machine2"):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.import_scene.gltf(filepath=str(SOURCE / (key + ".glb")))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    points = [o.matrix_world @ Vector(v) for o in meshes for v in o.bound_box]
    low = Vector([min(v[i] for v in points) for i in range(3)])
    high = Vector([max(v[i] for v in points) for i in range(3)])
    center = (low + high) / 2
    extent = max(high-low)
    records.append({'key':key, 'dimensions':list(high-low),'meshes':len(meshes), 'vertices':sum(len(o.data.vertices) for o in meshes)})
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 12
    scene.cycles.use_denoising = True
    scene.render.resolution_x = scene.render.resolution_y = 600
    scene.render.resolution_percentage = 100
    scene.world = bpy.data.worlds.new('PreviewWorld')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.25,.32,.38,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value = .8
    scene.view_settings.view_transform = 'Standard'
    bpy.ops.object.camera_add(location=center+Vector((1.0,-1.5,1.0))*extent)
    camera=bpy.context.object
    camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO'; camera.data.ortho_scale=extent*1.4
    scene.camera=camera
    bpy.ops.object.light_add(type='AREA',location=center+Vector((0,-2,3))*extent)
    light=bpy.context.object; light.data.energy=700*extent*extent; light.data.shape='DISK';light.data.size=extent*3
    light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(OUT/(key+'.png'))
    bpy.ops.render.render(write_still=True)
(OUT/'preview-audit.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
